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
  /// Правило обнаружения недостижимого кода после ExitFor.
  /// </summary>
  internal class UnreachableCodeRule : AbstractRule
  {
    private const string Code = "F028";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(UnreachableCodeRule).Name, "Правило обнаружения недостижимого кода после ExitFor."), true);

    public static IRuleInfo Info => info.Value;

    private class UnreachableCodeListener : IsblBaseListener
    {
      public List<IsblParser.ExitforStatementContext> UnreachableExits { get; } = new List<IsblParser.ExitforStatementContext>();

      public override void EnterExitforStatement(IsblParser.ExitforStatementContext context)
      {
        var parent = context.Parent as IsblParser.StatementBlockContext;
        if (parent == null) return;

        var exitIndex = -1;
        for (int i = 0; i < parent.ChildCount; i++)
        {
          if (parent.GetChild(i) == context)
          {
            exitIndex = i;
            break;
          }
        }

        if (exitIndex >= 0 && exitIndex < parent.ChildCount - 1)
        {
          UnreachableExits.Add(context);
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new UnreachableCodeListener();
      walker.Walk(listener, tree);

      foreach (var exit in listener.UnreachableExits)
      {
        report.AddWarning(Code, "Недостижимый код после ExitFor.", document, exit.Start.ToTextPosition());
      }
    }
  }
}
