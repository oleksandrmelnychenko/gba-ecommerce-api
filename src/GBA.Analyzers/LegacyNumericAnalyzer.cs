using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace GBA.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LegacyNumericAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "GBA0001";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Numeric conversion changed in .NET 11",
        "{0} gives different results on .NET 11 than on .NET 10; use {1} instead",
        "Reliability",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Conversions between decimal and double/float and Math.Round(double, digits) were re-implemented in .NET 11. "
            + "LegacyNumeric keeps the .NET 10 results that stored amounts, quantities and weights were computed with.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterOperationAction(AnalyzeConversion, OperationKind.Conversion);
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);
    }

    private static void AnalyzeConversion(OperationAnalysisContext context) {
        var conversion = (IConversionOperation)context.Operation;
        if (conversion.Operand.Type is null || conversion.Type is null) return;
        ITypeSymbol from = Underlying(conversion.Operand.Type);
        ITypeSymbol to = Underlying(conversion.Type);
        if (from.TypeKind == TypeKind.Dynamic) {
            if (conversion.Syntax is not CastExpressionSyntax) return;
            if (IsDecimal(to)) {
                Report(context, conversion.Syntax, "A dynamic to decimal cast", "LegacyNumeric.ToDecimal((object)value)");
            }
            return;
        }
        if (IsBinary(from) && IsDecimal(to)) {
            Report(context, conversion.Syntax, "A " + from.ToDisplayString() + " to decimal conversion", "LegacyNumeric.ToDecimal");
        } else if (IsDecimal(from) && IsBinary(to)) {
            Report(context, conversion.Syntax, "A decimal to " + to.ToDisplayString() + " conversion",
                to.SpecialType == SpecialType.System_Single ? "LegacyNumeric.ToSingle" : "LegacyNumeric.ToDouble");
        }
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        var invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol method = invocation.TargetMethod;
        ImmutableArray<IParameterSymbol> parameters = method.Parameters;
        string type = method.ContainingType?.ToDisplayString() ?? string.Empty;
        string name = method.Name;

        if (name == "Round" && type is "System.Math" or "System.MathF" or "double" or "float"
            && parameters.Length >= 2 && IsBinary(parameters[0].Type) && parameters[1].Type.SpecialType == SpecialType.System_Int32) {
            Report(context, invocation.Syntax, type + ".Round with a digits argument", "LegacyNumeric.Round");
        } else if (name == "ToDecimal" && type is "System.Convert" or "decimal" && parameters.Length >= 1
            && (IsBinary(parameters[0].Type) || parameters[0].Type.SpecialType == SpecialType.System_Object)) {
            Report(context, invocation.Syntax, "Convert.ToDecimal(" + parameters[0].Type.ToDisplayString() + ")", "LegacyNumeric.ToDecimal");
        } else if (name is "ToDouble" or "ToSingle" && type is "System.Convert" or "decimal" && parameters.Length >= 1 && IsDecimal(parameters[0].Type)) {
            Report(context, invocation.Syntax, type + "." + name + " of a decimal", "LegacyNumeric." + name);
        } else if (name == "GetValue" && method.IsGenericMethod && method.TypeArguments.Any(argument => IsDecimal(Underlying(argument)))
            && (method.ContainingNamespace?.ToDisplayString() ?? string.Empty).StartsWith("OfficeOpenXml", System.StringComparison.Ordinal)) {
            Report(context, invocation.Syntax, "EPPlus GetValue<decimal>", "GetLegacyDecimal or LegacyNumeric.ToDecimal(cell.Value)");
        }
    }

    private static void AnalyzeObjectCreation(OperationAnalysisContext context) {
        var creation = (IObjectCreationOperation)context.Operation;
        if (IsDecimal(creation.Type) && creation.Arguments.Length == 1 && IsBinary(creation.Arguments[0].Parameter?.Type)) {
            Report(context, creation.Syntax, "new decimal(" + creation.Arguments[0].Parameter!.Type.ToDisplayString() + ")", "LegacyNumeric.ToDecimal");
        }
    }

    private static void Report(OperationAnalysisContext context, SyntaxNode syntax, string what, string replacement) {
        if (context.ContainingSymbol?.ContainingType?.Name == "LegacyNumeric" || IsInsideExpressionTree(context.Operation.SemanticModel, syntax)) return;
        context.ReportDiagnostic(Diagnostic.Create(Rule, syntax.GetLocation(), what, replacement));
    }

    private static bool IsInsideExpressionTree(SemanticModel? model, SyntaxNode syntax) {
        if (model is null) return false;
        for (SyntaxNode? node = syntax.Parent; node is not null; node = node.Parent) {
            if (node is AnonymousFunctionExpressionSyntax lambda
                && model.GetTypeInfo(lambda).ConvertedType is { Name: "Expression" } converted
                && converted.ContainingNamespace?.ToDisplayString() == "System.Linq.Expressions") {
                return true;
            }
        }
        return false;
    }

    private static bool IsBinary(ITypeSymbol? type) => type?.SpecialType is SpecialType.System_Double or SpecialType.System_Single;

    private static bool IsDecimal(ITypeSymbol? type) => type?.SpecialType == SpecialType.System_Decimal;

    private static ITypeSymbol Underlying(ITypeSymbol type) =>
        type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable ? nullable.TypeArguments[0] : type;
}
