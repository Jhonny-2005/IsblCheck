using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
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
  /// Правило поиска использования несуществующего справочника.
  /// </summary>
  internal class UsingNotExistedReferenceRule : AbstractRule
  {
    #region Константы

    /// <summary>
    /// Код предупреждения.
    /// </summary>
    private const string Code = "F004";

    /// <summary>
    /// Обращение к справочнику через ReferenceFactory.
    /// </summary>
    private const string ReferenceFactoryMethod = "ReferenceFactory";

    /// <summary>
    /// Функция создания справочника.
    /// </summary>
    private const string CreateReferenceFunctionEn = "CreateReference";

    /// <summary>
    /// Функция создания справочника.
    /// </summary>
    private const string CreateReferenceFunctionRu = "СоздатьСправочник";

    /// <summary>
    /// Обращение к справочнику.
    /// </summary>
    private const string ReferencesFactoryVar = "References";

    #endregion

    #region Вложенные классы

    /// <summary>
    /// Поиск использования несуществующего справочника.
    /// </summary>
    private class NotExistReferenceListener : IsblBaseListener
    {
      /// <summary>
      /// Использование несуществующего справочника.
      /// </summary>
      public class UsingReferenceEntry
      {
        /// <summary>
        /// Имя справочника.
        /// </summary>
        public string ReferenceName { get; set; }
        /// <summary>
        /// Контекст использования.
        /// </summary>
        public ParserRuleContext Context { get; set; }
      }

      /// <summary>
      /// Главный контекст, из которого берутся справочники.
      /// </summary>
      private readonly IContext mainContext;

      /// <summary>
      /// Найденные места использования несуществующих справочников.
      /// </summary>
      public readonly List<UsingReferenceEntry> UsingReferenceEntries = new List<UsingReferenceEntry>();

      /// <summary>
      /// Список доступных прикладных справочников. 
      /// </summary>

      /// <summary>
      /// Конструктор.
      /// </summary>
      /// <param name="context">Главный контекст, который передается в правило.</param>
      public NotExistReferenceListener(IContext context)
      {
        this.mainContext = context;
      }

      /// <summary>
      /// Вход в выражение.
      /// </summary>
      /// <param name="context">Контекст.</param>
      public override void EnterInvocationCall(IsblParser.InvocationCallContext context)
      {
        if (context.identifier().GetText().Equals(ReferenceFactoryMethod, StringComparison.OrdinalIgnoreCase) &&
          context.parameterList() != null && context.parameterList().expression().Any())
        {
          // Обращение к справочнику через ReferencesFactory().
          var firstParam = ParameterListParser.GetExpressions(context.parameterList()).FirstOrDefault();
          CheckReferenceInExpressionExists(firstParam);
        }
        else if (context.Parent.GetChild(0) is IsblParser.VariableContext)
        {
          var variableName = context.Parent.GetChild(0).GetText().Trim('!');
          if (variableName.Equals(ReferencesFactoryVar, StringComparison.OrdinalIgnoreCase))
          {
            // Обращение к справочнику через предопределенную переменную References.

            // Проверить, что обращение к справочнику - это первый вызов после переменной References.
            IsblParser.InvocationCallContext firstInvocationCall = null;
            for (var i = 0; i < context.Parent.ChildCount; i++)
            {
              firstInvocationCall = context.Parent.GetChild(i) as IsblParser.InvocationCallContext;
              if (firstInvocationCall != null)
                break;
            }
            if (firstInvocationCall == context && context.parameterList() == null)
            {
              var referenceName = context.identifier().GetText();
              // Использование "устаревших" наименований справочников в случае с References не допускаются.
              if (!IsReferenceExists(referenceName, withOldReferences: false))
              {
                UsingReferenceEntries.Add(new UsingReferenceEntry
                {
                  Context = context.identifier(),
                  ReferenceName = referenceName
                });
              }
            }
          }
        }
      }

      /// <summary>
      /// Вход в выражение вызова функции.
      /// </summary>
      /// <param name="context">Контекст.</param>
      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var functionName = context.identifier().GetText();
        if ((functionName.Equals(CreateReferenceFunctionEn, StringComparison.OrdinalIgnoreCase) ||
          functionName.Equals(CreateReferenceFunctionRu, StringComparison.OrdinalIgnoreCase)) &&
          context.parameterList()?.expression().Any() == true &&
          !mainContext.HasUserFunction(functionName))
        {
          // Создание справочника через функцию CreateReference().
          var firstParam = ParameterListParser.GetExpressions(context.parameterList()).FirstOrDefault();
          CheckReferenceInExpressionExists(firstParam);
        }
      }

      /// <summary>
      /// Проверить, что справочник, указанный в выражении, существует.
      /// </summary>
      /// <param name="expression">Выражение с именем справочника.</param>
      private void CheckReferenceInExpressionExists(IsblParser.ExpressionContext expression)
      {
        while (expression?.operand()?.expression() is { } inner)
          expression = inner;
        var operand = expression?.operand();
        if (operand == null || operand.indexer() != null || operand.invocationCall().Length != 0 || operand.POINTER() != null) return;
        var stringContext = operand.@string();
        if (stringContext != null)
        {
          // Имя справочника в виде строки, например, "ПОЛ".
          var literal = stringContext.GetText();
          var referenceName = literal.Substring(1, literal.Length - 2);
          if (!IsReferenceExists(referenceName, withOldReferences: true))
          {
            UsingReferenceEntries.Add(new UsingReferenceEntry { Context = expression, ReferenceName = referenceName });
          }
        }
        else
        {
          var variableContext = operand.variable();
          if (variableContext != null)
          {
            var variableName = variableContext.GetText();
            if (mainContext.Application.IsExistConstant(variableName))
            {
              // Имя справочника в виде системной константы, например, SYSREF_USERS.
              var referenceName = mainContext.Application.GetConstantValue(variableName);
              if (!IsReferenceExists(referenceName, withOldReferences: true))
              {
                UsingReferenceEntries.Add(new UsingReferenceEntry { Context = expression, ReferenceName = referenceName });
              }
            }
          }
        }
      }

      /// <summary>
      /// Проверить, существует ли такой справочнник.
      /// </summary>
      /// <param name="name">Название справочника.</param>
      /// <param name="withOldReferences">Проверять ли старые названия справочников.</param>
      /// <returns>Возвращает false, если справочника не существует.</returns>
      private bool IsReferenceExists(string name, bool withOldReferences)
      {
        return mainContext.HasDevelopmentReference(name) ||
          mainContext.Application.IsExistsSysReference(name, withOldReferences);
      }
    }

    #endregion

    #region Поля

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(UsingNotExistedReferenceRule).Name, Resources.UsingNotExistedReferenceRuleDescription), true);

    /// <summary>
    /// Инфо правила.
    /// </summary>
    public static IRuleInfo Info => info.Value;

    #endregion

    #region Методы

    /// <summary>
    /// Убрать из названия спецсимволы.
    /// </summary>
    /// <param name="name">Входное имя справочника.</param>
    /// <returns>Выровненное имя справочника.</returns>
    public static string TrimReferenceName(string name)
    {
      return name.Trim('\'', '\"');
    }

    /// <summary>
    /// Применить правило.
    /// </summary>
    /// <param name="report">Отчет.</param>
    /// <param name="document">Документ.</param>
    /// <param name="context">Контекст.</param>
    public override void Apply(IReport report, IDocument document, IContext context)
    {
      if (context.Development is IsblCheck.Core.Context.Development.IMetadataAvailability metadata && !metadata.CanValidateReferences) return;
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new NotExistReferenceListener(context);
      walker.Walk(listener, tree);

      foreach (var entry in listener.UsingReferenceEntries)
      {
        report.Add(Severity.Warning, Code,
          $"Справочник \"{entry.ReferenceName}\" не найден в загруженном контексте. Проверьте полноту разработки и наличие справочника в установленной системе.",
          document, entry.Context.GetTextPosition(), new DiagnosticRecommendation(
            $"Сверьте код типа справочника «{entry.ReferenceName}» в целевой системе. Если ссылка верна, добавьте зависимость в комплект переноса или загрузите полный каталог для анализа.",
            "Повторите анализ с целевыми метаданными и проверьте открытие справочника под учётной записью, которая выполняет вычисление."));
      }
    }

    #endregion
  }
}

