using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
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
  /// <summary>
  /// Правило, по которому при создании исключения должен указываться класс исключения.
  /// </summary>
  public class ExceptionClassNotSpecifiedRule : AbstractRule
  {
    #region Константы

    /// <summary>
    /// Код правила.
    /// </summary>
    private const string Code = "F007";

    /// <summary>
    /// Имя функции создания исключения.
    /// </summary>
    private const string CreateExceptionFunctionName = "CreateException";

    #endregion

    #region Поля

    /// <summary>
    /// Инфо правила.
    /// </summary>
    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(ExceptionClassNotSpecifiedRule).Name, "Пустое имя исключения в CreateException."), true);

    /// <summary>
    /// Инфо правила.
    /// </summary>
    public static IRuleInfo Info => info.Value;

    #endregion

    #region Вложенные классы

    private class ExceptionClassNotSpecifiedRuleListener : IsblBaseListener
    {
      public List<ParserRuleContext> EmptyExceptionClassParams { get; } = new List<ParserRuleContext>();
      public IContext Context;

      #region IsblBaseListener

      public override void EnterFunction([NotNull] IsblParser.FunctionContext context)
      {
        var functionName = context.identifier().GetText();
        if (!string.Equals(functionName, CreateExceptionFunctionName, StringComparison.OrdinalIgnoreCase) ||
            Context.HasUserFunction(functionName))
          return;
        var functionParams = ParameterListParser.GetExpressions(context.parameterList());
        if (functionParams == null)
          return;
        if (functionParams.Length == 0)
          return;
        var exceptionClassStringValue = GetStringOperandValue(functionParams[0]);
        if (exceptionClassStringValue?.Trim() == string.Empty)
        {
          EmptyExceptionClassParams.Add(functionParams[0]);
        }
      }

      #endregion

      /// <summary>
      /// Получить строковую константу из выражения.
      /// </summary>
      /// <param name="expression">Выражение.</param>
      /// <returns>Строковая константа, либо null, если выражение не является строкой.</returns>
      private static string GetStringOperandValue(IsblParser.ExpressionContext expression)
      {
        var text = expression?.operand()?.@string()?.GetText();
        return text == null ? null : text.Substring(1, text.Length - 2);
      }
    }

    #endregion

    #region AbstractRule

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new ExceptionClassNotSpecifiedRuleListener { Context = context };
      walker.Walk(listener, tree);
      foreach (var emptyExceptionClassParam in listener.EmptyExceptionClassParams)
      {
        report.Add(Severity.Information, Code, "В CreateException передано пустое имя исключения. Имя важно, если обработчик различает причины по этому признаку.",
          document, emptyExceptionClassParam.GetTextPosition(), new DiagnosticRecommendation(
            "Посмотрите обработчики, которые получают это исключение. Если они различают ошибки по имени, укажите стабильное имя и согласуйте его с обработчиками. Сохраните назначение параметров сообщения и категории.",
            "Вызовите исключение в тестовом сценарии и проверьте, что его получает ожидаемый обработчик и журнал содержит нужный контекст."));
      }
    }

    #endregion

  }
}
