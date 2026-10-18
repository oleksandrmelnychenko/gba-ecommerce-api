using System;
using System.Data;
using System.Globalization;
using Dapper;

namespace GBA.Services.Infrastructure.Checkout;

public sealed class SqlCheckoutLedger : ICheckoutLedger {
    public const int DefaultClaimLockTimeoutMilliseconds = 20000;

    internal const string FindSql = """
        SELECT
            [OperationNetUid],
            [OperationName],
            [PrincipalNetUid],
            [ClientNetUid],
            [ModeFlag],
            [RequestFingerprint],
            [ResponsePayload],
            [CreatedUtc],
            [CompletedUtc]
        FROM [dbo].[EcommerceSalesCreationLedger]
        WHERE [OperationNetUid] = @OperationNetUid;
        """;

    internal const string ClaimSqlTemplate = """
        IF @@TRANCOUNT = 0
            THROW 51090, N'Checkout ledger claims require an enlisted transaction.', 1;

        DECLARE @Claim int;

        SET LOCK_TIMEOUT {LockTimeoutMilliseconds};

        BEGIN TRY
            INSERT INTO [dbo].[EcommerceSalesCreationLedger] (
                [OperationNetUid],
                [OperationName],
                [PrincipalNetUid],
                [ClientNetUid],
                [ModeFlag],
                [RequestFingerprint],
                [CreatedUtc])
            VALUES (
                @OperationNetUid,
                @OperationName,
                @PrincipalNetUid,
                @ClientNetUid,
                @ModeFlag,
                @RequestFingerprint,
                SYSUTCDATETIME());

            SET @Claim = 1;
        END TRY
        BEGIN CATCH
            IF ERROR_NUMBER() IN (2601, 2627)
                SET @Claim = 0;
            ELSE IF ERROR_NUMBER() = 1222
                SET @Claim = -1;
            ELSE
            BEGIN
                SET LOCK_TIMEOUT -1;
                THROW;
            END;
        END CATCH;

        SET LOCK_TIMEOUT -1;

        SELECT @Claim;
        """;

    internal const string CompleteSql = """
        UPDATE [dbo].[EcommerceSalesCreationLedger]
        SET [ResponsePayload] = @ResponsePayload,
            [CompletedUtc] = SYSUTCDATETIME()
        WHERE [OperationNetUid] = @OperationNetUid
          AND [OperationName] = @OperationName
          AND [PrincipalNetUid] = @PrincipalNetUid
          AND [ResponsePayload] IS NULL
          AND [CompletedUtc] IS NULL;
        """;

    private readonly string _claimSql;

    public SqlCheckoutLedger(int claimLockTimeoutMilliseconds = DefaultClaimLockTimeoutMilliseconds) {
        ArgumentOutOfRangeException.ThrowIfLessThan(claimLockTimeoutMilliseconds, 0);

        _claimSql = BuildClaimSql(claimLockTimeoutMilliseconds);
    }

    internal static string BuildClaimSql(int claimLockTimeoutMilliseconds) {
        return ClaimSqlTemplate.Replace(
            "{LockTimeoutMilliseconds}",
            claimLockTimeoutMilliseconds.ToString(CultureInfo.InvariantCulture),
            StringComparison.Ordinal);
    }

    public CheckoutLedgerEntry Find(IDbConnection connection, Guid operationNetUid) {
        ArgumentNullException.ThrowIfNull(connection);

        return connection.QuerySingleOrDefault<CheckoutLedgerEntry>(
            FindSql,
            new { OperationNetUid = operationNetUid });
    }

    public CheckoutClaim TryClaim(IDbConnection connection, CheckoutRequest request) {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(request);

        int claim = connection.ExecuteScalar<int>(
            _claimSql,
            new {
                request.OperationNetUid,
                OperationName = new DbString {
                    Value = request.OperationName,
                    IsAnsi = false,
                    Length = CheckoutRequest.MaxOperationNameLength
                },
                request.PrincipalNetUid,
                request.ClientNetUid,
                request.ModeFlag,
                request.RequestFingerprint
            });

        return claim switch {
            1 => CheckoutClaim.Claimed,
            0 => CheckoutClaim.AlreadyRecorded,
            -1 => CheckoutClaim.Busy,
            _ => throw new InvalidOperationException("The checkout ledger returned an unknown claim state.")
        };
    }

    public void Complete(IDbConnection connection, CheckoutRequest request, string responsePayload) {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(responsePayload);

        int affected = connection.Execute(
            CompleteSql,
            new {
                request.OperationNetUid,
                OperationName = new DbString {
                    Value = request.OperationName,
                    IsAnsi = false,
                    Length = CheckoutRequest.MaxOperationNameLength
                },
                request.PrincipalNetUid,
                ResponsePayload = responsePayload
            });

        if (affected != 1)
            throw new InvalidOperationException("The checkout receipt could not be completed.");
    }
}
