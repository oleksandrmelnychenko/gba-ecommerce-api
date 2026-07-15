# Pricing Change Tracking Release Runbook

This procedure configures the exact 15-table pricing Change Tracking allowlist and
publishes a caught-up Elasticsearch generation. Keep traffic disabled until both final
health gates pass.

## Environment and quiescence

Run from the repository root. Obtain credentials from the secret store; do not put
passwords or bearer tokens in command arguments or files.

```bash
set -euo pipefail

export GBA_SQL_SERVER='127.0.0.1,1433'
export GBA_SQL_DEPLOY_USER='<database owner/deployment login>'
export GBA_SQL_DEPLOY_PASSWORD='<deployment SQL password>'
export GBA_SQL_DATABASE='ConcordDb_V5'
export GBA_SQL_RUNTIME_DATABASE_USER='<application database user>'
export GBA_SQL_MAINTENANCE_USER='<pricing CT maintenance login>'
export GBA_SQL_MAINTENANCE_PASSWORD='<pricing CT maintenance password>'
export GBA_ECOMMERCE_BASE_URL='http://127.0.0.1:8084'
export GBA_ADMIN_BEARER_TOKEN='<administrator JWT>'

command -v sqlcmd
command -v curl
command -v jq
test -n "$GBA_SQL_DEPLOY_PASSWORD"
test -n "$GBA_SQL_RUNTIME_DATABASE_USER"
test -n "$GBA_SQL_MAINTENANCE_USER"
test -n "$GBA_SQL_MAINTENANCE_PASSWORD"
test -n "$GBA_ADMIN_BEARER_TOKEN"
test "${GBA_ECOMMERCE_BASE_URL%/}" = "$GBA_ECOMMERCE_BASE_URL"
```

1. Remove ecommerce from load balancers and block public ingress.
2. Stop **all write-capable** API replicas, workers, importers, scheduled jobs, admin
   tools, and direct database writers. Prevent automatic restarts.
3. Keep every writer stopped through setup/rotation, full rebuild, and catch-up.
4. Start exactly one loopback/admin-only controlled API with
   `SearchSync__Enabled=false`. Do not expose its normal write endpoints.

The override disables scheduled sync only. Manual authenticated sync remains available.

## SQL setup and runtime role

Run setup after application migrations with the approved owner/deployment identity:

```bash
SQLCMDPASSWORD="$GBA_SQL_DEPLOY_PASSWORD" sqlcmd \
  -S "$GBA_SQL_SERVER" -d "$GBA_SQL_DATABASE" -U "$GBA_SQL_DEPLOY_USER" \
  -C -b -r1 \
  -i db/pricing-cache-change-tracking.sql
```

Bind the application and maintenance users to their least-privilege roles:

```bash
SQLCMDPASSWORD="$GBA_SQL_DEPLOY_PASSWORD" sqlcmd \
  -S "$GBA_SQL_SERVER" -d "$GBA_SQL_DATABASE" -U "$GBA_SQL_DEPLOY_USER" \
  -C -b -r1 \
  -v RuntimeDatabaseUser="$GBA_SQL_RUNTIME_DATABASE_USER" \
     MaintenanceDatabaseUser="$GBA_SQL_MAINTENANCE_USER" \
  -Q "SET NOCOUNT ON;
      DECLARE @RuntimeUser sysname = N'\$(RuntimeDatabaseUser)';
      DECLARE @MaintenanceUser sysname = N'\$(MaintenanceDatabaseUser)';
      IF DATABASE_PRINCIPAL_ID(@RuntimeUser) IS NULL
          THROW 54757, N'Configured runtime database user does not exist.', 1;
      IF DATABASE_PRINCIPAL_ID(@MaintenanceUser) IS NULL
          THROW 54757, N'Configured maintenance database user does not exist.', 1;
      IF IS_ROLEMEMBER(N'GbaPricingChangeTrackingRuntime', @RuntimeUser) <> 1
      BEGIN
          DECLARE @Sql nvarchar(max) =
              N'ALTER ROLE [GbaPricingChangeTrackingRuntime] ADD MEMBER '
              + QUOTENAME(@RuntimeUser) + N';';
          EXEC sys.sp_executesql @Sql;
      END;
      IF IS_ROLEMEMBER(N'GbaPricingChangeTrackingMaintenance', @MaintenanceUser) <> 1
      BEGIN
          DECLARE @MaintenanceSql nvarchar(max) =
              N'ALTER ROLE [GbaPricingChangeTrackingMaintenance] ADD MEMBER '
              + QUOTENAME(@MaintenanceUser) + N';';
          EXEC sys.sp_executesql @MaintenanceSql;
      END;"
```

