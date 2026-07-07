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
  /// Правило рекомендации использовать именованные исключения для повторных попыток.
  /// </summary>
  internal class NamedExceptionForRetryRule : AbstractRule
  {
    private const string Code = "F052";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(NamedExceptionForRetryRule).Name,
        "Правило рекомендации использовать именованные исключения для повторных попыток."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> RetryableExceptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "ESBEdmsComponentLockedError",
      "ESBRepeatProcessCurrentObject",
      "ESBNotEnoughtWorkTimeCalendarData",
      "ESBInvalidUnlockParam"
    };

    private class NamedExceptionListener : IsblBaseListener
    {
      public List<IsblParser.FunctionContext> GenericExceptions { get; } = new List<IsblParser.FunctionContext>();

      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var name = context.identifier().GetText();
        if (string.Equals(name, "CreateException", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "СоздатьИсключение", StringComparison.OrdinalIgnoreCase))
        {
          if (context.parameterList()?.expression()?.Length > 0)
          {
            var firstParam = context.parameterList().expression()[0];
            var paramName = firstParam.GetText().Trim('"', '\'');
            if (!RetryableExceptions.Contains(paramName))
            {
              GenericExceptions.Add(context);
            }
          }
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      if (!document.Path.StartsWith("Events.", StringComparison.OrdinalIgnoreCase))
        return;

      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new NamedExceptionListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.GenericExceptions)
      {
        report.AddInformation(Code,
          "Для повторных ошибок рекомендуется использовать именованные исключения (ESBEdmsComponentLockedError и др.).",
          document, call.identifier().GetTextPosition());
      }
    }
  }
}
