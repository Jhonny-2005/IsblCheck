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
  public sealed class FinallyRaiseNewExceptionRule : AbstractRule
  {
    public static IRuleInfo Info { get; } = new RuleInfo(nameof(FinallyRaiseNewExceptionRule), "Новое исключение в блоке FINALLY.");
    private sealed class Listener : IsblBaseListener
    {
      public IReport Report;
      public IDocument Document;
      public IContext Context;
      public override void EnterInvokeStatement(IsblParser.InvokeStatementContext statement)
      {
        if (!CanEscapeFinally(statement)) return;
        var raise = statement.function();
        if (statement.invocationCall().Length != 0 || !string.Equals(raise?.identifier()?.GetText(), "Raise", StringComparison.OrdinalIgnoreCase) || IsOverridden("Raise") || IsOverridden("CreateException")) return;
        var args = ParameterListParser.GetExpressions(raise.parameterList());
        if (args.Length != 1) return;
        var expression = args[0];
        while (expression?.operand()?.expression() != null) expression = expression.operand().expression();
        var operand = expression?.operand();
        if (operand?.invocationCall().Length != 0 || !string.Equals(operand?.function()?.identifier()?.GetText(), "CreateException", StringComparison.OrdinalIgnoreCase)) return;
        Report.Add(Severity.Warning, "F036", "При достижении Raise в FINALLY выбрасывается новое исключение. Если TRY завершился с ошибкой, новая ошибка может скрыть исходную причину.",
          Document, raise.identifier().GetTextPosition(), new DiagnosticRecommendation(
            "Оставьте в FINALLY обязательное восстановление состояния. Перенесите проверку бизнес-условия, создающую новое исключение, в основную ветвь. Если сбой произошёл при очистке, сохраните сведения об обеих ошибках и определите, какую передать вызывающему коду.",
            "Проверьте нормальный выход, ошибку в TRY и одновременный сбой в TRY и при очистке. Исходная причина должна быть доступна в журнале или вызывающему коду."));
      }

      private bool CanEscapeFinally(IsblParser.InvokeStatementContext statement)
      {
        for (IParseTree node = statement; node.Parent != null; node = node.Parent)
        {
          if (node.Parent is IsblParser.IfStatementContext conditional && conditional.statementBlock().Length == 2 &&
              ReferenceEquals(node, conditional.statementBlock(1)) && ChecksNoPendingException(conditional))
            return false;
          if (node.Parent is not IsblParser.TryStatementContext attempt)
            continue;
          if (attempt.EXCEPT() != null && ReferenceEquals(node, attempt.statementBlock(0)))
            return false;
          if (attempt.FINALLY() != null && ReferenceEquals(node, attempt.statementBlock(1)))
            return true;
        }
        return false;
      }

      private bool ChecksNoPendingException(IsblParser.IfStatementContext conditional)
      {
        if (IsOverridden("Assigned") || IsOverridden("GetLastException") ||
            conditional.Parent is not IsblParser.StatementBlockContext block ||
            block.Parent is not IsblParser.TryStatementContext owner || owner.FINALLY() == null ||
            !ReferenceEquals(block, owner.statementBlock(1)))
          return false;

        var condition = conditional.expression().operand();
        var call = condition?.function();
        if (call == null || condition.invocationCall().Length != 0 ||
            !string.Equals(call.identifier().GetText(), "Assigned", StringComparison.OrdinalIgnoreCase) ||
            ParameterListParser.GetExpressions(call.parameterList()) is not [var argument])
          return false;
        var checkedValue = argument?.operand();
        if (checkedValue?.variable() == null || checkedValue.indexer() != null ||
            checkedValue.POINTER() != null || checkedValue.invocationCall().Length != 0)
          return false;

        // Only trust the direct GetLastException assignment immediately before IF.
        for (var i = 1; i < block.ChildCount; i++)
        {
          if (!ReferenceEquals(block.GetChild(i), conditional))
            continue;
          if (block.GetChild(i - 1) is not IsblParser.AssignStatementContext previous || previous.indexer() != null ||
              previous.invocationCall().Length != 0 ||
              !string.Equals(previous.variable()?.GetText(), checkedValue.variable().GetText(), StringComparison.OrdinalIgnoreCase))
            return false;
          var value = previous.expression().operand();
          var getException = value?.function();
          return getException != null && value.invocationCall().Length == 0 &&
            string.Equals(getException.identifier().GetText(), "GetLastException", StringComparison.OrdinalIgnoreCase) &&
            ParameterListParser.GetExpressions(getException.parameterList()).Length == 0;
        }
        return false;
      }

      private bool IsOverridden(string name) => Context.HasUserFunction(name);
    }
    public override void Apply(IReport report, IDocument document, IContext context) =>
      ParseTreeWalker.Default.Walk(new Listener { Report = report, Document = document, Context = context }, document.GetSyntaxTree());
  }
}
