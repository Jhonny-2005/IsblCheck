using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using IsblCheck.Core.Parser;

namespace IsblCheck.Reports
{
  /// <summary>Heuristic warnings only; never declares model advice correct or modifies it.</summary>
  internal static class AiAdviceGuard
  {
    private static bool Match(string text, string pattern) => Regex.IsMatch(text, pattern,
      RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    private static HashSet<string> Calls(string source)
    {
      var lexer = new IsblLexer(new AntlrInputStream(source ?? ""));
      lexer.RemoveErrorListeners();
      var tokens = new CommonTokenStream(lexer);
      tokens.Fill();
      var code = tokens.GetTokens().Where(t => t.Channel == TokenConstants.DefaultChannel).ToList();
      var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      for (var i = 0; i + 1 < code.Count; i++)
        if ((code[i].Type == IsblLexer.IDENTLITERAL || code[i].Type == IsblLexer.IN) && code[i + 1].Type == IsblLexer.L_PAREN)
          names.Add(code[i].Text);
      return names;
    }

    public static string Describe(string response, string source)
    {
      var warnings = new List<string>();
      var sample = response.Length > 200_000 ? response.Substring(0, 200_000) : response;
      try
      {
        if (Match(sample, @"(?m)^\s*const\s+[\p{L}_][\p{L}\p{N}_]*\s*="))
          warnings.Add("Обнаружено похожее на объявление Const: проверьте допустимость конструкции в classic ISBL; не применяйте без проверки.");
        if (Match(sample, @"(?is)(?:добав\w*|включ\w*)[^\r\n.]{0,100}\b(?:sum|avg|count|min|max)\s*\([^\r\n]{0,100}group\s+by|group\s+by[^\r\n.]{0,100}(?:добав\w*|включ\w*)[^\r\n.]{0,80}\b(?:sum|avg|count|min|max)\s*\("))
          warnings.Add("Проверьте совет включить агрегат в GROUP BY: агрегатные выражения туда не добавляются только из-за наличия в SELECT.");
        if (Match(sample, @"(?i)(?:экранир\w*|sanitize|escaping)[^\r\n.]{0,50}(?:через|посредством|с помощью)\s+loadstring|(?:использ\w*|примен\w*)\s+loadstring[^\r\n.]{0,50}(?:экранир\w*|sanitize|escaping)"))
          warnings.Add("Проверьте упоминание LoadString рядом с экранированием: это функция локализации, а не доказанный способ защиты SQL.");
        if (Match(sample, @"(?is)```isbl[^`]{0,12000}\bsql\s*\([^`]{0,3000}\?[^`]{0,3000}\)"))
          warnings.Add("SQL с плейсхолдером в примере ИИ требует проверки по справке API: во встроенном каталоге второй аргумент SQL — Alias, а не значение параметра.");
        if (Match(sample, @"(?i)\bsql\s*\(\s*(?:query|запрос)\s*[,;]\s*(?:param|параметр)\w*"))
          warnings.Add("Пример SQL(Query, Param...) не подтверждает передачу параметров: второй аргумент SQL в каталоге проекта — Alias; проверьте справку версии.");
        if (Match(sample, @"(?i)экранир\w*[^\r\n.]{0,100}(?:замен\w*|апостроф|кавыч)") || Match(sample, @"(?i)(?:замен\w*|апостроф)[^\r\n.]{0,100}экранир\w*"))
          warnings.Add("Замена кавычек сама по себе не подтверждает безопасность SQL: учитывайте тип, контекст и документированные возможности конкретного API.");
        if (Match(sample, @"(?i)sum\s*\([^\r\n]{0,100}(?:лучше|замен\w*)[^\r\n]{0,50}count"))
          warnings.Add("Замена SUM на COUNT может изменить результат: проверьте тип поля и требования расчёта.");
        // Generated examples may call APIs not present in this calculation. Their existence
        // cannot be checked from the selected document alone; flag rather than forbid them.
        var newCalls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var originalCalls = Calls(source);
        var fences = Regex.Matches(sample, @"(?ims)^\s*```isbl\s*\r?\n(?<code>.*?)^\s*```", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        foreach (Match fence in fences)
        {
          foreach (var name in Calls(fence.Groups["code"].Value))
          {
            if (!originalCalls.Contains(name)) newCalls.Add(name);
            if (newCalls.Count >= 5) break;
          }
          if (newCalls.Count >= 5) break;
        }
        if (newCalls.Count > 0)
          warnings.Add("Пример содержит вызовы, не найденные в исходном вычислении: " + string.Join(", ", newCalls) + ". Их существование, сигнатуры и побочные эффекты требуют проверки в контексте вашей системы.");
        if (Match(source ?? "", @"(?i)=\s*true\b") && Match(sample, @"(?i)=\s*1\b"))
          warnings.Add("Ответ содержит сравнение с 1 при исходном сравнении с True. Проверьте тип и преобразования: в classic ISBL числовое представление True описывается как -1, и замена не обязательно эквивалентна.");
        if (Match(sample, @"(?i)(?:удал\w*|убрат\w*|перенест\w*)[^\r\n.]{0,100}(?:nil|null)"))
          warnings.Add("Проверьте совет убрать или перенести присваивание nil/NULL: освобождение ссылки и жизненный цикл COM-объекта могут влиять на поведение.");
        var constantCalls = Regex.Matches(sample, @"(?i)\bGetConstant\s*\(\s*['""](?<name>[^'""\r\n]+)['""]", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        foreach (Match call in constantCalls)
        {
          if ((source ?? "").IndexOf(call.Groups["name"].Value, StringComparison.OrdinalIgnoreCase) < 0)
          {
            warnings.Add("Ответ предлагает имя константы, которого нет в исходном вычислении. Проверьте, что оно действительно существует в вашей системе; не подставляйте вымышленное значение.");
            break;
          }
        }
        if (Match(sample, @"(?i)(?:это|является)\s+(?:внешн\w*|пользовательск\w*)[^\r\n.]{0,30}значени"))
          warnings.Add("Проверьте утверждение о происхождении значения: получение параметра само по себе не доказывает управление им извне.");
        if (Match(sample, @"(?i)(?:sql[ -]?инъекци\w*[^\r\n.]{0,60}(?:доказан\w*|подтвержден\w*|точно)|(?:доказан\w*|подтвержден\w*)[^\r\n.]{0,60}sql[ -]?инъекци\w*)"))
          warnings.Add("Проверьте утверждение о доказанной SQL-инъекции: одной конкатенации недостаточно, нужен анализ происхождения данных и контекста.");
      }
      catch (RegexMatchTimeoutException) { warnings.Add("Эвристическая проверка ответа не завершена из-за сложности текста."); }
      if (response.Length > sample.Length) warnings.Add("Проверена только начальная часть большого ответа; остальная часть не анализировалась.");
      return warnings.Count == 0 ? "" : "Возможные проблемы ответа ИИ (эвристическая проверка, возможны ложные срабатывания):\n- " + string.Join("\n- ", warnings) + "\n\n";
    }
  }
}
