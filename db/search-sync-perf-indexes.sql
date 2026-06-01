-- Performance indexes for the Elasticsearch incremental product sync.
--
-- The incremental sync detects changes via `<column> > @Since` on these tables. Without
-- indexes on the change-tracking columns the detection table-scans ~1.2M rows every run,
-- which degraded badly on a cold buffer cache (observed 55s incremental after restart).
-- These indexes turn the scans into seeks (measured ~5x faster cold).
--
-- Idempotent. Target DB: ConcordDb (shared with the Concord/gba-server stack — coordinate
-- before applying to production). Additive, non-destructive.

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Product_Updated' AND object_id = OBJECT_ID('Product'))
    CREATE NONCLUSTERED INDEX IX_Product_Updated ON Product(Updated);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductAvailability_Updated' AND object_id = OBJECT_ID('ProductAvailability'))
    CREATE NONCLUSTERED INDEX IX_ProductAvailability_Updated ON ProductAvailability(Updated) INCLUDE (ProductID);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductOriginalNumber_Updated' AND object_id = OBJECT_ID('ProductOriginalNumber'))
    CREATE NONCLUSTERED INDEX IX_ProductOriginalNumber_Updated ON ProductOriginalNumber(Updated) INCLUDE (ProductID);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductOriginalNumber_Created' AND object_id = OBJECT_ID('ProductOriginalNumber'))
    CREATE NONCLUSTERED INDEX IX_ProductOriginalNumber_Created ON ProductOriginalNumber(Created) INCLUDE (ProductID);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OriginalNumber_Updated' AND object_id = OBJECT_ID('OriginalNumber'))
    CREATE NONCLUSTERED INDEX IX_OriginalNumber_Updated ON OriginalNumber(Updated);
