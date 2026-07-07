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
  /// Правило обнаружения ShowMessage/MessageBox в отчетах.
  /// </summary>
  internal class ShowMessageInReportRule : AbstractRule
  {
    private const string Code = "F049";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(ShowMessageInReportRule).Name,
        "Правило обнаружения ShowMessage/MessageBox в вычислениях отчетов."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> MessageFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "ShowMessage", "MessageBox", "MessageBoxEx",
      "ПоказатьСообщение", "Окно"
    };

    private class MessageListener : IsblBaseListener
    {
      public List<IsblParser.FunctionContext> MessageCalls { get; } = new List<IsblParser.FunctionContext>();

      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var name = context.identifier().GetText();
        if (MessageFunctions.Contains(name))
        {
          MessageCalls.Add(context);
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      if (document.ComponentType != ComponentType.CommonReport &&
          document.ComponentType != ComponentType.IntegratedReport)
        return;

      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new MessageListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.MessageCalls)
      {
        report.AddWarning(Code,
          "ShowMessage/MessageBox в вычислениях отчета. В веб-клиенте окна не отображаются. Используйте CreateException.",
          document, call.identifier().GetTextPosition());
      }
    }
  }
}
