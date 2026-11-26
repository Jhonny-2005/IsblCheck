using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using IsblCheck.BaseRules.Properties;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  public class DeprecatedFunctionRule : AbstractRule
  {
    private const string Code = "F013";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(DeprecatedFunctionRule).Name, "Использование устаревшей функции ISBL."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly Dictionary<string, string> DeprecatedFunctions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      { "СпрРеквПр", "Используйте СпрРекв с параметром НомерСтроки" },
      { "СпрТблСтрСозд", "Используйте методы набора данных" },
      { "СпрТблСтрУд", "Используйте методы набора данных" },
      { "СпрСоздать", "Используйте Справочник.СоздатьЗапись" },
      { "СпрОткрыть", "Используйте Справочник.ОткрытьЗапись" },
      { "СпрСохранить", "Используйте Справочник.СохранитьЗапись" },
      { "СпрУдалить", "Используйте Справочник.УдалитьЗапись" }
    };

    private class DeprecatedFunctionListener : IsblBaseListener
    {
      public IContext Context;
      public List<(IsblParser.FunctionContext Context, string Message)> DeprecatedCalls { get; } =
        new List<(IsblParser.FunctionContext, string)>();

      public override void EnterFunction([NotNull] IsblParser.FunctionContext context)
      {
        var functionName = context.identifier().GetText();
        if (DeprecatedFunctions.TryGetValue(functionName, out var message) &&
            !Context.HasUserFunction(functionName))
        {
          DeprecatedCalls.Add((context, message));
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new DeprecatedFunctionListener { Context = context };
      walker.Walk(listener, tree);

      foreach (var (funcCall, message) in listener.DeprecatedCalls)
      {
        report.AddWarning(Code, $"Устаревшая функция: {message}",
          document, funcCall.identifier().GetTextPosition());
      }
    }
  }
}
