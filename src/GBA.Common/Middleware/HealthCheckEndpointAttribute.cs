using System;

namespace GBA.Common.Middleware;

/// <summary>
/// Marks an endpoint whose service-unavailable response represents health state
/// rather than an unhandled application failure.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class HealthCheckEndpointAttribute : Attribute;
