using System;
using System.Linq;
using System.Text.RegularExpressions;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;
using static IsblCheck.BaseRules.Functions.LocalQueryOrigin;

namespace IsblCheck.BaseRules.Functions
{
  public sealed class SqlRestoreKeyRule : AbstractRule
  {
    public static IRuleInfo Info { get; } = new RuleInfo(nameof(SqlRestoreKeyRule),
      "При откате в EXCEPT значение поля взято из записи с другим ключом.");

    // Match complete single-row templates. Joins, filters with more predicates,
    // and computed values need a separate analysis.
    private const string SqlName = @"[A-Za-z_][A-Za-z_0-9]*";
    private const string Placeholder = @"%(?:[0-9]+:)?[sd]";
    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking;
    private static readonly Regex Select = new($@"^\s*select\s+(?<field>{SqlName})\s+from\s+(?<table>{SqlName})\s+where\s+(?<key>{SqlName})\s*=\s*(?<id>{Placeholder})\s*;?\s*\z", Options);
    private static readonly Regex Update = new($@"^\s*update\s+(?<table>{SqlName}|{Placeholder})\s+set\s+(?<field>{SqlName})\s*=\s*'(?<value>{Placeholder})'\s+where\s+(?<key>{SqlName})\s*=\s*(?<id>{Placeholder})\s*;?\s*\z", Options);

    private sealed class Listener(IReport report, IDocument document, IContext context) : IsblBaseListener
    {
      public override void EnterInvokeStatement(IsblParser.InvokeStatementContext statement)
      {
        if (statement.function() is not { } sql || statement.invocationCall().Length != 0 ||
            !IsBuiltIn(sql, "SQL") || !InExceptionHandler(statement) || ReadTemplate(sql, Update) is not { } update ||
            SimpleName(update.Argument("value")) is not { } saved ||
            SimpleName(update.Argument("id")) is not { } writeKey ||
            FindAssignment(statement, saved) is not { } capture ||
            ReadScalarSql(capture.expression()) is not { } readSql ||
            ReadTemplate(readSql, Select) is not { } read ||
            SimpleName(read.Argument("id")) is not { } readKey || NameEquals(readKey, writeKey) ||
            !NameEquals(read.Text("field"), update.Text("field")) || !NameEquals(read.Text("key"), update.Text("key")) ||
            !MatchesTable(read, update))
          return;

        if (FindAssignment(statement, writeKey) is not { } rowKey ||
            !IsCurrentRowKey(rowKey, capture, readKey))
          return;

        report.Add(Severity.Warning, "F044",
          $"В EXCEPT поле «{read.Text("field")}» для «{writeKey}» восстанавливается из «{saved}». Это значение прочитано по «{readKey}», а ключ «{writeKey}» получен из текущей строки FOREACH.",
          document, sql.identifier().GetTextPosition(), new DiagnosticRecommendation(
            $"Если EXCEPT должен отменять изменение текущей строки, считайте и сохраните исходное «{read.Text("field")}» по «{writeKey}» до изменения. Используйте это же значение в условии изменения и при восстановлении. Проверьте, что обе команды обращаются к одной таблице.",
            $"Воспроизведите отказ после изменения строки при разных значениях «{read.Text("field")}» у «{readKey}» и «{writeKey}». Сравните обе записи и проверьте, что сохраняется исходное исключение."));
      }

      private static bool InExceptionHandler(IParseTree node)
      {
        for (; node.Parent != null; node = node.Parent)
          if (node.Parent is IsblParser.TryStatementContext attempt)
            return attempt.EXCEPT() != null && ReferenceEquals(node, attempt.statementBlock(1));
        return false;
      }

