using System;
using System.Collections.Generic;
using System.Linq;
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
  class InteractiveWindowsOnEvents : AbstractRule
  {
    #region Константы

    /// <summary>
    /// Код правила.
    /// </summary>
    private const string Code = "J006";

    /// <summary>
    /// Префикс, с которого должен начинаться document.Path.
    /// </summary>
    private const string DocumentPathPrefix = "Events.";

    #endregion

    #region Вложенные классы

    /// <summary>
    /// Поиск использования интерактивных окон в событиях Сохранить до/после, форма-карточка скрытие.
    /// </summary>
    private class InteractiveWindowsOnEventsListener : IsblBaseListener
    {
      /// <summary>
      /// Список контекстов, попадающих под данное правило. 
      /// </summary>
      public List<IsblParser.FunctionContext> ContextWithFunctionErrors { get; } = new List<IsblParser.FunctionContext>();
      public IContext Context;

      /// <summary>
      /// Список интерактивных функций.
      /// </summary>
      private static readonly HashSet<string> FunctionsWithInteractiveWindow = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
      {
        "CreateCommentToAmendNoticeDialog",
        "CreateDialog",
        "CreateDualListDialog",
        "CreateEditor",
        "CreateFolderDialog",
        "CreateInputDialog",
        "CreateOpenDialog",
        "CreateRefusalReasonsRequestDialog",
        "CreateSaveDialog",
        "CreateTreeListSelectDialog",
        "CSelectSQL",
        "EditText",
        "InputDialog",
        "InputDialogEx",
        "MessageBox",
        "MessageBoxEx",
        "RMOpenDocumentActionsDialog",
        "RMShowAssignmentTreeInDialog",
        "SelectContactFromDialog",
        "SelectFolderDialog",
        "SelectFromDualList",
        "SelectionFromContactPersons",
        "SelectScriptFromList",
        "SelectSQL",
        "SetDialogFromParams",
        "ShowConstantsInputDialog",
        "ShowHintForFillReqs",
        "ShowDialog",
        "ShowMessage",
        "Ввод",
        "ВводМеню",
        "ВыборSQL",
        "ДиалогДаНет",
        "Меню",
        "МенюРасш",
        "Окно",
        "ПВыборSQL",
        "РедТекст",
        "РедактироватьРТФ"
      };

      /// <summary>
      /// Вход в выражение вызова функции.
      /// </summary>
      /// <param name="context">Контекст.</param>
      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var functionName = context.identifier().GetText();
        if (FunctionsWithInteractiveWindow.Contains(functionName) &&
            !Context.HasUserFunction(functionName))
        {
          ContextWithFunctionErrors.Add(context);
        }
      }
    }

    #endregion

    #region Поля

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(InteractiveWindowsOnEvents).Name, Resources.InteractiveWindowsOnEventsRuleDescription), true);

    /// <summary>
    /// Инфо правила.
    /// </summary>
    public static IRuleInfo Info => info.Value;

    /// <summary>
    /// События, проверяемые на наличие интерактивных функций.
    /// </summary>
    private static readonly Dictionary<EventType, string> CheckingEvents = new Dictionary<EventType, string>
    {
      { EventType.AfterUpdate, "Сохранение после" },
      { EventType.BeforeUpdate, "Сохранение до" },
      { EventType.AfterDelete, "Удаление после" },
      { EventType.BeforeDelete, "Удаление до" },
      { EventType.FormHide, "Форма-карточка. Скрытие" },
      { EventType.FormShow, "Форма-карточка. Показ" }
    };

    #endregion

    #region Методы

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var isReference = document.ComponentType == ComponentType.ReferenceType;
      var isDocumentCard = document.ComponentType == ComponentType.DocumentCardType;
      if ((!isReference && !isDocumentCard) ||
        string.IsNullOrEmpty(document.Path) ||
        !document.Path.StartsWith(DocumentPathPrefix, StringComparison.OrdinalIgnoreCase) ||
        !Enum.TryParse(document.Path.Substring(DocumentPathPrefix.Length), true, out EventType eventType) ||
        !CheckingEvents.TryGetValue(eventType, out var eventName) ||
        (isDocumentCard && eventType != EventType.BeforeUpdate && eventType != EventType.AfterUpdate))
      {
        return;
      }

      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new InteractiveWindowsOnEventsListener { Context = context };
      walker.Walk(listener, tree);

      var strictlyNumbered = isReference && context.Development.ReferenceTypes.Any(r =>
        string.Equals(r.Name, document.ComponentName, StringComparison.OrdinalIgnoreCase) &&
        r.NumerationMethod == NumerationMethod.StronglyAuto);
      var formEvent = eventType == EventType.FormHide || eventType == EventType.FormShow;
      foreach (var contextWithError in listener.ContextWithFunctionErrors)
      {
        var name = contextWithError.identifier().GetText();
        var description = $"Событие «{eventName}» содержит потенциально интерактивный вызов «{name}».";
        if (!formEvent)
          description += strictlyNumbered
            ? " Ожидание ответа пользователя может продлить блокировку в справочнике со строгой нумерацией."
            : " Если открывается окно, проверьте, удерживает ли событие блокировки.";
        var recommendation = formEvent
          ? new DiagnosticRecommendation(
            $"Уточните, нужно ли вызывать «{name}» автоматически при открытии или закрытии карточки. Если окно требуется по инициативе пользователя, перенесите вызов в отдельное действие формы.",
            "Проверьте открытие и закрытие карточки, включая отмену действия. Убедитесь, что дополнительные окна появляются только в предусмотренном сценарии.")
          : new DiagnosticRecommendation(
            $"Проверьте, показывает ли «{name}» окно в этой ветви. Если требуется ввод пользователя, получите его в клиентском действии до сохранения или удаления. Обработчик сохранения должен использовать уже полученные данные.",
            "Проверьте сохранение или удаление из формы и в пакетном режиме. В многопользовательском сценарии проверьте, что ожидание ввода не продлевает блокировку.");
        report.Add(formEvent ? Severity.Information : Severity.Warning, Code, description,
          document, contextWithError.identifier().GetTextPosition(), recommendation);
      }
    }

    #endregion
  }
}
