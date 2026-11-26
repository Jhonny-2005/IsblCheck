using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;

namespace IsblCheck.Core.Checker
{
  internal sealed class WorkflowParameterAccess : IsblBaseListener
  {
    internal enum Kind { Lookup, Reference, Read, Dereference, Write, Clear }
    internal sealed record Access(string Name, Kind Operation, IParseTree Node, TextPosition Position, bool RequiresDeclaration);
    internal static readonly StringComparer Names = StringComparer.OrdinalIgnoreCase;

    internal List<Access> Accesses { get; } = new();
    internal HashSet<string> Literals { get; } = new(Names);
    internal bool HasOpaqueAccess { get; private set; }
    internal bool MayChangeCatalog { get; private set; }
    internal bool HasEarlyTermination { get; private set; }
    private readonly HashSet<string> collectionAliases = new(Names);

    public override void EnterString(IsblParser.StringContext node)
    {
      var text = node.GetText();
      if (text.Length >= 2)
        Literals.Add(text[1..^1]);
    }

    public override void EnterFunction(IsblParser.FunctionContext node)
    {
      var name = node.identifier().GetText();
      if (Names.Equals(name, "Execute") || Names.Equals(name, "Выполнить"))
        HasOpaqueAccess = true;
      if (Names.Equals(name, "Exit") || Names.Equals(name, "Raise") || Names.Equals(name, "Прервать"))
        HasEarlyTermination = true;
    }

    public override void EnterOperand(IsblParser.OperandContext operand)
    {
      if (operand.variable() == null || operand.indexer() != null || operand.POINTER() != null)
        return;
      var members = operand.invocationCall();
      if (members.Length == 0)
      {
        if (collectionAliases.Contains(operand.variable().GetText()))
        {
          HasOpaqueAccess = true;
          MayChangeCatalog = true;
        }
        return;
      }
      if (IsDirectCollection(operand, operand.variable().GetText(), members) && members.Length == 1 &&
          SimpleAliasTarget(operand) is { } alias)
      {
        collectionAliases.Add(alias);
        return;
      }

      ReadAccess(operand, operand.variable().GetText(), members, null);
    }

    public override void EnterAssignStatement(IsblParser.AssignStatementContext assignment)
    {
      if (assignment.variable() != null && assignment.indexer() == null)
        ReadAccess(assignment, assignment.variable().GetText(), assignment.invocationCall(), assignment.expression());
    }

    public override void EnterInvokeStatement(IsblParser.InvokeStatementContext statement)
    {
      if (statement.variable() != null && statement.indexer() == null)
        ReadAccess(statement, statement.variable().GetText(), statement.invocationCall(), null);
    }

    private void ReadAccess(IParseTree node, string receiver, IsblParser.InvocationCallContext[] members,
      IsblParser.ExpressionContext written)
    {
      int lookupIndex;
      if (IsDirectCollection(node, receiver, members))
        lookupIndex = 1;
      else
      {
        if (members.Length == 0 || !collectionAliases.Contains(receiver))
          return;
        if (!IsCollectionAlias(node, receiver))
        {
          HasOpaqueAccess = true;
          var first = members[0].identifier().GetText();
          MayChangeCatalog |= IsCatalogMutation(first);
          return;
        }
        lookupIndex = 0;
      }

      if (lookupIndex >= members.Length)
      {
        HasOpaqueAccess = true;
        MayChangeCatalog = true;
        return;
      }
      var lookup = members[lookupIndex];
      var method = lookup.identifier().GetText();
      var exactLookup = Names.Equals(method, "ValueByName");
      if (!exactLookup && !Names.Equals(method, "FindItem") && !Names.Equals(method, "IndexOfName"))
      {
        if (!Names.Equals(method, "Count"))
          HasOpaqueAccess = true;
        MayChangeCatalog |= IsCatalogMutation(method);
        return;
      }
      var args = lookup.parameterList();
      if (args?.ChildCount != 1 || Unwrap(args.expression(0))?.operand()?.@string() is not { } literal)
      {
        HasOpaqueAccess = true;
        return;
      }

      var name = literal.GetText()[1..^1];
      var operation = Names.Equals(method, "IndexOfName") ? Kind.Lookup : Kind.Reference;
      if (members.Length > lookupIndex + 1 && Names.Equals(members[lookupIndex + 1].identifier().GetText(), "Value"))
      {
        operation = members.Length == lookupIndex + 2
          ? written == null ? Kind.Read : IsEmpty(written) ? Kind.Clear : Kind.Write
          : written == null ? Kind.Dereference : Kind.Reference;
      }
      if (operation == Kind.Read && node is IsblParser.OperandContext operand && IsReadThenDereferenced(operand))
        operation = Kind.Dereference;
      Accesses.Add(new Access(name, operation, node, Position(literal.Start), exactLookup));
    }

    private static bool IsReadThenDereferenced(IsblParser.OperandContext operand)
    {
      IParseTree value = operand;
      while (value.Parent is IsblParser.ExpressionContext || value.Parent is IsblParser.OperandContext)
        value = value.Parent;
      if (value.Parent is not IsblParser.AssignStatementContext assignment ||
          assignment.variable()?.GetText() is not { } name || !IsBinding(assignment, name) ||
          !ReferenceEquals(Unwrap(assignment.expression())?.operand(), operand) ||
          assignment.Parent is not IsblParser.StatementBlockContext block)
        return false;
      var index = block.children.IndexOf(assignment) + 1;
      if (index >= block.ChildCount)
        return false;
      return block.GetChild(index) switch
      {
        IsblParser.AssignStatementContext next => HasDirectDereference(next.expression(), name),
        IsblParser.InvokeStatementContext next => Names.Equals(next.variable()?.GetText(), name) && next.invocationCall().Length > 0,
        _ => false
      };
    }

