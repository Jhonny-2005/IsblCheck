using System;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using IsblCheck.BaseRules.Properties;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Other
{
  internal class TodoDoneCommentsRule : AbstractRule
  {
    private const string Code = "M001";
    public static IRuleInfo Info { get; } = new RuleInfo(nameof(TodoDoneCommentsRule), Resources.TodoDoneCommentsRuleDescription);
    private static readonly Regex Marker = new Regex(@"\b(?:todo|done)\b",
      RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var lexer = new IsblLexer(new AntlrInputStream(document.Text));
      lexer.RemoveErrorListeners();
      var tokens = new CommonTokenStream(lexer);
      tokens.Fill();
      foreach (var token in tokens.GetTokens())
      {
        if (token.Type != IsblLexer.LINE_COMMENT && token.Type != IsblLexer.BLOCK_COMMENT) continue;
        var body = token.Text.Substring(2);
        if (!Marker.IsMatch(body)) continue;
        var comment = token.Text.Trim();
        report.Add(Severity.Information, Code, $"Комментарий содержит отметку TODO/DONE: {comment.Substring(0, Math.Min(comment.Length, 50))}.",
          document, token.ToTextPosition(), new DiagnosticRecommendation(
            "Сверьте отметку с состоянием задачи. Перенесите актуальную незавершённую работу в список задач, а устаревшую отметку удалите или уточните.",
            "Убедитесь, что связь с задачей и причина решения остались доступны разработчикам."));
      }
    }
  }
}
