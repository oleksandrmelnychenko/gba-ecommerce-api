using System;
using System.Data;
using Dapper;
using GBA.Common.ResourceNames;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Services.Infrastructure.Checkout;

public static class CheckoutSaleNumbers {
    public const int SequenceWidth = 8;

    public const int LockTimeoutMilliseconds = 15000;

    public static string Prefix(Organization organization) {
        ArgumentNullException.ThrowIfNull(organization);

        return (organization.Code ?? string.Empty) + MonthCodesResourceNames.GetCurrentMonthCode();
    }

    public static long Add(IDbConnection connection, ISaleNumberRepository repository, Organization organization) {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(organization);

        CheckoutTransaction.RequireAmbient();

        string prefix = Prefix(organization);

        connection.Execute($"SET LOCK_TIMEOUT {LockTimeoutMilliseconds};");
        try {
            SaleNumber last = repository.GetLastGeneratedByOrganizationNetId(
                organization.NetUid,
                prefix,
                DateTime.Now.Year);

            return repository.Add(new SaleNumber {
                OrganizationId = organization.Id,
                Value = GeneratedSaleNumberPolicy.GetNextValue(prefix, last?.Value, SequenceWidth)
            });
        } finally {
            connection.Execute("SET LOCK_TIMEOUT -1;");
        }
    }
}
