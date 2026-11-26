using System;
using System.Collections.Generic;
using Antlr4.Runtime.Tree;
using IsblCheck.BaseRules.Properties;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.ObjectModel
{
  class UsingInfoReferenceRule : AbstractRule
  {
    #region Константы

    /// <summary>
    /// Код правила.
    /// </summary>
    private const string Code = "I013";

    /// <summary>
    /// Обращение к Info.Reference.
    /// </summary>
    private const string ReferenceProperty = "Reference";

    #endregion

    #region Вложенные классы

    /// <summary>
    /// Поиск использования свойства Info.Reference.
    /// </summary>
    private class UsingInfoReferenceListener : IsblBaseListener
    {
      /// <summary>
      /// Список контекстов, попадающих под данное правило. 
      /// </summary>
      public List<IsblParser.InvocationCallContext> ContextWithInvocationErrors { get; } = new List<IsblParser.InvocationCallContext>();


      /// <summary>
      /// Вход в выражение.
      /// </summary>
      /// <param name="context">Контекст.</param>
      public override void EnterInvocationCall(IsblParser.InvocationCallContext context)
      {
        if (context.identifier().GetText().Equals(ReferenceProperty,StringComparison.OrdinalIgnoreCase) &&
          context.parameterList() == null)
        {
          var parent = context.Parent;
          var index = 0;
          while (index < parent.ChildCount && parent.GetChild(index) != context) index++;
          if (index < 2) return;
          var receiver = parent.GetChild(index - 2);
          var receiverName = receiver is IsblParser.InvocationCallContext call
            ? call.identifier().GetText() : receiver.GetText();
          if (!receiverName.Equals("Info", StringComparison.OrdinalIgnoreCase)) return;
          ContextWithInvocationErrors.Add(context);
        }
      }
    }

    #endregion

    #region Поля

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(UsingInfoReferenceRule).Name, Resources.UsingInfoReferenceDescription), true);

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
      var listener = new UsingInfoReferenceListener();
      walker.Walk(listener, tree);
      var listContextsWithError = listener.ContextWithInvocationErrors;

      foreach (var contextWithError in listContextsWithError)
      {
        report.Add(Severity.Information, Code,
          Resources.UsingInfoReferenceRule,
          document, contextWithError.GetTextPosition(), new DiagnosticRecommendation(
            "Определите фактический тип Info и ожидаемый результат. Если нужен объект конкретной записи, используйте документированный для этого типа способ привязки к записи с её идентификатором.",
            "Проверьте выбор записи по идентификатору, случай отсутствующей записи и выполнение с рабочими правами доступа."));
      }
    }

    #endregion
  }

}
