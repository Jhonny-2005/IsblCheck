using System;
using System.Linq;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  public sealed class DelimitedListSubstringRule : AbstractRule
  {
    public static IRuleInfo Info { get; } = new RuleInfo(nameof(DelimitedListSubstringRule),
      "Поиск подстроки вместо проверки присутствия элемента в строковом списке.");

    private sealed class Listener(IReport report, IDocument document, IContext context) : IsblBaseListener
    {
      public override void EnterIfStatement(IsblParser.IfStatementContext statement)
      {
        var condition = LocalQueryOrigin.Unwrap(statement.expression());
        if (condition.EQ() == null && condition.NEQ() == null) return;
        var left = LocalQueryOrigin.Unwrap(condition.expression(0));
        var right = LocalQueryOrigin.Unwrap(condition.expression(1));
        var search = ReadAbsenceTest(left, right) ?? ReadAbsenceTest(right, left);
        if (search == null) return;
        var (call, item, list, objectList) = search.Value;

        // Look only in the branch where the element was considered absent.
        var branch = condition.EQ() != null ? statement.statementBlock(0) : statement.statementBlock(1);
        if (branch == null) return;
        for (var i = 0; i < branch.ChildCount; i++)
        {
          var child = branch.GetChild(i);
          if (objectList && child is IsblParser.InvokeStatementContext append &&
              Equal(append.variable()?.GetText(), list) && append.indexer() == null &&
              append.invocationCall() is [var add] && Equal(add.identifier().GetText(), "Add") &&
              ParameterListParser.GetExpressions(add.parameterList()) is [var value] && Equal(SimpleName(value), item))
          {
            report.Add(Severity.Warning, "F040",
              $"«{call.identifier().GetText()}» ищет «{item}» в «{list}.DelimitedText». При отсутствии подстроки код добавляет элемент в «{list}». Частичное совпадение может помешать добавить отдельный элемент.",
              document, call.identifier().GetTextPosition(), new DiagnosticRecommendation(
                $"Проверяйте целые элементы «{list}». Если в вашей версии IStringList.IndexOf поддерживает нужные правила сравнения, используйте `{list}.IndexOf({item}) < 0`. Другой вариант — перебрать элементы по индексу и сравнить их с «{item}» строковым оператором `==`.",
                "Проверьте пустой список, точный дубль и два разных значения с общей частью, например joann и ann в таком порядке. Уточните ожидаемое сравнение регистра."));
            return;
          }
          if (!objectList && child is IsblParser.AssignStatementContext assignment &&
              Equal(assignment.variable()?.GetText(), list) && assignment.indexer() == null &&
              assignment.invocationCall().Length == 0 && GetAppendDelimiter(assignment.expression(), item, list) is { } delimiter)
          {
            report.Add(Severity.Warning, "F040",
              $"«{call.identifier().GetText()}» ищет подстроку «{item}», а при её отсутствии код добавляет это значение в список «{list}» с разделителем {delimiter}. Поиск может принять часть другого элемента за совпадение.",
              document, call.identifier().GetTextPosition(), new DiagnosticRecommendation(
                $"Проверяйте целые элементы «{list}». Например, переберите результат CSubString с тем же разделителем {delimiter} и сравните каждый элемент с «{item}» как строку. Если значения гарантированно не содержат разделитель, можно сохранить поиск подстроки и окружить разделителем и искомое значение, и весь список.",
                "Проверьте пустой список, точное совпадение, совпадение в начале и конце списка и разные элементы с общим фрагментом, например 12 и 312."));
            return;
          }

          if (LocalQueryOrigin.WritesVariable(child, item) || LocalQueryOrigin.WritesVariable(child, list) ||
              (objectList && MentionsReceiver(child, list)))
            return;
        }
      }

      private (IsblParser.FunctionContext Call, string Item, string List, bool ObjectList)? ReadAbsenceTest(
        IsblParser.ExpressionContext value, IsblParser.ExpressionContext zero)
      {
        if (zero.operand()?.unsignedNumber()?.GetText() != "0") return null;
        var call = GetBuiltInCall(value, "CharPos", "ПоискСтр");
        if (call == null || ParameterListParser.GetExpressions(call.parameterList()) is not [var needle, var haystack] ||
            SimpleName(needle) is not { } item)
          return null;
        if (SimpleName(haystack) is { } list && !Equal(item, list))
          return (call, item, list, false);

        var operand = LocalQueryOrigin.Unwrap(haystack)?.operand();
        if (operand?.variable() is not { } receiver || operand.indexer() != null ||
            operand.invocationCall() is not [var property] || property.L_PAREN() != null ||
            !Equal(property.identifier().GetText(), "DelimitedText") || Equal(item, receiver.GetText()))
          return null;
        var origin = LocalQueryOrigin.FindAssignment(call, receiver.GetText());
        var constructor = GetBuiltInCall(origin?.expression(), "CreateStringList", "СоздатьСписокСтрок");
        return constructor != null
          ? (call, item, receiver.GetText(), true) : null;
      }

      private static bool MentionsReceiver(IParseTree node, string receiver) =>
        (node is IsblParser.VariableContext variable && Equal(variable.GetText(), receiver)) ||
        Enumerable.Range(0, node.ChildCount).Any(i => MentionsReceiver(node.GetChild(i), receiver));

      private string GetAppendDelimiter(IsblParser.ExpressionContext expression, string item, string list)
      {
        var call = GetBuiltInCall(expression, "AddSubString", "ДобПодстр");
        if (call == null || ParameterListParser.GetExpressions(call.parameterList()) is not [var value, var source, var delimiter] ||
            !Equal(SimpleName(value), item) || !Equal(SimpleName(source), list))
          return null;
        var literal = LocalQueryOrigin.Unwrap(delimiter)?.operand()?.@string()?.GetText();
        return literal?.Length > 2 ? literal : null;
      }

      private IsblParser.FunctionContext GetBuiltInCall(IsblParser.ExpressionContext expression, string english, string russian)
      {
        var operand = LocalQueryOrigin.Unwrap(expression)?.operand();
        var call = operand?.function();
        var name = call?.identifier().GetText();
        return (Equal(name, english) || Equal(name, russian)) && operand.invocationCall().Length == 0 &&
          !context.HasUserFunction(name) ? call : null;
      }

      private static string SimpleName(IsblParser.ExpressionContext expression) => LocalQueryOrigin.SimpleName(expression);

      private static bool Equal(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    public override void Apply(IReport report, IDocument document, IContext context) =>
      ParseTreeWalker.Default.Walk(new Listener(report, document, context), document.GetSyntaxTree());
  }
}
