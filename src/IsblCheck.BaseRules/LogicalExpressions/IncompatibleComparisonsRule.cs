using System;
using System.Collections.Generic;
using System.Globalization;
using Antlr4.Runtime.Tree;
using IsblCheck.BaseRules.Functions;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.LogicalExpressions
{
  public sealed class IncompatibleComparisonsRule : AbstractRule
  {
    public static IRuleInfo Info { get; } = new RuleInfo(nameof(IncompatibleComparisonsRule),
      "Сравнение одного значения с разными числами через OR или AND.");

    private sealed class Listener(IContext context) : IsblBaseListener
    {
      public readonly HashSet<string> LocalNames = new(StringComparer.OrdinalIgnoreCase);
      public readonly List<(IsblParser.ExpressionContext Condition, string Value, string Left, string Right, bool IsOr)> Findings = new();

      public override void EnterAssignStatement(IsblParser.AssignStatementContext assignment)
      {
        if (assignment.variable() != null && assignment.indexer() == null && assignment.invocationCall().Length == 0)
          LocalNames.Add(assignment.variable().GetText());
      }

      public override void EnterDeclareVariableStatement(IsblParser.DeclareVariableStatementContext declaration) =>
        LocalNames.Add(declaration.variable().GetText());

      public override void EnterForeachStatement(IsblParser.ForeachStatementContext loop) =>
        LocalNames.Add(loop.variable().GetText());

      public override void EnterOperand(IsblParser.OperandContext operand)
      {
        if (operand.POINTER() != null)
          LocalNames.Add(operand.variable().GetText());
      }

      public override void EnterExpression(IsblParser.ExpressionContext expression)
      {
        var isOr = expression.OR() != null;
        if (!isOr && expression.AND() == null) return;
        var left = ReadComparison(expression.expression(0), isOr);
        var right = ReadComparison(expression.expression(1), isOr);
        if (left == null || right == null || left.Value.Number == right.Value.Number ||
            !string.Equals(left.Value.Subject, right.Value.Subject, StringComparison.OrdinalIgnoreCase))
          return;
        Findings.Add((expression, left.Value.Subject, left.Value.Text, right.Value.Text, isOr));
      }

      private (string Subject, int Number, string Text)? ReadComparison(IsblParser.ExpressionContext expression, bool isOr)
      {
        expression = LocalQueryOrigin.Unwrap(expression);
        if (isOr ? expression.NEQ() == null : expression.EQ() == null) return null;
        var left = LocalQueryOrigin.Unwrap(expression.expression(0));
        var right = LocalQueryOrigin.Unwrap(expression.expression(1));
        return ReadPair(left, right) ?? ReadPair(right, left);
      }

      private (string Subject, int Number, string Text)? ReadPair(IsblParser.ExpressionContext subject, IsblParser.ExpressionContext constant)
      {
        var operand = subject.operand();
        var members = operand?.invocationCall();
        if (operand?.variable() == null || operand.indexer() != null || operand.POINTER() != null ||
            members.Length > 1 || (members.Length == 1 && members[0].L_PAREN() != null) ||
            context.Application?.IsExistEnumValue(operand.GetText()) == true)
          return null;
        if (int.TryParse(constant.GetText(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer))
          return (operand.GetText(), integer, constant.GetText());
        if (constant.operand()?.variable() is { } name && name.GetText() == constant.GetText() &&
            context.Application?.Enums?.TryGetValue(name.GetText(), out var entry) == true)
          return (operand.GetText(), entry, name.GetText());
        return null;
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var listener = new Listener(context);
      ParseTreeWalker.Default.Walk(listener, document.GetSyntaxTree());
      listener.LocalNames.UnionWith(document.ContextVariables);
      foreach (var (condition, value, left, right, isOr) in listener.Findings)
      {
        if (listener.LocalNames.Contains(left) || listener.LocalNames.Contains(right)) continue;
        var operation = isOr ? "OR" : "AND";
        var alternate = isOr ? "AND" : "OR";
        var outcome = isOr ? "истинно" : "ложно";
        var intent = isOr ? "исключить оба значения" : "разрешить любое из двух значений";
        var comparison = isOr ? "<>" : "=";
        report.Add(Severity.Warning, "B004",
          $"При одинаковом числовом значении «{value}» с обеих сторон условие с {operation} всегда {outcome}: сравнения используют разные значения «{left}» и «{right}».",
          document, (condition.OR() ?? condition.AND()).Symbol.ToTextPosition(),
          new DiagnosticRecommendation(
            $"Если нужно {intent}, используйте «({value} {comparison} {left}) {alternate} ({value} {comparison} {right})». Если свойство должно читаться один раз, сначала сохраните его значение в переменную.",
            $"Проверьте ветви для «{left}», «{right}» и третьего значения. Если «{value}» может быть NULL или строкой, отдельно проверьте преобразование типов в Directum."));
      }
    }
  }
}
