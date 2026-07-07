using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  internal class MissingErrorHandlingInEventsRule : AbstractRule
  {
    private const string Code = "F024";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(MissingErrorHandlingInEventsRule).Name, "Правило обнаружения отсутствия обработки ошибок в критических событиях (BeforeUpdate/AfterUpdate/BeforeDelete/AfterDelete)."), true);

    public static IRuleInfo Info => info.Value;

    private class ErrorHandlingListener : IsblBaseListener
    {
      public bool HasTryBlock { get; private set; }
      public bool HasCriticalEvent { get; private set; }

      public override void EnterTryStatement(IsblParser.TryStatementContext context)
      {
        HasTryBlock = true;
      }

      public void Reset()
      {
        HasTryBlock = false;
        HasCriticalEvent = false;
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      if (!document.Path.StartsWith("Events.", StringComparison.OrdinalIgnoreCase))
        return;

      var eventName = document.Path.Substring("Events.".Length);
      var criticalEvents = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
      {
        "BeforeUpdate", "AfterUpdate", "BeforeDelete", "AfterDelete",
        "BeforeInsert", "AfterInsert"
      };

      if (!criticalEvents.Contains(eventName))
        return;

      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new ErrorHandlingListener();
      walker.Walk(listener, tree);

      if (!listener.HasTryBlock)
      {
        report.AddInformation(Code, $"В критическом событии '{eventName}' отсутствует обработка ошибок (Try/Except).", document, new TextPosition());
      }
    }
  }
}
