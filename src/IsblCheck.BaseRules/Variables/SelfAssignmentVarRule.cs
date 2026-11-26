using System;
using System.Collections.Generic;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using IsblCheck.BaseRules.Properties;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Variables
{
  /// <summary>
  /// Правило поиска переменных, которые присваиваются сами себе.
  /// </summary>
  internal class SelfAssignmentVarRule : AbstractRule
  {
    #region Константы

    /// <summary>
    /// Код правила.
    /// </summary>
    private const string Code = "A005";

    #endregion

    #region Вложенные классы

    /// <summary>
    /// Поиск переменных, которые присваиваются сами себе.
    /// </summary>
    private class SelfAssignmentVarListener : IsblBaseListener
    {
      private readonly IContext mainContext;


      /// <summary>
      /// Конструктор.
      /// </summary>
      /// <param name="context">Контекст</param>
      public SelfAssignmentVarListener(IContext context)
      {
        this.mainContext = context;
      }

      /// <summary>
      /// Список переменных, попадающих под данное правило. 
      /// </summary>
      public List<IsblParser.AssignStatementContext> ContextsWithError { get; } = new List<IsblParser.AssignStatementContext>();

      /// <summary>
      /// Вход в предложение означивания.
      /// </summary>
      /// <param name="context">Контекст.</param>
      public override void EnterAssignStatement([NotNull]IsblParser.AssignStatementContext context)
      {
        // Property getters and setters can have side effects even without arguments.
        // Only report a plain local variable assignment.
        if (context.variable() == null || context.typedefinition() != null || context.indexer() != null || context.invocationCall().Length != 0)
          return;
        var right = context.expression()?.operand();
        while (right?.expression() is { } inner)
          right = inner.operand();
        if (right?.variable() == null || right.indexer() != null || right.POINTER() != null || right.invocationCall().Length != 0)
          return;

        var leftExpression = context.variable().GetText();
        var rightExpression = right.variable().GetText();
        if (mainContext.Application.IsExistPredefinedVariable(leftExpression))
        {
          leftExpression = leftExpression.Trim('!');
          rightExpression = rightExpression.Trim('!');
        }

        if (leftExpression.Equals(rightExpression, StringComparison.OrdinalIgnoreCase))
          this.ContextsWithError.Add(context);
      }

    }

    #endregion

    #region Поля

    /// <summary>
    /// Инфо правила.
    /// </summary>
    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() => 
      new RuleInfo(typeof(SelfAssignmentVarRule).Name, Resources.SelfAssignmentVarRuleDescription), true);

    /// <summary>
    /// Инфо правила.
    /// </summary>
    public static IRuleInfo Info => info.Value;

    #endregion

    #region Методы

    /// <summary>
    /// Применить правило.
    /// </summary>
    /// <param name="report">Отчет.</param>
    /// <param name="document">Документ.</param>
    /// <param name="context">Контекст.</param>
    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new SelfAssignmentVarListener(context);
      walker.Walk(listener, tree);
      var listContextesWithError = listener.ContextsWithError;

      foreach (var contextWithError in listContextesWithError)
      {
        var name = contextWithError.variable().GetText();
        report.Add(Severity.Warning, Code,
          string.Format(Resources.SelfAssignmentVarRuleDescription, contextWithError.expression().GetText()),
          document, contextWithError.GetTextPosition(), new DiagnosticRecommendation(
            $"Проверьте, какое значение предполагалось записать в «{name}». Если строка лишняя, удалите её после проверки того, что «{name}» уже получает значение на всех нужных путях.",
            "Выполните ветвь с присваиванием и проверьте значение «" + name + "» в месте следующего использования."));
      }
    }

    #endregion
  }
}
