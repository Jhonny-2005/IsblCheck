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
  /// Правило обнаружения загрузки справочников без фильтрации на сервере.
  /// </summary>
  internal class FullReferenceLoadOnServerRule : AbstractRule
  {
    private const string Code = "P001";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(FullReferenceLoadOnServerRule).Name,
        "Правило обнаружения загрузки справочников без фильтрации на сервере."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> ReferenceLoadMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "FindAll", "НайтиВсе", "Open", "Открыть"
    };

    private class ReferenceLoadListener : IsblBaseListener
    {
      public List<IsblParser.InvocationCallContext> UnsafeCalls { get; } = new List<IsblParser.InvocationCallContext>();

      public override void EnterInvocationCall(IsblParser.InvocationCallContext context)
      {
        var name = context.identifier().GetText();
        if (ReferenceLoadMethods.Contains(name) && IsInsideForeach(context))
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
      var listener = new ReferenceLoadListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.UnsafeCalls)
      {
        report.AddWarning(Code,
          "Загрузка справочника внутри цикла может привести к перегрузке сервера.",
          document, call.identifier().GetTextPosition());
      }
    }
  }
}

