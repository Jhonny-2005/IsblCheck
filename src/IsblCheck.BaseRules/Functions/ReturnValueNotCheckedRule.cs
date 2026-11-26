using System;
using System.Collections.Generic;
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
  public class ReturnValueNotCheckedRule : AbstractRule
  {
    private const string Code = "F025";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(ReturnValueNotCheckedRule).Name, "Возвращаемое значение функции не проверяется."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> CriticalFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "Save", "Сохранить", "Delete", "Удалить", "Open", "Открыть", "Close", "Закрыть",
      "Start", "Старт", "Abort", "Прекратить", "Perform", "Выполнить",
      "CopyFile", "КопироватьФайл", "MoveFile", "ПереместитьФайл",
      "DeleteFile", "УдалитьФайл", "WriteFile", "ФайлЗаписать"
    };

    private class ReturnValueListener : IsblBaseListener
    {
      public List<IsblParser.InvokeStatementContext> UncheckedCalls { get; } =
        new List<IsblParser.InvokeStatementContext>();

      public override void EnterInvokeStatement([NotNull] IsblParser.InvokeStatementContext context)
      {
        var function = context.function();
        if (function == null)
          return;

        var funcName = function.identifier().GetText();
        if (CriticalFunctions.Contains(funcName))
        {
          UncheckedCalls.Add(context);
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new ReturnValueListener();
      walker.Walk(listener, tree);

      foreach (var invoke in listener.UncheckedCalls)
      {
        var funcName = invoke.function()?.identifier()?.GetText() ?? "Неизвестно";
        report.AddWarning(Code, $"Возвращаемое значение функции {funcName} не используется. Проверьте результат выполнения.",
          document, invoke.Start.ToTextPosition());
      }
    }
  }
}
