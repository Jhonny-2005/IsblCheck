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
  internal class EmptyCatchBlockRule : AbstractRule
  {
    private const string Code = "F015";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(EmptyCatchBlockRule).Name, "Правило поиска пустых блоков обработки исключений (Except). Ошибки не должны поглощаться молча."), true);

    public static IRuleInfo Info => info.Value;

    private class EmptyCatchListener : IsblBaseListener
    {
      public List<IsblParser.TryStatementContext> EmptyCatchBlocks { get; } = new List<IsblParser.TryStatementContext>();

      public override void EnterTryStatement(IsblParser.TryStatementContext context)
      {
        var children = new List<IParseTree>();
        for (int i = 0; i < context.ChildCount; i++)
          children.Add(context.GetChild(i));

        for (int i = 0; i < children.Count; i++)
        {
          if (children[i] is TerminalNodeImpl term && term.GetText().Equals("Except", StringComparison.OrdinalIgnoreCase))
          {
            if (i + 1 < children.Count && children[i + 1] is IsblParser.StatementBlockContext block && block.ChildCount == 0)
            {
              EmptyCatchBlocks.Add(context);
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
      var listener = new EmptyCatchListener();
      walker.Walk(listener, tree);

      foreach (var block in listener.EmptyCatchBlocks)
      {
        report.AddWarning(Code, "Пустой блок Except — ошибки поглощаются молча. Добавьте обработку ошибки или логирование.", document, block.Start.ToTextPosition());
      }
    }
  }
}
