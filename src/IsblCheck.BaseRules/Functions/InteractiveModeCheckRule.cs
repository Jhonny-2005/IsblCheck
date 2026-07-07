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
  /// Правило обнаружения обращения к форме без проверки InteractiveMode().
  /// </summary>
  internal class InteractiveModeCheckRule : AbstractRule
  {
    private const string Code = "F047";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(InteractiveModeCheckRule).Name,
        "Правило обнаружения обращения к форме без проверки InteractiveMode()."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> FormProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "Form", "Форма"
    };

    private static readonly HashSet<string> InteractiveCheckFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "InteractiveMode", "ИнтерактивныйРежим", "IsWebRuntimeContext"
    };

    private class FormAccessListener : IsblBaseListener
    {
      public List<IsblParser.InvocationCallContext> UnsafeFormAccess { get; } = new List<IsblParser.InvocationCallContext>();
      private bool hasInteractiveCheck = false;

      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var name = context.identifier().GetText();
        if (InteractiveCheckFunctions.Contains(name))
        {
          hasInteractiveCheck = true;
        }
      }

      public override void EnterInvocationCall(IsblParser.InvocationCallContext context)
      {
        var name = context.identifier().GetText();
        if (FormProperties.Contains(name) && !hasInteractiveCheck)
        {
          UnsafeFormAccess.Add(context);
        }
      }

      public override void ExitIfStatement(IsblParser.IfStatementContext context)
      {
        hasInteractiveCheck = false;
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new FormAccessListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.UnsafeFormAccess)
      {
        report.AddWarning(Code,
          "Обращение к свойству Form без проверки InteractiveMode(). В невизуальном режиме это вызовет ошибку.",
          document, call.identifier().GetTextPosition());
      }
    }
  }
}
