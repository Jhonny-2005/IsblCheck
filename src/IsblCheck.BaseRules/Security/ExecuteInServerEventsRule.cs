using System;
using System.Collections.Generic;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Security
{
  /// <summary>
  /// Правило обнаружения Execute/Выполнить в серверных событиях.
  /// </summary>
  internal class ExecuteInServerEventsRule : AbstractRule
  {
    private const string Code = "S006";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(ExecuteInServerEventsRule).Name,
        "Правило обнаружения Execute/Выполнить в серверных событиях."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> ExecuteMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "Execute", "Выполнить"
    };

    private class ExecuteListener : IsblBaseListener
    {
      public List<IsblParser.FunctionContext> ExecuteCalls { get; } = new List<IsblParser.FunctionContext>();

      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var name = context.identifier().GetText();
        if (ExecuteMethods.Contains(name))
        {
          ExecuteCalls.Add(context);
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      if (!document.Path.StartsWith("Events.", StringComparison.OrdinalIgnoreCase))
        return;

      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new ExecuteListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.ExecuteCalls)
      {
        report.AddError(Code,
          "Динамическое выполнение кода (Execute) в серверном событии — угроза безопасности.",
          document, call.identifier().GetTextPosition());
      }
    }
  }
}
