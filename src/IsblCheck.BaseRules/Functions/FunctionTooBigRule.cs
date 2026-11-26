using System;
using IsblCheck.BaseRules.Properties;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  /// <summary>
  /// Правило ограничения максимальной длины функции.
  /// </summary>
  public class FunctionTooBigRule : AbstractRule
  {
    #region Константы

    /// <summary>
    /// Код правила.
    /// </summary>
    private const string Code = "F012";

    /// <summary>
    /// Максимальное количество строк в функции.
    /// </summary>
    private const int MaxFunctionLinesCount = 500;

    #endregion

    #region Поля

    /// <summary>
    /// Инфо правила.
    /// </summary>
    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(FunctionTooBigRule).Name, Resources.FunctionTooBigRuleDescription), true);

    /// <summary>
    /// Инфо правила.
    /// </summary>
    public static IRuleInfo Info => info.Value;

    #endregion

    #region AbstractRule

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      if (document.ComponentType != ComponentType.Function)
        return;

      var linesCount = 0;
      var functionText = document.Text;
      for (var i = 0; i < functionText.Length; i++)
      {
        if (functionText[i] == '\n')
          linesCount++;
      }
      if (functionText.Length > 0)
        linesCount++;
      if (linesCount > MaxFunctionLinesCount)
      {
        report.Add(Severity.Information, Code, $"В функции {linesCount} строк. Ориентир читаемости — {MaxFunctionLinesCount} строк.",
          document, new TextPosition { Line = 1, Column = 0, StartIndex = 0, EndIndex = 0 }, new DiagnosticRecommendation(
            "Начните с самостоятельного расчёта, который можно вынести в функцию с явными параметрами и Result. Учтите работу с переменными контекста, ожидаемые побочные эффекты и порядок обращения к объектам.",
            "Сравните результат, изменения объектов и обработку исключений на одинаковых входных данных до и после выделения функции."));
      }
    }

    #endregion
  }
}
