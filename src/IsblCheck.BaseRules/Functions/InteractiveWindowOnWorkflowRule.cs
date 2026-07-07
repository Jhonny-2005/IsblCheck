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
  /// Правило обнаружения использования интерактивных окон в серверных/workflow событиях.
  /// </summary>
  internal class InteractiveWindowOnWorkflowRule : AbstractRule
  {
    private const string Code = "F038";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(InteractiveWindowOnWorkflowRule).Name,
        "Правило обнаружения использования интерактивных окон в серверных и workflow событиях."), true);

    public static IRuleInfo Info => info.Value;

    private const string DocumentPathPrefix = "Events.";

    private static readonly HashSet<string> FunctionsWithInteractiveWindow = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "CreateDialog", "CreateEditor", "CreateInputDialog", "CreateOpenDialog",
      "CreateSaveDialog", "EditText", "InputDialog", "InputDialogEx",
      "MessageBox", "MessageBoxEx", "SelectSQL", "SelectFromDualList",
      "ShowDialog", "ShowMessage",
      "Ввод", "ВводМеню", "ВыборSQL", "ДиалогДаНет", "Меню", "МенюРасш",
      "Окно", "ПВыборSQL", "РедТекст", "РедактироватьРТФ"
    };

    private class WorkflowListener : IsblBaseListener
    {
      public List<IsblParser.FunctionContext> InteractiveCalls { get; } =
        new List<IsblParser.FunctionContext>();

      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var name = context.identifier().GetText();
        if (FunctionsWithInteractiveWindow.Contains(name))
        {
          InteractiveCalls.Add(context);
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      if (!document.Path.StartsWith(DocumentPathPrefix, StringComparison.OrdinalIgnoreCase))
        return;

      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new WorkflowListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.InteractiveCalls)
      {
        report.AddWarning(Code,
          "Использование интерактивного окна в серверном/workflow событии. Интерактивные вызовы могут блокировать обработку.",
          document, call.GetTextPosition());
      }
    }
  }
}
