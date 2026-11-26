using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using IsblCheck.BaseRules.Properties;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  public class MagicNumbersRule : AbstractRule, IOptionalRule
  {
    private const string Code = "F016";
    private static readonly HashSet<string> AllowedNumbers = new HashSet<string> { "0", "1", "-1", "0.0", "1.0" };

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(MagicNumbersRule).Name, "Необязательная подсказка о числовом литерале."), true);

    public static IRuleInfo Info => info.Value;

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var text = document.Text;
      var stream = new AntlrInputStream(text);
      var lexer = new IsblLexer(stream);
      lexer.RemoveErrorListeners();
      var tokenStream = new CommonTokenStream(lexer);
      tokenStream.Fill();

      foreach (var token in tokenStream.GetTokens())
      {
        if (token.Type == IsblParser.NUMBERLITERAL)
        {
          var value = token.Text;
          var zeroMantissa = value.Split('e', 'E')[0].All(c => c == '0' || c == '.');
          var numericOne = decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && number == 1;
          if (!AllowedNumbers.Contains(value) && !zeroMantissa && !numericOne)
          {
            var line = token.Line;
            var col = token.Column;
            report.Add(Severity.Information, Code, $"Числовой литерал {value}. Подсказка о читаемости.",
              document, new TextPosition { Line = line, Column = col, StartIndex = token.StartIndex, EndIndex = token.StopIndex },
              new DiagnosticRecommendation(
                $"Если {value} задаёт прикладное ограничение или порог, поясните его смысл и единицы измерения в комментарии либо присвойте его переменной с понятным именем. Обычный индекс или понятный коэффициент можно оставить.",
                "Проверьте, что значение, единица измерения и поведение на границе не изменились."));
          }
        }
      }
    }
  }
}
