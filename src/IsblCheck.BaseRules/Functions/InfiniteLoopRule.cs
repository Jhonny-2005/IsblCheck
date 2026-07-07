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
  /// Правило обнаружения бесконечных циклов (While True без ExitFor/Exit).
  /// </summary>
  internal class InfiniteLoopRule : AbstractRule
  {
    private const string Code = "F029";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(InfiniteLoopRule).Name, "Правило обнаружения бесконечных циклов (While True без ExitFor/Exit)."), true);

    public static IRuleInfo Info => info.Value;

    private class InfiniteLoopListener : IsblBaseListener
    {
      public List<IsblParser.WhileStatementContext> InfiniteLoops { get; } = new List<IsblParser.WhileStatementContext>();

      public override void EnterWhileStatement(IsblParser.WhileStatementContext context)
      {
        if (IsInfiniteLoop(context) && !HasExitStatement(context))
        {
          InfiniteLoops.Add(context);
        }
      }

      private bool IsInfiniteLoop(IsblParser.WhileStatementContext context)
      {
        var condition = context.expression();
        if (condition == null) return false;

        var text = condition.GetText().Trim().ToUpperInvariant();
        return text == "TRUE" || text == "1" || text == "-1";
      }

      private bool HasExitStatement(IsblParser.WhileStatementContext context)
      {
        var block = context.statementBlock();
        if (block == null) return false;

        var text = block.GetText().ToUpperInvariant();
        return text.Contains("EXITFOR") || text.Contains("EXIT(") || text.Contains("ВЫХОД(");
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new InfiniteLoopListener();
      walker.Walk(listener, tree);

      foreach (var loop in listener.InfiniteLoops)
      {
        report.AddWarning(Code, "Возможный бесконечный цикл: While True без ExitFor/Exit.", document, loop.Start.ToTextPosition());
      }
    }
  }
}