Validate every relevant principal. `public`, runtime, and maintenance must have explicit
singleton DML denials. SQL Server does not allow object-level permission changes on the
fixed `db_datawriter` role; its members inherit the `public` denial, and the real-SQL
release tests verify that the inherited denial overrides the fixed-role write grant.
Runtime may execute only the state reader; maintenance may execute the state reader and
owner-executed rotation procedure.

```bash
SQLCMDPASSWORD="$GBA_SQL_DEPLOY_PASSWORD" sqlcmd \
  -S "$GBA_SQL_SERVER" -d "$GBA_SQL_DATABASE" -U "$GBA_SQL_DEPLOY_USER" \
  -C -b -r1 \
  -v RuntimeDatabaseUser="$GBA_SQL_RUNTIME_DATABASE_USER" \
     MaintenanceDatabaseUser="$GBA_SQL_MAINTENANCE_USER" \
  -Q "SET NOCOUNT ON;
      DECLARE @RuntimeUser sysname = N'\$(RuntimeDatabaseUser)';
      DECLARE @MaintenanceUser sysname = N'\$(MaintenanceDatabaseUser)';
      DECLARE @SingletonObjectId int =
          OBJECT_ID(N'dbo.PricingChangeTrackingIncarnation');

      DECLARE @RequiredDeny TABLE (
          PrincipalName sysname NOT NULL,
          PermissionName nvarchar(60) NOT NULL
      );
      INSERT INTO @RequiredDeny (PrincipalName, PermissionName)
      SELECT principalName, permissionName
      FROM (VALUES
          (N'public'),
          (N'GbaPricingChangeTrackingRuntime'),
          (N'GbaPricingChangeTrackingMaintenance')) principal (principalName)
      CROSS JOIN (VALUES
          (N'INSERT'), (N'UPDATE'), (N'DELETE')) permission (permissionName);

      IF EXISTS (
          SELECT 1
          FROM @RequiredDeny required
          LEFT JOIN sys.database_principals principal
            ON principal.name = required.PrincipalName
          LEFT JOIN sys.database_permissions permission
            ON permission.grantee_principal_id = principal.principal_id
           AND permission.class = 1
           AND permission.major_id = @SingletonObjectId
           AND permission.permission_name = required.PermissionName
           AND permission.state = N'D'
          WHERE permission.permission_name IS NULL)
          THROW 54762, N'Required singleton DML denial is missing.', 1;

      EXECUTE AS USER = @RuntimeUser;
      IF IS_ROLEMEMBER(N'GbaPricingChangeTrackingRuntime') <> 1
         OR HAS_PERMS_BY_NAME(
                N'dbo.GetEcommercePricingChangeTrackingState', N'OBJECT', N'EXECUTE') <> 1
         OR HAS_PERMS_BY_NAME(
                N'dbo.RotateEcommercePricingChangeTrackingIncarnation', N'OBJECT', N'EXECUTE') <> 0
         OR HAS_PERMS_BY_NAME(
                N'dbo.PricingChangeTrackingIncarnation', N'OBJECT', N'SELECT') <> 0
         OR HAS_PERMS_BY_NAME(
                N'dbo.PricingChangeTrackingIncarnation', N'OBJECT', N'INSERT') <> 0
         OR HAS_PERMS_BY_NAME(
                N'dbo.PricingChangeTrackingIncarnation', N'OBJECT', N'UPDATE') <> 0
         OR HAS_PERMS_BY_NAME(
                N'dbo.PricingChangeTrackingIncarnation', N'OBJECT', N'DELETE') <> 0
          THROW 54762, N'Runtime effective permissions are not fail-closed.', 1;
      REVERT;

      EXECUTE AS USER = @MaintenanceUser;
      IF IS_ROLEMEMBER(N'GbaPricingChangeTrackingMaintenance') <> 1
         OR HAS_PERMS_BY_NAME(
                N'dbo.GetEcommercePricingChangeTrackingState', N'OBJECT', N'EXECUTE') <> 1
         OR HAS_PERMS_BY_NAME(
                N'dbo.RotateEcommercePricingChangeTrackingIncarnation', N'OBJECT', N'EXECUTE') <> 1
         OR HAS_PERMS_BY_NAME(
                N'dbo.PricingChangeTrackingIncarnation', N'OBJECT', N'INSERT') <> 0
         OR HAS_PERMS_BY_NAME(
                N'dbo.PricingChangeTrackingIncarnation', N'OBJECT', N'UPDATE') <> 0
         OR HAS_PERMS_BY_NAME(
                N'dbo.PricingChangeTrackingIncarnation', N'OBJECT', N'DELETE') <> 0
          THROW 54762, N'Maintenance effective permissions are not fail-closed.', 1;
      REVERT;"

SQLCMDPASSWORD="$GBA_SQL_DEPLOY_PASSWORD" sqlcmd \
  -S "$GBA_SQL_SERVER" -d "$GBA_SQL_DATABASE" -U "$GBA_SQL_DEPLOY_USER" \
  -C -b -r1 -v RuntimeDatabaseUser="$GBA_SQL_RUNTIME_DATABASE_USER" \
  -Q "SET NOCOUNT ON;
      EXECUTE AS USER = N'\$(RuntimeDatabaseUser)';
      CREATE TABLE #State (
          CurrentRecoveryForkId uniqueidentifier,
          RecordedRecoveryForkId uniqueidentifier,
          RecoveryIncarnationId uniqueidentifier,
          RepairGeneration bigint,
          RecoveryIncarnationPresent bit,
          RecoveryLineageMatches bit,
          RepairFenceValid bit,
          CurrentVersion bigint,
          ExpectedTrackedTableCount int,
          ActualTrackedTableCount int,
          MissingTrackedTableCount int,
          ExtraTrackedTableCount int,
          UnreadableTrackedTableIdentityCount int,
          PricingTrackedTableManifest nvarchar(max),
          ExpectedPriceFunctionCount int,
          ActualPriceFunctionCount int,
          ActualPricingModuleCount int,
          UnreadablePricingModuleCount int,
          PricingModuleHashManifest nvarchar(max),
          UnresolvedPriceDependencyCount int,
          CrossDatabasePriceDependencyCount int,
          SynonymBackedPriceDependencyCount int,
          UnlistedPriceInputCount int,
          NonInputManifestEntryCount int
      );
      INSERT INTO #State EXEC dbo.GetEcommercePricingChangeTrackingState;
      IF EXISTS (
          SELECT 1
          FROM #State
          WHERE RecoveryIncarnationPresent <> 1
             OR RecoveryLineageMatches <> 1
             OR RepairFenceValid <> 1
             OR RepairGeneration <= 0
             OR CurrentVersion IS NULL
             OR ExpectedTrackedTableCount <> 15
             OR ActualTrackedTableCount <> 15
             OR MissingTrackedTableCount <> 0
             OR ExtraTrackedTableCount <> 0
             OR UnreadableTrackedTableIdentityCount <> 0
             OR ExpectedPriceFunctionCount <> 2
             OR ActualPriceFunctionCount <> 2
             OR UnreadablePricingModuleCount <> 0
             OR UnresolvedPriceDependencyCount <> 0
             OR CrossDatabasePriceDependencyCount <> 0
             OR SynonymBackedPriceDependencyCount <> 0
             OR UnlistedPriceInputCount <> 0
             OR NonInputManifestEntryCount <> 0
             OR ISJSON(PricingTrackedTableManifest) <> 1
             OR (SELECT COUNT(*) FROM OPENJSON(PricingTrackedTableManifest)) <> 15
             OR ISJSON(PricingModuleHashManifest) <> 1)
          THROW 54762, N'Pricing Change Tracking runtime state is not releasable.', 1;
      REVERT;"
```

