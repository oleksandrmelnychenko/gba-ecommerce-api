using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using GBA.Common.Helpers;
using Microsoft.AspNetCore.Http;

namespace GBA.Common.Middleware;

public sealed class ReflectionTypeLoadExceptionLoggingMiddleware(RequestDelegate next) {
    public async Task InvokeAsync(HttpContext httpContext) {
        try {
            await next(httpContext);
        } catch (ReflectionTypeLoadException ex) {
            Console.WriteLine("ReflectionTypeLoadException!!! {0}", ex);

            if (ex.LoaderExceptions != null) {
                Console.WriteLine("Loader exceptions messages: ");

                foreach (Exception? exception in ex.LoaderExceptions) {
                    if (exception == null) continue;

                    string logFilePath = Path.Combine(ConfigurationManager.EnvironmentRootPath, "Data", "error_log.txt");
                    File.AppendAllText(logFilePath, exception.Message + Environment.NewLine);

                    Console.WriteLine(exception);
                }
            }

            throw;
        }
    }
}
