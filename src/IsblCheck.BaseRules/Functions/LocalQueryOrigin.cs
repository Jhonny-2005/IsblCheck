using System;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;

namespace IsblCheck.BaseRules.Functions
{
  /// <summary>Recognizes a direct CreateQuery assignment on the local path to a use.</summary>
  internal static class LocalQueryOrigin
  {
    internal static IsblParser.ExpressionContext Unwrap(IsblParser.ExpressionContext expression)
    {
      while (expression?.operand()?.expression() is { } inner)
        expression = inner;
      return expression;
    }

    internal static bool IsCreatedByBuiltInQuery(IParseTree use, string name, IContext context)
    {
      var operand = Unwrap(FindAssignment(use, name)?.expression())?.operand();
      var call = operand?.function();
      var constructor = call?.identifier().GetText();
      return call != null && operand.invocationCall().Length == 0 &&
        (NameEquals(constructor, "CreateQuery") || NameEquals(constructor, "СоздатьЗапрос")) &&
        !context.HasUserFunction(constructor) &&
        ParameterListParser.GetExpressions(call.parameterList()).Length <= 2;
    }

    internal static IsblParser.AssignStatementContext FindAssignment(IParseTree use, string name)
    {
      for (var node = use; node.Parent != null; node = node.Parent)
      {
        if (node.Parent is IsblParser.WhileStatementContext loop && ReferenceEquals(node, loop.expression()) &&
            WritesVariable(loop.statementBlock(), name))
          return null;
        if (node.Parent is not IsblParser.StatementBlockContext block)
          continue;

        var index = block.ChildCount - 1;
        while (index >= 0 && !ReferenceEquals(block.GetChild(index), node))
          index--;

        for (var i = index - 1; i >= 0; i--)
        {
          var prior = block.GetChild(i);
          if (prior is IsblParser.AssignStatementContext assignment && IsDirectAssignment(assignment, name))
            return assignment;

          if (WritesVariable(prior, name))
            return null;
        }

        // A local definition inside the loop was already considered. An outer
        // definition is sufficient only if it survives every loop iteration.
        if (block.Parent is IsblParser.WhileStatementContext or IsblParser.ForeachStatementContext &&
            WritesVariable(block.Parent, name))
          return null;

        // An exception handler may observe a different binding than the TRY entry.
        if (block.Parent is IsblParser.TryStatementContext attempt &&
            ReferenceEquals(block, attempt.statementBlock(1)) && WritesVariable(attempt.statementBlock(0), name))
          return null;
      }

      return null;
    }

    internal static string SimpleName(IsblParser.ExpressionContext expression)
    {
      var operand = Unwrap(expression)?.operand();
      return operand?.indexer() == null && operand?.POINTER() == null && operand?.invocationCall().Length == 0
        ? operand.variable()?.GetText() : null;
    }

    internal static bool IsDirectAssignment(IsblParser.AssignStatementContext assignment, string name) =>
      assignment.indexer() == null && assignment.invocationCall().Length == 0 &&
      NameEquals(assignment.variable()?.GetText(), name);

    internal static bool NameEquals(string actual, string expected) =>
      string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

    internal static bool WritesVariable(IParseTree node, string name)
    {
      switch (node)
      {
        case IsblParser.AssignStatementContext assignment when IsDirectAssignment(assignment, name):
        case IsblParser.DeclareVariableStatementContext declaration when NameEquals(declaration.variable().GetText(), name):
        case IsblParser.ForeachStatementContext loop when NameEquals(loop.variable().GetText(), name):
        case IsblParser.OperandContext pointer when pointer.POINTER() != null && NameEquals(pointer.variable()?.GetText(), name):
          return true;
      }

      for (var i = 0; i < node.ChildCount; i++)
        if (WritesVariable(node.GetChild(i), name))
          return true;
      return false;
    }
  }
}
