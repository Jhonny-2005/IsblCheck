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
  /// Правило обнаружения Exit() в критических событиях.
  /// </summary>
  internal class ExitInCriticalEventsRule : AbstractRule
  {
    private const string Code = "F043";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(ExitInCriticalEventsRule).Name,
        "Правило обнаружения Exit() в критических обработчиках событий."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> ExitMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "Exit", "ВЫХОД"
    };

    private static readonly HashSet<string> CriticalEvents = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "Events.BeforeUpdate", "Events.AfterUpdate",
      "Events.BeforeDelete", "Events.AfterDelete",
      "Events.BeforeInsert", "Events.AfterInsert"
    };

    private class ExitListener : IsblBaseListener
    {
      public List<IsblParser.FunctionContext> ExitCalls { get; } = new List<IsblParser.FunctionContext>();

      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var name = context.identifier().GetText();
        if (ExitMethods.Contains(name))
        {
          ExitCalls.Add(context);
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      if (!IsCriticalEvent(document.Path))
        return;

      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new ExitListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.ExitCalls)
      {
        report.AddWarning(Code,
          "Exit() в критическом обработчике события может прервать важные операции.",
          document, call.identifier().GetTextPosition());
      }
    }

    private bool IsCriticalEvent(string path)
    {
      if (string.IsNullOrEmpty(path))
        return false;

      foreach (var evt in CriticalEvents)
      {
        if (path.StartsWith(evt, StringComparison.OrdinalIgnoreCase))
          return true;
      }
      return false;
    }
  }
}
