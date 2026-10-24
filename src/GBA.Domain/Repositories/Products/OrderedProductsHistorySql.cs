namespace GBA.Domain.Repositories.Products;

public static class OrderedProductsHistorySql {
    // Shop history includes placed orders before invoicing, and keeps their original
    // order day after invoicing. Other channels retain the existing invoice history.
    public const string GroupedProductsCte = """
        ;WITH [GroupedOrderedProducts_CTE] AS (
            SELECT [Product].ID, SUM([OrderItem].Qty) AS OrderedQty
            FROM [Sale]
            INNER JOIN [Order] ON [Order].ID = [Sale].OrderID AND [Order].Deleted = 0
            INNER JOIN [OrderItem] ON [OrderItem].OrderID = [Order].ID
                AND [OrderItem].Deleted = 0 AND [OrderItem].Qty > 0
            INNER JOIN [Product] ON [Product].ID = [OrderItem].ProductID
            INNER JOIN [ClientAgreement] ON [ClientAgreement].ID = [Sale].ClientAgreementID
            INNER JOIN [Client] ON [Client].ID = [ClientAgreement].ClientID
            WHERE [Sale].Deleted = 0 AND [Client].NetUID = @NetId
              AND (([Order].OrderSource = 0 AND [Sale].Created >= @From AND [Sale].Created < @To)
                OR ([Order].OrderSource <> 0 AND [Sale].ChangedToInvoice >= @From AND [Sale].ChangedToInvoice < @To))
            GROUP BY [Product].ID
        ), [RowedOrderedProducts_CTE] AS (
            SELECT ID, OrderedQty,
                ROW_NUMBER() OVER(ORDER BY OrderedQty DESC, ID) AS RowNumber
            FROM [GroupedOrderedProducts_CTE]
        )
        """;

    public static string EligibleStoragePredicate(bool withVat) => withVat
        ? "[Storage].Deleted = 0 AND [Storage].Locale = @Culture AND [Storage].ForDefective = 0 AND [Storage].OrganizationID = @OrganizationId"
        : "[Storage].Deleted = 0 AND (([Storage].Locale = @Culture AND [Storage].ForDefective = 0 AND [Storage].AvailableForReSale = 1) OR [Storage].OrganizationID = @OrganizationId)";
}
