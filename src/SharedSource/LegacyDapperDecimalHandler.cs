using System.Data;
using System.Globalization;
using System.Runtime.CompilerServices;
using Dapper;
using GBA.Domain;

#pragma warning disable CA2255

file sealed class LegacyDapperDecimalHandler : SqlMapper.TypeHandler<decimal> {
    public override decimal Parse(object value) =>
        value is decimal number ? number : LegacyNumeric.ToDecimal(value, CultureInfo.InvariantCulture);

    public override void SetValue(IDbDataParameter parameter, decimal value) {
        parameter.DbType = DbType.Decimal;
        parameter.Value = value;
    }
}

file static class LegacyDapperDecimalHandlerRegistration {
    [ModuleInitializer]
    internal static void Register() => SqlMapper.AddTypeHandler(new LegacyDapperDecimalHandler());
}
