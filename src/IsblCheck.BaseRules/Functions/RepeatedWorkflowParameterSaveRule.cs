using System;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;
using static IsblCheck.BaseRules.Functions.LocalQueryOrigin;

namespace IsblCheck.BaseRules.Functions
{
  public sealed class RepeatedWorkflowParameterSaveRule : AbstractRule
  {
    public static IRuleInfo Info { get; } = new RuleInfo(nameof(RepeatedWorkflowParameterSaveRule),
      "Повторная запись параметров задач из одного списка во вложенном цикле.");

    private sealed class Listener(IReport report, IDocument document) : IsblBaseListener
    {
      public override void EnterInvokeStatement(IsblParser.InvokeStatementContext save)
      {
        if (document.ComponentType != ComponentType.StandardRoute ||
            save.variable()?.GetText() is not { } task || save.indexer() != null ||
            save.invocationCall() is not [var method] || !NameEquals(method.identifier().GetText(), "Save") ||
            ParameterListParser.GetExpressions(method.parameterList()).Length != 0 ||
            PreviousStatement(save) is not IsblParser.AssignStatementContext write ||
            write.variable()?.GetText() is not { } parameter || write.indexer() != null ||
            write.invocationCall() is not [var property] || !NameEquals(property.identifier().GetText(), "Value") ||
            property.L_PAREN() != null || SimpleName(write.expression()) is not { } source)
          return;

        var parameterOrigin = Unwrap(FindAssignment(write, parameter)?.expression())?.operand();
        if (!NameEquals(parameterOrigin?.variable()?.GetText(), task) || parameterOrigin.indexer() != null ||
            parameterOrigin.invocationCall() is not [var workflowParams, var findItem] ||
            !NameEquals(workflowParams.identifier().GetText(), "WorkflowParams") || workflowParams.L_PAREN() != null ||
            !NameEquals(findItem.identifier().GetText(), "FindItem") ||
            ParameterListParser.GetExpressions(findItem.parameterList()) is not [var name] ||
            Unwrap(name)?.operand()?.@string()?.GetText() is not { Length: > 2 } parameterName ||
            NearestLoop(save) is not IsblParser.WhileStatementContext inner ||
            NearestLoop(inner) is not { } outer)
          return;

        var taskOrigin = Unwrap(FindAssignment(write, task)?.expression())?.operand();
        if (taskOrigin?.variable()?.GetText() is not { } list || taskOrigin.indexer() != null ||
            taskOrigin.invocationCall() is not [var values, var taskProperty] ||
            !NameEquals(values.identifier().GetText(), "Values") ||
            !NameEquals(taskProperty.identifier().GetText(), "Task") || taskProperty.L_PAREN() != null ||
            ParameterListParser.GetExpressions(values.parameterList()) is not [var item] ||
            SimpleName(item) is not { } index || !IsFullIndexedTraversal(inner, list, index) ||
            WritesVariable(outer, list))
          return;

        if (FindAssignment(inner, source) is not { } sourceOrigin ||
            !ReferenceEquals(NearestLoop(sourceOrigin), outer) || WritesVariable(inner, source))
          return;

        report.Add(Severity.Information, "F045",
          $"Вложенный WHILE обходит «{list}» и может сохранять параметр {parameterName} каждой задачи. На следующей внешней итерации тот же список обрабатывается с очередным значением «{source}».",
          document, method.identifier().GetTextPosition(), new DiagnosticRecommendation(
            "Проверьте, какое значение должно попасть в каждую задачу. Если задаче соответствует одно вложение или сообщение, определите соответствие по устойчивому ключу и сохраняйте найденную задачу только при изменении параметра. Если значение общее, сначала вычислите итоговое значение и затем обойдите задачи один раз, при условии что порядок сохранения и события Directum это допускают.",
            "Проверьте два вложения с разными значениями и две подзадачи. Сравните итоговые параметры, число вызовов Save и поведение при ошибке сохранения."));
      }

      private static bool IsFullIndexedTraversal(IsblParser.WhileStatementContext loop, string list, string index)
      {
        var condition = Unwrap(loop.expression());
        if (condition.LT() == null || !NameEquals(SimpleName(condition.expression(0)), index))
          return false;
        var bound = Unwrap(condition.expression(1))?.operand();
        if (!NameEquals(bound?.variable()?.GetText(), list) || bound.indexer() != null ||
            bound.invocationCall() is not [var count] || !NameEquals(count.identifier().GetText(), "Count") || count.L_PAREN() != null ||
            Unwrap(FindAssignment(loop, index)?.expression())?.operand()?.unsignedNumber()?.GetText() != "0")
          return false;

        var body = loop.statementBlock();
        if (body.GetChild(body.ChildCount - 1) is not IsblParser.AssignStatementContext step || !IsDirectAssignment(step, index))
          return false;
        var increment = Unwrap(step.expression());
        return increment.PLUS() != null && increment.expression().Length == 2 &&
          NameEquals(SimpleName(increment.expression(0)), index) &&
          Unwrap(increment.expression(1)).operand()?.unsignedNumber()?.GetText() == "1";
      }

      private static IParseTree PreviousStatement(IParseTree statement)
      {
        if (statement.Parent is not IsblParser.StatementBlockContext block)
          return null;
        var index = block.children?.IndexOf(statement) ?? 0;
        return index > 0 ? block.GetChild(index - 1) : null;
      }

      private static IParseTree NearestLoop(IParseTree node)
      {
        for (var parent = node.Parent; parent != null; parent = parent.Parent)
          if (parent is IsblParser.WhileStatementContext or IsblParser.ForeachStatementContext)
            return parent;
        return null;
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context) =>
      ParseTreeWalker.Default.Walk(new Listener(report, document), document.GetSyntaxTree());
  }
}
