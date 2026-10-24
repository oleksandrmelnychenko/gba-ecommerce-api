using Dapper;
using GBA.Domain.Repositories.Products;
using Microsoft.Data.SqlClient;

namespace GBA.Ecommerce.Api.Tests;

public sealed class OrderHistorySqlTests {
    // Only connection-local temp tables are written; no application records are created.
    [CheckoutStockTempFact]
    public async Task Placed_shop_orders_are_visible_before_invoice_and_stay_on_their_order_day() {
        await using var db = new SqlConnection(Environment.GetEnvironmentVariable(CheckoutStockTempFactAttribute.ConnectionVariable));
        await db.OpenAsync();
        Assert.Equal(0, await db.ExecuteScalarAsync<int>("SELECT ISNULL(HAS_PERMS_BY_NAME('dbo.Sale','OBJECT','INSERT'),0);"));
        await db.ExecuteAsync("""
            CREATE TABLE #Product(ID bigint PRIMARY KEY);
            CREATE TABLE #Order(ID bigint PRIMARY KEY, Deleted bit, OrderSource int);
            CREATE TABLE #Sale(ID bigint PRIMARY KEY, OrderID bigint, ClientAgreementID bigint, Deleted bit, Created datetime2, ChangedToInvoice datetime2);
            CREATE TABLE #OrderItem(ID bigint PRIMARY KEY, OrderID bigint, ProductID bigint, Deleted bit, Qty float);
            CREATE TABLE #ClientAgreement(ID bigint PRIMARY KEY, ClientID bigint);
            CREATE TABLE #Client(ID bigint PRIMARY KEY, NetUID uniqueidentifier);
            CREATE TABLE #Storage(ID bigint PRIMARY KEY, Deleted bit, Locale varchar(2), ForDefective bit, AvailableForReSale bit, OrganizationID bigint);
            CREATE TABLE #ProductAvailability(ID bigint PRIMARY KEY, ProductID bigint, StorageID bigint, Amount float, Deleted bit);
            INSERT #Product VALUES(1),(2),(3),(4),(5),(6),(7),(8),(9),(10);
            INSERT #Client VALUES(1,'11111111-1111-1111-1111-111111111111'),(2,'22222222-2222-2222-2222-222222222222');
            INSERT #ClientAgreement VALUES(1,1),(2,2);
            INSERT #Order VALUES(1,0,0),(2,0,0),(3,0,1),(4,0,1),(5,0,0),(6,1,0),(7,0,0),(8,0,0),(9,0,0),(10,0,0),(11,0,0),(12,0,0),(13,0,0),(14,0,0);
            INSERT #Sale VALUES
                (1,1,1,0,'2026-10-23T21:00:00',NULL),
                (2,2,1,0,'2026-10-24T20:59:59.9999999','2026-10-26'),
                (3,3,1,0,'2026-10-20','2026-10-24T10:00:00'),
                (4,4,1,0,'2026-10-24T10:00:00',NULL),
                (5,5,1,1,'2026-10-24T10:00:00',NULL),
                (6,6,1,0,'2026-10-24T10:00:00',NULL),
                (7,7,2,0,'2026-10-24T10:00:00',NULL),
                (8,8,1,0,'2026-10-24T21:00:00',NULL),
                (9,9,1,0,'2026-10-23T20:59:59.9999999',NULL),
                (10,10,1,0,'2026-10-24T10:00:00',NULL),
                (11,11,1,0,'2026-10-24T10:00:00',NULL),
                (12,12,1,0,'2026-10-24T10:00:00',NULL),
                (13,13,1,0,'2026-10-22','2026-10-24T10:00:00'),
                (14,14,1,0,'2026-10-24T10:00:00',NULL);
            INSERT #OrderItem VALUES(1,1,1,0,16),(2,2,2,0,8),(3,3,3,0,4),(4,4,4,0,100),(5,5,5,0,100),(6,6,6,0,100),(7,7,7,0,100),(8,8,8,0,100),(9,9,9,0,100),(10,10,10,1,100),(11,11,10,0,0),(12,12,10,0,-5),(13,13,10,0,100),(14,14,3,0,4);
            INSERT #Storage VALUES(1,0,'uk',0,1,7),(2,0,'uk',0,0,8),(3,1,'uk',0,1,7);
            INSERT #ProductAvailability VALUES(1,1,1,5,0),(2,2,2,30,0),(3,2,3,40,0);
            """);
        string cte = TempSql(OrderedProductsHistorySql.GroupedProductsCte);
        var parameters = new { From = new DateTime(2026,10,23,21,0,0), To = new DateTime(2026,10,24,21,0,0), NetId = Guid.Parse("11111111-1111-1111-1111-111111111111"), Culture = "uk", OrganizationId = 7L };
        var rows = (await db.QueryAsync<(long ID,double Qty,long RowNumber)>(cte + " SELECT ID,OrderedQty,RowNumber FROM [RowedOrderedProducts_CTE] ORDER BY RowNumber;", parameters)).ToArray();
        Assert.Equal(new[] {(1L,16d,1L),(2L,8d,2L),(3L,8d,3L)}, rows);
        // Confirming a shop invoice later must not remove the item from the original day.
        await db.ExecuteAsync("UPDATE #Sale SET ChangedToInvoice='2026-10-27' WHERE ID=1;");
        Assert.Equal(rows, (await db.QueryAsync<(long,double,long)>(cte + " SELECT ID,OrderedQty,RowNumber FROM [RowedOrderedProducts_CTE] ORDER BY RowNumber;", parameters)).ToArray());
        // Same-quantity rows have deterministic page membership.
        Assert.Equal(2, await db.ExecuteScalarAsync<long>(cte + " SELECT ID FROM [RowedOrderedProducts_CTE] WHERE RowNumber=2;", parameters));
        foreach (bool vat in new[] {false,true}) {
            string sql = cte + """
                SELECT h.ID,pa.Amount FROM [RowedOrderedProducts_CTE] h
                LEFT JOIN #ProductAvailability pa ON pa.ProductID=h.ID AND pa.Deleted=0
                    AND EXISTS(SELECT 1 FROM #Storage [Storage] WHERE [Storage].ID=pa.StorageID AND
                """ + OrderedProductsHistorySql.EligibleStoragePredicate(vat) + ") ORDER BY h.RowNumber;";
            var stock = (await db.QueryAsync<(long ID,double? Amount)>(sql,parameters)).ToArray();
            Assert.Equal(new[] {(1L,(double?)5),(2L,(double?)null),(3L,(double?)null)}, stock);
        }
    }

    private static string TempSql(string sql) {
        foreach (string table in new[] {"Product","OrderItem","Order","Sale","ClientAgreement","Client"})
            sql = sql.Replace("["+table+"]", "[#"+table+"]");
        return sql;
    }
}
