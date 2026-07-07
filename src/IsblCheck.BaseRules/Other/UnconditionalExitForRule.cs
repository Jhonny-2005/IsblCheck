using System;
using System.Collections.Generic;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Other
{
  internal class UnconditionalExitForRule : AbstractRule
  {
    private const string Code = "F023";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(UnconditionalExitForRule).Name, "Правило обнаружения ExitFor без условия (бесконечный выход из цикла)."), true);

    public static IRuleInfo Info => info.Value;

    private class ExitForListener : IsblBaseListener
    {
      public List<IsblParser.ExitforStatementContext> UnconditionalExits { get; } = new List<IsblParser.ExitforStatementContext>();

      public override void EnterExitforStatement(IsblParser.ExitforStatementContext context)
      {
        if (context.Parent is IsblParser.StatementBlockContext block &&
            block.Parent is IsblParser.ForeachStatementContext foreachStmt)
        {
          if (foreachStmt.GetChild(3) == block)
          {
            UnconditionalExits.Add(context);
          }
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new ExitForListener();
      walker.Walk(listener, tree);

      foreach (var exit in listener.UnconditionalExits)
      {
        report.AddWarning(Code, "ExitFor без условия — бесконечный выход из цикла. Добавьте условие в If.", document, exit.Start.ToTextPosition());
      }
    }
  }
}
