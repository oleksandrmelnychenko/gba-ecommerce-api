using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using Akka.Actor;
using Dapper;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.SchedulerTasks;

namespace GBA.Services.Actors.SchedulerTasks;

public sealed class UpdateProductPricesTaskActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;

    public UpdateProductPricesTaskActor(IDbConnectionFactory connectionFactory) {
        _connectionFactory = connectionFactory;

        Receive<UpdateProductPricesMessage>(UpdateProductPrices);
    }

    private void UpdateProductPrices(UpdateProductPricesMessage message) {
        string logFilePath = $"{ConfigurationManager.EnvironmentRootPath}\\Logs\\update_product_prices_log.txt";

        try {
            string updateSqlScriptPath = @"D:\Sync\upload_pl\upload.sql";

            if (!File.Exists(updateSqlScriptPath)) {
                string logData = string.Format("\r\n Operation: FAILED \r\n Reason: Upload.sql file does not exists \r\n Finished at {0} UTC \r\n",
                    DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));

                File.AppendAllText(logFilePath, logData);
            } else {
                string tempFileName = $"{Guid.NewGuid().ToString()}.sql";

                string tempFilePath = Path.Combine(message.TempFolderPath, tempFileName);

                File.Copy(updateSqlScriptPath, tempFilePath);

                string[] allLines = File.ReadAllLines(tempFilePath);

                bool isValidData = false;

                List<string> validStrings = new();

                for (int i = 0; i < allLines.Length; i++)
                    if (allLines[i].StartsWith("INSERT ")) {
                        isValidData = allLines[i].StartsWith("INSERT INTO q2_shop_products_prices");
                    } else {
                        if (!isValidData || !allLines[i].StartsWith("(") && !allLines[i].StartsWith(",(")) continue;
                        // Use Span-based field extraction to avoid Split() array allocation
                        ReadOnlySpan<char> lineSpan = allLines[i].AsSpan();
                        ReadOnlySpan<char> field1 = StringOptimizations.GetField(lineSpan, ',', 1);
                        ReadOnlySpan<char> field2 = StringOptimizations.GetField(lineSpan, ',', 2);

                        if (!field1.SequenceEqual("'000000003'") && !field2.SequenceEqual("'000000003'")) continue;
                        // Remove leading ",(" or "(", trailing ")", and all quotes
                        allLines[i] = allLines[i].Replace(",(", "").Replace("(", "").Replace(")", "").Replace("'", "");

                        validStrings.Add(allLines[i]);
                    }

                StringBuilder builder = new();

                builder.Append("CREATE TABLE dbo.[#TempPrices] ");
                builder.Append("(");
                builder.Append("EcommerceId bigint, ");
                builder.Append("Price money");
                builder.Append(")");

                // Reusable span for field ranges to avoid allocations
                Span<Range> fieldRanges = stackalloc Range[8];

                for (int i = 0; i < validStrings.Count; i++) {
                    // Use Span-based parsing instead of Split() to avoid array allocation
                    ReadOnlySpan<char> lineSpan = validStrings[i].AsSpan();
                    StringOptimizations.ParseFields(lineSpan, ',', fieldRanges);
                    ReadOnlySpan<char> field0 = lineSpan[fieldRanges[0]];
                    ReadOnlySpan<char> field3 = lineSpan[fieldRanges[3]];

                    if (i % 20 == 0) {
                        builder.Append(";\r\nINSERT INTO dbo.[#TempPrices] (EcommerceId, Price)\r\n");
                        builder.Append("VALUES\r\n");

                        builder.Append('(').Append(field0).Append(',').Append(field3).Append(")\r\n");
                    } else {
                        builder.Append(",(").Append(field0).Append(',').Append(field3).Append(")\r\n");
                    }
                }

                builder.Append(";\r\nUPDATE [ProductPricing] ");
                builder.Append("SET Price = (SELECT Price FROM dbo.[#TempPrices] WHERE EcommerceId = [Product].OldEcommerceID) ");
                builder.Append("FROM [ProductPricing] ");
                builder.Append("LEFT JOIN [Product] ");
                builder.Append("ON [ProductPricing].ProductID = [Product].ID ");
                builder.Append("WHERE [Product].OldEcommerceID IN (SELECT EcommerceId FROM dbo.[#TempPrices]) ");
                builder.Append("AND [ProductPricing].PricingID = 1 ");
                builder.Append("\r\n;DROP TABLE dbo.[#TempPrices]");

                string query = builder.ToString();

                using (IDbConnection connection = _connectionFactory.NewSqlConnection()) {
                    try {
                        connection.Execute(query, commandTimeout: 3600);

                        connection.Execute("EXEC sp_updatestats");

                        string logData =
                            string.Format(
                                "\r\n Operation: SUCCESS \r\n \r\n Finished at {0} UTC \r\n",
                                DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
                            );

                        File.AppendAllText(logFilePath, logData);
                    } catch (Exception exc) {
                        string logData;

                        if (exc.InnerException != null)
                            logData =
                                string.Format(
                                    "\r\n Operation: FAILED \r\n Exception: {0} \r\n InnerException: {1} \r\n Finished at {2} UTC \r\n",
                                    exc.Message,
                                    exc.InnerException.Message,
                                    DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
                                );
                        else
                            logData =
                                string.Format(
                                    "\r\n Operation: FAILED \r\n Exception: {0} \r\n Finished at {1} UTC \r\n",
                                    exc.Message,
                                    DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
                                );

                        File.AppendAllText(logFilePath, logData);
                    }
                }

                isValidData = false;

                validStrings = new List<string>();

                for (int i = 0; i < allLines.Length; i++)
                    if (allLines[i].StartsWith("INSERT ")) {
                        isValidData = allLines[i].StartsWith("INSERT INTO q2_shop_products_prices");
                    } else {
                        if (!isValidData || !allLines[i].StartsWith("(") && !allLines[i].StartsWith(",(")) continue;
                        // Use Span-based field extraction to avoid Split() array allocation
                        ReadOnlySpan<char> lineSpan = allLines[i].AsSpan();
                        ReadOnlySpan<char> field1 = StringOptimizations.GetField(lineSpan, ',', 1);
                        ReadOnlySpan<char> field2 = StringOptimizations.GetField(lineSpan, ',', 2);

                        if (!field1.SequenceEqual("'000000013'") && !field2.SequenceEqual("'000000013'")) continue;
                        // Remove leading ",(" or "(", trailing ")", and all quotes
                        allLines[i] = allLines[i].Replace(",(", "").Replace("(", "").Replace(")", "").Replace("'", "");

                        validStrings.Add(allLines[i]);
                    }

                builder = new StringBuilder();

                builder.Append("CREATE TABLE dbo.[#TempPrices] ");
                builder.Append("(");
                builder.Append("EcommerceId bigint, ");
                builder.Append("Price money");
                builder.Append(")");

                // Reuse stackalloc span for field parsing
                fieldRanges = stackalloc Range[8];

                for (int i = 0; i < validStrings.Count; i++) {
                    // Use Span-based parsing instead of Split() to avoid array allocation
                    ReadOnlySpan<char> lineSpan = validStrings[i].AsSpan();
                    StringOptimizations.ParseFields(lineSpan, ',', fieldRanges);
                    ReadOnlySpan<char> field0 = lineSpan[fieldRanges[0]];
                    ReadOnlySpan<char> field3 = lineSpan[fieldRanges[3]];

                    if (i % 20 == 0) {
                        builder.Append(";\r\nINSERT INTO dbo.[#TempPrices] (EcommerceId, Price)\r\n");
                        builder.Append("VALUES\r\n");

                        builder.Append('(').Append(field0).Append(',').Append(field3).Append(")\r\n");
                    } else {
                        builder.Append(",(").Append(field0).Append(',').Append(field3).Append(")\r\n");
                    }
                }

                builder.Append(";\r\nUPDATE [ProductPricing] ");
                builder.Append("SET Price = (SELECT Price FROM dbo.[#TempPrices] WHERE EcommerceId = [Product].OldEcommerceID) ");
                builder.Append("FROM [ProductPricing] ");
                builder.Append("LEFT JOIN [Product] ");
                builder.Append("ON [ProductPricing].ProductID = [Product].ID ");
                builder.Append("WHERE [Product].OldEcommerceID IN (SELECT EcommerceId FROM dbo.[#TempPrices]) ");
                builder.Append("AND [ProductPricing].PricingID = 5 ");
                builder.Append("\r\n;DROP TABLE dbo.[#TempPrices]");

                query = builder.ToString();

                using (IDbConnection connection = _connectionFactory.NewSqlConnection()) {
                    try {
                        connection.Execute(query, commandTimeout: 3600);

                        connection.Execute("EXEC sp_updatestats");

                        string logData =
                            string.Format(
                                "\r\n Operation: SUCCESS \r\n \r\n Finished at {0} UTC \r\n",
                                DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
                            );

                        File.AppendAllText(logFilePath, logData);
                    } catch (Exception exc) {
                        string logData;

                        if (exc.InnerException != null)
                            logData =
                                string.Format(
                                    "\r\n Operation: FAILED \r\n Exception: {0} \r\n InnerException: {1} \r\n Finished at {2} UTC \r\n",
                                    exc.Message,
                                    exc.InnerException.Message,
                                    DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
                                );
                        else
                            logData =
                                string.Format(
                                    "\r\n Operation: FAILED \r\n Exception: {0} \r\n Finished at {1} UTC \r\n",
                                    exc.Message,
                                    DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
                                );

                        File.AppendAllText(logFilePath, logData);
                    }
                }

                File.Delete(tempFilePath);
            }
        } catch (Exception exception) {
            string logData;

            if (exception.InnerException != null)
                logData =
                    string.Format(
                        "\r\n Operation: FAILED \r\n Exception: {0} \r\n InnerException: {1} \r\n Finished at {2} UTC \r\n",
                        exception.Message,
                        exception.InnerException.Message,
                        DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
                    );
            else
                logData =
                    string.Format(
                        "\r\n Operation: FAILED \r\n Exception: {0} \r\n Finished at {1} UTC \r\n",
                        exception.Message,
                        DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
                    );

            File.AppendAllText(logFilePath, logData);
        }
    }
}