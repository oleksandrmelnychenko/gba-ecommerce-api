using System;
using System.Data;
using System.IO;
using GBA.Common.ResourceNames;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Services.Infrastructure.Checkout;
using Moq;

namespace GBA.Ecommerce.Api.Tests;

public sealed class CheckoutSaleNumberTests {
    [Theory]
    [InlineData("КИл", "КИл00002956", 8, "КИл00002957")]
    [InlineData("Ок", "Ок00002856", 8, "Ок00002857")]
    [InlineData("Ок", null, 8, "Ок00000001")]
    [InlineData("К", "К0000000041", 10, "К0000000042")]
    public void Next_value_continues_the_numeric_sequence_of_its_prefix(
        string prefix,
        string? previous,
        int width,
        string expected) {
        Assert.Equal(expected, GeneratedSaleNumberPolicy.GetNextValue(prefix, previous!, width));
    }

    [Theory]
    [InlineData("КИл", "1C")]
    [InlineData("Ок", "КОк00000001")]
    [InlineData("Ок", "Ок")]
    [InlineData("Ок", "Ок-0000001")]
    public void Next_value_rejects_a_previous_value_of_another_format(string prefix, string previous) {
        Assert.Throws<InvalidOperationException>(() =>
            GeneratedSaleNumberPolicy.GetNextValue(prefix, previous, 8));
    }

    [Fact]
    public void Next_value_refuses_to_overflow_the_sequence_width() {
        Assert.Throws<InvalidOperationException>(() =>
            GeneratedSaleNumberPolicy.GetNextValue("Ок", "Ок99999999", 8));
    }

    [Fact]
    public void Next_value_requires_a_prefix() {
        Assert.Throws<ArgumentException>(() => GeneratedSaleNumberPolicy.GetNextValue(" ", null!, 8));
    }

    [Theory]
    [InlineData("К", "К")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Shop_numbers_use_the_month_coded_prefix_even_without_an_organization_code(
        string? code,
        string expectedCode) {
        Assert.Equal(
            expectedCode + MonthCodesResourceNames.GetCurrentMonthCode(),
            CheckoutSaleNumbers.Prefix(new Organization { Code = code! }));
    }

    [Fact]
    public void Allocation_reads_the_last_generated_number_of_the_current_year_and_adds_the_next() {
        Organization organization = new() { Id = 7, NetUid = Guid.NewGuid(), Code = "К" };
        string prefix = "К" + MonthCodesResourceNames.GetCurrentMonthCode();
        Mock<IDbConnection> connection = new();
        Mock<IDbCommand> command = new();
        command.SetupProperty(candidate => candidate.CommandText);
        command.Setup(candidate => candidate.CreateParameter()).Returns(new Mock<IDbDataParameter>().Object);
        command.SetupGet(candidate => candidate.Parameters).Returns(new Mock<IDataParameterCollection>().Object);
        connection.Setup(candidate => candidate.CreateCommand()).Returns(command.Object);
        connection.SetupGet(candidate => candidate.State).Returns(ConnectionState.Open);
        Mock<ISaleNumberRepository> repository = new();
        repository
            .Setup(candidate => candidate.GetLastGeneratedByOrganizationNetId(organization.NetUid, prefix, DateTime.Now.Year))
            .Returns(new SaleNumber { Value = prefix + "00000041" });
        SaleNumber? added = null;
        repository
            .Setup(candidate => candidate.Add(It.IsAny<SaleNumber>()))
            .Callback<SaleNumber>(number => added = number)
            .Returns(99);

        using System.Transactions.TransactionScope scope = new();
        long id = CheckoutSaleNumbers.Add(connection.Object, repository.Object, organization);

        Assert.Equal(99, id);
        Assert.Equal(prefix + "00000042", added!.Value);
        Assert.Equal(organization.Id, added.OrganizationId);
    }

    [Fact]
    public void Shop_lookup_uses_exactly_the_gba_server_locking_query() {
        string repository = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src/GBA.Domain/Repositories/Sales/SaleNumberRepository.cs"));

        Assert.Contains("\"FROM [SaleNumber] WITH (UPDLOCK, HOLDLOCK) \" +", repository, StringComparison.Ordinal);
        Assert.Contains("\"AND LEFT([SaleNumber].[Value], LEN(@ValuePrefix)) = @ValuePrefix \" +", repository, StringComparison.Ordinal);
        Assert.Contains("\"AND [SaleNumber].[Created] >= @YearStart \" +", repository, StringComparison.Ordinal);
        Assert.Contains("\"[SaleNumber].[Value], LEN(@ValuePrefix) + 1, 64)) DESC, \" +", repository, StringComparison.Ordinal);
    }

    private static string RepositoryRoot() {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null) {
            if (File.Exists(Path.Combine(directory.FullName, "GBA.Ecommerce.slnx"))) return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("The gba-ecommerce-api repository root was not found.");
    }
}