## Restore, PITR, or data-loss failover

After every recovery event, keep all writers stopped and acknowledge the new fork:

```bash
SQLCMDPASSWORD="$GBA_SQL_MAINTENANCE_PASSWORD" sqlcmd \
  -S "$GBA_SQL_SERVER" -d "$GBA_SQL_DATABASE" -U "$GBA_SQL_MAINTENANCE_USER" \
  -C -b -r1 \
  -i db/pricing-cache-change-tracking-rotate-incarnation.sql

SQLCMDPASSWORD="$GBA_SQL_DEPLOY_PASSWORD" sqlcmd \
  -S "$GBA_SQL_SERVER" -d "$GBA_SQL_DATABASE" -U "$GBA_SQL_DEPLOY_USER" \
  -C -b -r1 \
  -i db/pricing-cache-change-tracking.sql
```

CI must run `BackupMutateRestore_ChangesRecoveryForkAndRequiresExplicitRotation` with
`GBA_ECOMMERCE_SQL_INTEGRATION_REQUIRED=1`. It performs a real backup, tracked mutation,
incarnation rotation, `RESTORE WITH REPLACE`, fail-closed check, and explicit recovery.
A manually assigned GUID is not accepted as restore evidence.

## Manual full rebuild

Keep all writers stopped and the controlled API at `SearchSync__Enabled=false`:

