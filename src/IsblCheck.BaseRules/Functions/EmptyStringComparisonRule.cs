using System;
using System.Collections.Generic;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using IsblCheck.BaseRules.Properties;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  public class EmptyStringComparisonRule : AbstractRule
  {
    private const string Code = "F024";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(EmptyStringComparisonRule).Name, "Сравнение с пустой строкой вместо VarIsNull/VarIsEmpty."), true);

    public static IRuleInfo Info => info.Value;

    private class EmptyStringComparisonListener : IsblBaseListener
    {
      public List<IsblParser.ExpressionContext> EmptyComparisons { get; } = new List<IsblParser.ExpressionContext>();

      public override void EnterExpression([NotNull] IsblParser.ExpressionContext context)
      {
        if (context.ChildCount != 3)
          return;

        var left = context.GetChild(0) as IsblParser.ExpressionContext;
        var op = context.GetChild(1) as TerminalNodeImpl;
        var right = context.GetChild(2) as IsblParser.ExpressionContext;

        if (op == null || left == null || right == null)
          return;

        if (op.Symbol.Type != IsblParser.EQ && op.Symbol.Type != IsblParser.NEQ
          && op.Symbol.Type != IsblParser.STR_EQ && op.Symbol.Type != IsblParser.STR_NEQ)
          return;

        bool leftEmpty = IsEmptyString(left);
        bool rightEmpty = IsEmptyString(right);

        if (leftEmpty || rightEmpty)
          EmptyComparisons.Add(context);
      }

      private static bool IsEmptyString(IsblParser.ExpressionContext expr)
      {
        var str = expr.operand()?.@string();
        if (str == null)
          return false;
        var text = str.GetText().Trim('\'', '"');
        return string.IsNullOrEmpty(text);
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new EmptyStringComparisonListener();
      walker.Walk(listener, tree);

      foreach (var expr in listener.EmptyComparisons)
      {
        report.AddInformation(Code, "Используйте VarIsNull или VarIsEmpty вместо сравнения с пустой строкой.",
          document, expr.Start.ToTextPosition());
      }
    }
  }
}
