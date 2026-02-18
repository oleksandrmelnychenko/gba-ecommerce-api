using System;
using Microsoft.AspNetCore.Mvc;

namespace GBA.Common.WebApi;

/// <summary>
/// Assign controller route.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AssignControllerRouteAttribute : RouteAttribute {
    /// <summary>
    /// ctor().
    /// </summary>
    /// <param name="environment">The environment.</param>
    /// <param name="version">The web api version.</param>
    /// <param name="template">The route template.</param>
    public AssignControllerRouteAttribute(string environment, int version, string template) :
        base($"{environment}/{BuildRouteVersion(version)}/{{culture}}/{template}") {
        //base($"{environment}/{BuildRouteVersion(version)}/{template}") {

        Version = BuildRouteVersion(version);
        Environment = environment;
    }

    /// <summary>
    /// Web Api version.
    /// </summary>
    public string Version { get; private set; }

    /// <summary>
    /// Dev or release environment.
    /// </summary>
    public string Environment { get; private set; }

    private static string BuildRouteVersion(int number) {
        return $"v{number.ToString()}";
    }
}