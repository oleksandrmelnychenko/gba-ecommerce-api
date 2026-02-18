using System;
using System.Globalization;
using System.IO;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Domain.Messages.Logging;

namespace GBA.Services.Actors.Logging;

public sealed class LogManagerActor : ReceiveActor {
    public LogManagerActor() {
        Receive<AddNewErrorLogMessage>(message => {
            string logFilePath = $"{ConfigurationManager.EnvironmentRootPath}\\Data\\error_log.txt";

            string logData =
                string.Format(
                    "\r\n Route: {0} \r\n Entity JSON:\r\n {1} \r\n Triggered at {2} UTC \r\n",
                    message.URL,
                    message.Entity,
                    DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
                );

            File.AppendAllText(logFilePath, logData);
        });

        Receive<NewErrorAddSupplyInvoiceToServicesMessage>(message => {
            string logFilePath = $"{ConfigurationManager.EnvironmentRootPath}\\Data\\add_supply_invoice_error_log.txt";

            string logData =
                string.Format(
                    "\r\n Error: {0} \r\n Entity JSON:\r\n {1} \r\n Triggered at {2} UTC \r\n",
                    message.ErrorMessage,
                    message.Entity,
                    DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
                );

            File.AppendAllText(logFilePath, logData);
        });

        Receive<AddDataSyncLogMessage>(message => {
            string logFilePath = $"{ConfigurationManager.EnvironmentRootPath}\\Data\\data_sync_log.txt";

            if (!string.IsNullOrEmpty(message.SerializedException)) {
                string logData =
                    string.Format(
                        "\r\n  Sync started by: {0} \r\n  Log message:\r\n{1} \r\n  Serialized Exception:\r\n{2} \r\n  Triggered at: {3} UTC \r\n",
                        message.UserFullName,
                        message.Message,
                        message.SerializedException,
                        DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
                    );

                File.AppendAllText(logFilePath, logData);
            } else {
                string logData =
                    string.Format(
                        "\r\n  Sync started by: {0} \r\n  Log message:\r\n{1} \r\n  Triggered at: {2} UTC \r\n",
                        message.UserFullName,
                        message.Message,
                        DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
                    );

                File.AppendAllText(logFilePath, logData);
            }
        });

        Receive<AddExpiredBillLogMessage>(message => {
            string logFilePath = $"{ConfigurationManager.EnvironmentRootPath}\\Data\\expired_bills_log.txt";

            if (!string.IsNullOrEmpty(message.SerializedException)) {
                string logData =
                    string.Format(
                        "\r\n  Log message:\r\n{0} \r\n  Serialized Exception:\r\n{1} \r\n  Triggered at: {2} UTC \r\n",
                        message.Message,
                        message.SerializedException,
                        DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
                    );

                File.AppendAllText(logFilePath, logData);
            } else {
                string logData =
                    string.Format(
                        "\r\n  Log message:\r\n{0} \r\n  Triggered at: {1} UTC \r\n",
                        message.Message,
                        DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
                    );

                File.AppendAllText(logFilePath, logData);
            }
        });
    }
}