```bash
FULL_SYNC_BODY="$(mktemp)"
FULL_SYNC_CODE="$(curl -sS -o "$FULL_SYNC_BODY" -w '%{http_code}' -X POST \
  -H "Authorization: Bearer $GBA_ADMIN_BEARER_TOKEN" \
  "$GBA_ECOMMERCE_BASE_URL/api/v1/uk/elasticsearch/sync/full")"

if ! test "$FULL_SYNC_CODE" = '200'; then
  jq . "$FULL_SYNC_BODY" || true
  echo "Full Elasticsearch rebuild returned HTTP $FULL_SYNC_CODE" >&2
  exit 1
fi
if ! jq -e '
    .StatusCode == 200 and .Body.Success == true and
    (.Body.Error == null or .Body.Error == "")
  ' "$FULL_SYNC_BODY" >/dev/null; then
  echo 'Full Elasticsearch rebuild returned Success=false' >&2
  exit 1
fi
rm -f "$FULL_SYNC_BODY"
```

Full rebuild alone must remain fail-closed until a later incremental catch-up:

```bash
PENDING_BODY="$(mktemp)"
PENDING_CODE="$(curl -sS -o "$PENDING_BODY" -w '%{http_code}' \
  "$GBA_ECOMMERCE_BASE_URL/api/v1/uk/elasticsearch/health")"
test "$PENDING_CODE" = '503'
jq -e '
  .Body.healthy == false and .Body.stale == true and
  .Body.incrementalCatchUpRequired == true
' "$PENDING_BODY" >/dev/null
rm -f "$PENDING_BODY"

SEARCH_PATHS=(
  '/api/v1/uk/products/search?value=filter&limit=1&offset=0'
  '/api/v1/uk/elasticsearch/search?query=filter&limit=1&offset=0'
)
for SEARCH_PATH in "${SEARCH_PATHS[@]}"; do
  SEARCH_BODY="$(mktemp)"
  SEARCH_CODE="$(curl -sS -o "$SEARCH_BODY" -w '%{http_code}' \
    "$GBA_ECOMMERCE_BASE_URL$SEARCH_PATH")"
  test "$SEARCH_CODE" = '503'
  jq -e '
    .StatusCode == 503 and .Body.syncStateReadable == true and
    .Body.stale == true and .Body.incrementalCatchUpRequired == true
  ' "$SEARCH_BODY" >/dev/null
  rm -f "$SEARCH_BODY"
done
```

