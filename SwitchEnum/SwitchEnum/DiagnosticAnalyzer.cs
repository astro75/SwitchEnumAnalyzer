using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SwitchEnum
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SwitchEnumAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SwitchEnumAnalyzer";

        const string DefaultUnreachableRuleTitle = "Switch default unreachable";

        static readonly DiagnosticDescriptor NotExhaustiveSwitchRule =
            new DiagnosticDescriptor(
                DiagnosticId,
                "Switch on enum is not exhaustive",
                "Missing cases:\n{0}",
                "Logic",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true
            );

        static readonly DiagnosticDescriptor DefaultUnreachableRule =
            new DiagnosticDescriptor(
                DiagnosticId,
                DefaultUnreachableRuleTitle,
                DefaultUnreachableRuleTitle,
                "Logic",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true
            );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(NotExhaustiveSwitchRule, DefaultUnreachableRule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.SwitchStatement);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is SwitchStatementSyntax switchSyntax)) return;

            var information = GetSwitchInformation(context.SemanticModel, switchSyntax, context.CancellationToken);
            if (information == null) return;

            if (information.NotExhaustiveSwitch)
            {
                var diagnostic = Diagnostic.Create(NotExhaustiveSwitchRule, switchSyntax.Expression.GetLocation(), string.Join("\n", information.NotFoundSymbolNames));
                context.ReportDiagnostic(diagnostic);
            }
            else if (information.UnreachableDefault)
            {
                var diagnostic = Diagnostic.Create(DefaultUnreachableRule, switchSyntax.Expression.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        static SwitchInformation GetSwitchInformation(SemanticModel model, SwitchStatementSyntax node, CancellationToken ct)
        {
            var type = model.GetTypeInfo(node.Expression, ct).Type;
            if (type == null || type.TypeKind != TypeKind.Enum) return null;

            var @defaultSection =
                node.Sections.FirstOrDefault(s =>
                    s.Labels.Any(l => l is DefaultSwitchLabelSyntax)
                );
            return
                new SwitchInformation(
                    GetUnusedSymbolNames(model, node, type, ct),
                    hasDefault:
                        @defaultSection != null,
                    defaultThrows:
                        defaultSection != null
                        && @defaultSection.Statements.Any(s => s is ThrowStatementSyntax)
                );
        }

        private static ImmutableArray<string> GetUnusedSymbolNames(SemanticModel model, SwitchStatementSyntax node, ITypeSymbol type, CancellationToken ct)
        {
            var symbolsUsed = node
                .Sections
                .SelectMany(s => s.Labels)
                .OfType<CaseSwitchLabelSyntax>()
                .Select(l => model.GetSymbolInfo(l.Value, ct).Symbol)
                .ToImmutableHashSet();
            return
                type.GetMembers()
                .Where(m => m.Kind == SymbolKind.Field && !symbolsUsed.Contains(m))
                .Select(m => m.Name)
                .ToImmutableArray();
        }
    }
}
