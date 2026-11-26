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
  /// <summary>
  /// Finds two documented ISBL reference-lifetime hazards: a child/detail row
  /// still held when its parent is released, and query field wrappers kept
  /// across reopening the same IQuery.
  /// </summary>
  public sealed class ObjectReferenceCleanupRule : AbstractRule
  {
    private const string Code = "F038";
    public static IRuleInfo Info { get; } = new RuleInfo(nameof(ObjectReferenceCleanupRule), "Очистка ссылок на дочерние объекты ISBL перед освобождением родителя.");

    private sealed class IteratorReference
    {
      public string Owner;
      public string DetailSet;
    }

    private sealed class Listener : IsblBaseListener
    {
      public IReport Report;
      public IDocument Document;
      public bool CreateQueryIsBuiltIn;

      public override void EnterStatementBlock(IsblParser.StatementBlockContext block) => AnalyzeBlock(block);

      private void AnalyzeBlock(IsblParser.StatementBlockContext block)
      {
        // This intentionally follows only direct assignments and calls in one block.
        // It does not infer aliases, branch joins, or arbitrary COM return types.
        var detailSetOwners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var foreachReferences = new Dictionary<string, IteratorReference>(StringComparer.OrdinalIgnoreCase);
        var queryFieldOwners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var queryVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var closedQueries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var alreadyReportedReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < block.ChildCount; i++)
        {
          var node = block.GetChild(i);
          if (node is IsblParser.AssignStatementContext assignment && IsSimpleTarget(assignment, out var target))
          {
            var value = assignment.expression();
            if (TryGetParentRelease(target, value, detailSetOwners, foreachReferences, out var heldNames))
            {
              heldNames = heldNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
              heldNames.RemoveAll(alreadyReportedReferences.Contains);
              if (heldNames.Count > 0)
              {
                var operation = IsNil(value) ? "очищается через nil" : "заменяется";
                var cleanup = string.Join(", ", heldNames.OrderBy(name => foreachReferences.ContainsKey(name) ? 0 : 1)
                  .Select(name => $"«{name} = nil»"));
                Report.Add(Severity.Warning, Code,
                  $"Ссылка на объект «{target}» {operation}, пока могут оставаться связанные дочерние ссылки ({string.Join(", ", heldNames)}). После последнего использования очистите дочерние ссылки через nil в обратном порядке создания. Проверьте фактические типы объектов и все алиасы.",
                  Document, assignment.variable().GetTextPosition(), new DiagnosticRecommendation(
                    $"Если дочерние ссылки больше не нужны, перед освобождением «{target}» выполните {cleanup} в указанном порядке. Сначала проверьте последние чтения этих переменных.",
                    "Проверьте пустой и непустой набор, а также выходы из ветвей. К моменту освобождения родителя дочерние ссылки не должны оставаться в других переменных."));
                foreach (var heldName in heldNames) alreadyReportedReferences.Add(heldName);
              }
            }

            var selfAssignment = TryGetSimpleVariable(value, out var sourceName) && string.Equals(sourceName, target, StringComparison.OrdinalIgnoreCase);
            if (selfAssignment) continue;

            // Any direct reassignment releases the previous local reference unless
            // it is immediately replaced by another known DetailDataSet reference.
            foreachReferences.Remove(target);
            alreadyReportedReferences.Remove(target);
            queryFieldOwners.Remove(target);
            detailSetOwners.Remove(target);
            queryVariables.Remove(target);
            closedQueries.Remove(target);
            // Existing fields belong to the old query, including when the name is reused.
            foreach (var field in queryFieldOwners.Where(pair => string.Equals(pair.Value, target, StringComparison.OrdinalIgnoreCase)).Select(pair => pair.Key).ToArray())
              queryFieldOwners.Remove(field);
            if (TryGetDetailSetOwner(value, out var owner)) detailSetOwners[target] = owner;
            if (TryGetQueryFieldsOwner(value, out var query) && queryVariables.Contains(query)) queryFieldOwners[target] = query;
            var operand = Unwrap(value)?.operand();
            if (CreateQueryIsBuiltIn && operand?.invocationCall().Length == 0 &&
                string.Equals(operand.function()?.identifier().GetText(), "CreateQuery", StringComparison.OrdinalIgnoreCase))
              queryVariables.Add(target);
            continue;
          }

          if (node is IsblParser.ForeachStatementContext loop && TryGetSimpleVariable(loop.expression(), out var source) &&
              detailSetOwners.TryGetValue(source, out var sourceOwner))
          {
            var iterator = loop.variable().GetText();
            alreadyReportedReferences.Remove(iterator);
            foreachReferences[iterator] = new IteratorReference { Owner = sourceOwner, DetailSet = source };
            continue;
          }

          if (node is IsblParser.ForeachStatementContext directLoop && TryGetDetailSetOwner(directLoop.expression(), out var directOwner))
          {
            var iterator = directLoop.variable().GetText();
            alreadyReportedReferences.Remove(iterator);
            foreachReferences[iterator] = new IteratorReference { Owner = directOwner };
            continue;
          }

          if (node is IsblParser.InvokeStatementContext call && TryGetObjectMethod(call, out var receiver, out var method) &&
              queryVariables.Contains(receiver))
          {
            if (method.Equals("Close", StringComparison.OrdinalIgnoreCase))
              closedQueries.Add(receiver);
            else if (method.Equals("Open", StringComparison.OrdinalIgnoreCase) && closedQueries.Remove(receiver))
            {
              var fields = queryFieldOwners.Where(pair => string.Equals(pair.Value, receiver, StringComparison.OrdinalIgnoreCase))
                .Select(pair => pair.Key).ToArray();
              if (fields.Length > 0)
                Report.Add(Severity.Warning, Code,
                  $"После закрытия запроса «{receiver}» он открывается повторно, но переменная(ые) поля запроса {string.Join(", ", fields)} ещё могут удерживать ссылки на старые поля. После Close очистите эти переменные через nil до следующего Open; проверьте, что нет других алиасов.",
                  Document, call.invocationCall(0).identifier().GetTextPosition(), new DiagnosticRecommendation(
                    $"Между «{receiver}.Close» и повторным «{receiver}.Open» очистите ссылки {string.Join(", ", fields.Select(field => $"«{field} = nil»"))} после их последнего использования. Ссылку на поле для нового результата получите после Open.",
                    "Откройте запрос повторно с новым набором результатов и проверьте, что чтение использует только поля нового результата."));
            }
          }
        }
      }

      private static bool TryGetParentRelease(string name, IsblParser.ExpressionContext value,
        IReadOnlyDictionary<string, string> detailSetOwners,
        IReadOnlyDictionary<string, IteratorReference> foreachReferences,
        out List<string> heldNames)
      {
        heldNames = new List<string>();
        foreach (var pair in detailSetOwners)
          if (!string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase) &&
              string.Equals(pair.Value, name, StringComparison.OrdinalIgnoreCase)) heldNames.Add(pair.Key);
        foreach (var pair in foreachReferences)
          if (string.Equals(pair.Value.Owner, name, StringComparison.OrdinalIgnoreCase) ||
              string.Equals(pair.Value.DetailSet, name, StringComparison.OrdinalIgnoreCase)) heldNames.Add(pair.Key);
        if (heldNames.Count == 0) return false;
        if (IsNil(value)) return true;
        // Self-assignment does not release the old reference.
        return !TryGetSimpleVariable(value, out var source) || !string.Equals(source, name, StringComparison.OrdinalIgnoreCase);
      }

      private static bool IsSimpleTarget(IsblParser.AssignStatementContext assignment, out string name)
      {
        name = null;
        if (assignment.variable() == null || assignment.indexer() != null || assignment.invocationCall().Length != 0 || assignment.function() != null) return false;
        name = assignment.variable().GetText();
        return !string.IsNullOrWhiteSpace(name);
      }

      private static bool IsNil(IsblParser.ExpressionContext expression) =>
        TryGetSimpleVariable(expression, out var value) &&
        value.Equals("nil", StringComparison.OrdinalIgnoreCase);

      private static bool TryGetSimpleVariable(IsblParser.ExpressionContext expression, out string name)
      {
        name = null;
        var operand = Unwrap(expression)?.operand();
        if (operand?.variable() == null || operand.indexer() != null || operand.invocationCall().Length != 0 || operand.POINTER() != null) return false;
        name = operand.variable().GetText();
        return !string.IsNullOrWhiteSpace(name);
      }

      private static bool TryGetDetailSetOwner(IsblParser.ExpressionContext expression, out string owner)
      {
        owner = null;
        var operand = Unwrap(expression)?.operand();
        var calls = operand?.invocationCall();
        if (operand?.variable() == null || operand.indexer() != null || operand.POINTER() != null || calls?.Length != 1 ||
            !calls[0].identifier().GetText().Equals("DetailDataSet", StringComparison.OrdinalIgnoreCase) || calls[0].parameterList() == null)
          return false;
        owner = operand.variable().GetText();
        return true;
      }

      private static bool TryGetQueryFieldsOwner(IsblParser.ExpressionContext expression, out string owner)
      {
        owner = null;
        var operand = Unwrap(expression)?.operand();
        var calls = operand?.invocationCall();
        if (operand?.variable() == null || operand.indexer() != null || operand.POINTER() != null || calls?.Length != 1 ||
            !calls[0].identifier().GetText().Equals("Fields", StringComparison.OrdinalIgnoreCase) || calls[0].parameterList() == null)
          return false;
        owner = operand.variable().GetText();
        return true;
      }

      private static bool TryGetObjectMethod(IsblParser.InvokeStatementContext call, out string receiver, out string method)
      {
        receiver = null;
        method = null;
        if (call.variable() == null || call.indexer() != null || call.invocationCall().Length != 1 || call.function() != null) return false;
        receiver = call.variable().GetText();
        method = call.invocationCall(0).identifier().GetText();
        return true;
      }

      private static IsblParser.ExpressionContext Unwrap(IsblParser.ExpressionContext expression)
      {
        while (expression?.operand()?.expression() != null) expression = expression.operand().expression();
        return expression;
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var createQueryIsBuiltIn = !context.HasUserFunction("CreateQuery");
      ParseTreeWalker.Default.Walk(new Listener { Report = report, Document = document, CreateQueryIsBuiltIn = createQueryIsBuiltIn }, document.GetSyntaxTree());
    }
  }
}