## Mandatory incremental catch-up

```bash
CATCHUP_BODY="$(mktemp)"
CATCHUP_CODE="$(curl -sS -o "$CATCHUP_BODY" -w '%{http_code}' -X POST \
  -H "Authorization: Bearer $GBA_ADMIN_BEARER_TOKEN" \
  "$GBA_ECOMMERCE_BASE_URL/api/v1/uk/elasticsearch/sync/incremental")"
test "$CATCHUP_CODE" = '200'
jq -e '
  .StatusCode == 200 and .Body.Success == true and
  (.Body.Error == null or .Body.Error == "")
' "$CATCHUP_BODY" >/dev/null
rm -f "$CATCHUP_BODY"
```

## Final readiness gates

Poll the explicit endpoint until it proves a current, caught-up, non-stale generation:

```bash
READINESS_BODY="$(mktemp)"
READINESS_DEADLINE=$((SECONDS + 900))
READINESS_CODE=''
while (( SECONDS < READINESS_DEADLINE )); do
  READINESS_CODE="$(curl -sS -o "$READINESS_BODY" -w '%{http_code}' \
    "$GBA_ECOMMERCE_BASE_URL/api/v1/uk/elasticsearch/health")"
  if test "$READINESS_CODE" = '200' && jq -e '
      .Body.healthy == true and .Body.status == "healthy" and
      .Body.ClusterAvailable == true and .Body.HasActiveGeneration == true and
      .Body.PointedIndexExists == true and .Body.ConfigurationConsistent == true and
      .Body.PricingRevisionsCurrent == true and .Body.AliasConsistent == true and
      .Body.syncStateReadable == true and .Body.hasWatermark == true and
      .Body.stale == false and .Body.incrementalCatchUpRequired == false
    ' "$READINESS_BODY" >/dev/null; then
    break
  fi
  sleep 5
done
test "$READINESS_CODE" = '200'
jq -e '.Body.stale == false and .Body.incrementalCatchUpRequired == false' \
  "$READINESS_BODY" >/dev/null
rm -f "$READINESS_BODY"

ROOT_HEALTH_BODY="$(mktemp)"
ROOT_HEALTH_CODE="$(curl -sS -o "$ROOT_HEALTH_BODY" -w '%{http_code}' \
  "$GBA_ECOMMERCE_BASE_URL/health")"
test "$ROOT_HEALTH_CODE" = '200'
jq -e '
  .status == "Healthy" and
  any(.checks[];
      .name == "elasticsearch-active-generation" and .status == "Healthy" and
      .data.syncStateReadable == true and .data.hasWatermark == true and
      .data.stale == false and .data.incrementalCatchUpRequired == false)
' "$ROOT_HEALTH_BODY" >/dev/null
rm -f "$ROOT_HEALTH_BODY"

for SEARCH_PATH in "${SEARCH_PATHS[@]}"; do
  SEARCH_BODY="$(mktemp)"
  SEARCH_CODE="$(curl -sS -o "$SEARCH_BODY" -w '%{http_code}' \
    "$GBA_ECOMMERCE_BASE_URL$SEARCH_PATH")"
  test "$SEARCH_CODE" = '200'
  rm -f "$SEARCH_BODY"
done
```

Only after both gates pass may you stop the controlled instance, remove the
`SearchSync__Enabled=false` override, start normal replicas with
`SearchSync__Enabled=true`, re-enable writers/jobs, and restore public traffic.
