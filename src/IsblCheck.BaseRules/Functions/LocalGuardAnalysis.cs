using System;
using System.Linq;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;

namespace IsblCheck.BaseRules.Functions
{
  /// <summary>Recognizes only local, dominating checks whose value has not visibly changed.</summary>
  internal static class LocalGuardAnalysis
  {
    public static bool IsProtected(IParseTree location, string variable,
      Func<IsblParser.ExpressionContext, bool, bool> conditionProtects, IContext context)
    {
      for (var node = location; node?.Parent != null; node = node.Parent)
      {
        // Do not rely on short-circuit evaluation. A check elsewhere in
        // the same expression is not a guard; unknown calls can change values.
        if (node.Parent is IsblParser.ExpressionContext expression && MayChange(expression, variable)) return false;
        if (node.Parent is not IsblParser.StatementBlockContext block) continue;
        var index = -1;
        for (var i = 0; i < block.ChildCount; i++)
          if (ReferenceEquals(block.GetChild(i), node)) { index = i; break; }
        if (index < 0) return false;
        for (var i = index - 1; i >= 0; i--)
        {
          var previous = block.GetChild(i);
          if (previous is IsblParser.IfStatementContext guard && IsExitGuard(guard, context))
          {
            if (conditionProtects(guard.expression(), false)) return true;
            // A pure check followed by a terminating call cannot change the
            // value on the continuing path, even if it guards another value.
            if (!MayChange(guard.expression(), variable)) continue;
          }
          if (MayChange(previous, variable)) return false;
        }
        if (block.Parent is IsblParser.IfStatementContext branch)
        {
          if (conditionProtects(branch.expression(), ReferenceEquals(block, branch.statementBlock(0)))) return true;
          if (MayChange(branch.expression(), variable)) return false;
        }
        // An outer check need not survive a previous iteration or exception path.
        if (block.Parent is IsblParser.WhileStatementContext || block.Parent is IsblParser.ForeachStatementContext ||
            block.Parent is IsblParser.TryStatementContext) return false;
      }
      return false;
    }

    private static bool IsExitGuard(IsblParser.IfStatementContext guard, IContext context)
    {
      if (guard.statementBlock().Length != 1 || guard.statementBlock(0).ChildCount != 1 ||
          guard.statementBlock(0).GetChild(0) is not IsblParser.InvokeStatementContext call || call.invocationCall().Length != 0) return false;
      var name = call.function()?.identifier()?.GetText();
      return (string.Equals(name, "Exit", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Raise", StringComparison.OrdinalIgnoreCase)) &&
        !context.HasUserFunction(name);
    }

    public static bool MayChange(IParseTree node, string variable)
    {
      var visitor = new MutationVisitor { Variable = variable };
      ParseTreeWalker.Default.Walk(visitor, node);
      return visitor.Found;
    }

    private sealed class MutationVisitor : IsblBaseListener
    {
      public string Variable;
      public bool Found;
      public override void EnterAssignStatement(IsblParser.AssignStatementContext assignment)
      {
        if (assignment.variable() == null || assignment.indexer() != null || assignment.invocationCall().Length != 0 ||
            string.Equals(assignment.variable().GetText(), Variable, StringComparison.OrdinalIgnoreCase)) Found = true;
      }
      public override void EnterDeclareVariableStatement(IsblParser.DeclareVariableStatementContext declaration)
      {
        if (string.Equals(declaration.variable()?.GetText(), Variable, StringComparison.OrdinalIgnoreCase)) Found = true;
      }
      public override void EnterFunction(IsblParser.FunctionContext function) => Found = true;
      public override void EnterInvocationCall(IsblParser.InvocationCallContext call) => Found = true;
      public override void EnterOperand(IsblParser.OperandContext operand)
      {
        if (operand.POINTER() != null || operand.indexer() != null) Found = true;
      }
      public override void EnterWhileStatement(IsblParser.WhileStatementContext loop) => Found = true;
      public override void EnterForeachStatement(IsblParser.ForeachStatementContext loop) => Found = true;
      public override void EnterTryStatement(IsblParser.TryStatementContext statement) => Found = true;
    }
  }
}
