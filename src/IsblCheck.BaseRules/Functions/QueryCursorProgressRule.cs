using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  public sealed class QueryCursorProgressRule : AbstractRule
  {
    public static IRuleInfo Info { get; } = new RuleInfo(nameof(QueryCursorProgressRule),
      "Цикл по IQuery.EOF без видимого перехода к следующей строке.");

    private static readonly HashSet<string> QueryProperties = new(StringComparer.OrdinalIgnoreCase)
      { "Active", "BOF", "EOF", "FieldCount", "RecordCount" };
    private static readonly HashSet<string> FieldValues = new(StringComparer.OrdinalIgnoreCase)
      { "Value", "AsInteger", "AsString", "AsFloat", "AsBoolean", "AsDateTime", "IsNull", "Name" };

    private sealed class Listener(IReport report, IDocument document, IContext context) : IsblBaseListener
    {
      public override void EnterWhileStatement(IsblParser.WhileStatementContext loop)
      {
        var condition = LocalQueryOrigin.Unwrap(loop.expression());
        if (condition.NOT() == null) return;
        var eof = LocalQueryOrigin.Unwrap(condition.expression(0)).operand();
        var query = eof?.variable()?.GetText();
        if (query == null || eof.indexer() != null || eof.invocationCall() is not [var member] ||
            !Equal(member.identifier().GetText(), "EOF") || member.L_PAREN() != null ||
            !LocalQueryOrigin.IsCreatedByBuiltInQuery(loop, query, context))
          return;

        var body = loop.statementBlock();
        if (LocalQueryOrigin.WritesVariable(body, query) || MayChangeCursorOrExit(body, loop, query) ||
            HasEarlierAlias(document.GetSyntaxTree(), loop.Start.StartIndex, query))
          return;

        report.Add(Severity.Warning, "F041",
          $"Цикл проверяет «{query}.EOF», но в теле не найден переход или выход. При непустой выборке может повторно обрабатываться та же строка.",
          document, member.identifier().GetTextPosition(), new DiagnosticRecommendation(
            $"После обработки текущей строки вызывайте «{query}.Next» на всех путях, которые продолжают цикл. Если нужен только последовательный перебор, рассмотрите FOREACH по уже открытому запросу. Не открывайте запрос и не выполняйте SQL заново ради перехода к следующей строке.",
            "Проверьте пустую выборку, одну строку и несколько строк, включая путь, на котором обработка строки пропускается. Цикл должен обработать каждую нужную строку один раз и завершиться."));
      }

      private bool MayChangeCursorOrExit(IParseTree node, IsblParser.WhileStatementContext loop, string query)
      {
        switch (node)
        {
          case IsblParser.OperandContext operand when Equal(operand.variable()?.GetText(), query):
            if (!IsRead(operand)) return true;
            break;
          case IsblParser.InvokeStatementContext invocation:
            if (Equal(invocation.variable()?.GetText(), query)) return true;
            var function = invocation.function()?.identifier().GetText();
            if ((Equal(function, "Exit") || Equal(function, "Прервать") || Equal(function, "Raise")) &&
                !context.HasUserFunction(function))
              return true;
            break;
          case IsblParser.AssignStatementContext assignment when Equal(assignment.variable()?.GetText(), query):
            return true;
          case IsblParser.ExitforStatementContext exit:
            var target = InvalidExitForRule.FindTarget(exit);
            for (var parent = loop.Parent; target != null && parent != null; parent = parent.Parent)
              if (ReferenceEquals(parent, target)) return true;
            break;
        }
        for (var i = 0; i < node.ChildCount; i++)
          if (MayChangeCursorOrExit(node.GetChild(i), loop, query)) return true;
        return false;
      }

      private static bool HasEarlierAlias(IParseTree node, int before, string query)
      {
        if (node is ParserRuleContext rule && rule.Start.StartIndex >= before) return false;
        if (node is IsblParser.OperandContext operand && Equal(operand.variable()?.GetText(), query) && !IsRead(operand))
          return true;
        for (var i = 0; i < node.ChildCount; i++)
          if (HasEarlierAlias(node.GetChild(i), before, query)) return true;
        return false;
      }

      private static bool IsRead(IsblParser.OperandContext operand)
      {
        if (operand.POINTER() != null || operand.indexer() != null) return false;
        return operand.invocationCall() switch
        {
          [var property] => property.L_PAREN() == null && QueryProperties.Contains(property.identifier().GetText()),
          [var field, var property] =>
            (Equal(field.identifier().GetText(), "Fields") || Equal(field.identifier().GetText(), "FieldByIndex")) &&
            field.L_PAREN() != null && property.L_PAREN() == null && FieldValues.Contains(property.identifier().GetText()),
          _ => false
        };
      }

      private static bool Equal(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    public override void Apply(IReport report, IDocument document, IContext context) =>
      ParseTreeWalker.Default.Walk(new Listener(report, document, context), document.GetSyntaxTree());
  }
}
