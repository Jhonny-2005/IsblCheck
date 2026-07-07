using System;
using System.Collections.Generic;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Other
{
  /// <summary>
  /// Правило обнаружения использования устаревших переменных в событиях.
  /// </summary>
  internal class DeprecatedEventVariableRule : AbstractRule
  {
    private const string Code = "F036";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(DeprecatedEventVariableRule).Name,
        "Правило обнаружения использования устаревших переменных в обработчиках событий."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly Dictionary<string, string> DeprecatedVariables =
      new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      { "DocContext", "Используйте предопределенные переменные вместо DocContext" },
      { "RefContext", "Используйте предопределенные переменные вместо RefContext" },
      { "FormContext", "Используйте предопределенные переменные вместо FormContext" },
      { "CardContext", "Используйте предопределенные переменные вместо CardContext" }
    };

    private class DeprecatedVariableListener : IsblBaseListener
    {
      public List<IsblParser.VariableContext> DeprecatedVars { get; } =
        new List<IsblParser.VariableContext>();

      public override void EnterVariable(IsblParser.VariableContext context)
      {
        var name = context.GetText();
        if (DeprecatedVariables.ContainsKey(name))
        {
          DeprecatedVars.Add(context);
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new DeprecatedVariableListener();
      walker.Walk(listener, tree);

      foreach (var variable in listener.DeprecatedVars)
      {
        var name = variable.GetText();
        report.AddWarning(Code,
          $"Использование устаревшей переменной '{name}'. {DeprecatedVariables[name]}.",
          document, variable.Start.ToTextPosition());
      }
    }
  }
}
