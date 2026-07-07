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
  /// Правило обнаружения ExceptionsOff вне блока Try/Except.
  /// </summary>
  internal class ExceptionsOffOutsideTryExceptRule : AbstractRule
  {
    private const string Code = "F040";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(ExceptionsOffOutsideTryExceptRule).Name,
        "Правило обнаружения ExceptionsOff вне блока Try/Except."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> ExceptionsOffMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "ExceptionsOff", "ОтключитьИсключения"
    };

    private class ExceptionsOffListener : IsblBaseListener
    {
      public List<IsblParser.FunctionContext> UnsafeCalls { get; } = new List<IsblParser.FunctionContext>();

      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var name = context.identifier().GetText();
        if (!ExceptionsOffMethods.Contains(name))
          return;

        if (!IsInsideTryStatement(context))
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
      var listener = new ExceptionsOffListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.UnsafeCalls)
      {
        report.AddError(Code,
          "ExceptionsOff вызван вне блока Try/Except. Исключения подавлены без восстановления.",
          document, call.identifier().GetTextPosition());
      }
    }
  }
}
