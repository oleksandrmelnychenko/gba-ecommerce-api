using System.Data;

namespace GBA.Domain.DbConnectionFactory.Contracts;

public interface IDbConnectionFactory {
    IDbConnection NewSqlConnection();
    IDbConnection NewDataAnalyticSqlConnection();
    IDbConnection NewIdentitySqlConnection();

    IDbConnection NewFenixOneCSqlConnection();

    IDbConnection NewAmgOneCSqlConnection();
}