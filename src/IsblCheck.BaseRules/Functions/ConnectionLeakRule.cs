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
  internal class ConnectionLeakRule : AbstractRule
  {
    private const string Code = "F018";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(ConnectionLeakRule).Name, "Правило обнаружения утечек соединений (создание без закрытия)."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> ConnectionCreateMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "CreateConnection", "СоздатьСоединение"
    };

    private static readonly HashSet<string> ConnectionCloseMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "Close", "Закрыть", "СоединениеЗакрыть"
    };

    private class ConnectionLeakListener : IsblBaseListener
    {
      public List<IsblParser.FunctionContext> LeakedConnections { get; } = new List<IsblParser.FunctionContext>();

      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var name = context.identifier().GetText();
        if (ConnectionCreateMethods.Contains(name))
        {
          LeakedConnections.Add(context);
        }
      }

      public override void EnterInvocationCall(IsblParser.InvocationCallContext context)
      {
        var name = context.identifier().GetText();
        if (ConnectionCloseMethods.Contains(name))
        {
          if (context.Parent is IsblParser.InvokeStatementContext invoke &&
              invoke.GetChild(0) is IsblParser.VariableContext variable)
          {
            var varName = variable.GetText();
            for (int i = LeakedConnections.Count - 1; i >= 0; i--)
            {
              var create = LeakedConnections[i];
              if (create.parameterList()?.expression()?.Length > 0)
              {
                var firstParam = create.parameterList().expression()[0];
                if (firstParam.GetText().Equals(varName, StringComparison.OrdinalIgnoreCase))
                {
                  LeakedConnections.RemoveAt(i);
                  break;
                }
              }
            }
          }
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new ConnectionLeakListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.LeakedConnections)
      {
        report.AddWarning(Code, "Возможная утечка соединения: CreateConnection без последующего Close.", document, call.identifier().GetTextPosition());
      }
    }
  }
}
