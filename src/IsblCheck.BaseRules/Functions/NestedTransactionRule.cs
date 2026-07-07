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
  /// Правило обнаружения вложенных транзакций.
  /// </summary>
  internal class NestedTransactionRule : AbstractRule
  {
    private const string Code = "F030";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(NestedTransactionRule).Name, "Правило обнаружения вложенных транзакций (StartTransaction внутри активной транзакции)."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> TransactionStartMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "StartTransaction", "НачатьТранзакцию"
    };

    private static readonly HashSet<string> TransactionEndMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "CommitTransaction", "ЗафиксироватьТранзакцию",
      "RollbackTransaction", "ОтменитьТранзакцию"
    };

    private class NestedTransactionListener : IsblBaseListener
    {
      public List<IsblParser.FunctionContext> NestedCalls { get; } = new List<IsblParser.FunctionContext>();
      private int transactionDepth = 0;

      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var name = context.identifier().GetText();
        if (TransactionStartMethods.Contains(name))
        {
          transactionDepth++;
          if (transactionDepth > 1)
          {
            NestedCalls.Add(context);
          }
        }
        else if (TransactionEndMethods.Contains(name))
        {
          if (transactionDepth > 0)
            transactionDepth--;
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new NestedTransactionListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.NestedCalls)
      {
        report.AddWarning(Code, "Вложенная транзакция: StartTransaction вызывается внутри активной транзакции.", document, call.identifier().GetTextPosition());
      }
    }
  }
}
