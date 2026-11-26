using System.Collections.Concurrent;
using System.Collections.Generic;
using IsblCheck.Core.Checker;

namespace IsblCheck.Core.Reports
{
  /// <summary>
  /// Отчет проверки.
  /// </summary>
  internal class Report : IReport, IReportCoverage, IReportRuleSelection
  {
    #region IReport

    /// <summary>
    /// Сообщения.
    /// </summary>
    public IEnumerable<IReportMessage> Messages => this.messages;
    public IEnumerable<IDocument> Documents => this.documents;
    public bool? CanValidateReferences { get; internal set; }
    public bool? CanValidateLocalization { get; internal set; }
    public bool? OptionalRulesIncluded { get; internal set; }
    internal void AddDocument(IDocument document) => this.documents.Add(document);

    /// <summary>
    /// Добавить ошибку.
    /// </summary>
    /// <param name="severity">Уровень.</param>
    /// <param name="code">Код.</param>
    /// <param name="description">Описание.</param>
    /// <param name="document">Документ.</param>
    /// <param name="position">Позиция.</param>
    public void Add(Severity severity, string code, string description, IDocument document, TextPosition position) =>
      this.Add(severity, code, description, document, position, null);

    public void Add(Severity severity, string code, string description, IDocument document, TextPosition position,
      DiagnosticRecommendation recommendation)
    {
      var reportMessage = new ReportMessage
      {
        Severity = severity,
        Code = code,
        Description = description,
        Document = document,
        Position = position,
        Recommendation = recommendation
      };
      this.messages.Add(reportMessage);
    }

    /// <summary>
    /// Добавить ошибку.
    /// </summary>
    /// <param name="code">Код.</param>
    /// <param name="description">Описание.</param>
    /// <param name="document">Документ.</param>
    /// <param name="position">Позиция.</param>
    public void AddError(string code, string description, IDocument document, TextPosition position)
    {
      this.Add(Severity.Error, code, description, document, position);
    }

    /// <summary>
    /// Добавить предупреждение.
    /// </summary>
    /// <param name="code">Код.</param>
    /// <param name="description">Описание.</param>
    /// <param name="document">Документ.</param>
    /// <param name="position">Позиция.</param>
    public void AddWarning(string code, string description, IDocument document, TextPosition position)
    {
      this.Add(Severity.Warning, code, description, document, position);
    }

    /// <summary>
    /// Добавить информацию.
    /// </summary>
    /// <param name="code">Код.</param>
    /// <param name="description">Описание.</param>
    /// <param name="document">Документ.</param>
    /// <param name="position">Позиция.</param>
    public void AddInformation(string code, string description, IDocument document, TextPosition position)
    {
      this.Add(Severity.Information, code, description, document, position);
    }

    /// <summary>
    /// Напечатать отчет.
    /// </summary>
    public void Print()
    {
      foreach (var printer in printers)
        printer.Print(this);
    }

    #endregion

    #region Поля и свойства

    /// <summary>
    /// Сообщения.
    /// </summary>
    private readonly ConcurrentBag<IReportMessage> messages = new ConcurrentBag<IReportMessage>();
    private readonly ConcurrentBag<IDocument> documents = new ConcurrentBag<IDocument>();

    /// <summary>
    /// Принтеры.
    /// </summary>
    private readonly IEnumerable<IReportPrinter> printers;

    #endregion

    #region Конструкторы

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="printers">Список принтеров.</param>
    internal Report(IEnumerable<IReportPrinter> printers)
    {
      this.printers = printers;
    }

    #endregion
  }
}
