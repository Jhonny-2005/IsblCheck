using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Tree;
using IsblCheck.BaseRules.Properties;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  /// <summary>
  /// Правило поиска использования устаревших функций ISBL.
  /// </summary>
  internal class DeprecatedFunctionRule : AbstractRule
  {
    private const string Code = "F025";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(DeprecatedFunctionRule).Name, "Правило поиска использования устаревших функций ISBL, заменённых в Directum 5.8."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly Dictionary<string, string> DeprecatedFunctions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["GetComponent"] = "Метод GetComponent устарел. Используйте фабрики компонент (ReferencesFactory, ScriptFactory, ReportFactory).",
      ["ExecuteComponent"] = "Метод ExecuteComponent устарел. Используйте методы Execute у фабрик компонент.",
      ["CreateComponentDescription"] = "Метод CreateComponentDescription устарел. Используйте фабрики компонент.",
      ["CreateHistoryDescription"] = "Метод CreateHistoryDescription устарел. Используйте свойства History фабрик.",
      ["DepartmentContext"] = "Свойство DepartmentContext устарело и не рекомендуется к использованию.",
    };

    private class DeprecatedFunctionListener : IsblBaseListener
    {
      public List<IsblParser.FunctionContext> DeprecatedCalls { get; } = new List<IsblParser.FunctionContext>();
      public Dictionary<IsblParser.FunctionContext, string> Messages { get; } = new Dictionary<IsblParser.FunctionContext, string>();

      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var funcName = context.identifier().GetText();
        if (DeprecatedFunctions.TryGetValue(funcName, out string message))
        {
          DeprecatedCalls.Add(context);
          Messages[context] = message;
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new DeprecatedFunctionListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.DeprecatedCalls)
      {
        var description = listener.Messages[call];
        report.AddWarning(Code, description, document, call.identifier().GetTextPosition());
      }
    }
  }
}
