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

		public static DiagnosticSeverity DefaultUnreachableSeverity = DiagnosticSeverity.Warning;
		public static DiagnosticSeverity NotExhaustiveSwitchSeverity = DiagnosticSeverity.Error;

		internal static DiagnosticDescriptor NotExhaustiveSwitchRule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, NotExhaustiveSwitchSeverity, isEnabledByDefault: true);
		internal static DiagnosticDescriptor DefaultUnreachableRule = new DiagnosticDescriptor(DiagnosticId, Title2, Title2, Category, DefaultUnreachableSeverity, isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(NotExhaustiveSwitchRule, DefaultUnreachableRule);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.SwitchStatement);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			if (context.Node is SwitchStatementSyntax switchSyntax)
			{
				SwitchInformation information = GetInformationAboutSwitch(context.SemanticModel, switchSyntax, context.CancellationToken);

				if (information is SwitchInformation)
				{
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
			}
		}
		

		SwitchInformation GetInformationAboutSwitch(SemanticModel model, SwitchStatementSyntax node, CancellationToken ct)
		{
			try
			{
				SwitchInformation info = new SwitchInformation();

				var type = model.GetTypeInfo(node.Expression, ct).Type;
				if (isEnum(type))
				{
					var defaults = node.Sections.SelectMany(s => s.Labels).OfType<DefaultSwitchLabelSyntax>().ToArray();
					info.HasDefault = defaults.Any();
					if (info.HasDefault)
					{
						var d = defaults.First();
						var first = ((SwitchSectionSyntax)d.Parent).Statements.FirstOrDefault();
						info.DefaultIsThrow = first is ThrowStatementSyntax;
					}

					info.NotFoundSymbolNames = GetUnusedSymbolNames(model, node, type, ct);

					return info;
				}
			}
			catch (Exception)
			{
				// ignored
			}
			return null;
		}

		private static List<string> GetUnusedSymbolNames(SemanticModel model, SwitchStatementSyntax node, ITypeSymbol type, CancellationToken ct)
		{
			var enumSymbols = type.GetMembers()
				.Where(m => m.Kind == SymbolKind.Field);

			var symbolsUsed = node
				.Sections
				.SelectMany(s => s.Labels.OfType<CaseSwitchLabelSyntax>().Select(l => model.GetSymbolInfo(l.Value, ct).Symbol))
				.ToArray();

			var a = enumSymbols
				.Where(m => !symbolsUsed.Any(s => Equals(s, m)))
				.Select(m => m.Name)
				.ToList();
			return a;
		}

		private static bool isEnum(ITypeSymbol type)
		{
			return type.TypeKind == TypeKind.Enum;
		}
	}
}
