using System;
using Microsoft.Extensions.Configuration;

namespace GBA.Common.Helpers;

public static class ConfigurationManager {
    public static string LocalDataAnalyticConnectionString { get; private set; }

    public static string LocalDatabaseConnectionString { get; private set; }

    public static string LocalIdentityConnectionString { get; private set; }

    public static string FenixOneCConnectionString { get; private set; }

    public static string AmgOneCConnectionString { get; private set; }

    public static string EnvironmentRootPath { get; private set; }

    public static string MailUserName { get; private set; }

    public static string MailPassword { get; private set; }

    public static string MailSenderMail { get; private set; }

    public static string MailSenderName { get; private set; }

    public static int MailPort { get; private set; }

    public static string MailSmtpUrl { get; private set; }

    public static bool MailRequireSsl { get; private set; }

    public static string RecommendationsURL { get; private set; }

    public static string RecommendationsEndpoint { get; private set; }

    public static string RecommendationsBatchEndpoint { get; private set; }

    public static string SalesForecastEndpoint { get; private set; }

    public static void SetAppSettingsProperties(IConfiguration configuration) {
        LocalDataAnalyticConnectionString = configuration.GetConnectionString(ConnectionStringNames.LocalDataAnalitic);

        LocalDatabaseConnectionString = configuration.GetConnectionString(ConnectionStringNames.Local);

        LocalIdentityConnectionString = configuration.GetConnectionString(ConnectionStringNames.LocalIdentity);

        FenixOneCConnectionString = configuration.GetConnectionString(ConnectionStringNames.FenixOneC);

        AmgOneCConnectionString = configuration.GetConnectionString(ConnectionStringNames.AmgOneC);

        MailUserName = configuration.GetSection("Mail")[ConfigurationStringNames.MailUserName];

        MailPassword = configuration.GetSection("Mail")[ConfigurationStringNames.MailPassword];

        MailSenderMail = configuration.GetSection("Mail")[ConfigurationStringNames.MailSenderMail];

        MailSenderName = configuration.GetSection("Mail")[ConfigurationStringNames.MailSenderName];

        MailPort = Convert.ToInt32(configuration.GetSection("Mail")[ConfigurationStringNames.MailPort]);

        MailSmtpUrl = configuration.GetSection("Mail")[ConfigurationStringNames.MailSmtpUrl];

        MailRequireSsl = Convert.ToBoolean(configuration.GetSection("Mail")[ConfigurationStringNames.MailRequireSsl]);

        RecommendationsURL = configuration.GetSection("RecommendationAPI")[ConfigurationStringNames.URL];

        RecommendationsEndpoint = configuration.GetSection("RecommendationAPI")[ConfigurationStringNames.Recommendations];

        RecommendationsBatchEndpoint = configuration.GetSection("RecommendationAPI")[ConfigurationStringNames.RecommendationsBatch];

        SalesForecastEndpoint = configuration.GetSection("RecommendationAPI")[ConfigurationStringNames.SalesForecast];
    }

    public static void SetAppEnvironmentRootPath(string path) {
        EnvironmentRootPath = path;
    }
}