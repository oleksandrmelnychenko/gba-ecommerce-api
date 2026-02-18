using System.Data;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using Microsoft.Data.SqlClient;

namespace GBA.Domain.DbConnectionFactory;

public sealed class DbConnectionFactory : IDbConnectionFactory {
    public IDbConnection NewSqlConnection() {
        return new SqlConnection(ConfigurationManager.LocalDatabaseConnectionString);
    }

    public IDbConnection NewDataAnalyticSqlConnection() {
        return new SqlConnection(ConfigurationManager.LocalDataAnalyticConnectionString);
    }

    public IDbConnection NewIdentitySqlConnection() {
        return new SqlConnection(ConfigurationManager.LocalIdentityConnectionString);
    }

    public IDbConnection NewFenixOneCSqlConnection() {
        return new SqlConnection(ConfigurationManager.FenixOneCConnectionString);
    }

    public IDbConnection NewAmgOneCSqlConnection() {
        return new SqlConnection(ConfigurationManager.AmgOneCConnectionString);
    }
}