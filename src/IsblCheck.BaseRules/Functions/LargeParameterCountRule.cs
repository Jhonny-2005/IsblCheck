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
  internal class LargeParameterCountRule : AbstractRule
  {
    private const string Code = "F019";
    private const int MaxParameterCount = 10;

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(LargeParameterCountRule).Name, "Правило обнаружения вызовов функций с чрезмерным количеством параметров (>" + MaxParameterCount + ")."), true);

    public static IRuleInfo Info => info.Value;

    private class LargeParamListener : IsblBaseListener
    {
      public List<IsblParser.FunctionContext> LargeParamCalls { get; } = new List<IsblParser.FunctionContext>();

      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var paramList = context.parameterList();
        if (paramList != null && paramList.expression().Length > MaxParameterCount)
        {
          LargeParamCalls.Add(context);
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new LargeParamListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.LargeParamCalls)
      {
        var count = call.parameterList().expression().Length;
        report.AddWarning(Code, $"Вызов функции '{call.identifier().GetText()}' содержит {count} параметров (рекомендуется не более {MaxParameterCount}).", document, call.identifier().GetTextPosition());
      }
    }
  }
}
