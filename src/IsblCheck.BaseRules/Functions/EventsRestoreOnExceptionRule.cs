using System;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  /// <summary>Detects event disabling that can outlive a throwing operation without guaranteed cleanup.</summary>
  public sealed class EventsRestoreOnExceptionRule : AbstractRule
  {
    public static IRuleInfo Info { get; } = new RuleInfo(nameof(EventsRestoreOnExceptionRule), "Восстановление событий объекта при исключении.");
    private sealed class Listener : IsblBaseListener
    {
      public IReport Report;
      public IDocument Document;
      public override void EnterInvokeStatement(IsblParser.InvokeStatementContext statement)
      {
        if (!TryEventCall(statement, "DisableAll", out var receiver)) return;
        var guardedByFinally = false;
        for (var p = statement.Parent; p != null; p = p.Parent)
        {
          if (p is not IsblParser.TryStatementContext t || t.FINALLY() == null || t.statementBlock().Length < 2 ||
              !IsDescendantOf(statement, t.statementBlock(0))) continue;
          guardedByFinally = true;
          // A FINALLY is not enough by itself: restoration must run before any
          // other cleanup statement can throw and bypass it.
          if (BeginsWithRestore(t.statementBlock(1), statement, receiver) &&
              !MayReassign(t.statementBlock(0), receiver, statement.Stop.StopIndex)) return;
        }
        if (statement.Parent is not IsblParser.StatementBlockContext block)
        {
          if (guardedByFinally) ReportMissingFinallyRestore(statement, receiver);
          return;
        }
        var index = -1;
        for (var i = 0; i < block.ChildCount; i++) if (ReferenceEquals(block.GetChild(i), statement)) { index = i; break; }
        if (index < 0) return;
        for (var i = index + 1; i < block.ChildCount && i <= index + 16; i++)
        {
          var node = block.GetChild(i);
          if (node is IsblParser.InvokeStatementContext call && IsRestoreCall(call, statement, receiver))
          {
            if (i > index + 1)
              Report.Add(Severity.Warning, "F034", $"После {receiver}.Events.DisableAll и до восстановления выполняются другие операции. Если после отключения возникнет исключение, восстановление может не выполниться.",
                Document, statement.invocationCall(1).identifier().GetTextPosition(), RecommendRestoration(receiver));
            return;
          }
          if (MayReassign(node, receiver))
          {
            if (guardedByFinally) ReportMissingFinallyRestore(statement, receiver);
            return;
          }
        }
        if (guardedByFinally) ReportMissingFinallyRestore(statement, receiver);
      }
      private void ReportMissingFinallyRestore(IsblParser.InvokeStatementContext statement, string receiver) =>
        Report.Add(Severity.Warning, "F034", $"После {receiver}.Events.DisableAll не найдено безусловное восстановление того же объекта в FINALLY охватывающей TRY. Исключение может оставить события отключёнными.",
          Document, statement.invocationCall(1).identifier().GetTextPosition(), RecommendRestoration(receiver));

      private static DiagnosticRecommendation RecommendRestoration(string receiver) => new DiagnosticRecommendation(
        $"Если «{receiver}.Events» поддерживает контрольные точки, сохраните состояние через «{receiver}.Events.AddCheckPoint()» перед TRY. В TRY отключите события и выполните нужные операции. Первым действием в FINALLY вызовите «{receiver}.Events.ReleaseCheckPoint()» на том же объекте. Если «{receiver}» переназначается, сохраните исходную ссылку для восстановления.",
        "Проверьте нормальное завершение и исключение после отключения. В обоих случаях набор включённых событий должен совпадать с исходным.");

      private static bool BeginsWithRestore(IsblParser.StatementBlockContext block, IsblParser.InvokeStatementContext disable, string receiver) =>
        block?.ChildCount > 0 && block.GetChild(0) is IsblParser.InvokeStatementContext call && IsRestoreCall(call, disable, receiver);

      private static bool MayReassign(IParseTree node, string receiver, int afterIndex = -1)
      {
        var mutation = new ReceiverMutation { Name = receiver, AfterIndex = afterIndex };
        ParseTreeWalker.Default.Walk(mutation, node);
        return mutation.Found;
      }

      private static bool IsRestoreCall(IsblParser.InvokeStatementContext call, IsblParser.InvokeStatementContext disable, string receiver) =>
        TryEventCall(call, "EnableAll", out var enableReceiver) && string.Equals(receiver, enableReceiver, StringComparison.OrdinalIgnoreCase) ||
        TryEventCall(call, "ReleaseCheckPoint", out var checkpointReceiver) &&
        string.Equals(receiver, checkpointReceiver, StringComparison.OrdinalIgnoreCase) && HasCheckpointBefore(disable, receiver);

      private static bool HasCheckpointBefore(IsblParser.InvokeStatementContext statement, string receiver)
      {
        for (IParseTree node = statement; node != null; node = node.Parent)
        {
          if (node.Parent is not IsblParser.StatementBlockContext block) continue;
          var index = -1;
          for (var i = 0; i < block.ChildCount; i++)
            if (ReferenceEquals(block.GetChild(i), node)) { index = i; break; }
          if (index < 0) continue;
          for (var i = index - 1; i >= 0; i--)
          {
            var prior = block.GetChild(i);
            if (prior is IsblParser.InvokeStatementContext priorCall)
            {
              if (TryEventCall(priorCall, "AddCheckPoint", out var addReceiver) && string.Equals(receiver, addReceiver, StringComparison.OrdinalIgnoreCase)) return true;
              if (TryEventCall(priorCall, "ReleaseCheckPoint", out var releaseReceiver) && string.Equals(receiver, releaseReceiver, StringComparison.OrdinalIgnoreCase)) return false;
            }
            if (ContainsCheckpointCall(prior, receiver) || MayReassign(prior, receiver)) return false;
          }
        }
        return false;
      }

      private static bool ContainsCheckpointCall(IParseTree node, string receiver)
      {
        if (node is IsblParser.InvokeStatementContext call &&
            ((TryEventCall(call, "AddCheckPoint", out var addReceiver) && string.Equals(receiver, addReceiver, StringComparison.OrdinalIgnoreCase)) ||
             (TryEventCall(call, "ReleaseCheckPoint", out var releaseReceiver) && string.Equals(receiver, releaseReceiver, StringComparison.OrdinalIgnoreCase)))) return true;
        for (var i = 0; i < node.ChildCount; i++)
          if (ContainsCheckpointCall(node.GetChild(i), receiver)) return true;
        return false;
      }

      private static bool IsDescendantOf(IParseTree node, IParseTree ancestor)
      {
        for (var parent = node; parent != null; parent = parent.Parent)
          if (ReferenceEquals(parent, ancestor)) return true;
        return false;
      }
      private static bool TryEventCall(IsblParser.InvokeStatementContext statement, string method, out string receiver)
      {
        receiver = null;
        if (statement.variable() == null || statement.indexer() != null || statement.invocationCall().Length != 2) return false;
        var calls = statement.invocationCall();
        if (!string.Equals(calls[0].identifier()?.GetText(), "Events", StringComparison.OrdinalIgnoreCase) || calls[0].parameterList() != null ||
            !string.Equals(calls[1].identifier()?.GetText(), method, StringComparison.OrdinalIgnoreCase)) return false;
        if (calls[1].parameterList() != null && ParameterListParser.GetExpressions(calls[1].parameterList()).Length != 0) return false;
        receiver = statement.variable().GetText(); return true;
      }
    }
    private sealed class ReceiverMutation : IsblBaseListener
    {
      public string Name;
      public int AfterIndex;
      public bool Found;
      public override void EnterAssignStatement(IsblParser.AssignStatementContext assignment)
      {
        if (assignment.Start.StartIndex > AfterIndex && assignment.indexer() == null && assignment.invocationCall().Length == 0 && string.Equals(assignment.variable()?.GetText(), Name, StringComparison.OrdinalIgnoreCase)) Found = true;
      }
      public override void EnterOperand(IsblParser.OperandContext operand)
      {
        if (operand.Start.StartIndex > AfterIndex && operand.POINTER() != null && string.Equals(operand.variable()?.GetText(), Name, StringComparison.OrdinalIgnoreCase)) Found = true;
      }
      public override void EnterForeachStatement(IsblParser.ForeachStatementContext loop)
      {
        if (loop.Start.StartIndex > AfterIndex && string.Equals(loop.variable().GetText(), Name, StringComparison.OrdinalIgnoreCase)) Found = true;
      }
    }
    public override void Apply(IReport report, IDocument document, IContext context) =>
      ParseTreeWalker.Default.Walk(new Listener { Report = report, Document = document }, document.GetSyntaxTree());
  }
}
