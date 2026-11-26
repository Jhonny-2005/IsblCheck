using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using IsblCheck.BaseRules.Properties;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Context.Development;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  public class IncorrectFunctionParamsCountRule : AbstractRule
  {
    #region Константы

    /// <summary>
    /// Код правила.
    /// </summary>
    private const string Code = "F001";

    #endregion

    #region Поля

    /// <summary>
    /// Инфо правила.
    /// </summary>
    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(IncorrectFunctionParamsCountRule).Name, Resources.IncorrectFunctionParamsCountRuleDescription), true);

    /// <summary>
    /// Инфо правила.
    /// </summary>
    public static IRuleInfo Info => info.Value;

    #endregion

    private class IncorrectFunctionParamsCountListener : IsblBaseListener
    {
      // Имена функций-исключений из правил.
      private static readonly List<string> RuleExceptions = new List<string>
      {
        "ArrayOf",
        "Массив",
        "Вызвать",
        "CallProcedure",
        "CreateArray",
        "СоздатьМассив"
      };

      private readonly IContext analysisContext;

      public List<(IsblParser.FunctionContext Call, int Actual, int Expected)> FunctionCalls { get; } = new();

      #region IsblBaseListener

      public override void EnterFunction([NotNull] IsblParser.FunctionContext context)
      {
        var functionName = context.identifier().GetText();
        if (RuleExceptions.Contains(functionName, StringComparer.OrdinalIgnoreCase))
          return;
        var function = this.analysisContext.FindFunctionSignature(functionName);
        if (function == null) return;
        var parameters = context.parameterList();
        var count = parameters.ChildCount == 0 ? 0 : parameters.COMMA().Length + 1;
        if (count > function.Arguments.Count)
          this.FunctionCalls.Add((context, count, function.Arguments.Count));
      }

      #endregion

      public IncorrectFunctionParamsCountListener(IContext context)
      {
        this.analysisContext = context;
      }
    }

    #region AbstractRule

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new IncorrectFunctionParamsCountListener(context);
      walker.Walk(listener, tree);
      foreach (var (call, actual, expected) in listener.FunctionCalls)
      {
        var name = call.identifier().GetText();
        report.Add(Severity.Warning, Code, $"В вызове «{name}» указано позиций: {actual}. В загруженной сигнатуре: {expected}.",
          document, call.identifier().GetTextPosition(), new DiagnosticRecommendation(
            $"Сверьте параметры «{name}» с карточкой функции или справкой целевой версии. Сопоставьте каждую позицию с назначением параметра, включая пропуски между разделителями. Удаляйте позицию только после сверки сигнатуры и смысла вызова.",
            "Проверьте вызов в IS-Builder с обычными данными и с опущенными необязательными параметрами. Если сигнатура отличается, обновите каталог разработки для анализа."));
      }
    }

    #endregion

  }
}
