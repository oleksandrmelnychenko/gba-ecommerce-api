using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using GBA.Common.Exceptions.CustomExceptions;
using GBA.Common.Helpers;
using GBA.Common.Search;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Agreements;
using GBA.Domain.Repositories.Clients;
using GBA.Domain.Repositories.Currencies;
using GBA.Domain.Repositories.ExchangeRates;
using GBA.Domain.Repositories.Pricings;
using GBA.Domain.Repositories.Products;
using GBA.Domain.Repositories.Sales;
using GBA.Domain.Repositories.Storages;
using GBA.Services.Infrastructure.Checkout;
using GBA.Services.Services.Clients;
using Microsoft.Data.SqlClient;

namespace GBA.Ecommerce.Api.Tests;

public sealed class StockReservationRaceFactAttribute : FactAttribute {
    public const string ConnectionVariable = "STOCK_RACE_CONNECTION_STRING";

    public StockReservationRaceFactAttribute() {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionVariable)))
            Skip = $"Set {ConnectionVariable} to a disposable restored Concord copy to run the shop stock race.";
    }
}

public sealed class StockReservationRaceSqlTests {
    [StockReservationRaceFact]
    public async Task Shop_clients_racing_for_the_last_units_reserve_exactly_the_free_quantity() {
        string connectionString = Environment.GetEnvironmentVariable(StockReservationRaceFactAttribute.ConnectionVariable)!;
        long productId = long.Parse(Required("STOCK_RACE_PRODUCT_ID"), CultureInfo.InvariantCulture);
        Guid productNetId = Guid.Parse(Required("STOCK_RACE_PRODUCT_NET_ID"));
        Guid[] clients = Required("STOCK_RACE_SHOP_CLIENTS")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Guid.Parse)
            .ToArray();
        double qty = double.Parse(Environment.GetEnvironmentVariable("STOCK_RACE_QTY") ?? "1", CultureInfo.InvariantCulture);
        DateTime startUtc = DateTime.Parse(
            Required("STOCK_RACE_START_UTC"),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
        string resultPath = Required("STOCK_RACE_RESULT_PATH");

        string serverPath = Path.Combine(Path.GetTempPath(), "gba-stock-race-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(serverPath);
        NoltFolderManager.InitializeFolderManager(serverPath);
        File.WriteAllText(
            NoltFolderManager.GetEcommerceCrmConfigJsonFilePath(),
            "{\"CrmServerUrl\":\"http://crm.stock-race.test\",\"CrmServerUrlRelease\":\"http://crm.stock-race.test\"}");

        ClientShoppingCartService service = NewCartService(connectionString);
        Guid[] keys = clients.Select(_ => Guid.NewGuid()).ToArray();
        var outcomes = new ConcurrentBag<RaceOutcome>();

        TimeSpan untilStart = startUtc - DateTime.UtcNow;
        if (untilStart > TimeSpan.Zero) await Task.Delay(untilStart);

        await Task.WhenAll(clients.Select((client, index) => Task.Factory.StartNew(() => {
            DateTime started = DateTime.UtcNow;
            var outcome = new RaceOutcome {
                Side = "shop",
                ClientNetUid = client,
                OperationNetUid = keys[index],
                RequestedQty = qty,
                StartedUtc = started
            };

            try {
                OrderItem line = service.Add(new OrderItem { ProductId = productId, Product = new GBA.Domain.Entities.Products.Product { NetUid = productNetId }, Qty = qty }, client, true, keys[index])
                    .GetAwaiter().GetResult();
                outcome.OrderItemId = line.Id;
                outcome.ReservedQty = qty;
            } catch (Exception exception) {
                outcome.Error = exception is LocalizedException localized
                    ? localized.LocalizedMessageKey + ":" + localized.UnlocalizeElementMessage
                    : exception.Message;
                outcome.ErrorType = exception.GetType().Name;
                outcome.Detail = exception.ToString();
            }

            outcome.FinishedUtc = DateTime.UtcNow;
            outcomes.Add(outcome);
        }, TaskCreationOptions.LongRunning)));

        RaceOutcome? firstWinner = outcomes
            .Where(outcome => outcome.OrderItemId.HasValue)
            .OrderBy(outcome => outcome.FinishedUtc)
            .FirstOrDefault();
        var replays = new List<RaceOutcome>();
        if (firstWinner != null) {
            string before = ReservationFingerprint(connectionString, firstWinner.OrderItemId!.Value);
            int index = Array.IndexOf(keys, firstWinner.OperationNetUid);
            OrderItem[] lines = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Task.Factory.StartNew(() =>
                service.Add(new OrderItem { ProductId = productId, Product = new GBA.Domain.Entities.Products.Product { NetUid = productNetId }, Qty = qty }, clients[index], true, keys[index])
                    .GetAwaiter().GetResult(), TaskCreationOptions.LongRunning)));
            replays.AddRange(lines.Select(line => new RaceOutcome {
                Side = "shop-replay",
                ClientNetUid = clients[index],
                OperationNetUid = keys[index],
                RequestedQty = qty,
                OrderItemId = line.Id,
                ReservedQty = line.Qty,
                FinishedUtc = DateTime.UtcNow
            }));
            Assert.Equal(before, ReservationFingerprint(connectionString, firstWinner.OrderItemId!.Value));
            Assert.All(replays, replay => Assert.Equal(firstWinner.OrderItemId, replay.OrderItemId));
        }

