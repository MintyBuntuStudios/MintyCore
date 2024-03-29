using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;
using SharedCode;

namespace MintyCore.Generator.LogEnricher;

[Generator]
public class LogEnricherGenerator : IIncrementalGenerator
{
    private const string TemplateDirectory = "MintyCore.Generator.LogEnricher";

    private static Template LogInterceptorTemplate =>
        Template.Parse(EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TemplateDirectory}.LogInterceptors.sbncs"));

    private static readonly string[] _loggerTypes =
    {
        "Serilog.Log",
        "Serilog.ILogger"
    };

    private static readonly string[] _logMethods =
    {
        "Write",
        "Verbose",
        "Debug",
        "Information",
        "Warning",
        "Error",
        "Fatal"
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //collect all log invocations
        var logInvocationProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                (node, _) => node is InvocationExpressionSyntax,
                (INamedTypeSymbol?, IMethodSymbol, InvocationExpressionSyntax)? (ctx, _) =>
                {
                    if (ctx.Node is not InvocationExpressionSyntax invocationExpressionSyntax)
                        return null;
                    var containingClassSyntax =
                        invocationExpressionSyntax.FirstAncestorOrSelf<ClassDeclarationSyntax>();
                    var containingClassSymbol = containingClassSyntax is not null
                        ? ctx.SemanticModel.GetDeclaredSymbol(containingClassSyntax) as INamedTypeSymbol
                        : null;

                    if (ctx.SemanticModel.GetSymbolInfo(invocationExpressionSyntax).Symbol is not IMethodSymbol
                        methodSymbol)
                        return null;

                    methodSymbol = methodSymbol.ConstructedFrom;

                    return IsValidLogMethod(methodSymbol)
                        ? (containingClassSymbol, methodSymbol, invocationExpressionSyntax)
                        : null;
                }
            ).Where(x => x is not null)
            .Select((x, _) => x!.Value);

        var byClass = logInvocationProvider.Collect()
            .SelectMany((methodInvocations, _) =>
                methodInvocations.GroupBy(tuple => tuple.Item1, SymbolEqualityComparer.Default));

        context.RegisterSourceOutput(byClass, GenerateLogInterceptor);
    }

    private void GenerateLogInterceptor(SourceProductionContext ctx,
        IGrouping<ISymbol?, (INamedTypeSymbol?, IMethodSymbol, InvocationExpressionSyntax)> grouping)
    {
        if (grouping.Key is not INamedTypeSymbol classSymbol)
            return;

        var @namespace = classSymbol.ContainingNamespace;

        //find the root namespace
        while (@namespace.ContainingNamespace is not null && !@namespace.ContainingNamespace.IsGlobalNamespace)
            @namespace = @namespace.ContainingNamespace;

        var description = new LogMethodsDescription
        {
            Class = classSymbol.Name,
            RootNamespace = @namespace.ToDisplayString(),
            LogMethods = new List<LogMethodEntry>()
        };

        var logs = grouping.ToArray();


        foreach (var (_, logMethod, logInvocation) in logs)
        {
            var logExpression = logInvocation.Expression;
            if (logExpression is not MemberAccessExpressionSyntax memberAccessExpression)
                continue;

            var methodLocation = memberAccessExpression.Name.GetLocation().GetLineSpan();

            var methodEntry = new LogMethodEntry
            {
                MethodName = logMethod.Name,
                StaticLogger = logMethod.IsStatic,
                FileLocation = methodLocation.Path,
                LineNumber = (methodLocation.StartLinePosition.Line + 1).ToString(),
                CharacterNumber =
                    (methodLocation.StartLinePosition.Character + 1).ToString(),
                GenericParameters = logMethod.TypeParameters.Select(x => x.ToDisplayString()).ToList(),
            };

            foreach (var parameterSymbol in logMethod.Parameters)
            {
                var parameterStrings = parameterSymbol.ToDisplayString(new SymbolDisplayFormat(
                        SymbolDisplayGlobalNamespaceStyle.Included,
                        SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                        parameterOptions: SymbolDisplayParameterOptions.IncludeType,
                        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier))
                    .Split(' ');


                methodEntry.Parameters.Add(new LogMethodParameters()
                {
                    Type = parameterStrings[0],
                    Name = parameterStrings[1]
                });
            }

            description.LogMethods.Add(methodEntry);
        }

        var result = LogInterceptorTemplate.Render(description, member => member.Name);
        ctx.AddSource($"{classSymbol.Name}.LogInterceptor.cs", result);
    }

    private bool IsValidLogMethod(IMethodSymbol methodSymbol)
    {
        if (!Array.Exists(_logMethods, x => x == methodSymbol.Name))
            return false;

        var methodTypeName = methodSymbol.OriginalDefinition.ContainingType?.ToDisplayString();
        return Array.Exists(_loggerTypes, x => x == methodTypeName);
    }

    class LogMethodsDescription
    {
        [UsedImplicitly] public string? RootNamespace { get; set; }
        [UsedImplicitly] public string? Class { get; set; }
        [UsedImplicitly] public List<LogMethodEntry>? LogMethods { get; set; }
    }

    class LogMethodEntry
    {
        [UsedImplicitly] public List<string> GenericParameters { get; set; } = new();
        [UsedImplicitly] public bool StaticLogger { get; set; }
        [UsedImplicitly] public List<LogMethodParameters> Parameters { get; set; } = new();
        [UsedImplicitly] public string? MethodName { get; set; }

        [UsedImplicitly] public string? FileLocation { get; set; }
        [UsedImplicitly] public string? LineNumber { get; set; }
        [UsedImplicitly] public string? CharacterNumber { get; set; }
    }

    class LogMethodParameters
    {
        [UsedImplicitly] public string? Type { get; set; }
        [UsedImplicitly] public string? Name { get; set; }
    }
}