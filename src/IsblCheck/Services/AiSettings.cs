using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IsblCheck.Core.Reports;
using IsblCheck.Reports;

namespace IsblCheck.Services
{
  internal sealed record AiReviewConfiguration(bool Enabled, bool ChatCompletions, string Endpoint, string Model, string ApiKey, int MaximumDocuments, int TimeoutSeconds);

  // Session-only: API keys are never written to settings, reports or logs.
  internal static class AiSettings
  {
    public static bool Enabled = false;
    public static bool ChatCompletions = false;
    public static string Endpoint = "http://localhost:11434";
    public static string Model = "";
    public static string ApiKey = "";
    public static int MaximumDocuments = 5;
    public static int TimeoutSeconds = 300;
    public static AiReviewConfiguration Capture() => new(Enabled, ChatCompletions, Endpoint, Model, ApiKey, MaximumDocuments, TimeoutSeconds);
    private sealed class Result { public string Text; }
    private static readonly ConditionalWeakTable<IReport, Result> Results = new();
    public static string Get(IReport report) => report != null && Results.TryGetValue(report, out var result) ? result.Text : null;
    public static void Set(IReport report, string text) { Results.Remove(report); Results.Add(report, new Result { Text = text }); }
    public static async Task ReviewAsync(IReport report, HttpClient testClient = null, AiReviewConfiguration configuration = null, CancellationToken cancellationToken = default, IProgress<(int Current, int Total)> progress = null)
    {
      var documents = (report as IReportCoverage)?.Documents?.ToList();
      if (documents == null) { Set(report, "Охват ИИ недоступен: список проверенных документов не предоставлен."); return; }
      var settings = configuration ?? Capture();
      var endpoint = settings.Endpoint; var model = settings.Model; var key = settings.ApiKey; var protocol = settings.ChatCompletions; var limit = settings.MaximumDocuments;
      var messages = report.Messages.Where(m => m.Document != null).GroupBy(m => m.Document).ToDictionary(g => g.Key, g => g.ToList());
      // Deterministic selection: errors first, then warnings, then other and clean documents.
      var selected = documents.Where(d => d != null && !string.IsNullOrWhiteSpace(d.Text))
        .OrderBy(d => messages.TryGetValue(d, out var found) ? found.Min(m => (int)m.Severity) : int.MaxValue)
        .ThenBy(d => d.ComponentType).ThenBy(d => d.ComponentName, StringComparer.OrdinalIgnoreCase)
        .ThenBy(d => d.Path, StringComparer.OrdinalIgnoreCase).ThenBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
        .Take(limit).ToList();
      var text = new StringBuilder();
      var succeeded = 0; var failed = 0; var attempted = 0; var interrupted = false; var cancelled = false;
      progress?.Report((0, selected.Count));
      using var ownedHttp = testClient == null ? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds), MaxResponseContentBufferSize = 1024 * 1024 } : null;
      var client = new AiReviewClient(testClient ?? ownedHttp);
      foreach (var doc in selected)
      {
        if (cancellationToken.IsCancellationRequested) { cancelled = true; break; }
        attempted++;
        progress?.Report((attempted, selected.Count));
        text.Append("\n--- ").Append(doc.Name).Append(" ---\n");
        try
        {
          var diagnostics = messages.TryGetValue(doc, out var found) ? found : new List<IReportMessage>();
          var findings = string.Join("\n", diagnostics.OrderBy(m => m.Severity).ThenBy(m => m.Position.Line).Take(30).Select(m => $"{m.Code} ({m.Severity.InRussian()}), строка {m.Position.Line}, столбец {m.Position.Column}: {m.Description}"));
          var context = $"Тип: {doc.ComponentType}; компонент: {doc.ComponentName}; путь: {doc.Path}; документ: {doc.Name}; контекстные переменные: {string.Join(", ", doc.ContextVariables ?? new List<string>())}. Показано диагностик: {Math.Min(30, diagnostics.Count)} из {diagnostics.Count}.";
          text.AppendLine(await client.ReviewAsync(endpoint, model, doc.Text, findings, cancellationToken, protocol, key, context).ConfigureAwait(false));
          succeeded++;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
          failed++; cancelled = true;
          text.AppendLine("Запрос отменён пользователем; документ не проверен ИИ.");
          break;
        }
        catch (AiResponseLanguageException ex)
        {
          failed++;
          text.AppendLine(ex.Message);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
          failed++;
          NLog.LogManager.GetCurrentClassLogger().Warn("Ошибка запроса ИИ: {0}", ex.GetType().Name);
          // Do not include provider error text: it may contain the endpoint, key or source.
          text.AppendLine(doc.Text.Length > 1_000_000 ? "Пропущено: документ превышает 1 000 000 символов." : "Не удалось получить анализ документа. Проверьте подключение и параметры API.");
          if (ex is HttpRequestException || ex is TaskCanceledException) { interrupted = true; break; }
        }
      }
      text.Insert(0, $"Автоматический анализ ИИ. Документов в статической проверке: {documents.Count}; выбрано: {selected.Count}; запросов/попыток: {attempted}; успешно: {succeeded}; не удалось или пропущено: {failed}; без результата ИИ: {documents.Count - succeeded}. Лимит: {limit}. Модель: {model}.\n" + (cancelled ? "Анализ ИИ отменён пользователем; оставшиеся документы не проверены.\n" : interrupted ? "Анализ ИИ остановлен после ошибки подключения; оставшиеся документы не проверены.\n" : "") + "Документы с ошибками и предупреждениями выбираются первыми. Для исправления выбранной проблемы используйте отдельный запрос. Вывод ИИ требует проверки специалистом.\n");
      Set(report, text.ToString());
    }
  }
}
