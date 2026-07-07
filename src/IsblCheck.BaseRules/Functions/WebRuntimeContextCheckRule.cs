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
  /// Правило обнаружения использования десктопных функций без проверки IsWebRuntimeContext().
  /// </summary>
  internal class WebRuntimeContextCheckRule : AbstractRule
  {
    private const string Code = "F048";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(WebRuntimeContextCheckRule).Name,
        "Правило обнаружения использования десктопных функций без проверки IsWebRuntimeContext()."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> DesktopOnlyFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "CreateOpenDialog", "CreateSaveDialog", "CreateFolderDialog",
      "CreateInputDialog", "CreateDualListDialog",
      "СоздатьДиалогОткрытия", "СоздатьДиалогСохранения", "СоздатьДиалогПапки"
    };

    private static readonly HashSet<string> RuntimeCheckFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "IsWebRuntimeContext", "InteractiveMode", "IsNOMADRuntimeContext"
    };

    private class DesktopFunctionListener : IsblBaseListener
    {
      public List<IsblParser.FunctionContext> UnsafeCalls { get; } = new List<IsblParser.FunctionContext>();
      private bool hasRuntimeCheck = false;

      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var name = context.identifier().GetText();
        if (RuntimeCheckFunctions.Contains(name))
        {
          hasRuntimeCheck = true;
        }
        else if (DesktopOnlyFunctions.Contains(name) && !hasRuntimeCheck)
        {
          UnsafeCalls.Add(context);
        }
      }

      public override void ExitIfStatement(IsblParser.IfStatementContext context)
      {
        hasRuntimeCheck = false;
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new DesktopFunctionListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.UnsafeCalls)
      {
        report.AddWarning(Code,
          "Десктопная функция без проверки IsWebRuntimeContext(). В веб-клиенте это вызовет ошибку.",
          document, call.identifier().GetTextPosition());
      }
    }
  }
}
