using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using IsblCheck.Context.Application;
using IsblCheck.Context.Development;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context.Application;
using IsblCheck.Core.Context.Development;
using IsblCheck.Core.Reports;

namespace IsblCheck.Services
{
  /// <summary>
  /// Сервис проверки кода.
  /// </summary>
  public class CodeCheckerService : ICodeCheckerService
  {
    /// <summary>
    /// Чекер.
    /// </summary>
    private readonly ICodeChecker codeChecker = new CodeChecker();
    private readonly object aiLock = new object();
    private CancellationTokenSource aiCancellation;
    public event Action<string> AiStatusChanged;
    public void CancelAiReview()
    {
      lock (this.aiLock) this.aiCancellation?.Cancel();
      this.AiStatusChanged?.Invoke("Отмена анализа ИИ…");
    }

    /// <summary>
    /// Фабрика контекста приложения.
    /// </summary>
    private readonly IApplicationContextFactory applicationContextFactory = new ApplicationContextFactory();

    /// <summary>
    /// Фабрика контекста разработки.
    /// </summary>
    private readonly IDevelopmentContextFactory developmentContextFactory = new DevelopmentContextFactory();

    #region ICodeCheckerService

    public ICodeChecker CodeChecker => this.codeChecker;

    /// <summary>
    /// Очистить провайдеры разработки.
    /// </summary>
    public void ClearProviders()
    {
      this.developmentContextFactory.Providers.Clear();
    }

    /// <summary>
    /// Добавить провайдер разработки.
    /// </summary>
    /// <param name="provider">Провайдер разработки.</param>
    public void AddProvider(IDevelopmentContextProvider provider)
    {
      this.developmentContextFactory.Providers.Add(provider);
      this.codeChecker.ContextManager.Refresh();
    }

    /// <summary>
    /// Проверить документ.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="progress">Прогресс выполнения.</param>
    /// <returns>Отчет проверки.</returns>
    public Task<IReport> Check(IDocument document, IProgress<int> progress)
    {
      return this.Check(new List<IDocument> { document }, progress);
    }

    /// <summary>
    /// Проверить документы.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <param name="progress">Прогессы выполнения.</param>
    /// <returns>Отчет проверки.</returns>
    public async Task<IReport> Check(IEnumerable<IDocument> documents, IProgress<int> progress)
    {
      var aiConfiguration = AiSettings.Capture();
      var report = await this.codeChecker.Check(documents, progress);
      if (!aiConfiguration.Enabled) return report;
      CancellationTokenSource cancellation;
      lock (this.aiLock)
      {
        if (this.aiCancellation != null)
        {
          AiSettings.Set(report, "ИИ не запускался: уже выполняется другой запрос. Статическая проверка завершена.");
          return report;
        }
        cancellation = new CancellationTokenSource();
        this.aiCancellation = cancellation;
      }
      this.AiStatusChanged?.Invoke("Подготовка анализа ИИ…");
      var aiProgress = new Progress<(int Current, int Total)>(value => {
        lock (this.aiLock) if (!ReferenceEquals(this.aiCancellation, cancellation)) return;
        this.AiStatusChanged?.Invoke($"ИИ: документ {value.Current} из {value.Total}");
      });
      try { await AiSettings.ReviewAsync(report, configuration: aiConfiguration, cancellationToken: cancellation.Token, progress: aiProgress); }
      finally
      {
        lock (this.aiLock) if (ReferenceEquals(this.aiCancellation, cancellation)) this.aiCancellation = null;
        cancellation.Dispose();
        this.AiStatusChanged?.Invoke(null);
      }
      return report;
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
          lock (this.aiLock) this.aiCancellation?.Cancel();
          this.codeChecker.Dispose();
        }
        disposedValue = true;
      }
    }

    public void Dispose()
    {
      Dispose(true);
    }

    #endregion

    /// <summary>
    /// Конструктор.
    /// </summary>
    public CodeCheckerService()
    {
      this.codeChecker.ContextManager.Load(applicationContextFactory);
      this.codeChecker.ContextManager.Load(developmentContextFactory);

      var rulesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Rules");
      this.codeChecker.RuleManager.LoadLibraries(rulesPath);
    }
  }
}
