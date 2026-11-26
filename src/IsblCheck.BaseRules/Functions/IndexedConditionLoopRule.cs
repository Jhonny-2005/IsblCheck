using System;
using System.Linq;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;
using static IsblCheck.BaseRules.Functions.LocalQueryOrigin;

namespace IsblCheck.BaseRules.Functions
{
  public sealed class IndexedConditionLoopRule : AbstractRule
  {
    public static IRuleInfo Info { get; } = new RuleInfo(nameof(IndexedConditionLoopRule),
      "Проверка списка значений в WHILE теряет результаты отдельных итераций.");

    private sealed class Listener(IReport report, IDocument document, IContext context) : IsblBaseListener
    {
      public override void EnterWhileStatement(IsblParser.WhileStatementContext loop)
      {
        var test = Unwrap(loop.expression());
        if (test.LEQ() == null || SimpleName(test.expression(0)) is not { } index ||
            SimpleName(test.expression(1)) is not { } count || NameEquals(index, count))
          return;

        var body = loop.statementBlock();
        if (body.ChildCount is < 2 or > 3 ||
            body.GetChild(0) is not IsblParser.AssignStatementContext calculation ||
            calculation.variable()?.GetText() is not { } result || !IsDirectAssignment(calculation, result) ||
            NameEquals(index, result) || NameEquals(count, result) ||
            !HasIndexedSubstring(calculation.expression(), index) ||
            body.GetChild(body.ChildCount - 1) is not IsblParser.AssignStatementContext step ||
            !IsDirectAssignment(step, index))
          return;

        var following = (loop.Parent as IsblParser.StatementBlockContext)?.children;
        var indexInBlock = following?.IndexOf(loop) ?? -1;
        var nextIndex = indexInBlock + 1;
        if (indexInBlock < 0 || nextIndex >= following.Count ||
            following[nextIndex] is not IsblParser.IfStatementContext use ||
            !IsResultTest(use.expression(), result))
          return;

        if (body.ChildCount == 3 && !IsBooleanNormalization(body.GetChild(1), result))
          return;
        if (WritesVariable(calculation.expression(), index) || WritesVariable(calculation.expression(), count))
          return;

        if (IsPlusOne(step.expression(), count))
          report.Add(Severity.Warning, "F042",
            $"Счётчик «{index}» безусловно становится больше «{count}». WHILE проверит максимум одно значение и передаст «{result}» в следующее условие.",
            document, step.variable().GetTextPosition(), Advice(result));
        else if (IsPlusOne(step.expression(), index) && !Mentions(calculation.expression(), result))
          report.Add(Severity.Warning, "F042",
            $"На каждой итерации «{result}» вычисляется заново из элемента «{index}» без учёта предыдущего результата. После цикла проверяется итог последней итерации.",
            document, calculation.variable().GetTextPosition(), Advice(result));
      }

      private static DiagnosticRecommendation Advice(string result) => new(
        $"Уточните смысл списка. Для альтернатив начните с `{result} = FALSE`, сохраните TRUE при первом совпадении и завершите обход. В WHILE увеличивайте счётчик при несовпадении, а при совпадении устанавливайте его за верхнюю границу. Если должны выполняться все условия, явно объединяйте результаты через AND.",
        "Проверьте пустой список, одно значение, совпадение только с первым или последним элементом, отсутствие совпадений и перестановку элементов.");

      private bool HasIndexedSubstring(IParseTree node, string index)
      {
        if (node is IsblParser.FunctionContext call)
        {
          var name = call.identifier().GetText();
          if ((NameEquals(name, "SubString") || NameEquals(name, "Подстр")) &&
              !context.HasUserFunction(name) && ParameterListParser.GetExpressions(call.parameterList()) is [_, _, var position] &&
              NameEquals(SimpleName(position), index))
            return true;
        }
        return Enumerable.Range(0, node.ChildCount).Any(i => HasIndexedSubstring(node.GetChild(i), index));
      }

      private static bool Mentions(IParseTree node, string name) =>
        (node is IsblParser.VariableContext variable && NameEquals(variable.GetText(), name)) ||
        Enumerable.Range(0, node.ChildCount).Any(i => Mentions(node.GetChild(i), name));

      private static bool IsResultTest(IsblParser.ExpressionContext expression, string result)
      {
        expression = Unwrap(expression);
        return NameEquals(SimpleName(expression), result) ||
          (expression.NOT() != null && NameEquals(SimpleName(expression.expression(0)), result));
      }

      private static bool IsBooleanNormalization(IParseTree node, string result)
      {
        if (node is not IsblParser.IfStatementContext choice || choice.statementBlock() is not [var yes, var no] ||
            yes.ChildCount != 1 || no.ChildCount != 1)
          return false;
        var comparison = Unwrap(choice.expression());
        return comparison.EQ() != null &&
          NameEquals(SimpleName(comparison.expression(0)), result) &&
          Unwrap(comparison.expression(1)) is { } expected &&
          (expected.operand()?.unsignedNumber() != null ||
            (expected.MINUS() != null && expected.expression().Length == 1 &&
             Unwrap(expected.expression(0)).operand()?.unsignedNumber() != null)) &&
          IsBooleanAssignment(yes.GetChild(0), result, "TRUE") && IsBooleanAssignment(no.GetChild(0), result, "FALSE");
      }

      private static bool IsBooleanAssignment(IParseTree node, string name, string value) =>
        node is IsblParser.AssignStatementContext assignment && IsDirectAssignment(assignment, name) &&
        NameEquals(SimpleName(assignment.expression()), value);

      private static bool IsPlusOne(IsblParser.ExpressionContext expression, string name)
      {
        expression = Unwrap(expression);
        return expression.PLUS() != null && expression.expression().Length == 2 &&
          NameEquals(SimpleName(expression.expression(0)), name) &&
          Unwrap(expression.expression(1)).operand()?.unsignedNumber()?.GetText() == "1";
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context) =>
      ParseTreeWalker.Default.Walk(new Listener(report, document, context), document.GetSyntaxTree());
  }
}
