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
  /// Правило обнаружения несбалансированных ExceptionsOff/ExceptionsOn.
  /// </summary>
  internal class ExceptionsOffBalanceRule : AbstractRule
  {
    private const string Code = "F031";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(ExceptionsOffBalanceRule).Name, "Правило обнаружения несбалансированных ExceptionsOff/ExceptionsOn."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> ExceptionsOffMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "ExceptionsOff", "ОтключитьИсключения"
    };

    private static readonly HashSet<string> ExceptionsOnMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "ExceptionsOn", "ВключитьИсключения"
    };

    private class ExceptionsBalanceListener : IsblBaseListener
    {
      public List<IsblParser.FunctionContext> UnbalancedOffCalls { get; } = new List<IsblParser.FunctionContext>();
      public int FinalBalance => balance;
      private int balance = 0;

      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var name = context.identifier().GetText();
        if (ExceptionsOffMethods.Contains(name))
        {
          balance++;
          if (balance > 1)
          {
            UnbalancedOffCalls.Add(context);
          }
        }
        else if (ExceptionsOnMethods.Contains(name))
        {
          if (balance > 0)
            balance--;
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new ExceptionsBalanceListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.UnbalancedOffCalls)
      {
        report.AddError(Code, "Несбалансированный вызов ExceptionsOff: исключения подавлены без восстановления.", document, call.identifier().GetTextPosition());
      }

      if (listener.FinalBalance > 0)
      {
        report.AddError(Code, "ExceptionsOff вызван без соответствующего ExceptionsOn: исключения подавлены до конца области.", document, new TextPosition { Line = 1, Column = 0, StartIndex = 0, EndIndex = 0 });
      }
    }
  }
}
