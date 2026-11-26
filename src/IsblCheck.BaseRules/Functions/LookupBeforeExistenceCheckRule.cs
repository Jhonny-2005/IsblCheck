using System;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  public sealed class LookupBeforeExistenceCheckRule : AbstractRule
  {
    public static IRuleInfo Info { get; } = new RuleInfo(nameof(LookupBeforeExistenceCheckRule), "Получение значения списка до проверки наличия имени.");
    private sealed class Listener : IsblBaseListener
    {
      public IReport Report;
      public IDocument Document;
      public IContext Context;
      public override void EnterIfStatement(IsblParser.IfStatementContext statement)
      {
        var condition = Unwrap(statement.expression());
        if (condition?.GEQ() == null || condition.expression().Length != 2 || Unwrap(condition.expression(1))?.operand()?.unsignedNumber()?.GetText() != "0") return;
        if (!TryCall(Unwrap(condition.expression(0))?.operand(), "IndexOfName", out var receiver, out var name)) return;
        if (statement.Parent is not IsblParser.StatementBlockContext block) return;
        var index = -1;
        for (var i = 0; i < block.ChildCount; i++) if (ReferenceEquals(block.GetChild(i), statement)) { index = i; break; }
        for (var i = index - 1; i >= 0 && i >= index - 3; i--)
        {
          var node = block.GetChild(i);
          if (node is IsblParser.IfStatementContext || node is IsblParser.WhileStatementContext || node is IsblParser.ForeachStatementContext || node is IsblParser.TryStatementContext || node.GetText().Contains("^", StringComparison.Ordinal)) return;
          if (node is not IsblParser.AssignStatementContext assignment || assignment.variable() == null) return;
          if (assignment.indexer() == null && assignment.invocationCall().Length == 0 && string.Equals(assignment.variable().GetText(), receiver, StringComparison.OrdinalIgnoreCase)) return;
          var value = Unwrap(assignment.expression())?.operand();
          if (assignment.indexer() == null && assignment.invocationCall().Length == 0 &&
              TryCall(value, "ValueByName", out var target, out var key) && string.Equals(receiver, target, StringComparison.OrdinalIgnoreCase) && name == key)
          {
            if (LocalGuardAnalysis.IsProtected(assignment, receiver,
                (guard, truth) => ChecksPresence(guard, truth, receiver, name), Context)) return;
            Report.Add(Severity.Information, "F031", $"«{receiver}.ValueByName» вызывается до проверки IndexOfName того же имени. Последствия зависят от контракта списка.",
              Document, value.invocationCall(0).identifier().GetTextPosition(), new DiagnosticRecommendation(
                $"Проверьте тип «{receiver}» и контракт ValueByName. Если отсутствие имени должно обрабатываться отдельно, проверьте IndexOfName до чтения и явно задайте результат ветви отсутствия.",
                "Проверьте первое имя, отсутствующее имя и существующее имя с пустым значением. Убедитесь, что ветви сохраняют различие между пустым и отсутствующим значением."));
            return;
          }
          // The later lookup may observe a different list state after a call,
          // property/index write or an unknown getter. Do not connect that check
          // to an earlier read across such operations (including possible aliases).
          if (LocalGuardAnalysis.MayChange(assignment, receiver)) return;
        }
      }
      private static bool ChecksPresence(IsblParser.ExpressionContext expression, bool truth, string receiver, string name)
      {
        expression = Unwrap(expression);
        // NOT is bitwise in ISBL: -1 (missing) becomes zero, found indexes become nonzero.
        if (truth && expression?.NOT() != null && expression.expression().Length == 1 &&
            TryCall(Unwrap(expression.expression(0))?.operand(), "IndexOfName", out var notReceiver, out var notName))
          return string.Equals(notReceiver, receiver, StringComparison.OrdinalIgnoreCase) && notName == name;
        if (expression?.expression().Length != 2 || !TryCall(Unwrap(expression.expression(0))?.operand(), "IndexOfName", out var target, out var key) ||
            !string.Equals(receiver, target, StringComparison.OrdinalIgnoreCase) || name != key) return false;
        var right = Unwrap(expression.expression(1))?.GetText();
        return truth
          ? (expression.GEQ() != null && right == "0") || (expression.NEQ() != null && right == "-1") || (expression.GT() != null && right == "-1")
          : (expression.LT() != null && right == "0") || (expression.EQ() != null && right == "-1") || (expression.LEQ() != null && right == "-1");
      }
      private static bool TryCall(IsblParser.OperandContext operand, string method, out string receiver, out string name)
      {
        receiver = null; name = null;
        if (operand?.variable() == null || operand.indexer() != null || operand.POINTER() != null || operand.invocationCall().Length != 1) return false;
        var call = operand.invocationCall(0);
        if (!string.Equals(call.identifier()?.GetText(), method, StringComparison.OrdinalIgnoreCase) || call.parameterList() == null) return false;
        var args = ParameterListParser.GetExpressions(call.parameterList());
        if (args.Length != 1) return false;
        var literal = Unwrap(args[0])?.operand()?.@string()?.GetText();
        if (literal == null || literal.Length < 2) return false;
        receiver = operand.variable().GetText(); name = literal.Substring(1, literal.Length - 2);
        return true;
      }
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
