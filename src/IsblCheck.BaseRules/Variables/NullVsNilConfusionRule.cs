using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Variables
{
  /// <summary>
  /// Правило обнаружения путаницы nil и NULL в сравнениях.
  /// </summary>
  internal class NullVsNilConfusionRule : AbstractRule
  {
    private const string Code = "F035";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(NullVsNilConfusionRule).Name,
        "Правило обнаружения использования nil/NULL в сравнениях с неподходящими типами."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly Regex NilComparisonPattern = new Regex(
      @"(\w+)\s*=\s*(nil|NULL)",
      RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private class NullVsNilListener : IsblBaseListener
    {
      public List<IsblParser.ExpressionContext> SuspiciousExpressions { get; } =
        new List<IsblParser.ExpressionContext>();

      public override void EnterExpression(IsblParser.ExpressionContext context)
      {
        var text = context.GetText();
        if (NilComparisonPattern.IsMatch(text))
        {
          SuspiciousExpressions.Add(context);
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new NullVsNilListener();
      walker.Walk(listener, tree);

      foreach (var expr in listener.SuspiciousExpressions)
      {
        report.AddWarning(Code,
          "Сравнение с nil/NULL. Убедитесь, что сравниваемый объект является ссылочным типом.",
          document, expr.Start.ToTextPosition());
      }
    }
  }
}
