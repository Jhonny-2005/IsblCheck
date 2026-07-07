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
  /// Правило обнаружения Sleep/Пауза в серверных событиях.
  /// </summary>
  internal class SleepInServerEventsRule : AbstractRule
  {
    private const string Code = "F044";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(SleepInServerEventsRule).Name,
        "Правило обнаружения Sleep/Пауза в серверных событиях."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> SleepMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "Sleep", "Пауза"
    };

    private class SleepListener : IsblBaseListener
    {
      public List<IsblParser.FunctionContext> SleepCalls { get; } = new List<IsblParser.FunctionContext>();

      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var name = context.identifier().GetText();
        if (SleepMethods.Contains(name))
        {
          SleepCalls.Add(context);
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      if (!document.Path.StartsWith("Events.", StringComparison.OrdinalIgnoreCase))
        return;

      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new SleepListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.SleepCalls)
      {
        report.AddWarning(Code,
          "Sleep/Пауза в серверном событии блокирует поток и снижает производительность.",
          document, call.identifier().GetTextPosition());
      }
    }
  }
}
