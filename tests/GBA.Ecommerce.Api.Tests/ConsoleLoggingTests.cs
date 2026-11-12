using System.ComponentModel;
using System.Data.Common;
using System.Text.Json;
using System.Xml.Linq;
using GBA.Ecommerce.Background;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;

namespace GBA.Ecommerce.Api.Tests;

public sealed class ConsoleLoggingTests {
    [Theory]
    [InlineData(1, "WARN")]
    [InlineData(4, "WARN")]
    [InlineData(5, "ERROR")]
    [InlineData(6, "ERROR")]
    public void Monitor_sql_failures_keep_their_severity_and_diagnostics_in_one_console_record(
        int consecutiveFailure,
        string expectedLevel) {
        using LogFactory factory = CreateFactory();
        using NLogLoggerFactory loggerFactory = new(new NLogProviderOptions(), factory);
        ILogger<RetailPricingContextMonitor> logger = loggerFactory.CreateLogger<RetailPricingContextMonitor>();
        SqlException exception = Assert.Throws<SqlException>(() => ThrowTimeout(TestSqlExceptions.Create(-2)));

        RetailPricingContextMonitor.LogDatabaseFailure(logger, exception, consecutiveFailure);

        string console = Assert.Single(factory.Configuration!.FindTargetByName<MemoryTarget>("console").Logs);
        Assert.DoesNotContain("\r", console);
        Assert.DoesNotContain("\n", console);
        Assert.Contains($"{expectedLevel.PadRight(5)} [--------] RetailPricingContextMonitor |", console);
        Assert.Contains($"({consecutiveFailure}/5); retrying.", console);
        Assert.Contains("Microsoft.Data.SqlClient.SqlException", console);
        Assert.Contains(exception.Message, console);
        Assert.Contains("Void ThrowTimeout(Microsoft.Data.SqlClient.SqlException)", console);
        Assert.Contains("at GBA.Ecommerce.Api.Tests.ConsoleLoggingTests.ThrowTimeout", console);

        using JsonDocument structured = JsonDocument.Parse(
            Assert.Single(factory.Configuration.FindTargetByName<MemoryTarget>("jsonFile").Logs));
        Assert.Equal(expectedLevel, structured.RootElement.GetProperty("level").GetString());
        Assert.Equal(typeof(SqlException).FullName, structured.RootElement.GetProperty("exceptionType").GetString());
        Assert.Contains("ThrowTimeout", structured.RootElement.GetProperty("stackTrace").GetString());
    }

    [Fact]
    public void Multiline_messages_and_inner_exceptions_keep_the_warning_on_one_line() {
        using LogFactory factory = CreateFactory();
        using NLogLoggerFactory loggerFactory = new(new NLogProviderOptions(), factory);
        ILogger<RetailPricingContextMonitor> logger = loggerFactory.CreateLogger<RetailPricingContextMonitor>();

        RetailPricingContextMonitor.LogDatabaseFailure(logger, new MultilineDbException(), 1);

        string console = Assert.Single(factory.Configuration!.FindTargetByName<MemoryTarget>("console").Logs);
        Assert.DoesNotContain("\r", console);
        Assert.DoesNotContain("\n", console);
        Assert.Contains("WARN", console);
        Assert.Contains("Execution Timeout Expired.", console);
        Assert.Contains("The wait operation timed out.", console);
        Assert.Contains("System.ComponentModel.Win32Exception", console);
        Assert.Contains("Unknown error 258", console);
    }

    [Fact]
    public void Multiline_error_messages_without_exceptions_remain_error_records() {
        using LogFactory factory = CreateFactory();

        factory.GetLogger("GBA.Ecommerce.Background.RetailPricingContextMonitor")
            .Error("Failed to monitor.\r\nRetry failed.\nConnection unavailable.");

        string console = Assert.Single(factory.Configuration!.FindTargetByName<MemoryTarget>("console").Logs);
        Assert.DoesNotContain("\r", console);
        Assert.DoesNotContain("\n", console);
        Assert.Contains("ERROR [--------] RetailPricingContextMonitor |", console);
        Assert.Contains("Failed to monitor.", console);
        Assert.Contains("Retry failed.", console);
        Assert.Contains("Connection unavailable.", console);
    }

    private static void ThrowTimeout(SqlException exception) => throw exception;

    private static LogFactory CreateFactory() {
        string path = Path.Combine(CheckoutSqlFixture.FindRepositoryRoot(), "src/GBA.Ecommerce/nlog.config");
        XDocument document = XDocument.Load(path);
        XNamespace nlog = document.Root!.Name.Namespace;
        XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
        XElement targets = document.Root.Element(nlog + "targets")!;
        targets.Attribute("async")!.Remove();
        foreach (XElement target in targets.Elements(nlog + "target")) {
            target.SetAttributeValue(xsi + "type", "Memory");
            foreach (XAttribute attribute in target.Attributes().ToArray()) {
                if (attribute.Name != "name" && attribute.Name != "layout" && attribute.Name != xsi + "type")
                    attribute.Remove();
            }
        }

        LogFactory factory = new() { ThrowConfigExceptions = true, ThrowExceptions = true };
        factory.Configuration = new XmlLoggingConfiguration(new StringReader(document.ToString()), null, factory);
        return factory;
    }

    private sealed class MultilineDbException() : DbException(
        "Execution Timeout Expired.\r\nThe wait operation timed out.",
        new Win32Exception(258, "Unknown error 258"));
}
