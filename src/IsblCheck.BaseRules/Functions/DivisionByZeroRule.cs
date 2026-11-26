using System.Linq;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  public class DivisionByZeroRule : AbstractRule
  {
    public static IRuleInfo Info { get; } = new RuleInfo(nameof(DivisionByZeroRule), "Деление на числовой литерал ноль.");
    private sealed class Listener : IsblBaseListener
    {
      public IReport Report;
      public IDocument Document;
      public override void EnterExpression(IsblParser.ExpressionContext context)
      {
        if (context.DIV() == null || context.expression().Length != 2) return;
        var denominator = context.expression(1);
        if (IsZero(denominator))
          Report.Add(Severity.Error, "F027", "Деление на нулевой числовой литерал.", Document, denominator.GetTextPosition(),
            new DiagnosticRecommendation(
              $"Сверьте формулу «{context.GetText()}» и подставьте предусмотренный формулой знаменатель вместо «{denominator.GetText()}». Если ноль допустим по предметной области, определите отдельный исход для этого случая до деления.",
              "Проверьте результат с ненулевым знаменателем и ожидаемую обработку нуля."));
      }
      private static bool IsZero(IsblParser.ExpressionContext expression)
      {
        var operand = expression.operand();
        if (operand?.unsignedNumber() != null)
          return operand.GetText().Split('e', 'E')[0].All(c => c == '0' || c == '.');
        if (operand?.expression() != null) return IsZero(operand.expression());
        return expression.expression().Length == 1 && (expression.PLUS() != null || expression.MINUS() != null)
          && IsZero(expression.expression(0));
      }
    }
    public override void Apply(IReport report, IDocument document, IContext context) =>
      ParseTreeWalker.Default.Walk(new Listener { Report = report, Document = document }, document.GetSyntaxTree());
  }
}
