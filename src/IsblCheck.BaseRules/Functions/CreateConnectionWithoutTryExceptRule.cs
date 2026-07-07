using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  /// <summary>
  /// Правило обнаружения CreateConnection без Try/Except.
  /// </summary>
  internal class CreateConnectionWithoutTryExceptRule : AbstractRule
  {
    private const string Code = "F042";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(CreateConnectionWithoutTryExceptRule).Name,
        "Правило обнаружения CreateConnection без блока Try/Except."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> ConnectionMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "CreateConnection", "СоздатьСоединение"
    };

    private class ConnectionListener : IsblBaseListener
    {
      public List<IsblParser.FunctionContext> UnsafeCalls { get; } = new List<IsblParser.FunctionContext>();

      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var name = context.identifier().GetText();
        if (ConnectionMethods.Contains(name) && !IsInsideTryStatement(context))
        {
          UnsafeCalls.Add(context);
        }
      }

      private bool IsInsideTryStatement(ParserRuleContext context)
      {
        var parent = context.Parent;
        while (parent != null)
        {
          if (parent is IsblParser.TryStatementContext)
            return true;
          parent = parent.Parent;
        }
        return false;
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new ConnectionListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.UnsafeCalls)
      {
        report.AddWarning(Code,
          "CreateConnection без Try/Except. Создание соединения может завершиться ошибкой.",
          document, call.identifier().GetTextPosition());
      }
    }
  }
}
