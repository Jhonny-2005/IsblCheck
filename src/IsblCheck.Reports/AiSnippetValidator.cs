using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IsblCheck.BaseRules.Functions;
using IsblCheck.Core.Checker;

namespace IsblCheck.Reports
{
  /// <summary>Checks only fenced examples, never executes or applies generated code.</summary>
  internal static class AiSnippetValidator
  {
    private static readonly Regex Fenced = new Regex(@"(?ms)^\s*```(?<language>[^\r\n`]*)\r?\n(?<code>.*?)^\s*```\s*$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    public static async Task<string> DescribeAsync(string response, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var sample = response.Length > 200_000 ? response.Substring(0, 200_000) : response;
      MatchCollection matches;
      try
      {
        matches = Fenced.Matches(sample);
        _ = matches.Count;
      }
      catch (RegexMatchTimeoutException)
      {
        return "Проверка примеров ИИ не завершена из-за сложности текста; примеры требуют проверки разработчиком.";
      }
      if (matches.Count == 0)
        return (sample.Length != response.Length ? "Проверено только начало ответа (200 000 символов); остальная часть не проверялась. " : "") +
          "Синтаксическая проверка примеров ИИ: блоков ```isbl не найдено или блок незакрыт; код вне таких блоков не проверялся. Поведение и рекомендации требуют проверки разработчиком.";
      var result = new StringBuilder("Проверка примеров ИИ (синтаксис и узкие проверки F026/F027; не подтверждает семантику, API и выполнение):\n");
      if (sample.Length != response.Length) result.AppendLine("Проверяется только начало ответа (200 000 символов); остальная часть не проверялась.");
      var number = 0;
      foreach (Match match in matches)
      {
        cancellationToken.ThrowIfCancellationRequested();
        number++;
        if (number > 8) { result.AppendLine("Остальные блоки не проверены: превышен лимит восьми блоков."); break; }
        var language = match.Groups["language"].Value.Trim();
        var source = match.Groups["code"].Value;
        if (!language.Equals("isbl", StringComparison.OrdinalIgnoreCase))
        { result.AppendLine($"Блок {number}: не ISBL, синтаксис не проверялся."); continue; }
        if (source.Length > 8000 || string.IsNullOrWhiteSpace(source))
        { result.AppendLine($"Блок {number}: не проверен (лимит примеров, размер или пустой текст)."); continue; }
        try
        {
          using var checker = new CodeChecker();
          var document = new Document("Пример ИИ", source, allowExpression: true);
          var report = await checker.Check(new[] { document }).ConfigureAwait(false);
          var errors = report.Messages.Where(m => m.Code == "ISBL0001").ToList();
          if (errors.Count == 0)
          {
            result.AppendLine($"Блок {number}: синтаксических ошибок грамматики не обнаружено; корректность исполнения не подтверждена.");
            // Reuse the same narrow rules as the main analyzer. Metadata-dependent
            // rules would produce misleading results for a standalone replacement example.
            new DivisionByZeroRule().Apply(report, document, checker.ContextManager.Context);
            new InvalidExitForRule().Apply(report, document, checker.ContextManager.Context);
            foreach (var finding in report.Messages.Take(5))
              result.AppendLine(finding.Code == "F026"
                ? $"Блок {number}, строка {finding.Position.Line}: F026 — EXITFOR вне FOREACH в самостоятельном примере. Если фрагмент находится внутри исходного FOREACH, проверьте место вставки; иначе исправьте пример."
                : $"Блок {number}, строка {finding.Position.Line}: {finding.Code} — {finding.Description} Пример требует исправления, если это выражение будет исполняться.");
          }
          else result.AppendLine($"Блок {number}: возможная синтаксическая ошибка в строке {errors[0].Position.Line}; не применяйте без исправления и проверки.");
        }
        catch (Exception e) when (e is not OutOfMemoryException && e is not OperationCanceledException)
        { result.AppendLine($"Блок {number}: проверка синтаксиса не выполнена."); }
      }
      cancellationToken.ThrowIfCancellationRequested();
      if (sample.Split("```", StringSplitOptions.None).Length % 2 == 0)
        result.AppendLine("Есть незакрытый блок; его содержимое не проверено.");
      return result.ToString().TrimEnd();
    }
  }
}
