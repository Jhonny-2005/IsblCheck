using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using IsblCheck.Core.Context;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;
using IMetadataAvailability = IsblCheck.Core.Context.Development.IMetadataAvailability;

namespace IsblCheck.Core.Checker
{
  /// <summary>
  /// Контролер проверки исходного кода.
  /// </summary>
  public class CodeChecker : ICodeChecker
  {
    #region Поля и свойства

    /// <summary>
    /// Логгер.
    /// </summary>
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    private readonly ContextManager contextManager = new ContextManager();
    private readonly RuleManager ruleManager = new RuleManager();
    private readonly ReportManager reportManager = new ReportManager();

    #endregion

    #region ICodeChecker

    /// <summary>
    /// Менеджер контекста.
    /// </summary>
    public IContextManager ContextManager => this.contextManager;

    /// <summary>
    /// Менеджер правил.
    /// </summary>
    public IRuleManager RuleManager => this.ruleManager;

    /// <summary>
    /// Менеджер отчетов.
    /// </summary>
    public IReportManager ReportManager => this.reportManager;

    public bool IncludeOptionalRules { get; set; }

    /// <summary>
    /// Выполнить проверку.
    /// </summary>
    /// <param name="documents">Проверяемые документы.</param>
    /// <returns>Отчет о проверке.</returns>
    public Task<IReport> Check(IEnumerable<IDocument> documents)
    {
      return this.Check(documents, null);
    }

    /// <summary>
    /// Выполнить проверку.
    /// </summary>
    /// <param name="documents">Проверяемые документы.</param>
    /// <param name="progress">Объект отображения прогресса выполнения.</param>
    /// <returns>Отчет о проверке.</returns>
    public Task<IReport> Check(IEnumerable<IDocument> documents, IProgress<int> progress)
    {
      if (documents == null)
        throw new ArgumentNullException(nameof(documents));

      // Capture selection before the asynchronous parallel check starts.
      var includeOptionalRules = this.IncludeOptionalRules;
      var rules = this.RuleManager.GetAllRules().Where(r => includeOptionalRules || r is not IOptionalRule).ToList();
      var report = this.ReportManager.Create();
      var context = new AnalysisCatalog(this.ContextManager.Context);
      if (report is Report concreteReport)
      {
        concreteReport.OptionalRulesIncluded = includeOptionalRules;
        if (context.Development is IMetadataAvailability metadata)
        {
          concreteReport.CanValidateReferences = metadata.CanValidateReferences;
          concreteReport.CanValidateLocalization = metadata.CanValidateLocalization;
        }
      }
      var counter = new ProgressCounter(progress);

      return Task.Run(() =>
      {
        var batch = documents.ToArray();
        Parallel.ForEach(batch, document =>
        {
          if (document == null)
          {
            log.Error("Проверка пропущена: список содержит пустой документ.");
            counter.Increment();
            return;
          }

          if (report is Report documentReport) documentReport.AddDocument(document);

          if (document is WorkflowSchemeDocument)
          {
            counter.Increment();
            return;
          }

          if (document is Document parsedDocument)
          {
            parsedDocument.GetSyntaxTree();
            foreach (var diagnostic in parsedDocument.ParseDiagnostics)
              report.Add(Severity.Error, "ISBL0001", diagnostic.Message, document, diagnostic.Position,
                new DiagnosticRecommendation(
                  "Начните с первого сообщения в этом вычислении. Сверьте выделенный фрагмент и предыдущую строку с синтаксисом classic ISBL в целевой версии, включая кавычки, разделители аргументов и закрытие блоков.",
                  "Проверьте текст в IS-Builder и повторите анализ. Пока синтаксическая ошибка остаётся, прикладные правила для этого вычисления пропускаются."));
            if (parsedDocument.ParseDiagnostics.Any())
            {
              counter.Increment();
              return;
            }
          }

          foreach (var rule in rules)
          {
            try
            {
              rule.Apply(report, document, context);
            }
            catch (Exception ex)
            {
              log.Error(ex, $"Правило {rule.GetType().Name} завершилось с ошибкой для документа {document.Name}.");
              report.Add(Severity.Error, "ISBL9999",
                $"Не удалось выполнить правило {rule.GetType().Name}. Подробности записаны в журнал.", document,
                new TextPosition { StartIndex = 0, EndIndex = 0, Line = 1, Column = 0 },
                new DiagnosticRecommendation(
                  "Проверьте запись об исключении в журнале анализатора. Зафиксируйте версию инструмента, имя правила и минимальный фрагмент, который приводит к сбою.",
                  "После устранения сбоя повторите анализ и убедитесь, что правило завершилось. Остальные сообщения относятся к правилам, которые удалось выполнить."));
            }
          }
          counter.Increment();
        });
        foreach (var route in batch.OfType<Document>().Where(document => document.WorkflowRoute != null)
          .GroupBy(document => document.WorkflowRoute))
        {
          try { WorkflowRouteAnalysis.Check(route.Key, route, report); }
          catch (Exception ex)
          {
            log.Error(ex, $"Не удалось проанализировать схему маршрута {route.Key.Name}.");
            report.Add(Severity.Error, "ISBL9999", "Не удалось проанализировать схему маршрута. Подробности записаны в журнал.",
              route.First(), WorkflowSchemeDocument.Start, new DiagnosticRecommendation(
                "Проверьте запись журнала и сохраните копию схемы маршрута для воспроизведения сбоя.",
                "Повторите проверку маршрута и убедитесь, что анализ схемы завершился."));
          }
        }
        return report;
      });
    }

    #endregion

    #region IDisposable Support

    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          this.ruleManager.Dispose();
        }
        disposedValue = true;
      }
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    ~CodeChecker()
    {
      Dispose(false);
    }

    #endregion
  }
}
