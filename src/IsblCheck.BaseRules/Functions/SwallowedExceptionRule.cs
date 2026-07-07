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
  internal class SwallowedExceptionRule : AbstractRule
  {
    private const string Code = "F020";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(SwallowedExceptionRule).Name, "Правило обнаружения поглощённых исключений (ExceptionExists без GetLastException)."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> ExceptionCheckFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "ExceptionExists", "КонстЕсть"
    };

    private static readonly HashSet<string> ExceptionReadFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "GetLastException", "ОтлИнфДобавить"
    };

    private class SwallowedExceptionListener : IsblBaseListener
    {
      public List<IsblParser.FunctionContext> SwallowedExceptions { get; } = new List<IsblParser.FunctionContext>();
      private bool lastWasExceptionRead = false;

      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var name = context.identifier().GetText();
        if (ExceptionCheckFunctions.Contains(name))
        {
          if (!lastWasExceptionRead)
          {
            SwallowedExceptions.Add(context);
          }
          lastWasExceptionRead = false;
        }
        else if (ExceptionReadFunctions.Contains(name))
        {
          lastWasExceptionRead = true;
        }
        else
        {
          lastWasExceptionRead = false;
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new SwallowedExceptionListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.SwallowedExceptions)
      {
        report.AddInformation(Code, "Проверка исключения без чтения его данных (ExceptionExists без GetLastException).", document, call.identifier().GetTextPosition());
      }
    }
  }
}
