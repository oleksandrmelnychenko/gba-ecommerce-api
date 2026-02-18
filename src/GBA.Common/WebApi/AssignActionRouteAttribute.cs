using System;
using Microsoft.AspNetCore.Mvc;

namespace GBA.Common.WebApi;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AssignActionRouteAttribute : RouteAttribute {
    public AssignActionRouteAttribute(string template) : base(template) { }
}