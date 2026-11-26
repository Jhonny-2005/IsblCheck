using System;
using System.Linq;
using System.Text.RegularExpressions;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  /// <summary>Flags direct formatting of quoted SQL values for manual verification.</summary>
  public sealed class SqlFormattedValueRule : AbstractRule
  {
    public static IRuleInfo Info { get; } = new RuleInfo(nameof(SqlFormattedValueRule), "Подстановка значений через Format в SQL.");
    private sealed class Listener : IsblBaseListener
    {
      public IReport Report;
      public IDocument Document;
      public IContext Context;
      public override void EnterFunction(IsblParser.FunctionContext call)
      {
        var name = call.identifier()?.GetText();
        if (name == null || !SqlInjectionRiskRule.TryGetQueryArgument(name, out var queryIndex) || Overridden(name)) return;
        var args = ParameterListParser.GetExpressions(call.parameterList());
        if (args.Length <= queryIndex) return;
        var query = Unwrap(args[queryIndex]);
        var operand = query?.operand();
        if (operand?.variable() is { } variable && operand.indexer() == null && operand.POINTER() == null &&
            operand.invocationCall().Length == 0 && GetDirectStatement(call) is { } statement &&
            PreviousAssignment(statement) is { } assignment &&
            Equal(assignment.variable()?.GetText(), variable.GetText()) && assignment.indexer() == null &&
            assignment.invocationCall().Length == 0)
          query = assignment.expression();
        CheckFormat(query, name);
      }

      public override void EnterInvokeStatement(IsblParser.InvokeStatementContext statement)
      {
        var receiver = statement.variable()?.GetText();
        if (receiver == null || statement.indexer() != null || statement.invocationCall() is not [var open] ||
            !Equal(open.identifier().GetText(), "Open") ||
            ParameterListParser.GetExpressions(open.parameterList()).Length != 0 ||
            PreviousAssignment(statement) is not { } assignment || !Equal(assignment.variable()?.GetText(), receiver) ||
            assignment.indexer() != null || assignment.invocationCall() is not [var member] ||
            !Equal(member.identifier().GetText(), "CommandText") || member.L_PAREN() != null ||
            !LocalQueryOrigin.IsCreatedByBuiltInQuery(statement, receiver, Context))
          return;
        CheckFormat(assignment.expression(), "IQuery.Open");
      }

      private void CheckFormat(IsblParser.ExpressionContext query, string name)
      {
        var operand = Unwrap(query)?.operand();
        var format = operand?.function();
        var formatName = format?.identifier()?.GetText();
        if (format == null || operand.invocationCall().Length != 0 ||
            (!string.Equals(formatName, "Format", StringComparison.OrdinalIgnoreCase) && !string.Equals(formatName, "Формат", StringComparison.OrdinalIgnoreCase)) ||
            Overridden(formatName)) return;
        var parameters = ParameterListParser.GetExpressions(format.parameterList());
        if (parameters.Length < 2) return;
        var template = Unwrap(parameters[0])?.operand()?.@string()?.GetText();
        if (template == null || !Regex.IsMatch(template, "'%(?:[0-9]+:)?s'", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) return;
        // Without resolving Format's positional mapping, a mixed constant/dynamic
        // argument set could put only a constant into the quoted placeholder.
        if (!parameters.Skip(1).All(IsDynamicOnly)) return;
        var advice = SqlInjectionRiskRule.RecommendSqlReview(name);
        Report.Add(Severity.Warning, "F037",
          $"«{formatName}» подставляет значение в SQL-литерал запроса для «{name}». Риск зависит от происхождения значения и способа экранирования.",
          Document, format.identifier().GetTextPosition(), new DiagnosticRecommendation(advice.Action,
            "В тестовой системе проверьте обычный текст, строку с апострофом и пустое значение. Для выборки сравните возвращённые строки, а для записи проверьте, что сохраняются ожидаемые значения и права доступа."));
      }

      private static IParseTree GetDirectStatement(IsblParser.FunctionContext call)
      {
        if (call.Parent is IsblParser.InvokeStatementContext invocation && invocation.invocationCall().Length == 0)
          return invocation;
        if (call.Parent is IsblParser.OperandContext { Parent: IsblParser.ExpressionContext expression } value &&
            value.invocationCall().Length == 0 && expression.Parent is IsblParser.AssignStatementContext assignment &&
            ReferenceEquals(assignment.expression(), expression))
          return assignment;
        return null;
      }

      private static IsblParser.AssignStatementContext PreviousAssignment(IParseTree statement)
      {
        if (statement.Parent is not IsblParser.StatementBlockContext block) return null;
        for (var i = 1; i < block.ChildCount; i++)
          if (ReferenceEquals(block.GetChild(i), statement))
            return block.GetChild(i - 1) as IsblParser.AssignStatementContext;
        return null;
      }

      private static bool Equal(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
      private bool IsDynamicOnly(IsblParser.ExpressionContext expression)
      {
        expression = Unwrap(expression);
        var operand = expression?.operand();
        if (expression == null || operand?.@string() != null || operand?.unsignedNumber() != null) return false;
        if ((expression.PLUS() != null || expression.MINUS() != null) && expression.expression().Length == 1 && Unwrap(expression.expression(0))?.operand()?.unsignedNumber() != null) return false;
        var function = operand?.function();
        var functionName = function?.identifier()?.GetText();
        if (functionName == null || operand.invocationCall().Length != 0 ||
            (!functionName.Equals("ArrayOf", StringComparison.OrdinalIgnoreCase) && !functionName.Equals("Массив", StringComparison.OrdinalIgnoreCase)) ||
            Overridden(functionName)) return true;
        var elements = ParameterListParser.GetExpressions(function.parameterList());
        return elements.Length > 0 && elements.All(IsDynamicOnly);
      }
      private bool Overridden(string name) => Context.HasUserFunction(name);
      private static IsblParser.ExpressionContext Unwrap(IsblParser.ExpressionContext expression)
      {
        while (expression?.operand()?.expression() != null) expression = expression.operand().expression();
        return expression;
      }
    }
    public override void Apply(IReport report, IDocument document, IContext context) =>
      ParseTreeWalker.Default.Walk(new Listener { Report = report, Document = document, Context = context }, document.GetSyntaxTree());
  }
}
