using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Other
{
  /// <summary>
  /// Правило обнаружения закомментированных блоков кода.
  /// </summary>
  internal class CommentedCodeBlockRule : AbstractRule
  {
    private const string Code = "M002";
    private const int MinCommentLines = 5;

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(CommentedCodeBlockRule).Name,
        "Правило обнаружения крупных блоков закомментированного кода."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly Regex CommentLineRegex = new Regex(
      @"^\s*//\s*(.+)$",
      RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex CodePatternRegex = new Regex(
      @"(if |then|endif|else|while|endwhile|foreach|endforeach|try|except|endexcept|function|procedure|begin|end|x =|y =|return|exit)",
      RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var text = document.Text;
      var matches = CommentLineRegex.Matches(text);

      int consecutiveCodeComments = 0;
      int startIndex = 0;

      foreach (Match match in matches)
      {
        var commentBody = match.Groups[1].Value.Trim();
        if (CodePatternRegex.IsMatch(commentBody))
        {
          if (consecutiveCodeComments == 0)
          {
            startIndex = match.Index;
          }
          consecutiveCodeComments++;
        }
        else
        {
          if (consecutiveCodeComments >= MinCommentLines)
          {
            report.AddInformation(Code,
              $"Обнаружен блок закомментированного кода ({consecutiveCodeComments} строк). Рекомендуется удалить неиспользуемый код.",
              document, new TextPosition { StartIndex = startIndex, EndIndex = startIndex + consecutiveCodeComments * 50 });
          }
          consecutiveCodeComments = 0;
        }
      }

      if (consecutiveCodeComments >= MinCommentLines)
      {
        report.AddInformation(Code,
          $"Обнаружен блок закомментированного кода ({consecutiveCodeComments} строк). Рекомендуется удалить неиспользуемый код.",
          document, new TextPosition { StartIndex = startIndex, EndIndex = startIndex + consecutiveCodeComments * 50 });
      }
    }
  }
}
