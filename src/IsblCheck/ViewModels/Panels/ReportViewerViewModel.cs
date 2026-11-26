using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using IsblCheck.Common.Mvvm;
using IsblCheck.Common.Dialogs;
using IsblCheck.Common.Localization;
using IsblCheck.Common.Panels;
using IsblCheck.Core.Reports;
using IsblCheck.Reports.Printers;
using IsblCheck.Services;
using IsblCheck.Views;
using System.Windows.Controls;

namespace IsblCheck.ViewModels.Panels
{
  /// <summary>
  /// Список ошибок.
  /// </summary>
  public class ReportViewerViewModel : PanelViewModelBase
  {
    #region Константы

    /// <summary>
    /// Заголовок панели.
    /// </summary>
    private const string PanelTitle = "REPORT_VIEWER";

    /// <summary>
    /// Фильтр CSV файлов.
    /// </summary>
    private const string CsvFileFilter = "CSV File(*.csv)|*.csv";

    #endregion

    #region Поля и свойства

    /// <summary>
    /// Отчет.
    /// </summary>
    public IReport Report { get; private set; }

    /// <summary>
    /// Признак показа ошибок.
    /// </summary>
    public bool IsErrorsShown
    {
      get { return this.isErrorsShown; }
      set
      {
        if (this.isErrorsShown == value)
          return;
        this.isErrorsShown = value;
        this.RaisePropertyChanged();

        this.ReportMessages?.Refresh();
      }
    }
    private bool isErrorsShown = true;

    /// <summary>
    /// Признак показа предупреждений.
    /// </summary>
    public bool IsWarningsShown
    {
      get { return this.isWarningsShown; }
      set
      {
        if (this.isWarningsShown == value)
          return;
        this.isWarningsShown = value;
        this.RaisePropertyChanged();
        this.ReportMessages?.Refresh();
      }
    }
    private bool isWarningsShown = true;

    /// <summary>
    /// Признак показа информации.
    /// </summary>
    public bool IsInformationsShown
    {
      get { return this.isInformationsShown; }
      set
      {
        if (this.isInformationsShown == value)
          return;
        this.isInformationsShown = value;
        this.RaisePropertyChanged();

        this.ReportMessages?.Refresh();
      }
    }
    private bool isInformationsShown = true;

    /// <summary>
    /// Количество ошибок.
    /// </summary>
    public int ErrorCount
    {
      get
      {
        return this.Report?.Messages.Count(m => m.Severity == Severity.Error) ?? 0;
      }
    }

    /// <summary>
    /// Количество предупреждений.
    /// </summary>
    public int WarningCount
    {
      get
      {
        return this.Report?.Messages.Count(m => m.Severity == Severity.Warning) ?? 0;
      }
    }

    /// <summary>
    /// Количество информации.
    /// </summary>
    public int InformationCount
    {
      get
      {
        return this.Report?.Messages.Count(m => m.Severity == Severity.Information) ?? 0;
      }
    }

    /// <summary>
    /// Представление коллекции сообщений.
    /// </summary>
    public ICollectionView ReportMessages
    {
      get { return this.reportMessages; }
      set
      {
        if (this.reportMessages == value)
          return;
        this.reportMessages = value;
        this.RaisePropertyChanged();
      }
    }
    private ICollectionView reportMessages;

    private string searchText = string.Empty;
    public string SearchText
    {
      get => this.searchText;
      set
      {
        if (this.searchText == value) return;
        this.searchText = value ?? string.Empty;
        this.RaisePropertyChanged();
        this.ReportMessages?.Refresh();
      }
    }

    public string EmptyStateText => this.Report == null
      ? "Результаты появятся после проверки кода."
      : !this.Report.Messages.Any()
        ? "Проверка завершена. Замечаний не найдено."
        : "Нет результатов для выбранных фильтров. Измените поиск или уровень сообщений.";

    #endregion

    #region Команды

    /// <summary>
    /// Экспортировать как Csv.
    /// </summary>
    public ICommand ExportAsCsvCommand { get; private set; }
    public ICommand ExportAsHtmlCommand { get; private set; }
    public ICommand ReviewWithAiCommand { get; private set; }
    public ICommand ShowRecommendationCommand { get; private set; }
    public ICommand ShowAiCommand { get; private set; }
    private string aiRecommendations;
    private bool aiBusy;
    public string NumericHintsStatus => (this.Report as IReportRuleSelection)?.OptionalRulesIncluded switch
    { true => "F016: вкл.", false => "F016: выкл.", _ => "F016: неизвестно" };

    /// <summary>
    /// Команда показа сообещния отчета.
    /// </summary>
    public ICommand ShowReportMessageCommand { get; private set; }

    #endregion

    #region Методы

