using System;
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

public sealed class UpdatePolishStorageProductsAvailabilityActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;

    public UpdatePolishStorageProductsAvailabilityActor(IDbConnectionFactory connectionFactory) {
        _connectionFactory = connectionFactory;

        Receive<UpdatePolishStorageProductsAvailabilityMessage>(UpdateAvailability);
    }

    private void UpdateAvailability(UpdatePolishStorageProductsAvailabilityMessage message) {
        string logFilePath = $"{ConfigurationManager.EnvironmentRootPath}\\Logs\\update_availability_pl_log.txt";

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

                string allText = File.ReadAllText(tempFilePath);

                File.Delete(tempFilePath);

                File.AppendAllText(tempFilePath, allText.Replace("\r\n'", "'").Replace("1/8', ", "1/8, "));

                string[] allLines = File.ReadAllLines(tempFilePath);

                bool isValidData = false;

                StringBuilder builder = new();

                builder.Append(@"CREATE TABLE dbo.#TempProducts 
                    (
                    id bigint,
                    shop_category_id nvarchar(9),
                    part_number nvarchar(150),
                    [name] nvarchar(550),
                    [description] nvarchar(2550),
                    remain float DEFAULT 0.00, 
                    remain_vat float DEFAULT 0.00, 
                    [weight] nvarchar(50), 
                    size nvarchar(100), 
                    main_original_number nvarchar(2650),
                    [status] nvarchar(100), 
                    unixtime  nvarchar(100) 
                    )");

                int insertValuesCount = 0;
                int insertsCount = 0;

                using (IDbConnection connection = _connectionFactory.NewSqlConnection()) {
                    connection.Execute("UPDATE ProductAvailability SET Amount = 0 WHERE [ProductAvailability].StorageID = 2 AND [ProductAvailability].Deleted = 0");
                }

                bool moreValuesExists = false;

                for (int i = 0; i < allLines.Length; i++)
                    if (allLines[i].StartsWith("INSERT ")) {
                        if (allLines[i].StartsWith("INSERT INTO q2_shop_products ")) {
                            isValidData = true;

                            builder.Append(allLines[i].Replace("INSERT INTO q2_shop_products ", "INSERT INTO dbo.#TempProducts "));

                            insertValuesCount = 1;
                        } else {
                            isValidData = false;
                        }
                    } else {
                        if (isValidData) {
                            if (allLines[i].StartsWith(" ON DUPLICATE KEY")) continue;

                            if (allLines[i].StartsWith(" weight")) continue;

                            if (allLines[i].StartsWith(" size")) continue;

                            if (allLines[i].StartsWith(" main_original_number")) continue;

                            if (allLines[i].StartsWith(" description")) continue;

                            if (allLines[i].StartsWith(" unixtime")) continue;

                            if (allLines[i].StartsWith(" remain")) continue;

                            if (allLines[i].StartsWith(" remain_vat")) continue;

                            if (allLines[i].StartsWith(" name")) continue;

                            if (allLines[i].StartsWith(" part_number")) continue;

                            if (allLines[i].StartsWith(" shop_category_id")) continue;

                            if (allLines[i].StartsWith(" id")) continue;

                            if (allLines[i].StartsWith(" status")) continue;

                            if (insertValuesCount.Equals(900)) {
                                insertValuesCount = 1;

                                if (insertsCount.Equals(20) || i.Equals(allLines.Length - 1)) {
                                    builder.Append(@"
                    DELETE FROM dbo.#TempProducts WHERE remain = 0;
                    UPDATE [ProductAvailability]
                    SET Amount = (SELECT remain FROM dbo.#TempProducts WHERE [Product].OldEcommerceID = dbo.#TempProducts.id)
                    FROM [ProductAvailability]
                    	LEFT JOIN [Product]
                    		ON [Product].ID = [ProductAvailability].ProductID
                    WHERE [ProductAvailability].StorageID = 2
                    AND [Product].OldEcommerceID IN (SELECT dbo.#TempProducts.id FROM dbo.#TempProducts) 
                    AND [ProductAvailability].Deleted = 0");

                                    builder.Append("DROP TABLE dbo.#TempProducts ");

                                    string query = builder.ToString();

                                    builder = new StringBuilder();

                                    using (IDbConnection connection = _connectionFactory.NewSqlConnection()) {
                                        connection.Execute(query, commandTimeout: 3600);
                                    }

                                    builder.Append(@"CREATE TABLE dbo.#TempProducts 
                    (
                    id bigint,
                    shop_category_id nvarchar(9),
                    part_number nvarchar(150),
                    [name] nvarchar(550),
                    [description] nvarchar(2550),
                    remain float DEFAULT 0.00, 
                    remain_vat float DEFAULT 0.00, 
                    [weight] nvarchar(50), 
                    size nvarchar(100), 
                    main_original_number nvarchar(2650),
                    [status] nvarchar(100), 
                    unixtime  nvarchar(100) 
                    )");
                                    insertsCount = 0;
                                }

                                builder.Append(
                                    "INSERT INTO dbo.#TempProducts (id,shop_category_id,part_number,name,description,remain,remain_vat, weight, size, main_original_number,status, unixtime) VALUES ");

                                builder.Append(allLines[i].Replace(@"\'", "") + "\r\n");

                                moreValuesExists = true;
                            } else {
                                insertValuesCount++;

                                if (insertValuesCount.Equals(900)) {
                                    if (allLines[i].EndsWith(",")) {
                                        builder.Append(allLines[i].Substring(0, allLines[i].Length - 1).Replace(@"\'", "") + "\r\n");

                                        moreValuesExists = true;
                                    }

                                    insertsCount++;
                                } else {
                                    builder.Append(allLines[i].Replace(@"\'", "") + "\r\n");

                                    moreValuesExists = true;
                                }
                            }
                        } else if (moreValuesExists) {
                            builder.Append(@"
                    DELETE FROM dbo.#TempProducts WHERE remain = 0;
                    UPDATE [ProductAvailability]
                    SET Amount = (SELECT remain FROM dbo.#TempProducts WHERE [Product].OldEcommerceID = dbo.#TempProducts.id)
                    FROM [ProductAvailability]
                    	LEFT JOIN [Product]
                    		ON [Product].ID = [ProductAvailability].ProductID
                    WHERE [ProductAvailability].StorageID = 2
                    AND [Product].OldEcommerceID IN (SELECT dbo.#TempProducts.id FROM dbo.#TempProducts) 
                    AND [ProductAvailability].Deleted = 0");

                            builder.Append("DROP TABLE dbo.#TempProducts ");

                            string query = builder.ToString();

                            using IDbConnection connection = _connectionFactory.NewSqlConnection();
                            connection.Execute(query, commandTimeout: 3600);

                            moreValuesExists = false;
                        }
                    }

                using (IDbConnection connection = _connectionFactory.NewSqlConnection()) {
                    connection.Execute("EXEC sp_updatestats");
                }

                File.Delete(tempFilePath);

                string logData =
                    string.Format(
                        "\r\n Operation: SUCCESS \r\n Finished at {0} UTC \r\n",
                        DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
                    );

                File.AppendAllText(logFilePath, logData);
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