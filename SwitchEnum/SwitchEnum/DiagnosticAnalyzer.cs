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
		internal const string Title = "Switch on enum is not exhaustive";
		internal const string Title2 = "Switch default unreachable";
		internal const string MessageFormat = "Missing cases:\n{0}";
		internal const string Category = "Logic";

		internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true);
		internal static DiagnosticDescriptor Rule2 = new DiagnosticDescriptor(DiagnosticId, Title2, Title2, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, Rule2);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.SwitchStatement);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			var switchSyntax = context.Node as SwitchStatementSyntax;
      if (switchSyntax == null) return;
			var result = VisitSwitchStatement(context.SemanticModel, switchSyntax, context.CancellationToken);
		  if (result == null) return;
		  var hasDefault = result.Item2;
		  var defaultIsThrow = result.Item3;
      if (result.Item1.Count > 0) {
		    if (!hasDefault || defaultIsThrow) {
		      var diagnostic = Diagnostic.Create(Rule, switchSyntax.Expression.GetLocation(), string.Join("\n", result.Item1));
          context.ReportDiagnostic(diagnostic);
        }
      }
		  else {
        if (hasDefault && !defaultIsThrow) {
          var diagnostic = Diagnostic.Create(Rule2, switchSyntax.Expression.GetLocation());
          context.ReportDiagnostic(diagnostic);
        }
      }
		}

		Tuple<List<string>,bool,bool> VisitSwitchStatement(SemanticModel model, SwitchStatementSyntax node, CancellationToken ct)
		{
			try {
				var type = model.GetTypeInfo(node.Expression, ct).Type;
				if (type.TypeKind == TypeKind.Enum) {
					                                        // Exclude ctor
					var members = type.GetMembers().Where(m => m.Kind == SymbolKind.Field);
					var defaults = node.Sections.SelectMany(s => s.Labels).OfType<DefaultSwitchLabelSyntax>().ToArray();
				  var hasDefault = defaults.Any();
				  var defaultIsThrow = false;
					if (hasDefault) {
					  var d = defaults.First();
					  var first = ((SwitchSectionSyntax) d.Parent).Statements.FirstOrDefault();
					  defaultIsThrow = first is ThrowStatementSyntax;
					}
          var symbols = node.Sections.SelectMany(
              s => s.Labels.OfType<CaseSwitchLabelSyntax>().Select(l => model.GetSymbolInfo(l.Value, ct).Symbol)).ToArray();
          var notFound = members.Where(m => !symbols.Any(s => Equals(s, m))).Select(m => m.Name).ToList();
          return Tuple.Create(notFound, hasDefault, defaultIsThrow);
        }
			}
			catch (Exception) {
				// ignored
			}
			return null;
		}
	}
}