    /// <summary>
    /// Показать отчет.
    /// </summary>
    /// <param name="report">Отчет.</param>
    public void ShowReport(IReport report)
    {
      if (report == null) throw new ArgumentNullException(nameof(report));
      this.Report = report;
      this.aiRecommendations = IsblCheck.Services.AiSettings.Get(report);
      this.RaisePropertyChanged(nameof(NumericHintsStatus));

      this.ReportMessages = CollectionViewSource.GetDefaultView(this.Report.Messages.ByImportance().ToList());
      this.ReportMessages.Filter = this.IsReportMessageShow;
      this.RaisePropertyChanged(nameof(EmptyStateText));
      CommandManager.InvalidateRequerySuggested();

      this.RaisePropertyChanged("ErrorCount");
      this.RaisePropertyChanged("WarningCount");
      this.RaisePropertyChanged("InformationCount");
    }

    /// <summary>
    /// Проверить возможность экспорта в Csv.
    /// </summary>
    /// <returns>true, если возможность есть, иначе false.</returns>
    private bool CanExportAsCsv()
    {
      return this.Report != null && this.Report.Messages.Any();
    }

    /// <summary>
    /// Экспортировать в Csv формат.
    /// </summary>
    private void ExportAsCsv()
    {
      var settings = new SaveFileDialogSettings
      {
        AddExtension = true,
        DefaultExt = ".csv",
        Filter = CsvFileFilter,
        InitialDirectory = Environment.CurrentDirectory,
        CheckFileExists = false,
        OverwritePrompt = true
      };
      
      var result = this.viewService.ShowSaveFileDialog(PanelManager.Instance.PanelContainer, settings);
      if (!result.HasValue || !result.Value)
        return;

      var filename = settings.FileName;
      if (string.IsNullOrEmpty(filename))
        return;

      var csvPrinter = new CsvReportPrinter(filename);
      csvPrinter.Print(this.Report);
      var message = LocalizationManager.Instance.LocalizeString("EXPORT_SUCCESS");
      this.viewService.ShowMessageBox(message, icon: MessageBoxImage.Information);
    }

    private void ExportAsHtml()
    {
      var settings = new SaveFileDialogSettings { AddExtension = true, DefaultExt = ".html", Filter = "HTML (*.html)|*.html", InitialDirectory = Environment.CurrentDirectory, CheckFileExists = false, OverwritePrompt = true };
      if (this.viewService.ShowSaveFileDialog(PanelManager.Instance.PanelContainer, settings) != true || string.IsNullOrEmpty(settings.FileName)) return;
      new HtmlReportPrinter(settings.FileName) { AiRecommendations = this.aiRecommendations }.Print(this.Report);
      this.viewService.ShowMessageBox(LocalizationManager.Instance.LocalizeString("EXPORT_SUCCESS"), icon: MessageBoxImage.Information);
    }

    private async void ReviewWithAi(IReportMessage selected)
    {
      if (this.aiBusy || selected?.Document == null) return;
      if (!IsblCheck.Views.AiSettingsWindow.ShowSettings()) return;
      var endpoint = IsblCheck.Services.AiSettings.Endpoint;
      var model = IsblCheck.Services.AiSettings.Model;
      var key = IsblCheck.Services.AiSettings.ApiKey;
      var protocol = IsblCheck.Services.AiSettings.ChatCompletions;
      var timeout = IsblCheck.Services.AiSettings.TimeoutSeconds;
      this.aiBusy = true;
      CommandManager.InvalidateRequerySuggested();
      var sourceReport = this.Report;
      try
      {
        using var handler = new System.Net.Http.HttpClientHandler { AllowAutoRedirect = false };
        using var client = new System.Net.Http.HttpClient(handler) { Timeout = TimeSpan.FromSeconds(timeout), MaxResponseContentBufferSize = 1024 * 1024 };
        var findings = string.Join("\n", new[] { selected }.Concat(sourceReport.Messages.Where(m => m.Document == selected.Document && !ReferenceEquals(m, selected)).OrderBy(m => m.Severity).ThenBy(m => m.Position.Line)).Select(m => $"{m.Code} ({m.Severity.InRussian()}), строка {m.Position.Line}: {m.Description}"));
        var result = await new IsblCheck.Reports.AiReviewClient(client).ReviewAsync(endpoint, model, selected.Document.Text, findings,
          System.Threading.CancellationToken.None, protocol, key,
          $"Тип: {selected.Document.ComponentType}; компонент: {selected.Document.ComponentName}; путь: {selected.Document.Path}; контекстные переменные: {string.Join(", ", (selected.Document.ContextVariables ?? new System.Collections.Generic.List<string>()))}",
          $"{selected.Code} ({selected.Severity.InRussian()}), строка {selected.Position.Line}, столбец {selected.Position.Column}: {selected.Description}",
          selected.Recommendation);
        if (!ReferenceEquals(this.Report, sourceReport))
          throw new InvalidOperationException("Отчёт изменился во время запроса; повторите анализ для текущего отчёта.");
        this.aiRecommendations = (this.aiRecommendations ?? "") + $"\nДокумент: {selected.Document.Name}\nПроблема: {selected.Code}, строка {selected.Position.Line}, столбец {selected.Position.Column}: {selected.Description}\nМодель: {model}\n\n{result}\n";
        ShowAdvice("Рекомендации ИИ", result);
      }
      catch (Exception error) { NLog.LogManager.GetCurrentClassLogger().Warn("Ошибка запроса ИИ: {0}", error.GetType().Name); this.viewService.ShowMessageBox(selected.Document.Text?.Length > 1_000_000 ? "Вычисление превышает 1 000 000 символов; целиком отправить его невозможно." : "Не удалось получить ответ ИИ. Проверьте адрес, модель, ключ API и время ожидания.", icon: MessageBoxImage.Error); }
      finally { this.aiBusy = false; CommandManager.InvalidateRequerySuggested(); }
    }