      private static bool IsCurrentRowKey(IsblParser.AssignStatementContext rowKey,
        IsblParser.AssignStatementContext capture, string readKey)
      {
        if (rowKey.Parent is not IsblParser.StatementBlockContext body ||
            body.Parent is not IsblParser.ForeachStatementContext loop ||
            !ReferenceEquals(capture.Parent, body) || WritesVariable(loop, readKey) ||
            !ReferenceEquals(FindAssignment(capture, rowKey.variable().GetText()), rowKey))
          return false;

        // Match a value taken directly from the current cursor row.
        var operand = Unwrap(rowKey.expression())?.operand();
        return NameEquals(operand?.variable()?.GetText(), loop.variable().GetText()) &&
          operand.indexer() == null && operand.invocationCall() is [var field, var value] &&
          (NameEquals(field.identifier().GetText(), "FieldByIndex") || NameEquals(field.identifier().GetText(), "Fields")) &&
          NameEquals(value.identifier().GetText(), "Value") && value.L_PAREN() == null;
      }

      private static bool MatchesTable(Template read, Template update)
      {
        if (!update.Text("table").StartsWith('%'))
          return NameEquals(read.Text("table"), update.Text("table"));
        var operand = Unwrap(update.Argument("table"))?.operand();
        // In route calculations Object.SQLTableName is resolved only at runtime.
        return NameEquals(operand?.variable()?.GetText(), "Object") && operand.indexer() == null &&
          operand.invocationCall() is [var property] && property.L_PAREN() == null &&
          NameEquals(property.identifier().GetText(), "SQLTableName");
      }

      private bool IsBuiltIn(IsblParser.FunctionContext call, string name) =>
        NameEquals(call.identifier().GetText(), name) && !context.HasUserFunction(call.identifier().GetText());

      private IsblParser.FunctionContext ReadScalarSql(IsblParser.ExpressionContext expression)
      {
        var operand = Unwrap(expression)?.operand();
        return operand?.function() is { } call && IsBuiltIn(call, "SQL") &&
          operand.invocationCall().Length == 0 ? call : null;
      }

      private Template ReadTemplate(IsblParser.FunctionContext sql, Regex pattern)
      {
        var arguments = ParameterListParser.GetExpressions(sql.parameterList());
        if (arguments.Length == 0 || arguments.Skip(1).Any(argument => argument != null))
          return null;
        var expression = arguments[0];
        if (SimpleName(expression) is { } name)
          expression = FindAssignment(sql, name)?.expression();

        var operand = Unwrap(expression)?.operand();
        if (operand?.function() is not { } format || operand.invocationCall().Length != 0 ||
            (!IsBuiltIn(format, "Format") && !IsBuiltIn(format, "Формат")) ||
            ParameterListParser.GetExpressions(format.parameterList()) is not [var source, var values])
          return null;
        var literal = Unwrap(source)?.operand()?.@string()?.GetText();
        if (literal == null)
          return null;
        var match = pattern.Match(literal[1..^1]);
        if (!match.Success)
          return null;
        var array = Unwrap(values)?.operand();
        if (array?.function() is { } call && array.invocationCall().Length == 0 && IsBuiltIn(call, "ArrayOf"))
          return new Template(match, ParameterListParser.GetExpressions(call.parameterList()));
        return new Template(match, [values]);
      }
    }

    private sealed record Template(Match Match, IsblParser.ExpressionContext[] Arguments)
    {
      public string Text(string group) => Match.Groups[group].Value;

      public IsblParser.ExpressionContext Argument(string group)
      {
        var part = Match.Groups[group];
        var placeholder = part.Value;
        if (!part.Success || !placeholder.StartsWith('%'))
          return null;
        // Templates matched above contain only these three placeholder positions.
        // The non-indexed form is accepted for a single substitution only.
        if (placeholder.Length == 2)
          return Arguments.Length == 1 ? Arguments[0] : null;
        return int.TryParse(placeholder.AsSpan(1, placeholder.Length - 3), out var index) && index < Arguments.Length
          ? Arguments[index] : null;
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context) =>
      ParseTreeWalker.Default.Walk(new Listener(report, document, context), document.GetSyntaxTree());
  }
}
