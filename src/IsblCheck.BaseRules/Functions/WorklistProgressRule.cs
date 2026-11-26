using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  /// <summary>Warns about a narrow worklist loop pattern when no progress guard is visible.</summary>
  public sealed class WorklistProgressRule : AbstractRule
  {
    public static IRuleInfo Info { get; } = new RuleInfo(nameof(WorklistProgressRule), "Цикл обработки списка без видимой проверки отсутствия прогресса.");
    private sealed class Listener : IsblBaseListener
    {
      public IReport Report;
      public IDocument Document;
      public IContext Context;
      public override void EnterWhileStatement(IsblParser.WhileStatementContext loop)
      {
        var condition = Unwrap(loop.expression());
        if (condition?.NOT() == null || condition.expression().Length != 1) return;
        var doneOperand = Unwrap(condition.expression(0))?.operand();
        if (doneOperand?.variable() == null || doneOperand.indexer() != null || doneOperand.invocationCall().Length != 0) return;
        var doneName = doneOperand.variable().GetText();
        var body = new BodyVisitor { DoneName = doneName, Context = Context, TargetLoop = loop };
        ParseTreeWalker.Default.Walk(body, loop.statementBlock());
        if (body.HasPossibleExit || body.HasNestedWhile || body.DoneWrites != 1 || body.Candidates.Count != 1) return;
        var candidate = body.Candidates[0];
        if (!body.Recreated.Contains(candidate.ListName) || !body.Added.Contains(candidate.ListName)) return;
        if (!IsZero(candidate.Zero, loop)) return;
        Report.Add(Severity.Warning, "F030", $"Цикл пересоздаёт и заполняет список \"{candidate.ListName}\", а найденное условие завершения проверяет его опустошение. При неизменном непустом списке цикл может не завершиться.",
          Document, loop.Start.ToTextPosition(), new DiagnosticRecommendation(
            $"Определите, что считается прогрессом при обработке «{candidate.ListName}», например удаление обработанной записи по её идентификатору. Если список остаётся непустым и состояние на проходе не изменилось, обработайте этот случай до повторного входа в WHILE. Сохраните смысл признака «{doneName}» и выберите исход по требованиям вычисления.",
            "Проверьте пустой список, нормальное уменьшение списка и повтор того же непустого состояния. В последнем случае вычисление должно завершаться предусмотренным способом."));
      }
      private bool IsZero(IsblParser.ExpressionContext expression, IsblParser.WhileStatementContext loop)
      {
        var expr = Unwrap(expression);
        if (expr?.operand()?.unsignedNumber() != null)
          return expr.operand().unsignedNumber().GetText().Split('e', 'E')[0].All(c => c == '0' || c == '.');
        var variable = expr?.operand()?.variable();
        if (variable == null || expr.operand().invocationCall().Length != 0 || expr.operand().indexer() != null) return false;
        var finder = new ZeroDefinitionVisitor { Name = variable.GetText(), BeforeIndex = loop.Start.StartIndex, ParentBlock = loop.Parent };
        ParseTreeWalker.Default.Walk(finder, Document.GetSyntaxTree());
        return finder.Writes == 1 && finder.IsZero && !finder.MayEscape;
      }
    }
    private sealed class ZeroDefinitionVisitor : IsblBaseListener
    {
      public string Name;
      public int BeforeIndex;
      public IParseTree ParentBlock;
      public int Writes;
      public bool IsZero;
      public bool MayEscape;
      public override void EnterAssignStatement(IsblParser.AssignStatementContext assignment)
      {
        if (assignment.variable() == null || assignment.indexer() != null || assignment.invocationCall().Length != 0 || !string.Equals(assignment.variable().GetText(), Name, StringComparison.OrdinalIgnoreCase)) return;
        Writes++;
        var number = Unwrap(assignment.expression())?.operand()?.unsignedNumber()?.GetText();
        IsZero = ReferenceEquals(assignment.Parent, ParentBlock) && assignment.Start.StartIndex < BeforeIndex && number != null && number.Split('e', 'E')[0].All(c => c == '0' || c == '.');
      }
      public override void EnterOperand(IsblParser.OperandContext operand)
      {
        if (operand.POINTER() != null && string.Equals(operand.variable()?.GetText(), Name, StringComparison.OrdinalIgnoreCase)) MayEscape = true;
      }
    }
    private sealed class Candidate
    {
      public string ListName;
      public IsblParser.ExpressionContext Zero;
    }
    private sealed class BodyVisitor : IsblBaseListener
    {
      public string DoneName;
      public IContext Context;
      public IsblParser.WhileStatementContext TargetLoop;
      public int DoneWrites;
      public bool HasPossibleExit;
      public bool HasNestedWhile;
      public readonly HashSet<string> Recreated = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      public readonly HashSet<string> Added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      public readonly List<Candidate> Candidates = new List<Candidate>();
      public override void EnterWhileStatement(IsblParser.WhileStatementContext loop) => HasNestedWhile = true;
      public override void EnterExitforStatement(IsblParser.ExitforStatementContext statement)
      {
        var exitTarget = InvalidExitForRule.FindTarget(statement);
        if (exitTarget == null) return;
        // A FOREACH inside this WHILE can end without ending the worklist loop.
        for (var parent = TargetLoop.Parent; parent != null; parent = parent.Parent)
          if (ReferenceEquals(parent, exitTarget))
          {
            HasPossibleExit = true;
            return;
          }
      }
      public override void EnterFunction(IsblParser.FunctionContext function)
      {
        var name = function.identifier().GetText();
        if ((name.Equals("Exit", StringComparison.OrdinalIgnoreCase) || name.Equals("Raise", StringComparison.OrdinalIgnoreCase)) &&
            !Context.HasUserFunction(name)) HasPossibleExit = true;
      }
      public override void EnterAssignStatement(IsblParser.AssignStatementContext assignment)
      {
        if (assignment.variable() == null || assignment.indexer() != null || assignment.invocationCall().Length != 0) return;
        var name = assignment.variable().GetText();
        if (name.Equals(DoneName, StringComparison.OrdinalIgnoreCase)) DoneWrites++;
        var value = Unwrap(assignment.expression())?.operand();
        var function = value?.function();
        if (function != null && value.invocationCall().Length == 0 &&
            (function.identifier().GetText().Equals("CreateList", StringComparison.OrdinalIgnoreCase) || function.identifier().GetText().Equals("CreateStringList", StringComparison.OrdinalIgnoreCase)) &&
            !Context.HasUserFunction(function.identifier().GetText())) Recreated.Add(name);
      }
      public override void EnterInvokeStatement(IsblParser.InvokeStatementContext statement)
      {
        if (statement.variable() != null && statement.indexer() == null && statement.invocationCall().Length == 1 &&
            statement.invocationCall(0).identifier().GetText().Equals("Add", StringComparison.OrdinalIgnoreCase) && statement.invocationCall(0).parameterList() != null)
          Added.Add(statement.variable().GetText());
      }
      public override void EnterIfStatement(IsblParser.IfStatementContext statement)
      {
        var condition = Unwrap(statement.expression());
        if (condition?.EQ() == null || condition.expression().Length != 2) return;
        var left = Unwrap(condition.expression(0)); var right = Unwrap(condition.expression(1));
        var list = CountReceiver(left); var zero = right;
        if (list == null) { list = CountReceiver(right); zero = left; }
        if (list == null) return;
        var thenBlock = statement.statementBlock(0);
        for (var i = 0; i < thenBlock.ChildCount; i++)
          if (thenBlock.GetChild(i) is IsblParser.AssignStatementContext assignment &&
              assignment.variable() != null && assignment.indexer() == null && assignment.invocationCall().Length == 0 &&
              assignment.variable().GetText().Equals(DoneName, StringComparison.OrdinalIgnoreCase) &&
              string.Equals(Unwrap(assignment.expression())?.GetText(), "TRUE", StringComparison.OrdinalIgnoreCase))
            Candidates.Add(new Candidate { ListName = list, Zero = zero });
      }
      private static string CountReceiver(IsblParser.ExpressionContext expr)
      {
        var operand = expr?.operand();
        if (operand?.variable() == null || operand.indexer() != null || operand.invocationCall().Length != 1) return null;
        var call = operand.invocationCall(0);
        return call.parameterList() == null && call.identifier().GetText().Equals("Count", StringComparison.OrdinalIgnoreCase) ? operand.variable().GetText() : null;
      }
    }
    private static IsblParser.ExpressionContext Unwrap(IsblParser.ExpressionContext expression)
    {
      while (expression?.operand()?.expression() != null) expression = expression.operand().expression();
      return expression;
    }
    public override void Apply(IReport report, IDocument document, IContext context) =>
      ParseTreeWalker.Default.Walk(new Listener { Report = report, Document = document, Context = context }, document.GetSyntaxTree());
  }
}
