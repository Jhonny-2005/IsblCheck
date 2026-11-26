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
  /// <summary>Detects a direct numeric index used as a boolean in a narrow, locally established context.</summary>
  public sealed class IndexOfNameConditionRule : AbstractRule
  {
    public static IRuleInfo Info { get; } = new RuleInfo(nameof(IndexOfNameConditionRule), "Результат IndexOfName именованного списка IList в условии без сравнения.");

    private sealed class Listener : IsblBaseListener
    {
      public IReport Report;
      public IDocument Document;
      public IContext Context;
      private bool CreateListIsOverridden() => Context.HasUserFunction("CreateList");

      public override void EnterIfStatement(IsblParser.IfStatementContext context) => Check(context, context.expression());
      public override void EnterWhileStatement(IsblParser.WhileStatementContext context) => Check(context, context.expression());

      private void Check(Antlr4.Runtime.ParserRuleContext statement, IsblParser.ExpressionContext expression)
      {
        var current = expression;
        // Unwrap parentheses only. ISBL NOT is bitwise: NOT(-1)=0, so a
        // negated index can be an intentional existence test and must not be flagged.
        while (current != null)
        {
          if (current.operand()?.expression() != null) { current = current.operand().expression(); continue; }
          break;
        }
        var operand = current?.operand();
        if (operand?.variable() == null || operand.indexer() != null || operand.invocationCall().Length != 1) return;
        var call = operand.invocationCall(0);
        if (!string.Equals(call.identifier()?.GetText(), "IndexOfName", StringComparison.OrdinalIgnoreCase) || call.parameterList() == null) return;
        var name = operand.variable().GetText();
        if (!HasNearbyListDefinition(statement, name, CreateListIsOverridden)) return;
        Report.Add(Severity.Warning, "F028", $"Результат «{name}.IndexOfName» используется как условие. В IList индекс 0 соответствует первому элементу, а −1 означает отсутствие имени.",
          Document, call.identifier().GetTextPosition(), new DiagnosticRecommendation(
            $"Если условие проверяет наличие имени в «{name}», явно сравните результат IndexOfName с 0 через >=. Для проверки отсутствия используйте < 0. Сначала проверьте, какой из этих случаев нужен ветви.",
            "Проверьте имя с индексом 0, имя в середине списка и отсутствующее имя. Для WHILE также проверьте, что цикл меняет состояние и завершается."));
      }

      private static bool HasNearbyListDefinition(Antlr4.Runtime.ParserRuleContext statement, string name, Func<bool> createListIsOverridden)
      {
        if (statement.Parent is not IsblParser.StatementBlockContext block) return false;
        var foundStatement = false;
        for (var i = block.ChildCount - 1; i >= 0; i--)
        {
          var node = block.GetChild(i);
          if (!foundStatement) { if (ReferenceEquals(node, statement)) foundStatement = true; continue; }
          // Do not infer a type through branches, loops, exception handlers or by-reference calls.
          if (node is IsblParser.IfStatementContext || node is IsblParser.WhileStatementContext || node is IsblParser.ForeachStatementContext || node is IsblParser.TryStatementContext || node.GetText().Contains("^", StringComparison.Ordinal)) return false;
          if (node is IsblParser.AssignStatementContext assignment && assignment.variable() != null &&
              assignment.indexer() == null && assignment.invocationCall().Length == 0 &&
              string.Equals(assignment.variable().GetText(), name, StringComparison.OrdinalIgnoreCase))
          {
            if (string.Equals(assignment.typedefinition()?.GetText(), "IList", StringComparison.OrdinalIgnoreCase)) return true;
            var operand = assignment.expression()?.operand();
            var function = operand?.function();
            return function != null && operand.invocationCall().Length == 0 &&
              string.Equals(function.identifier()?.GetText(), "CreateList", StringComparison.OrdinalIgnoreCase) && !createListIsOverridden();
          }
          if (node is IsblParser.DeclareVariableStatementContext declaration && string.Equals(declaration.variable()?.GetText(), name, StringComparison.OrdinalIgnoreCase))
            return string.Equals(declaration.typedefinition()?.GetText(), "IList", StringComparison.OrdinalIgnoreCase);
        }
        return false;
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context) =>
      ParseTreeWalker.Default.Walk(new Listener { Report = report, Document = document, Context = context }, document.GetSyntaxTree());
  }
}
