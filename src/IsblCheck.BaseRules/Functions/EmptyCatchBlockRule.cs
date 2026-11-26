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
  public class EmptyCatchBlockRule : AbstractRule
  {
    private const string Code = "F011";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(EmptyCatchBlockRule).Name, "Обнаружен пустой блок обработки исключений."), true);

    public static IRuleInfo Info => info.Value;

    private class EmptyCatchListener : IsblBaseListener
    {
      public List<IToken> EmptyCatchBlocks { get; } = new List<IToken>();

      public override void EnterTryStatement([NotNull] IsblParser.TryStatementContext context)
      {
          var children = context.children;
          if (children == null || children.Count == 0) return;

          for (int i = 0; i < children.Count; i++)
          {
            if (children[i] is TerminalNodeImpl terminal && terminal.Symbol.Type == IsblParser.EXCEPT)
            {
              if (i + 1 >= children.Count) break;
              var catchBlock = children[i + 1] as IsblParser.StatementBlockContext;
              if (catchBlock != null && catchBlock.ChildCount == 0)
              {
                EmptyCatchBlocks.Add(terminal.Symbol);
              }
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
      var listener = new EmptyCatchListener();
      walker.Walk(listener, tree);

      foreach (var tryStmt in listener.EmptyCatchBlocks)
      {
        var position = tryStmt?.ToTextPosition() ?? new TextPosition { Line = 0, Column = 0, StartIndex = 0, EndIndex = 0 };
        report.Add(Severity.Warning, Code, "Пустой EXCEPT подавляет исключение из TRY. Вычисление может продолжиться с частично изменённым состоянием.",
          document, position, new DiagnosticRecommendation(
            "Обработайте только ожидаемое исключение и задайте явный результат этого пути. Для неожиданной ошибки сохраните сведения об исходном исключении и передайте его выше. Если блок предназначен лишь для освобождения ресурсов, используйте FINALLY для очистки.",
            "Проверьте успешный сценарий, ожидаемую ошибку и неожиданный сбой. Убедитесь, что частично изменённые данные не воспринимаются как успешный результат."));
      }
    }
  }
}
