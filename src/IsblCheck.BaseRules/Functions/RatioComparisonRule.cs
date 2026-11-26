using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  /// <summary>Highlights comparisons of two ratios for review of denominator contracts.</summary>
  public sealed class RatioComparisonRule : AbstractRule
  {
    public static IRuleInfo Info { get; } = new RuleInfo(nameof(RatioComparisonRule), "Сравнение отношений с переменными знаменателями.");
    private sealed class Listener : IsblBaseListener
    {
      public IReport Report;
      public IDocument Document;
      public IContext Context;
      private readonly HashSet<string> reportedDenominatorPairs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      public override void EnterExpression(IsblParser.ExpressionContext expression)
      {
        if (expression.expression().Length != 2 || (expression.EQ() == null && expression.NEQ() == null && expression.LT() == null && expression.LEQ() == null && expression.GT() == null && expression.GEQ() == null)) return;
        var left = Unwrap(expression.expression(0));
        var right = Unwrap(expression.expression(1));
        if (left?.DIV() == null || right?.DIV() == null || left.expression().Length != 2 || right.expression().Length != 2) return;
        var denominators = new[] { Variable(Unwrap(left.expression(1))), Variable(Unwrap(right.expression(1))) };
        if (denominators.Any(n => n == null)) return;
        var uncheckedNames = denominators.Where(n => LocalGuardAnalysis.MayChange(expression, n) ||
          !LocalGuardAnalysis.IsProtected(expression, n, (guard, truth) => ExcludesZero(guard, truth, n), Context))
          .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (uncheckedNames.Length == 0) return;
        // The same pair is often compared in both branches (for example, first
        // `>` and then `<`). Keep one message for that denominator contract.
        var pairKey = string.Join("|", denominators.OrderBy(n => n, StringComparer.OrdinalIgnoreCase));
        if (!reportedDenominatorPairs.Add(pairKey)) return;
        var names = string.Join(", ", uncheckedNames);
        Report.Add(Severity.Information, "F033", $"Для знаменателей {names} не установлена проверка на ноль перед сравнением отношений.",
          Document, expression.GetTextPosition(), new DiagnosticRecommendation(
            $"Уточните, могут ли {names} быть нулевыми, пустыми или нечисловыми. Если это допустимые входные данные, обработайте их до деления с результатом, предусмотренным расчётом.",
            "Сравните результаты на нормальных данных, нуле, NULL и отрицательных знаменателях, если такие значения допустимы. Проверьте, что порядок сравнения сохранился."));
      }
      private static bool ExcludesZero(IsblParser.ExpressionContext expression, bool truth, string name)
      {
        expression = Unwrap(expression);
        if (expression == null || LocalGuardAnalysis.MayChange(expression, name)) return false;
        if (expression.expression().Length != 2) return false;
        if (expression.AND() != null)
          return truth && (ExcludesZero(expression.expression(0), true, name) || ExcludesZero(expression.expression(1), true, name));
        if (expression.OR() != null)
          return !truth && (ExcludesZero(expression.expression(0), false, name) || ExcludesZero(expression.expression(1), false, name));
        var left = Unwrap(expression.expression(0)); var right = Unwrap(expression.expression(1));
        if (!((string.Equals(Variable(left), name, StringComparison.OrdinalIgnoreCase) && IsZero(right)) ||
              (string.Equals(Variable(right), name, StringComparison.OrdinalIgnoreCase) && IsZero(left)))) return false;
        return truth ? expression.NEQ() != null || expression.GT() != null || expression.LT() != null
          : expression.EQ() != null || expression.GEQ() != null || expression.LEQ() != null;
      }
      private static string Variable(IsblParser.ExpressionContext expr)
      {
        var operand = expr?.operand();
        return operand?.variable() != null && operand.indexer() == null && operand.invocationCall().Length == 0 && operand.POINTER() == null ? operand.variable().GetText() : null;
      }
      private static bool IsZero(IsblParser.ExpressionContext expr)
      {
        var number = expr?.operand()?.unsignedNumber()?.GetText();
        return number != null && number.Split('e', 'E')[0].All(c => c == '0' || c == '.');
      }
      private static IsblParser.ExpressionContext Unwrap(IsblParser.ExpressionContext expr)
      {
        while (expr?.operand()?.expression() != null) expr = expr.operand().expression();
        return expr;
      }
    }
    public override void Apply(IReport report, IDocument document, IContext context) =>
      ParseTreeWalker.Default.Walk(new Listener { Report = report, Document = document, Context = context }, document.GetSyntaxTree());
  }
}
