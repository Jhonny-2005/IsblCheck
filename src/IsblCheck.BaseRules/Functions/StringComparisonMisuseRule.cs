using System;
using System.Collections.Generic;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  /// <summary>
  /// Правило обнаружения некорректного использования оператора ==.
  /// </summary>
  internal class StringComparisonMisuseRule : AbstractRule
  {
    private const string Code = "F033";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(StringComparisonMisuseRule).Name,
        "Правило обнаружения использования оператора == вместо = для числовых сравнений."), true);

    public static IRuleInfo Info => info.Value;

    private class StringComparisonMisuseListener : IsblBaseListener
    {
      public List<IsblParser.ExpressionContext> MisusedExpressions { get; } =
        new List<IsblParser.ExpressionContext>();

      public override void EnterExpression(IsblParser.ExpressionContext context)
      {
        for (int i = 0; i < context.ChildCount; i++)
        {
          var child = context.GetChild(i) as TerminalNodeImpl;
          if (child != null && child.Symbol.Type == IsblParser.STR_EQ)
          {
            MisusedExpressions.Add(context);
            break;
          }
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new StringComparisonMisuseListener();
      walker.Walk(listener, tree);

      foreach (var expr in listener.MisusedExpressions)
      {
        report.AddWarning(Code,
          "Оператор == используется для сравнения. Для числовых выражений используйте =.",
          document, expr.Start.ToTextPosition());
      }
    }
  }
}
