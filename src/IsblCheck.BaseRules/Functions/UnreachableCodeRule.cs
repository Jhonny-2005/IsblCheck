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
  /// Правило обнаружения недостижимого кода после ExitFor/Exit.
  /// </summary>
  internal class UnreachableCodeRule : AbstractRule
  {
    private const string Code = "F028";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(UnreachableCodeRule).Name, "Правило обнаружения недостижимого кода после ExitFor/Exit."), true);

    public static IRuleInfo Info => info.Value;

    private class UnreachableCodeListener : IsblBaseListener
    {
      public List<IsblParser.StatementContext> UnreachableStatements { get; } = new List<IsblParser.StatementContext>();

      public override void EnterExitforStatement(IsblParser.ExitforStatementContext context)
      {
        CheckForUnreachableCode(context);
      }

      public override void EnterExitStatement(IsblParser.ExitStatementContext context)
      {
        CheckForUnreachableCode(context);
      }

      private void CheckForUnreachableCode(ParserRuleContext exitContext)
      {
        var parent = exitContext.Parent;
        if (parent == null) return;

        if (parent is IsblParser.StatementBlockContext block)
        {
          var exitIndex = -1;
          for (int i = 0; i < block.ChildCount; i++)
          {
            if (block.GetChild(i) == exitContext)
            {
              exitIndex = i;
              break;
            }
          }

          if (exitIndex >= 0 && exitIndex < block.ChildCount - 1)
          {
            for (int i = exitIndex + 1; i < block.ChildCount; i++)
            {
              var child = block.GetChild(i);
              if (child is IsblParser.StatementContext stmt && !(child is IsblParser.EmptyStatementContext))
              {
                UnreachableStatements.Add(stmt);
                break;
              }
            }
          }
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new UnreachableCodeListener();
      walker.Walk(listener, tree);

      foreach (var stmt in listener.UnreachableStatements)
      {
        report.AddWarning(Code, "Недостижимый код после ExitFor/Exit.", document, stmt.Start.ToTextPosition());
      }
    }
  }
}
