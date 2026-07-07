using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Other
{
  internal class DeepNestingRule : AbstractRule
  {
    private const string Code = "F016";
    private const int MaxNestingDepth = 5;

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(DeepNestingRule).Name, "Правило обнаружения чрезмерно глубокой вложенности кода (более " + MaxNestingDepth + " уровней)."), true);

    public static IRuleInfo Info => info.Value;

    private class NestingListener : IsblBaseListener
    {
      private int currentDepth = 0;
      public List<IsblParser.StatementBlockContext> DeepBlocks { get; } = new List<IsblParser.StatementBlockContext>();

      public override void EnterIfStatement(IsblParser.IfStatementContext context) { currentDepth++; CheckDepth(context); }
      public override void ExitIfStatement(IsblParser.IfStatementContext context) { currentDepth--; }
      public override void EnterForeachStatement(IsblParser.ForeachStatementContext context) { currentDepth++; CheckDepth(context); }
      public override void ExitForeachStatement(IsblParser.ForeachStatementContext context) { currentDepth--; }
      public override void EnterWhileStatement(IsblParser.WhileStatementContext context) { currentDepth++; CheckDepth(context); }
      public override void ExitWhileStatement(IsblParser.WhileStatementContext context) { currentDepth--; }
      public override void EnterTryStatement(IsblParser.TryStatementContext context) { currentDepth++; }
      public override void ExitTryStatement(IsblParser.TryStatementContext context) { currentDepth--; }

      private void CheckDepth(ParserRuleContext context)
      {
        if (currentDepth > MaxNestingDepth)
        {
          for (int i = 0; i < context.ChildCount; i++)
          {
            if (context.GetChild(i) is IsblParser.StatementBlockContext block)
            {
              DeepBlocks.Add(block);
              return;
            }
          }
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new NestingListener();
      walker.Walk(listener, tree);

      foreach (var block in listener.DeepBlocks)
      {
        report.AddWarning(Code, $"Глубокая вложенность кода (более {MaxNestingDepth} уровней). Рекомендуется рефакторинг.", document, block.Start.ToTextPosition());
      }
    }
  }
}
