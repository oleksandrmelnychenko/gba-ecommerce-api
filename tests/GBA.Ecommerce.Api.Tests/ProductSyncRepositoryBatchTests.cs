using GBA.Search.Sync;

namespace GBA.Ecommerce.Api.Tests;

/// <summary>
/// Verifies that targeted Elasticsearch refreshes stay below SQL Server's parameter limit.
/// </summary>
public sealed class ProductSyncRepositoryBatchTests {
    /// <summary>
    /// Ensures large, duplicate input sets are de-duplicated and split into safe SQL batches.
    /// </summary>
    [Fact]
    public void Product_ids_are_partitioned_below_the_sql_server_parameter_limit() {
        List<long> ids = Enumerable
            .Range(1, ProductSyncRepository.SqlParameterBatchSize * 2 + 505)
            .Select(value => (long)value)
            .Concat([1, 2, 3])
            .ToList();

        long[][] batches = ProductSyncRepository.PartitionProductIds(ids).ToArray();

        Assert.Equal(3, batches.Length);
        Assert.All(
            batches,
            batch => Assert.InRange(
                batch.Length,
                1,
                ProductSyncRepository.SqlParameterBatchSize));
        Assert.Equal(
            ids.Distinct().OrderBy(id => id),
            batches.SelectMany(batch => batch).OrderBy(id => id));
    }
}
