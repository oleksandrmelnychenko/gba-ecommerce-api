using System;
using System.IO;

namespace GBA.Ecommerce.Api.Tests;

public sealed class ClientPhoneAliasSqlContractTests {
    private const string RepositoryPath =
        "src/GBA.Domain/Repositories/Clients/ClientRepository.cs";

    [Fact]
    public void Login_resolves_all_stored_phone_aliases_only_when_the_client_is_unambiguous() {
        string repository = File.ReadAllText(
            Path.Combine(
                FindRepositoryRoot(),
                RepositoryPath));
        string method = SliceBetween(
            repository,
            "public Client GetClientNetIdByMobileNumber(string value)",
            "public Client GetById(long id)");

        Assert.Contains(
            "SELECT TOP (2) [Client].*",
            method,
            StringComparison.Ordinal);
        Assert.Contains(
            "[Client].[MobileNumber] = @Value",
            method,
            StringComparison.Ordinal);
        Assert.Contains(
            "[Client].[ClientNumber] = @Value",
            method,
            StringComparison.Ordinal);
        Assert.Contains(
            "[Client].[SMSNumber] = @Value",
            method,
            StringComparison.Ordinal);
        Assert.Contains(
            "matches.Count == 1",
            method,
            StringComparison.Ordinal);
    }

    private static string SliceBetween(
        string source,
        string startMarker,
        string endMarker) {
        int start = source.IndexOf(
            startMarker,
            StringComparison.Ordinal);
        Assert.True(start >= 0, $"Missing start marker: {startMarker}");
        int end = source.IndexOf(
            endMarker,
            start + startMarker.Length,
            StringComparison.Ordinal);
        Assert.True(end > start, $"Missing end marker: {endMarker}");
        return source.Substring(start, end - start);
    }

    private static string FindRepositoryRoot() {
        DirectoryInfo? directory =
            new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null) {
            if (File.Exists(
                    Path.Combine(
                        directory.FullName,
                        RepositoryPath)))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "The gba-ecommerce-api repository root was not found.");
    }
}
