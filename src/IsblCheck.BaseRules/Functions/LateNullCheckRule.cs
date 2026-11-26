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
  /// <summary>Finds a member dereference immediately before an explicit presence check of its receiver.</summary>
  public sealed class LateNullCheckRule : AbstractRule
  {
    public static IRuleInfo Info { get; } = new RuleInfo(nameof(LateNullCheckRule), "Проверка наличия объекта после обращения к его свойству.");

    private sealed class Listener : IsblBaseListener
    {
      public IReport Report;
      public IDocument Document;
      public IContext Context;
      public override void EnterIfStatement(IsblParser.IfStatementContext context)
      {
        if (GetReceiverTest(context.expression()) is not { } check) return;
        if (context.Parent is not IsblParser.StatementBlockContext block) return;
        IsblParser.AssignStatementContext previous = null;
        IsblParser.IfStatementContext earlierGuard = null;
        for (var i = 0; i < block.ChildCount; i++)
        {
          if (ReferenceEquals(block.GetChild(i), context))
          {
            if (i > 0) previous = block.GetChild(i - 1) as IsblParser.AssignStatementContext;
            if (i > 1) earlierGuard = block.GetChild(i - 2) as IsblParser.IfStatementContext;
            break;
          }
        }
        if (previous?.variable() == null || previous.indexer() != null || previous.invocationCall().Length != 0) return;
        var checkedName = check.Name;
        if (IsExitingPresenceGuard(earlierGuard, check)) return;
        if (string.Equals(previous.variable().GetText(), checkedName, StringComparison.OrdinalIgnoreCase)) return;
        var accessed = Unwrap(previous.expression())?.operand();
        if (accessed?.variable() == null || accessed.indexer() != null || accessed.invocationCall().Length != 1 || accessed.invocationCall(0).parameterList() != null ||
            !string.Equals(accessed.variable().GetText(), checkedName, StringComparison.OrdinalIgnoreCase)) return;
        var member = accessed.invocationCall(0).identifier();
        var target = previous.variable().GetText();
        var predicate = $"{check.Function}({checkedName})";
        var availability = check.Function.Equals("VarIsNull", StringComparison.OrdinalIgnoreCase) ? "NULL" : "отсутствующим объектом";
        Report.Add(Severity.Warning, "F029", $"Свойство «{checkedName}.{member.GetText()}» читается до проверки «{predicate}». Если объект отсутствует, чтение происходит до обработки этого случая.",
          Document, member.GetTextPosition(), new DiagnosticRecommendation(
            $"Выполните «{predicate}» до чтения «{checkedName}.{member.GetText()}». Присваивайте «{target}» из свойства только на пути с объектом. Если это значение нужно для сообщения об отсутствии, возьмите идентификатор из аргумента поиска или название из независимых метаданных.",
            $"Проверьте путь с {availability} и путь с существующим «{checkedName}». На первом пути должна выполниться предусмотренная обработка отсутствия без обращения к объекту. На втором проверьте прежнее значение «{target}» и результат вычисления."));
      }

      private (string Name, string Function, bool TestsAbsence)? GetReceiverTest(IsblParser.ExpressionContext expression)
      {
        var test = Unwrap(expression);
        var negated = test?.NOT() != null;
        if (negated)
          test = Unwrap(test.expression(0));
        var callee = test?.operand();
        var function = callee?.function();
        var name = function?.identifier()?.GetText();
        var isNull = string.Equals(name, "VarIsNull", StringComparison.OrdinalIgnoreCase);
        if ((!isNull && !string.Equals(name, "Assigned", StringComparison.OrdinalIgnoreCase)) ||
            callee.invocationCall().Length != 0 || Context.HasUserFunction(name))
          return null;
        var args = ParameterListParser.GetExpressions(function.parameterList());
        if (args.Length != 1) return null;
        var operand = Unwrap(args[0])?.operand();
        if (operand?.variable() == null || operand.indexer() != null || operand.invocationCall().Length != 0 ||
            operand.POINTER() != null) return null;
        return (operand.variable().GetText(), name, isNull != negated);
      }

      private bool IsExitingPresenceGuard(IsblParser.IfStatementContext guard, (string Name, string Function, bool TestsAbsence) check)
      {
        if (guard == null || guard.statementBlock().Length != 1 ||
            GetReceiverTest(guard.expression()) is not { TestsAbsence: true } earlier ||
            !string.Equals(earlier.Function, check.Function, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(earlier.Name, check.Name, StringComparison.OrdinalIgnoreCase)) return false;
        var block = guard.statementBlock(0);
        if (block.ChildCount != 1 || block.GetChild(0) is not IsblParser.InvokeStatementContext invoke || invoke.invocationCall().Length != 0) return false;
        var called = invoke.function()?.identifier()?.GetText();
        if (!string.Equals(called, "Exit", StringComparison.OrdinalIgnoreCase) && !string.Equals(called, "Raise", StringComparison.OrdinalIgnoreCase)) return false;
        return !Context.HasUserFunction(called);
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
