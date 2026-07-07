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
  /// Правило обнаружения Try/Finally без Except (неполная обработка ошибок).
  /// </summary>
  internal class TryFinallyWithoutExceptRule : AbstractRule
  {
    private const string Code = "F045";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(TryFinallyWithoutExceptRule).Name,
        "Правило обнаружения Try/Finally без Except (неполная обработка ошибок)."), true);

    public static IRuleInfo Info => info.Value;

    private class TryFinallyListener : IsblBaseListener
    {
      public List<IsblParser.TryStatementContext> TryFinallyBlocks { get; } = new List<IsblParser.TryStatementContext>();

      public override void EnterTryStatement(IsblParser.TryStatementContext context)
      {
        bool hasFinally = false;
        bool hasExcept = false;

        for (int i = 0; i < context.ChildCount; i++)
        {
          var child = context.GetChild(i) as TerminalNodeImpl;
          if (child != null)
          {
            if (child.Symbol.Type == IsblParser.FINALLY)
              hasFinally = true;
            if (child.Symbol.Type == IsblParser.EXCEPT)
              hasExcept = true;
          }
        }

        if (hasFinally && !hasExcept)
        {
          TryFinallyBlocks.Add(context);
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new TryFinallyListener();
      walker.Walk(listener, tree);

      foreach (var tryStmt in listener.TryFinallyBlocks)
      {
        report.AddInformation(Code,
          "Try/Finally без Except: исключения не перехватываются, только выполняется Finally-блок.",
          document, tryStmt.Start.ToTextPosition());
      }
    }
  }
}
