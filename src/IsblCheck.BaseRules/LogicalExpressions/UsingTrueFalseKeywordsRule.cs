using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using IsblCheck.BaseRules.Properties;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.LogicalExpressions
{
  /// <summary>
  /// Подсказка о прямом сравнении значения Variant с True или False.
  /// </summary>
  public class UsingTrueFalseKeywordsRule : AbstractRule
  {
    #region Константы

    /// <summary>
    /// Код правила.
    /// </summary>
    private const string Code = "B003";

    #endregion

    #region Поля

    /// <summary>
    /// Инфо правила.
    /// </summary>
    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(UsingTrueFalseKeywordsRule).Name, Resources.UsingTrueFalseKeywordsRuleDescription), true);

    /// <summary>
    /// Инфо правила.
    /// </summary>
    public static IRuleInfo Info => info.Value;

    #endregion

    private class UsingTrueFalseKeywordsListener : IsblBaseListener
    {
      private const string TrueConstant = "True";
      private const string FalseConstant = "False";

      public List<ParserRuleContext> Entries { get; } = new List<ParserRuleContext>();

      #region IsblBaseListener

      public override void EnterExpression([NotNull] IsblParser.ExpressionContext context)
      {
        if (context.EQ() == null && context.NEQ() == null)
          return;

        var constant = GetConstant(context.expression(0)) ?? GetConstant(context.expression(1));
        if (constant != null)
          Entries.Add(constant);
      }

      #endregion
      
      private static bool IsTrueFalseConstant(string constName)
      {
        return string.Equals(constName, TrueConstant, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(constName, FalseConstant, StringComparison.OrdinalIgnoreCase);
      }

      private static IsblParser.VariableContext GetConstant(IsblParser.ExpressionContext expression)
      {
        while (expression?.operand()?.expression() is { } inner)
          expression = inner;
        var operand = expression?.operand();
        return operand?.variable() is { } variable && operand.indexer() == null &&
          operand.POINTER() == null && operand.invocationCall().Length == 0 &&
          IsTrueFalseConstant(variable.GetText()) ? variable : null;
      }
    }

    #region AbstractRule

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new UsingTrueFalseKeywordsListener();
      walker.Walk(listener, tree);
      foreach (var entry in listener.Entries)
      {
        report.Add(Severity.Information, Code, "Прямое сравнение с True/False. Применимость упрощения зависит от типа второго операнда.", document, entry.GetTextPosition(),
          new DiagnosticRecommendation(
            "Сокращайте условие только для значения, которое гарантированно логическое. Для числового или смешанного Variant сохраните явную проверку, если она выражает требуемый контракт. Не заменяйте True числом 1.",
            "Сравните результат исходного и изменённого условия для True, False и допустимых не-логических значений, включая NULL, если он возможен."));
      }
    }

    #endregion

  }
}
