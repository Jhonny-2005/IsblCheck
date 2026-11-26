using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr4.Runtime;
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
  /// <summary>
  /// Правило для проверки форматной строки, передаваемой в функции Format(), LoadStringFmt().
  /// Правило учитывает варианты вызова функции как со строковой константой, так и с константой локализации.
  /// Если в функцию передаётся константа локализации, то анализируются значения константы для всех поддерживаемых языков.
  /// Обрабатываются форматы вызовов:
  ///   Format(LoadString(...); ArrayOf(...))
  ///   Format("..."; ArrayOf(...))
  ///   Формат(LoadString(...); Массив(...))
  ///   Формат("..."; Массив(...))
  ///   LoadStringFmt(...)
  /// </summary>
  public class IncorrectFormatStringRule : AbstractRule
  {
    #region Константы

    /// <summary>
    /// Код правила "Для описателя форматной строки не передан соответствующий аргумент".
    /// </summary>
    private const string FormatItemNotFoundRuleCode = "F005";
    /// <summary>
    /// Код правила "Для описателя форматной строки передан пустой параметр".
    /// </summary>
    private const string EmptyArgumentForFormatItemRuleCode = "F021";
    /// <summary>
    /// Код правила "Для переданного параметра не найден соответствующий описатель".
    /// </summary>
    private const string ArgumentForFormatItemNotFound = "F022";

    #endregion

    #region Поля

    /// <summary>
    /// Инфо правила.
    /// </summary>
    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(IncorrectFormatStringRule).Name, Resources.IncorrectFormatStringRuleDescription), true);

    /// <summary>
    /// Инфо правила.
    /// </summary>
    public static IRuleInfo Info => info.Value;

    #endregion

    /// <summary>
    /// Поиск некорректных вызовов функций форматирования строки.
    /// </summary>
    private class IncorrectFormatStringListener : IsblBaseListener
    {
      public enum TemplateStringSource
      {
        StringConstant,
        LocalizationString,
        StringExpression
      }

      public class IncorrectFormatEntry<T>
      {
        public string TemplateString { get; set; }
        public TemplateStringSource TemplateStringSource { get; set; }
        public ParserRuleContext Context { get; set; }
        public T Data { get; set; }
        public DiagnosticRecommendation Recommendation { get; set; }
      }

      /// <summary>
      /// Контекст приложения и разработки.
      /// </summary>
      private readonly IContext globalContext;

      /// <summary>
      /// Описатели шаблона строки, для которых не найдены соответствующие аргументы.
      /// </summary>
      public List<IncorrectFormatEntry<FormatItem>> FormatItemsWithoutArguments { get; } = new List<IncorrectFormatEntry<FormatItem>>();

      /// <summary>
      /// Описатели шаблона строки, для которых переданы пустые аргументы.
      /// </summary>
      public List<IncorrectFormatEntry<FormatItem>> FormatItemsWithEmptyArguments { get; } = new List<IncorrectFormatEntry<FormatItem>>();

      /// <summary>
      /// Аргументы, для которых в шаблоне строки не найдены описатели.
      /// </summary>
      public List<IncorrectFormatEntry<ParameterValue>> ArgumentsWithoutFormatItems { get; } = new List<IncorrectFormatEntry<ParameterValue>>();

      #region IsblBaseListener

      public override void EnterFunction([NotNull] IsblParser.FunctionContext context)
      {
        var functionName = context.identifier().GetText();
        if (IsOverridden(functionName)) return;
        if (functionName.Equals("Format", StringComparison.OrdinalIgnoreCase) ||
            functionName.Equals("Формат", StringComparison.OrdinalIgnoreCase))
        {
          ProcessFormatFunction(context);
        }
        else if (functionName.Equals("LoadStringFmt", StringComparison.OrdinalIgnoreCase))
        {
          ProcessLoadStringFmtFunction(context);
        }
      }

      #endregion

      /// <summary>
      /// Обработать вызов функции LoadStringFmt.
      /// </summary>
      /// <param name="function">Контекст вызова функции.</param>
      private void ProcessLoadStringFmtFunction(IsblParser.FunctionContext function)
      {
        if (function.parameterList() == null)
          return;
        var parameters = ParameterListParser.GetExpressions(function.parameterList());
        if (parameters.Length < 3)
          return;
        var templateLocalizationString = this.GetLocalizedString(function);
        if (templateLocalizationString == null)
          return;
        if (parameters[2]?.ChildCount != 1)
          return;
        var argsListOperand = parameters[2]?.operand();
        if (argsListOperand?.function() == null || argsListOperand.invocationCall().Length != 0)
          return;
        var argsListFunctionName = argsListOperand.function().identifier().GetText();
        if (!argsListFunctionName.Equals("ArrayOf", StringComparison.OrdinalIgnoreCase) &&
            !argsListFunctionName.Equals("Массив", StringComparison.OrdinalIgnoreCase))
          return;
        if (IsOverridden(argsListFunctionName)) return;
        var arrayOfFunctionParams = argsListOperand.function().parameterList();
        if (arrayOfFunctionParams == null)
          return;

        foreach (var localizationValue in templateLocalizationString.Values)
        {
          this.CheckTemplateString(localizationValue.Value, ParameterListParser.GetExpressions(function.parameterList()).First(), 
            TemplateStringSource.LocalizationString, arrayOfFunctionParams);
        }
      }

      /// <summary>
      /// Обработать вызов функции Format.
      /// </summary>
      /// <param name="function">Контекст вызова функции.</param>
      private void ProcessFormatFunction(IsblParser.FunctionContext function)
      {
        if (function.parameterList() == null)
          return;
        var parameters = ParameterListParser.GetExpressions(function.parameterList());
        if (parameters.Length != 2)
          return;
        if (parameters[1]?.ChildCount != 1)
          return;
        var argsListOperand = parameters[1]?.operand();
        if (argsListOperand?.function() == null || argsListOperand.invocationCall().Length != 0)
          return;
        var argsListFunctionName = argsListOperand.function().identifier().GetText();
        if (!argsListFunctionName.Equals("ArrayOf", StringComparison.OrdinalIgnoreCase) &&
           !argsListFunctionName.Equals("Массив", StringComparison.OrdinalIgnoreCase))
          return;
        if (IsOverridden(argsListFunctionName)) return;
        var arrayOfFunctionParams = argsListOperand.function().parameterList();
        if (arrayOfFunctionParams == null)
          return;
        var operand = parameters[0]?.operand();
        if (operand?.@string() != null)
        {
          // Шаблон - строковая константа.
          var templateString = operand.@string().GetText();
          this.CheckTemplateString(templateString, operand.@string(), TemplateStringSource.StringConstant, arrayOfFunctionParams);
        }
        else if (operand?.function() != null && operand.invocationCall().Length == 0 &&
          operand.function().identifier().GetText().Equals("LoadString", StringComparison.OrdinalIgnoreCase))
        {
          if (IsOverridden("LoadString")) return;
          // Шаблон - функция LoadString.
          var templateLocalizationString = this.GetLocalizedString(operand.function());
          if (templateLocalizationString == null)
            return;
          foreach (var localizationValue in templateLocalizationString.Values)
          {
            this.CheckTemplateString(localizationValue.Value, operand.function(), TemplateStringSource.LocalizationString, arrayOfFunctionParams);
          }
        }
        else
        {
          var literalTemplate = new StringBuilder();
          if (TryAppendLiteralTemplate(parameters[0], literalTemplate))
            this.CheckTemplateString(literalTemplate.ToString(), parameters[0], TemplateStringSource.StringExpression, arrayOfFunctionParams);
        }
      }

      private static bool TryAppendLiteralTemplate(IsblParser.ExpressionContext expression, StringBuilder template)
      {
        if (expression?.operand()?.@string() is { } literal)
        {
          var token = literal.GetText();
          template.Append(token, 1, token.Length - 2);
          return true;
        }
        if (expression?.operand()?.expression() is { } parenthesized)
          return TryAppendLiteralTemplate(parenthesized, template);
        if (expression?.CONCAT() == null) return false;
        return TryAppendLiteralTemplate(expression.expression(0), template) &&
          TryAppendLiteralTemplate(expression.expression(1), template);
      }

      /// <summary>
      /// Получить строку локализации из контекста вызова функции LoadString.
      /// </summary>
      /// <param name="loadStringFunctionContext">Контекст вызова фунции LoadString.</param>
      /// <returns>Найденная строка локализации, или null, если строку локализации определить не удаётся.</returns>
      private LocalizationString GetLocalizedString(IsblParser.FunctionContext loadStringFunctionContext)
      {
        var loadStringParamsContext = loadStringFunctionContext.parameterList();
        if (loadStringParamsContext == null)
          return null;
        var parameters = ParameterListParser.GetExpressions(loadStringParamsContext);
        if (parameters.Length < 2)
          return null;
        var name = GetStringOperandValue(parameters[0]);
        if (name == null)
          return null;
        var group = GetStringOperandValue(parameters[1]);
        if (group == null)
          return null;
        return this.globalContext.FindLocalization(group, name);
      }

      private bool IsOverridden(string name) => this.globalContext.HasUserFunction(name);

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

      private void CheckTemplateString(string templateString, ParserRuleContext stringSourceContext,
        TemplateStringSource templateStringSource, IsblParser.ParameterListContext paramsOfArrayFunction)
      {
        var templateParser = new TemplateStringParser(templateString);
        var paramsParser = new ParameterListParser(paramsOfArrayFunction);
        templateParser.Parse();
        paramsParser.Parse();
        foreach(var formatItem in templateParser.FormatItems)
        {
          var foundArg = paramsParser.ParameterValues.FirstOrDefault(a => a.Index == formatItem.Index);
          if(foundArg == null)
          {
            this.FormatItemsWithoutArguments.Add(new IncorrectFormatEntry<FormatItem>
            {
              TemplateString = templateString,
              Data = formatItem,
              Context = stringSourceContext,
              TemplateStringSource = templateStringSource
            });
          }
          else if(foundArg.IsEmpty)
          {
            this.FormatItemsWithEmptyArguments.Add(new IncorrectFormatEntry<FormatItem>
            {
              TemplateString = templateString,
              Data = formatItem,
              Context = stringSourceContext,
              TemplateStringSource = templateStringSource
            });
          }
        }
        var usedIndexes = templateParser.FormatItems.Select(f => f.Index).ToHashSet();
        var unused = paramsParser.ParameterValues.Where(p => !p.IsEmpty && !usedIndexes.Contains(p.Index)).ToArray();
        foreach (var parameter in unused)
        {
          var recommendation = unused.Length == 1 && templateStringSource != TemplateStringSource.LocalizationString
            ? SuggestRepeatedIndex(templateString, templateParser.FormatItems, parameter) : null;
          this.ArgumentsWithoutFormatItems.Add(new IncorrectFormatEntry<ParameterValue>
          {
            TemplateString = templateString,
            Data = parameter,
            Context = parameter.Context,
            TemplateStringSource = templateStringSource,
            Recommendation = recommendation ?? new DiagnosticRecommendation(
              $"Сопоставьте аргумент «{parameter.Text}» в позиции {parameter.Index} с ожидаемым полем результата. Если поле требуется, восстановите описатель в нужном месте шаблона. Если аргумент лишний, перед удалением проверьте последующие индексы и побочные эффекты выражения.",
              "Подставьте различимые значения в соседние позиции и сравните результат во всех используемых переводах.")
          });
        }
      }

      private static DiagnosticRecommendation SuggestRepeatedIndex(string template, List<FormatItem> items, ParameterValue missing)
      {
        // A proposal requires an unambiguous local pattern: k-1, k-1, k+1,
        // explicit indexes throughout, and the same separator in both gaps.
        if (missing.Index < 1 || items.Any(item => !IsExplicit(item))) return null;
        var previous = items.Where(item => item.Index == missing.Index - 1).ToArray();
        var next = items.Where(item => item.Index == missing.Index + 1).ToArray();
        if (previous.Length != 2 || next.Length != 1 || previous[0].Text != previous[1].Text ||
            next[0].Pos.StartIndex <= previous[1].Pos.EndIndex) return null;
        var firstGap = template[(previous[0].Pos.EndIndex + 1)..previous[1].Pos.StartIndex];
        var secondGap = template[(previous[1].Pos.EndIndex + 1)..next[0].Pos.StartIndex];
        if (firstGap != secondGap) return null;
        var repeated = previous[1].Text;
        var replacement = $"%{missing.Index}{repeated[repeated.IndexOf(':')..]}";
        return new DiagnosticRecommendation(
          $"Шаблон повторяет «{repeated}» перед «{next[0].Text}». Если между ними должно выводиться значение «{missing.Text}», замените второе «{repeated}» на «{replacement}». Порядок аргументов ArrayOf сохраните.",
          $"Подайте разные значения для индексов {missing.Index - 1} и {missing.Index}. Убедитесь, что они появляются в своих полях, а следующие поля остались на месте.");
      }

      private static bool IsExplicit(FormatItem item) =>
        item.Text.StartsWith($"%{item.Index}:", StringComparison.Ordinal);

      /// <summary>
      /// Конструктор.
      /// </summary>
      /// <param name="context">Контекст.</param>
      public IncorrectFormatStringListener(IContext context)
      {
        this.globalContext = context;
      }
    }

    #region AbstractRule

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new IncorrectFormatStringListener(context);
      walker.Walk(listener, tree);
      foreach (var entry in listener.FormatItemsWithoutArguments)
      {
        var description = string.Format(Resources.ArgumentForTemplateNotFound, 
          entry.Data.Text, entry.Data.Index, entry.TemplateString);
        var position = entry.Context.GetTextPosition();
        if(entry.TemplateStringSource == IncorrectFormatStringListener.TemplateStringSource.StringConstant)
        {
          // Подсвечиваем некорректный описатель прямо в строковой константе.
          var startIndex = position.StartIndex;
          position.StartIndex = startIndex + entry.Data.Pos.StartIndex;
          position.EndIndex = startIndex + entry.Data.Pos.EndIndex;
          position.Column = entry.Data.Pos.Line == 0 ? position.Column + entry.Data.Pos.Column : entry.Data.Pos.Column;
          position.Line += entry.Data.Pos.Line;
        }
        report.Add(Severity.Error, FormatItemNotFoundRuleCode, description, document, position,
          new DiagnosticRecommendation(
            $"Для «{entry.Data.Text}» нужно значение в позиции {entry.Data.Index}. Если это поле должно выводиться, передайте в этой позиции вычисление нужного значения. Если индекс ошибочен, исправьте его в шаблоне после сверки с ArrayOf.",
            "Проверьте отдельно значение этого поля и соседних полей. Для локализованного шаблона повторите проверку на используемых языках."));
      }
      foreach (var entry in listener.FormatItemsWithEmptyArguments)
      {
        var description = $"Для описателя \"{entry.Data.Text}\" в позиции {entry.Data.Index} передан пропущенный аргумент. Проверьте назначение позиции и поведение форматирования в установленной версии Directum.";
        var position = entry.Context.GetTextPosition();
        if (entry.TemplateStringSource == IncorrectFormatStringListener.TemplateStringSource.StringConstant)
        {
          // Подсвечиваем некорректный описатель прямо в строковой константе.
          var startIndex = position.StartIndex;
          position.StartIndex = startIndex + entry.Data.Pos.StartIndex;
          position.EndIndex = startIndex + entry.Data.Pos.EndIndex;
          position.Column = entry.Data.Pos.Line == 0 ? position.Column + entry.Data.Pos.Column : entry.Data.Pos.Column;
          position.Line += entry.Data.Pos.Line;
        }
        report.Add(Severity.Warning, EmptyArgumentForFormatItemRuleCode, description, document, position,
          new DiagnosticRecommendation($"Уточните, какое значение должен выводить «{entry.Data.Text}». Если оно требуется, заполните пропущенную позицию {entry.Data.Index} выражением ожидаемого типа.",
            "Проверьте вывод с заполненным и отсутствующим исходным значением. Сверьте следующие позиции, если меняли шаблон."));
      }
      foreach (var incorrectFormatEntry in listener.ArgumentsWithoutFormatItems)
      {
        var description = string.Format(Resources.RedutantFormatArgument,
          incorrectFormatEntry.Data.Text, incorrectFormatEntry.Data.Index, incorrectFormatEntry.TemplateString);
        report.Add(Severity.Warning, ArgumentForFormatItemNotFound, description, document,
          incorrectFormatEntry.Context.GetTextPosition(), incorrectFormatEntry.Recommendation);
      }
    }

    #endregion

  }
}
