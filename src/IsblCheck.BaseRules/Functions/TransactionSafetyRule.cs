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
  internal class TransactionSafetyRule : AbstractRule
  {
    private const string Code = "F017";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(TransactionSafetyRule).Name, "Правило проверки корректности использования транзакций (StartTransaction без Commit/Rollback)."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> TransactionStartMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "StartTransaction", "НачТран"
    };

    private static readonly HashSet<string> TransactionEndMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "CommitTransaction", "RollbackTransaction", "КонТран"
    };

    private class TransactionListener : IsblBaseListener
    {
      private bool hasTransactionStart = false;
      private bool hasTransactionEnd = false;
      private IsblParser.FunctionContext startContext = null;

      public List<IsblParser.FunctionContext> UnbalancedTransactions { get; } = new List<IsblParser.FunctionContext>();

      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var name = context.identifier().GetText();
        if (TransactionStartMethods.Contains(name))
        {
          hasTransactionStart = true;
          hasTransactionEnd = false;
          startContext = context;
        }
        else if (TransactionEndMethods.Contains(name))
        {
          hasTransactionEnd = true;
        }
      }

      public override void ExitTryStatement(IsblParser.TryStatementContext context)
      {
        if (hasTransactionStart && !hasTransactionEnd && startContext != null)
        {
          UnbalancedTransactions.Add(startContext);
        }
        hasTransactionStart = false;
        hasTransactionEnd = false;
        startContext = null;
      }

      public void CheckFinalBalance()
      {
        if (hasTransactionStart && !hasTransactionEnd && startContext != null)
        {
          UnbalancedTransactions.Add(startContext);
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new TransactionListener();
      walker.Walk(listener, tree);
      listener.CheckFinalBalance();

      foreach (var call in listener.UnbalancedTransactions)
      {
        report.AddError(Code, "Несбалансированная транзакция: StartTransaction без CommitTransaction/RollbackTransaction.", document, call.identifier().GetTextPosition());
      }
    }
  }
}
