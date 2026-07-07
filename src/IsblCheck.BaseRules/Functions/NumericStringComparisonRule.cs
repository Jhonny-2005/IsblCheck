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
  /// Правило обнаружения использования строковых операторов сравнения для числовых выражений.
  /// </summary>
  internal class NumericStringComparisonRule : AbstractRule
  {
    private const string Code = "F050";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(NumericStringComparisonRule).Name,
        "Правило обнаружения использования строковых операторов сравнения для числовых выражений."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<int> StringComparisonOperators = new HashSet<int>
    {
      IsblParser.STR_EQ, IsblParser.STR_NEQ,
      IsblParser.STR_GT, IsblParser.STR_GEQ,
      IsblParser.STR_LT, IsblParser.STR_LEQ
    };

    private class StringComparisonListener : IsblBaseListener
    {
      public List<IsblParser.ExpressionContext> MisusedExpressions { get; } = new List<IsblParser.ExpressionContext>();

      public override void EnterExpression(IsblParser.ExpressionContext context)
      {
        for (int i = 0; i < context.ChildCount; i++)
        {
          var child = context.GetChild(i) as TerminalNodeImpl;
          if (child != null && StringComparisonOperators.Contains(child.Symbol.Type))
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
      var listener = new StringComparisonListener();
      walker.Walk(listener, tree);

      foreach (var expr in listener.MisusedExpressions)
      {
        report.AddWarning(Code,
          "Использование строкового оператора сравнения. Для чисел используйте =, <>, >, <, >=, <=.",
          document, expr.Start.ToTextPosition());
      }
    }
  }
}
