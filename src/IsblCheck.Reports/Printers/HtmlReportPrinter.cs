using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using IsblCheck.Core.Reports;

namespace IsblCheck.Reports.Printers
{
  /// <summary>Self-contained report. All imported text is HTML-encoded.</summary>
  public sealed class HtmlReportPrinter : IReportPrinter
  {
    private readonly string filePath;
    public string AiRecommendations { get; set; }
    public HtmlReportPrinter(string filePath) => this.filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    public void Print(IReport report)
    {
      var items = report.Messages.ByImportance().ToList();
      var coverage = report as IReportCoverage;
      var documents = coverage?.Documents.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase).ThenBy(d => d.Path, StringComparer.Ordinal).ToList();
      var syntaxFailures = items.Where(m => m.Code == "ISBL0001").Select(m => m.Document).ToHashSet();
      var incompleteRuleRuns = items.Where(m => m.Code == "ISBL9999").Select(m => m.Document).ToHashSet();
      static string Escape(string s) => WebUtility.HtmlEncode(s ?? string.Empty);
      var html = new StringBuilder();
      html.Append("<!doctype html><html lang=\"ru\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>ISBL Check — отчёт</title><style>");
      html.Append("body{font:15px system-ui,sans-serif;background:#f3f6fc;color:#1d2940;margin:0}main{max-width:1400px;margin:auto;padding:32px}header{background:#162342;color:white;border-radius:18px;padding:28px}h1{margin:0 0 8px}.metrics{display:flex;gap:12px;flex-wrap:wrap;margin:22px 0}.metric{background:white;border:1px solid #dfe6f1;border-radius:12px;padding:15px 22px;min-width:120px}.metric strong{display:block;font-size:24px}.bar{display:flex;gap:10px;margin:18px 0}input,select{padding:10px;border:1px solid #cbd5e4;border-radius:8px;font:inherit}input{flex:1}table{width:100%;border-collapse:collapse;background:white;border-radius:12px;overflow:hidden}th,td{text-align:left;padding:12px;border-bottom:1px solid #edf0f6;vertical-align:top}th{background:#e9eef8}td:first-child{font-weight:700;white-space:nowrap}.Error{color:#b42318}.Warning{color:#a15c00}.Information{color:#17618a}.empty{padding:24px;color:#607087}footer{color:#607087;margin-top:20px} @media(max-width:700px){main{padding:12px}table{display:block;overflow:auto}.bar{flex-wrap:wrap}}");
      html.Append("</style></head><body><main><header><h1>Отчёт ISBL Check</h1><div>Результаты статического анализа · ").Append(Escape(DateTime.Now.ToString("yyyy-MM-dd HH:mm"))).Append("</div></header><div class=\"metrics\">");
      foreach (var severity in new[]{Severity.Error,Severity.Warning,Severity.Information}) html.Append("<div class=\"metric ").Append(severity).Append("\"><strong>").Append(items.Count(x=>x.Severity==severity)).Append("</strong>").Append(severity==Severity.Error?"Ошибки":severity==Severity.Warning?"Предупреждения":"Информация").Append("</div>");
      if (report is IReportRuleSelection selection && selection.OptionalRulesIncluded.HasValue)
        html.Append("<div class=\"metric\">F016: ").Append(selection.OptionalRulesIncluded.Value ? "включено" : "отключено").Append("<small> · подсказки о читаемости</small></div>");
      html.Append("</div>");
      if (coverage?.CanValidateReferences == false || coverage?.CanValidateLocalization == false)
      {
        html.Append("<p>Проверка зависимостей ограничена. Источник не содержит полного каталога ");
        html.Append(coverage.CanValidateReferences == false && coverage.CanValidateLocalization == false
          ? "справочников и строк локализации" : coverage.CanValidateReferences == false ? "справочников" : "строк локализации");
        html.Append(".</p>");
      }
      if (syntaxFailures.Count > 0 || incompleteRuleRuns.Count > 0)
        html.Append("<p>Документы с пропуском прикладных правил из-за синтаксиса — ").Append(syntaxFailures.Count)
          .Append(". Документы со сбоем отдельных правил — ").Append(incompleteRuleRuns.Count).Append(".</p>");
      html.Append("<div class=\"bar\"><input id=\"q\" type=\"search\" placeholder=\"Поиск по коду, сообщению и документу\" aria-label=\"Поиск\"><select id=\"level\" aria-label=\"Уровень\"><option value=\"\">Все уровни</option><option value=\"Error\">Ошибки</option><option value=\"Warning\">Предупреждения</option><option value=\"Information\">Информация</option></select></div><table><thead><tr><th>Уровень / код</th><th>Описание</th><th>Документ</th><th>Строка: столбец*</th></tr></thead><tbody>");
      foreach (var m in items)
      {
        html.Append("<tr data-level=\"").Append(m.Severity).Append("\"><td class=\"").Append(m.Severity).Append("\">")
          .Append(m.Severity == Severity.Error ? "Ошибка" : m.Severity == Severity.Warning ? "Предупреждение" : "Информация")
          .Append(" · ").Append(Escape(m.Code)).Append("</td><td>").Append(Escape(m.Description));
        if (m.Recommendation is { } recommendation)
          html.Append("<details><summary>Как исправить</summary><p>").Append(Escape(recommendation.Action))
            .Append("</p><p><strong>Проверка</strong> ").Append(Escape(recommendation.Verification)).Append("</p></details>");
        html.Append("</td><td>").Append(Escape(m.Document?.Name));
        if (!string.IsNullOrEmpty(m.Document?.ComponentName) || !string.IsNullOrEmpty(m.Document?.Path))
          html.Append("<br><small>").Append(Escape(string.Join(" · ", new[] { m.Document.ComponentName, m.Document.Path }
            .Where(value => !string.IsNullOrEmpty(value))))).Append("</small>");
        html.Append("</td><td>").Append(m.Position.Line).Append(':').Append(m.Position.Column).Append("</td></tr>");
      }
      html.Append("</tbody></table>");
      if (documents != null && documents.Count > 0)
      {
        var counts = items.Where(x => x.Document != null).GroupBy(x => x.Document).ToDictionary(g => g.Key, g => g.Count());
        html.Append("<details style=\"background:white;padding:20px;border-radius:12px;margin-top:24px\"><summary>Документы в прогоне: ").Append(documents.Count).Append("</summary><ul>");
        foreach (var doc in documents)
        {
          html.Append("<li>").Append(Escape(doc.Name)).Append(" · ").Append(Escape(doc.Path)).Append(" — замечаний: ")
            .Append(counts.TryGetValue(doc, out var count) ? count : 0);
          if (syntaxFailures.Contains(doc)) html.Append(" · правила пропущены из-за синтаксиса");
          if (incompleteRuleRuns.Contains(doc)) html.Append(" · отдельные правила завершились со сбоем");
          html.Append("</li>");
        }
        html.Append("</ul></details>");
      }
      if (!string.IsNullOrWhiteSpace(AiRecommendations)) html.Append("<section style=\"background:white;padding:20px;border-radius:12px;margin-top:24px\"><h2>Рекомендации ИИ</h2><p>Сформировано моделью для выбранных документов. Примеры и рекомендации требуют проверки разработчиком.</p><pre style=\"white-space:pre-wrap;font:inherit\">").Append(Escape(AiRecommendations)).Append("</pre></section>");
      html.Append("<p id=\"none\" class=\"empty\" hidden>Нет подходящих замечаний.</p><footer>* Номер строки начинается с 1, номер столбца — с 0. Рекомендации проверяйте в контексте разработки и целевой версии Directum.</footer></main><script>const q=document.querySelector('#q'),l=document.querySelector('#level'),rows=[...document.querySelectorAll('tbody tr')],none=document.querySelector('#none');function filter(){let n=0;for(const r of rows){const ok=r.textContent.toLocaleLowerCase().includes(q.value.toLocaleLowerCase())&&(!l.value||r.dataset.level===l.value);r.hidden=!ok;if(ok)n++}none.hidden=n!==0}q.addEventListener('input',filter);l.addEventListener('change',filter);filter()</script></body></html>");
      File.WriteAllText(filePath, html.ToString(), new UTF8Encoding(false));
    }
  }
}
