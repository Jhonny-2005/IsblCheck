using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Security
{
  internal class UnsafeSQLConcatRule : AbstractRule
  {
    private const string Code = "S001";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(UnsafeSQLConcatRule).Name, "Правило поиска потенциальных SQL-инъекций через конкатенацию строк в запросах."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> SQLFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "SQL", "CSQL", "SelectSQL", "CSelectSQL", "SQL2XLSTAB"
    };

    private class SQLInjectionListener : IsblBaseListener
    {
      public List<IsblParser.FunctionContext> UnsafeCalls { get; } = new List<IsblParser.FunctionContext>();

      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var funcName = context.identifier().GetText();
        if (!SQLFunctions.Contains(funcName))
          return;

        var paramList = context.parameterList();
        if (paramList == null)
          return;

        foreach (var expr in paramList.expression())
        {
          if (HasConcatOperator(expr))
          {
            UnsafeCalls.Add(context);
            return;
          }
        }
      }

      private static bool HasConcatOperator(IsblParser.ExpressionContext expr)
      {
        if (expr == null) return false;
        for (int i = 0; i < expr.ChildCount; i++)
        {
          var child = expr.GetChild(i) as TerminalNodeImpl;
          if (child != null && child.Symbol.Type == IsblParser.CONCAT)
            return true;
        }
        return false;
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new SQLInjectionListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.UnsafeCalls)
      {
        var description = "Возможная SQL-инъекция: конкатенация строкового выражения в параметре запроса. Рекомендуется использовать параметризованные запросы.";
        report.AddWarning(Code, description, document, call.identifier().GetTextPosition());
      }
    }
  }
}
