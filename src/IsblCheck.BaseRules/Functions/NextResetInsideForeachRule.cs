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
  /// Правило обнаружения вызова Next/Reset внутри foreach.
  /// </summary>
  internal class NextResetInsideForeachRule : AbstractRule
  {
    private const string Code = "F041";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(NextResetInsideForeachRule).Name,
        "Правило обнаружения вызова Next/Reset внутри foreach (нарушает итерацию)."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> ForbiddenMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "Next", "Reset", "Следующий", "Сбросить"
    };

    private class NextResetListener : IsblBaseListener
    {
      public List<IsblParser.InvocationCallContext> ForbiddenCalls { get; } = new List<IsblParser.InvocationCallContext>();

      public override void EnterInvocationCall(IsblParser.InvocationCallContext context)
      {
        var name = context.identifier().GetText();
        if (ForbiddenMethods.Contains(name) && IsInsideForeach(context))
        {
          ForbiddenCalls.Add(context);
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
      var listener = new NextResetListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.ForbiddenCalls)
      {
        report.AddError(Code,
          "Вызов Next/Reset внутри foreach нарушает итерацию. Используйте другой подход к обходу коллекции.",
          document, call.identifier().GetTextPosition());
      }
    }
  }
}
