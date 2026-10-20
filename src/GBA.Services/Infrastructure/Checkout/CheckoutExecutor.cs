using System;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Transactions;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers.ProductAvailabilityModels;
using Microsoft.Data.SqlClient;
using NLog;

namespace GBA.Services.Infrastructure.Checkout;

public interface ICheckoutExecutor {
    CheckoutResult Execute(CheckoutRequest request, Func<IDbConnection, Sale> createSale);

    CheckoutResult FindReplay(CheckoutRequest request);
}

public sealed class CheckoutExecutor : ICheckoutExecutor {
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public const string ConcurrentChangeMessage =
        "The checkout collided with a concurrent change and was rolled back. Retry the request with the same key.";

    internal static readonly JsonSerializerOptions ReceiptJsonOptions = new() {
        PropertyNamingPolicy = null,
        DictionaryKeyPolicy = null,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICheckoutLedger _ledger;

    public CheckoutExecutor(IDbConnectionFactory connectionFactory, ICheckoutLedger ledger) {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
    }

    public CheckoutResult Execute(CheckoutRequest request, Func<IDbConnection, Sale> createSale) {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(createSale);

        if (Transaction.Current != null)
            throw new InvalidOperationException("Checkout must own its database transaction.");

        CheckoutResult replay = FindReplay(request);
        if (replay != null) return replay;

        CheckoutClaim claim;
        Sale createdSale = null;

        for (int attempt = 1; ; attempt++) {
            try {
                using TransactionScope scope = CheckoutTransaction.Begin();
                using IDbConnection connection = _connectionFactory.NewSqlConnection();
                connection.Open();

                claim = _ledger.TryClaim(connection, request);

                if (claim == CheckoutClaim.Claimed) {
                    createdSale = createSale(connection);

                    if (createdSale == null || createdSale.Id <= 0 || createdSale.NetUid == Guid.Empty)
                        throw new InvalidOperationException("The created sale could not be reloaded.");

                    _ledger.Complete(
                        connection,
                        request,
                        JsonSerializer.Serialize(createdSale, ReceiptJsonOptions));

                    scope.Complete();
                }

                break;
            } catch (Exception exception) when (IsTransientConcurrencyFailure(exception)) {
                if (attempt >= CheckoutRetry.MaxAttempts)
                    throw new CheckoutConflictException(ConcurrentChangeMessage);

                _logger.Warn(
                    "Checkout {Operation} {OperationNetUid} attempt {Attempt}/{MaxAttempts} was rolled back by a transient SQL conflict and will be retried: {Message}",
                    request.OperationName,
                    request.OperationNetUid,
                    attempt,
                    CheckoutRetry.MaxAttempts,
                    exception.Message);
                createdSale = null;
                CheckoutRetry.Wait(CheckoutRetry.Backoff(attempt));
            } catch (InsufficientProductAvailabilityException exception) {
                throw new CheckoutConflictException(exception.Message);
            }
        }

        if (claim == CheckoutClaim.Claimed) return new CheckoutResult(createdSale, false);
        if (claim == CheckoutClaim.Busy)
            throw new CheckoutConflictException(CheckoutConflictException.InProgressMessage);

        return FindReplay(request) ??
               throw new CheckoutConflictException(CheckoutConflictException.InProgressMessage);
    }

    internal static bool IsTransientConcurrencyFailure(Exception exception) {
        for (Exception current = exception; current != null; current = current.InnerException)
            if (current is SqlException { Number: 1205 or 1222 })
                return true;

        return false;
    }

    public CheckoutResult FindReplay(CheckoutRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        CheckoutLedgerEntry entry;

        using (IDbConnection connection = _connectionFactory.NewSqlConnection())
            entry = _ledger.Find(connection, request.OperationNetUid);

        if (entry == null) return null;

        if (!request.IsSameRequestAs(entry)) throw new CheckoutKeyReuseException();

        if (!entry.IsCompleted)
            throw new CheckoutConflictException(CheckoutConflictException.InProgressMessage);

        Sale sale = JsonSerializer.Deserialize<Sale>(entry.ResponsePayload, ReceiptJsonOptions);

        if (sale == null || sale.Id <= 0 || sale.NetUid == Guid.Empty)
            throw new InvalidOperationException("The stored checkout receipt is invalid.");

        return new CheckoutResult(sale, true);
    }
}