        File.WriteAllText(resultPath, JsonSerializer.Serialize(new {
            Side = "shop",
            Requests = clients.Length,
            Qty = qty,
            Outcomes = outcomes.OrderBy(outcome => outcome.FinishedUtc).ToArray(),
            Replays = replays
        }, new JsonSerializerOptions { WriteIndented = true }));

        Directory.Delete(serverPath, recursive: true);

        Assert.All(outcomes, outcome => Assert.True(
            outcome.OrderItemId.HasValue || outcome.Error != null,
            $"Request {outcome.OperationNetUid} neither reserved stock nor returned an error."));
        Assert.All(outcomes.Where(outcome => outcome.Error != null), outcome => Assert.True(
            outcome.ErrorType is nameof(LocalizedException) or nameof(CheckoutConflictException),
            $"Unexpected shop failure {outcome.ErrorType}: {outcome.Error}"));
    }

    private static ClientShoppingCartService NewCartService(string connectionString) {
        return new ClientShoppingCartService(
            new ClientRepositoriesFactory(),
            new SaleRepositoriesFactory(),
            new ProductRepositoriesFactory(),
            new StorageRepositoryFactory(),
            new PricingRepositoriesFactory(),
            new ExchangeRateRepositoriesFactory(),
            new CurrencyRepositoriesFactory(),
            new RaceConnectionFactory(connectionString),
            new AgreementRepositoriesFactory(),
            new RecordingCrmHandler().Factory(),
            new SearchReindexSignal(),
            new SqlCheckoutLedger());
    }

    private static string ReservationFingerprint(string connectionString, long orderItemId) {
        using var connection = new SqlConnection(connectionString);
        return connection.QuerySingle<string>("""
            SELECT CONCAT(
                (SELECT CONVERT(varchar(40), Qty) FROM dbo.OrderItem WHERE ID = @OrderItemId),
                '|',
                (SELECT COUNT(*) FROM dbo.ProductReservation WHERE OrderItemID = @OrderItemId AND Deleted = 0),
                '|',
                (SELECT CONVERT(varchar(40), SUM(Qty)) FROM dbo.ProductReservation WHERE OrderItemID = @OrderItemId AND Deleted = 0),
                '|',
                (SELECT COUNT(*) FROM dbo.OrderItem AS Sibling
                 WHERE Sibling.Deleted = 0
                   AND ((Sibling.OrderID IS NOT NULL AND Sibling.OrderID = (SELECT OrderID FROM dbo.OrderItem WHERE ID = @OrderItemId))
                     OR (Sibling.ClientShoppingCartID IS NOT NULL
                         AND Sibling.ClientShoppingCartID = (SELECT ClientShoppingCartID FROM dbo.OrderItem WHERE ID = @OrderItemId)))))
            """, new { OrderItemId = orderItemId });
    }

    private static string Required(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"{name} is required for the stock race.");

    public sealed class RaceOutcome {
        public string Side { get; set; } = string.Empty;
        public Guid ClientNetUid { get; set; }
        public Guid OperationNetUid { get; set; }
        public double RequestedQty { get; set; }
        public double ReservedQty { get; set; }
        public long? OrderItemId { get; set; }
        public string? Error { get; set; }
        public string? ErrorType { get; set; }
        public string? Detail { get; set; }
        public DateTime StartedUtc { get; set; }
        public DateTime FinishedUtc { get; set; }
    }

    private sealed class RaceConnectionFactory(string connectionString) : IDbConnectionFactory {
        public IDbConnection NewSqlConnection() => new SqlConnection(connectionString);

        public IDbConnection NewDataAnalyticSqlConnection() => throw new NotSupportedException();

        public IDbConnection NewIdentitySqlConnection() => throw new NotSupportedException();

        public IDbConnection NewFenixOneCSqlConnection() => throw new NotSupportedException();

        public IDbConnection NewAmgOneCSqlConnection() => throw new NotSupportedException();
    }
}
