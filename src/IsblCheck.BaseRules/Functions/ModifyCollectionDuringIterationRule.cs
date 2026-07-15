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
  /// Правило обнаружения модификации коллекции во время итерации.
  /// </summary>
  internal class ModifyCollectionDuringIterationRule : AbstractRule
  {
    private const string Code = "F053";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(ModifyCollectionDuringIterationRule).Name,
        "Правило обнаружения модификации коллекции во время итерации foreach."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> ModifyMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "Delete", "Add", "Insert", "Clear", "Remove",
      "Удалить", "Добавить", "Вставить", "Очистить", "Убрать"
    };

    private class CollectionModifyListener : IsblBaseListener
    {
      public List<IsblParser.InvocationCallContext> UnsafeCalls { get; } = new List<IsblParser.InvocationCallContext>();

      public override void EnterInvocationCall(IsblParser.InvocationCallContext context)
      {
        var name = context.identifier().GetText();
        if (ModifyMethods.Contains(name) && IsInsideForeach(context))
        {
          UnsafeCalls.Add(context);
        }
      }

      private bool IsInsideForeach(ParserRuleContext context)
      {
        var parent = context.Parent;
        while (parent != null)
        {
          if (parent is IsblParser.ForeachStatementContext)
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
      var listener = new CollectionModifyListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.UnsafeCalls)
      {
        report.AddError(Code,
          "Модификация коллекции во время итерации foreach может привести к неправильной работе цикла.",
          document, call.identifier().GetTextPosition());
      }
    }
  }
}

