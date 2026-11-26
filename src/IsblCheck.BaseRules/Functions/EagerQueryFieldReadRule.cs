using System;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  public sealed class EagerQueryFieldReadRule : AbstractRule
  {
    public static IRuleInfo Info { get; } = new RuleInfo(nameof(EagerQueryFieldReadRule),
      "Чтение поля IQuery в аргументе IfThen при проверке наличия строк.");

    private sealed class Listener : IsblBaseListener
    {
      public IReport Report;
      public IDocument Document;
      public IContext Context;

      public override void EnterFunction(IsblParser.FunctionContext call)
      {
        var name = call.identifier().GetText();
        if ((!Equal(name, "IfThen") && !Equal(name, "ЕслиТо")) || Context.HasUserFunction(name))
          return;
        if (ParameterListParser.GetExpressions(call.parameterList()) is not [var condition, var yes, var no] ||
            GetRowsTest(condition) is not { } test)
          return;
        var field = LocalQueryOrigin.Unwrap(test.HasRowsWhenTrue ? yes : no)?.operand();
        if (!Equal(field?.variable()?.GetText(), test.Receiver) || field.indexer() != null ||
            field.invocationCall() is not [var fields, var value] ||
            !Equal(fields.identifier().GetText(), "Fields") ||
            ParameterListParser.GetExpressions(fields.parameterList()) is not [not null] ||
            value.parameterList() != null || !IsValueAccessor(value.identifier().GetText()))
          return;
        if (!LocalQueryOrigin.IsCreatedByBuiltInQuery(call, test.Receiver, Context))
          return;

        // Earlier row checks can protect a direct assignment with a simple fallback.
        var fallback = LocalQueryOrigin.Unwrap(test.HasRowsWhenTrue ? no : yes)?.operand();
        if (fallback != null && fallback.function() == null && fallback.invocationCall().Length == 0 &&
            fallback.indexer() == null && fallback.POINTER() == null && fallback.expression() == null &&
            call.Parent is IsblParser.OperandContext { Parent: IsblParser.ExpressionContext { Parent: IsblParser.AssignStatementContext assignment } } &&
            LocalGuardAnalysis.IsProtected(assignment, test.Receiver, (guard, truth) =>
              GetRowsTest(guard) is { } outer && outer.HasRowsWhenTrue == truth && Equal(outer.Receiver, test.Receiver), Context))
          return;

        Report.Add(Severity.Warning, "F039",
          $"«{name}» вычисляет аргументы до выбора результата. Чтение поля запроса «{test.Receiver}» выполняется и при пустой выборке.",
          Document, value.identifier().GetTextPosition(), new DiagnosticRecommendation(
            $"Перенесите чтение поля «{test.Receiver}» в ветвь IF с текущей записью. В ELSE присвойте значение, которое исходный {name} выбирал для пустой выборки. Если вызов входит в более крупное выражение, сначала вычислите результат в переменную и сохраните порядок операций.",
            "Выполните расчёт с пустой выборкой, одной строкой и строкой с NULL в этом поле. Проверьте, что при пустой выборке чтение поля пропущено, а результат с записью сохранился."));
      }

      private static (string Receiver, bool HasRowsWhenTrue)? GetRowsTest(IsblParser.ExpressionContext expression)
      {
        var test = LocalQueryOrigin.Unwrap(expression);
        if (test?.NOT() != null)
        {
          var operand = LocalQueryOrigin.Unwrap(test.expression(0))?.operand();
          var eof = GetReceiver(operand, "EOF");
          return eof == null ? null : (eof, true);
        }
        if (GetReceiver(test?.operand(), "EOF") is { } receiver)
          return (receiver, false);
        if (test?.expression().Length != 2 ||
            (test.GT() == null && test.EQ() == null && test.NEQ() == null))
          return null;
        var query = GetReceiver(LocalQueryOrigin.Unwrap(test.expression(0))?.operand(), "RecordCount");
        var threshold = LocalQueryOrigin.Unwrap(test.expression(1))?.operand()?.unsignedNumber()?.GetText();
        return query != null && threshold == "0" ? (query, test.EQ() == null) : null;
      }

      private static string GetReceiver(IsblParser.OperandContext operand, string member) =>
        operand?.variable() != null && operand.indexer() == null && operand.invocationCall() is [var call] &&
        call.parameterList() == null && Equal(call.identifier().GetText(), member) ? operand.variable().GetText() : null;

      private static bool IsValueAccessor(string name) =>
        Equal(name, "Value") || Equal(name, "AsString") || Equal(name, "AsInteger");

      private static bool Equal(string actual, string expected) =>
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }

    public override void Apply(IReport report, IDocument document, IContext context) =>
      ParseTreeWalker.Default.Walk(new Listener { Report = report, Document = document, Context = context }, document.GetSyntaxTree());
  }
}
