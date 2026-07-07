using System;
using System.Collections.Generic;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Security
{
  /// <summary>
  /// Правило обнаружения запуска внешних программ в серверных событиях.
  /// </summary>
  internal class ShellExecRule : AbstractRule
  {
    private const string Code = "S004";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(ShellExecRule).Name, "Правило обнаружения запуска внешних программ (Shell/ShellExecute) в серверных событиях."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> ShellMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "Shell", "ShellExecute", "Run", "ЗапуститьПрограмму", "ВыполнитьПрограмму"
    };

    private class ShellExecListener : IsblBaseListener
    {
      public List<IsblParser.FunctionContext> ShellCalls { get; } = new List<IsblParser.FunctionContext>();

      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var name = context.identifier().GetText();
        if (ShellMethods.Contains(name))
        {
          ShellCalls.Add(context);
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      if (string.IsNullOrEmpty(document.Path) || !document.Path.StartsWith("Events.", StringComparison.OrdinalIgnoreCase))
        return;

      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new ShellExecListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.ShellCalls)
      {
        report.AddError(Code, "Запуск внешней программы в серверном событии — потенциальная угроза безопасности.", document, call.identifier().GetTextPosition());
      }
    }
  }
}