    private static void ShowRecommendation(IReportMessage message)
    {
      if (message?.Recommendation is not { } recommendation) return;
      var text = $"{message.Document?.Name}\nСтрока {message.Position.Line}\n\n{message.Description}\n\nЧто изменить\n{recommendation.Action}\n\nКак проверить\n{recommendation.Verification}";
      ShowAdvice($"Как исправить · {message.Code}", text);
    }

    private static void ShowAdvice(string title, string text)
    {
      var content = new System.Windows.Controls.TextBox
      {
        Text = text,
        IsReadOnly = true,
        TextWrapping = TextWrapping.Wrap,
        AcceptsReturn = true,
        VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
        Margin = new Thickness(12, 12, 12, 4),
        VerticalContentAlignment = VerticalAlignment.Top,
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
      };
      var close = new Button
      {
        Content = "Закрыть", IsCancel = true,
        HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(12, 4, 12, 12)
      };
      DockPanel.SetDock(close, Dock.Bottom);
      var layout = new DockPanel();
      layout.Children.Add(close);
      layout.Children.Add(content);
      var window = new Window
      {
        Title = title, Content = layout, Width = 720, Height = 460, MinWidth = 300, MinHeight = 210,
        Owner = Application.Current.MainWindow, WindowStartupLocation = WindowStartupLocation.CenterOwner
      };
      window.SourceInitialized += DarkTitleBar.Apply;
      window.SourceInitialized += WindowSizing.FitDialog;
      window.ContentRendered += WindowSizing.KeepInitialPositionVisible;
      window.ShowDialog();
    }

    /// <summary>
    /// Фильтр сообщений.
    /// </summary>
    /// <param name="item">Сообщение.</param>
    /// <returns>true, если сообщение нужно показать, иначе false.</returns>
    private bool IsReportMessageShow(object item)
    {
      var reportMessage = item as IReportMessage;
      if (reportMessage == null) return false;
      var query = this.SearchText.Trim();
      if (query.Length > 0 &&
          (reportMessage.Code ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 &&
          (reportMessage.Description ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 &&
          (reportMessage.Document?.Name ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
        return false;
      var reportMessageShown = true;

      if (!this.IsErrorsShown && reportMessage.Severity == Severity.Error)
        reportMessageShown = false;

      if (!this.IsWarningsShown && reportMessage.Severity == Severity.Warning)
        reportMessageShown = false;

      if (!this.IsInformationsShown && reportMessage.Severity == Severity.Information)
        reportMessageShown = false;

      return reportMessageShown;
    }

    /// <summary>
    /// Показать сообщение отчета.
    /// </summary>
    /// <param name="message"></param>
    private void ShowReportMessage(IReportMessage message)
    {
      if (message?.Document == null) return;
      var documentViewer = PanelManager.Instance
        .GetPanels<DocumentViewerViewModel>()
        .FirstOrDefault(v => v.Document == message.Document);
      if (documentViewer == null)
      {
        documentViewer = PanelManager.Instance.CreatePanel<DocumentViewerViewModel>(false, message.Document);

        var messages = this.Report.Messages
          .Where(m => m.Document == message.Document);
        documentViewer.AddReportMessages(messages);
      }

      documentViewer.Show();
      documentViewer.CaretOffset = Math.Clamp(message.Position.StartIndex, 0, message.Document.Text.Length);
    }

    #endregion

    #region Конструкторы

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="viewService">Сервис представлений.</param>
    public ReportViewerViewModel(IViewService viewService) : base(viewService)
    {
      this.TitleLocalizationKey = PanelTitle;
      this.CanClose = false;
      this.ReportMessages = CollectionViewSource.GetDefaultView(new IReportMessage[0]);

      this.ExportAsCsvCommand = new RelayCommand(this.ExportAsCsv, this.CanExportAsCsv);
      this.ExportAsHtmlCommand = new RelayCommand(this.ExportAsHtml, () => this.Report != null);
      this.ShowAiCommand = new RelayCommand(() => ShowAdvice("Рекомендации ИИ", this.aiRecommendations), () => !string.IsNullOrEmpty(this.aiRecommendations));
      this.ShowRecommendationCommand = new RelayCommand<IReportMessage>(ShowRecommendation, selected => selected?.Recommendation != null);
      this.ReviewWithAiCommand = new RelayCommand<IReportMessage>(this.ReviewWithAi, selected => !this.aiBusy && selected?.Document != null);
      this.ShowReportMessageCommand = new RelayCommand<IReportMessage>(this.ShowReportMessage);
    }

    #endregion
  }
}
