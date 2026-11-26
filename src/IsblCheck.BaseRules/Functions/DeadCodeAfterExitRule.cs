using System;
using System.Collections.Generic;
using Antlr4.Runtime;
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
  public class DeadCodeAfterExitRule : AbstractRule
  {
    private const string Code = "F020";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(DeadCodeAfterExitRule).Name, "Недостижимый код после оператора выхода из цикла."), true);

    public static IRuleInfo Info => info.Value;

    private class DeadCodeListener : IsblBaseListener
    {
      public List<ParserRuleContext> DeadCodeBlocks { get; } = new List<ParserRuleContext>();

      public override void EnterStatementBlock([NotNull] IsblParser.StatementBlockContext context)
      {
          var children = context.children;
          if (children == null || children.Count < 2) return;

          for (int i = 0; i < children.Count - 1; i++)
          {
            if (children[i] is IsblParser.ExitforStatementContext exit && InvalidExitForRule.FindTarget(exit) != null)
            {
              DeadCodeBlocks.Add((ParserRuleContext)children[i + 1]);
              break;
            }
          }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      if (tree == null) return;

      var walker = new ParseTreeWalker();
      var listener = new DeadCodeListener();
      walker.Walk(listener, tree);

      foreach (var block in listener.DeadCodeBlocks)
      {
        var position = block.Start?.ToTextPosition() ?? new TextPosition { Line = 0, Column = 0, StartIndex = 0, EndIndex = 0 };
        report.Add(Severity.Warning, Code, "Следующий оператор в этом блоке недостижим после безусловного EXITFOR.",
          document, position, new DiagnosticRecommendation(
            "Если оператор должен выполниться для завершающей итерации, перенесите его перед EXITFOR. Если он нужен один раз после обхода, перенесите его после ENDFOREACH. Удаляйте его только после проверки назначения.",
            "Проверьте случай срабатывания EXITFOR и обычное завершение перебора. Сравните число вызовов и итоговые изменения."));
      }
    }
  }
}
