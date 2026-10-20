using System;
using System.IO;

namespace GBA.Ecommerce.Api.Tests;

public sealed class OrganizationVatPayerContractTests {
    [Fact]
    public void Accounting_movements_treat_a_zero_rate_organization_as_non_vat() {
        string source = ReadSource("src/GBA.Domain/Repositories/Consignments/ConsignmentInfoRepository.cs");
        const string payer =
            "EXISTS (SELECT 1 FROM [VatRate] WHERE [VatRate].[ID] = [Organization].[VatRateID] AND [VatRate].[Value] <> 0) ";

        Assert.Equal(1, Count(source, "builder.Append(\"AND " + payer + "\");"));
        Assert.Equal(1, Count(source, "builder.Append(\"AND NOT " + payer + "\");"));
        Assert.Equal(0, Count(source, "[Organization].[VatRateID] IS NOT NULL"));
        Assert.Equal(0, Count(source, "[Organization].[VatRateID] IS NULL"));
    }

    private static int Count(string source, string value) {
        int count = 0;
        for (int index = source.IndexOf(value, StringComparison.Ordinal); index >= 0;
             index = source.IndexOf(value, index + value.Length, StringComparison.Ordinal))
            count++;
        return count;
    }

    private static string ReadSource(string relativePath) {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null) {
            string candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate)) return File.ReadAllText(candidate);
            directory = directory.Parent;
        }

        throw new FileNotFoundException(relativePath);
    }
}
