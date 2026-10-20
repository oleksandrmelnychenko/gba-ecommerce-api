using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Transactions;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.EntityHelpers.ProductAvailabilityModels;
using GBA.Domain.Repositories.Products.Contracts;
using NLog;

namespace GBA.Services.Infrastructure.Checkout;

public static class CheckoutStockReconciliation {
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public static IReadOnlyList<StockReservationReconciliationRow> Snapshot(
        IProductRepositoriesFactory productRepositoriesFactory,
        IDbConnection connection,
        IEnumerable<long> orderItemIds) {
        return productRepositoriesFactory?
            .NewStockReservationReconciliationRepository(connection)?
            .GetRows(orderItemIds)?
            .ToList() ?? new List<StockReservationReconciliationRow>();
    }

    public static IReadOnlyList<long> OrderItemIds(
        IProductRepositoriesFactory productRepositoriesFactory,
        IDbConnection connection,
        long orderId) {
        return productRepositoriesFactory?
            .NewStockReservationReconciliationRepository(connection)?
            .GetOrderItemIds(orderId)?
            .ToList() ?? new List<long>();
    }

    public static void Verify(
        IProductRepositoriesFactory productRepositoriesFactory,
        IDbConnectionFactory connectionFactory,
        IDbConnection connection,
        IEnumerable<StockReservationReconciliationRow> before,
        IEnumerable<long> orderItemIds,
        string source,
        Guid operationNetUid,
        Guid principalNetUid) {
        IReadOnlyList<StockReservationMismatch> mismatches = StockReservationReconciliationPolicy.Evaluate(
            before,
            Snapshot(productRepositoriesFactory, connection, orderItemIds));
        if (mismatches.Count == 0) return;

        foreach (StockReservationMismatch mismatch in mismatches)
            _logger.Warn(
                "Stock reservation mismatch source={Source} operation={Operation} orderItem={OrderItemId} product={ProductId} qty={Qty} reserved={Reserved} shipped={Shipped} discrepancy={Discrepancy} previous={Previous} introduced={Introduced}",
                source,
                operationNetUid,
                mismatch.Row.OrderItemId,
                mismatch.Row.ProductId,
                mismatch.Row.OrderItemQty,
                mismatch.Row.ReservedQty,
                mismatch.Row.ShippedQty,
                mismatch.Row.Discrepancy,
                mismatch.PreviousDiscrepancy,
                mismatch.IntroducedByOperation);

        Record(productRepositoriesFactory, connectionFactory, mismatches, source, operationNetUid, principalNetUid);

        List<StockReservationMismatch> introduced = mismatches.Where(mismatch => mismatch.IntroducedByOperation).ToList();
        if (introduced.Count > 0) throw new StockReservationMismatchException(introduced);
    }

    private static void Record(
        IProductRepositoriesFactory productRepositoriesFactory,
        IDbConnectionFactory connectionFactory,
        IReadOnlyList<StockReservationMismatch> mismatches,
        string source,
        Guid operationNetUid,
        Guid principalNetUid) {
        if (connectionFactory == null || productRepositoriesFactory == null) return;

        try {
            using TransactionScope suppressed = new(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);
            using IDbConnection connection = connectionFactory.NewSqlConnection();
            connection.Open();

            IStockReservationReconciliationRepository repository =
                productRepositoriesFactory.NewStockReservationReconciliationRepository(connection);

            foreach (StockReservationMismatch mismatch in mismatches)
                repository?.RecordMismatch(mismatch, source, operationNetUid, principalNetUid);

            suppressed.Complete();
        } catch (Exception exception) {
            _logger.Error(exception, "Stock reservation mismatch could not be recorded source={Source} operation={Operation}", source, operationNetUid);
        }
    }
}
