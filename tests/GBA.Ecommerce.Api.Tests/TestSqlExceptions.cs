using System;
using System.Linq;
using System.Reflection;
using Microsoft.Data.SqlClient;

namespace GBA.Ecommerce.Api.Tests;

internal static class TestSqlExceptions {
    public static SqlException Create(int number) {
        ConstructorInfo errorConstructor = typeof(SqlError)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .OrderByDescending(constructor => constructor.GetParameters().Length)
            .First();
        bool numberAssigned = false;
        object?[] arguments = errorConstructor.GetParameters().Select(parameter => {
            if (parameter.ParameterType == typeof(int) && !numberAssigned) {
                numberAssigned = true;
                return (object)number;
            }

            if (parameter.ParameterType == typeof(byte)) return (byte)11;
            if (parameter.ParameterType == typeof(string)) return "checkout test";
            return parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null;
        }).ToArray();
        SqlError error = (SqlError)errorConstructor.Invoke(arguments);
        SqlErrorCollection errors = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)!;
        typeof(SqlErrorCollection)
            .GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(errors, new object[] { error });

        return (SqlException)typeof(SqlException)
            .GetMethod(
                "CreateException",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(SqlErrorCollection), typeof(string) },
                null)!
            .Invoke(null, new object[] { errors, "16.00.0000" })!;
    }
}
