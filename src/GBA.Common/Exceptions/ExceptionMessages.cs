namespace GBA.Common.Exceptions;

public sealed class ExceptionMessages {
    public static string BuildIntRouteConstraintMessage(string routeParameterName) {
        return string.Format("Parameter '{0}' should be integer", routeParameterName);
    }

    public static string BuildGuidRouteConstraintMessage(string routeParameterName) {
        return string.Format("Parameter '{0}' should be guid", routeParameterName);
    }

    public static string BuildLongRouteConstraintMessage(string routeParameterName) {
        return string.Format("Parameter '{0}' should be long", routeParameterName);
    }
}