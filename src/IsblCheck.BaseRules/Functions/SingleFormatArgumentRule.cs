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
  /// Правило для обнаружения вызовов функций Format, LoadStringFormat, в которые передаётся одно значение через ArrayOf.
  /// </summary>
  public class SingleFormatArgumentRule : AbstractRule
  {
    #region Константы

    /// <summary>
    /// Код правила.
    /// </summary>
    private const string Code = "F018";

    #endregion

    #region Поля

    /// <summary>
    /// Инфо правила.
    /// </summary>
    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(SingleFormatArgumentRule).Name, Resources.SingleFormatArgumentRuleDescription), true);

    /// <summary>
    /// Инфо правила.
    /// </summary>
    public static IRuleInfo Info => info.Value;

    #endregion

    private class SingleFormatArgumentRuleListener : IsblBaseListener
    {
      public List<ParserRuleContext> SingleArgumentEntries { get; } = new List<ParserRuleContext>();
      public IContext Context;

      #region IsblBaseListener

      public override void EnterFunction([NotNull] IsblParser.FunctionContext context)
      {
        var functionName = context.identifier().GetText();
        if (!functionName.Equals("Format", StringComparison.OrdinalIgnoreCase) &&
            !functionName.Equals("Формат", StringComparison.OrdinalIgnoreCase) &&
            !functionName.Equals("LoadStringFmt", StringComparison.OrdinalIgnoreCase))
          return;
        if (Context.HasUserFunction(functionName))
          return;
        if (context.parameterList() == null)
          return;
        var parameters = ParameterListParser.GetExpressions(context.parameterList());
        IsblParser.OperandContext argsListOperand = null;
        if (functionName.Equals("LoadStringFmt", StringComparison.OrdinalIgnoreCase))
        {
          if (parameters.Length < 3)
            return;
          argsListOperand = parameters[2]?.operand();
        }
        else
        {
          if (parameters.Length < 2)
            return;
          argsListOperand = parameters[1]?.operand();
        }
        while (argsListOperand?.expression() != null)
          argsListOperand = argsListOperand.expression().operand();
        if (argsListOperand?.function() == null || argsListOperand.invocationCall().Length != 0)
          return;
        var argsListFunctionName = argsListOperand.function().identifier().GetText();
        if (!argsListFunctionName.Equals("ArrayOf", StringComparison.OrdinalIgnoreCase) &&
            !argsListFunctionName.Equals("Массив", StringComparison.OrdinalIgnoreCase))
          return;
        if (Context.HasUserFunction(argsListFunctionName))
          return;
        var arrayOfFunctionParams = argsListOperand.function().parameterList();
        if (arrayOfFunctionParams == null)
          return;
        var arguments = ParameterListParser.GetExpressions(arrayOfFunctionParams);
        if (arguments.Length == 1 && arguments[0] != null)
        {
          this.SingleArgumentEntries.Add(argsListOperand.function());
        }
      }

      #endregion
    }

    #region AbstractRule

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new SingleFormatArgumentRuleListener { Context = context };
      walker.Walk(listener, tree);
      foreach (var argument in listener.SingleArgumentEntries)
      {
        var description = "В форматирование передан ArrayOf с одним элементом. Скалярная форма может быть короче; перед заменой проверьте тип значения и поведение функции в установленной версии Directum.";
        report.Add(Severity.Information, Code, description, document, argument.GetTextPosition(), new DiagnosticRecommendation(
          "Упростите передачу единственного аргумента только после сверки сигнатуры и типа элемента. Сохраните ArrayOf, если сам элемент может быть массивом либо текущая форма намеренно задаёт один позиционный аргумент.",
          "Сравните полученную строку для обычного и пустого значения. Для локализованного шаблона сравните также все поддерживаемые языки."));
      }
    }

    #endregion
  }
}