    private static bool HasDirectDereference(IsblParser.ExpressionContext expression, string name)
    {
      var operand = Unwrap(expression)?.operand();
      return Names.Equals(operand?.variable()?.GetText(), name) && operand.invocationCall().Length > 0;
    }

    private static bool IsCatalogMutation(string method) => Names.Equals(method, "Add") || Names.Equals(method, "Delete") ||
      Names.Equals(method, "Clear") || Names.Equals(method, "Remove") || Names.Equals(method, "SetVar");

    private static string SimpleAliasTarget(IsblParser.OperandContext operand)
    {
      IParseTree value = operand;
      while (value.Parent is IsblParser.ExpressionContext || value.Parent is IsblParser.OperandContext)
        value = value.Parent;
      return value.Parent is IsblParser.AssignStatementContext assignment &&
        ReferenceEquals(Unwrap(assignment.expression())?.operand(), operand) &&
        assignment.indexer() == null && assignment.invocationCall().Length == 0
        ? assignment.variable()?.GetText() : null;
    }

    private static bool IsDirectCollection(IParseTree use, string receiver, IsblParser.InvocationCallContext[] members)
    {
      if (!Names.Equals(receiver.TrimStart('!'), "Object") || members.Length == 0 ||
          !Names.Equals(members[0].identifier().GetText(), "WorkflowParams") || members[0].L_PAREN() != null)
        return false;
      return LocalOrigin(use, receiver, out var blocked) == null && !blocked;
    }

    private static bool IsCollectionAlias(IParseTree use, string receiver)
    {
      var assignment = LocalOrigin(use, receiver, out _);
      var operand = Unwrap(assignment?.expression())?.operand();
      return operand?.variable() is { } root && operand.indexer() == null && operand.POINTER() == null &&
        operand.invocationCall() is { Length: 1 } members && IsDirectCollection(operand, root.GetText(), members);
    }

    private static IsblParser.AssignStatementContext LocalOrigin(IParseTree use, string name, out bool blocked)
    {
      blocked = false;
      for (var node = use; node.Parent != null; node = node.Parent)
      {
        if (node.Parent is not IsblParser.StatementBlockContext body)
          continue;
        var index = body.children?.IndexOf(node) ?? 0;
        for (var i = index - 1; i >= 0; i--)
        {
          var prior = body.GetChild(i);
          if (prior is IsblParser.AssignStatementContext assignment && IsBinding(assignment, name))
            return assignment;
          if (WritesBinding(prior, name))
          {
            blocked = true;
            return null;
          }
        }
        if ((body.Parent is IsblParser.WhileStatementContext or IsblParser.ForeachStatementContext &&
             WritesBinding(body.Parent, name)) ||
            (body.Parent is IsblParser.TryStatementContext attempt && ReferenceEquals(body, attempt.statementBlock(1)) &&
             WritesBinding(attempt.statementBlock(0), name)))
        {
          blocked = true;
          return null;
        }
      }
      return null;
    }

    private static bool IsBinding(IsblParser.AssignStatementContext assignment, string name) =>
      Names.Equals(assignment.variable()?.GetText(), name) && assignment.indexer() == null && assignment.invocationCall().Length == 0;

    private static bool WritesBinding(IParseTree node, string name) =>
      node switch
      {
        IsblParser.AssignStatementContext assignment when IsBinding(assignment, name) => true,
        IsblParser.ForeachStatementContext loop when Names.Equals(loop.variable().GetText(), name) => true,
        IsblParser.OperandContext operand when operand.POINTER() != null && Names.Equals(operand.variable()?.GetText(), name) => true,
        _ => Enumerable.Range(0, node.ChildCount).Any(i => WritesBinding(node.GetChild(i), name))
      };

    internal static bool IsStraightLine(IParseTree node)
    {
      for (var ancestor = node.Parent; ancestor != null; ancestor = ancestor.Parent)
        if (ancestor is IsblParser.IfStatementContext or IsblParser.WhileStatementContext or
            IsblParser.ForeachStatementContext or IsblParser.TryStatementContext)
          return false;
      return true;
    }

    private static IsblParser.ExpressionContext Unwrap(IsblParser.ExpressionContext expression)
    {
      while (expression?.operand()?.expression() is { } inner)
        expression = inner;
      return expression;
    }

    private static bool IsEmpty(IsblParser.ExpressionContext expression)
    {
      var operand = Unwrap(expression)?.operand();
      var text = operand?.@string()?.GetText();
      return text is { Length: 2 } ||
        (operand?.variable() is { } variable && operand.invocationCall().Length == 0 && operand.indexer() == null &&
         (Names.Equals(variable.GetText(), "nil") || Names.Equals(variable.GetText(), "null")));
    }

    private static TextPosition Position(IToken token) => new()
    {
      Line = token.Line, Column = token.Column, StartIndex = token.StartIndex, EndIndex = token.StopIndex
    };
  }
}
