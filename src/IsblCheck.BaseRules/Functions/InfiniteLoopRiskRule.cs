using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using IsblCheck.BaseRules.Properties;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  public class InfiniteLoopRiskRule : AbstractRule
  {
    private const string Code = "F014";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(InfiniteLoopRiskRule).Name, "Обнаружен возможный бесконечный цикл."), true);

    public static IRuleInfo Info => info.Value;

    private class InfiniteLoopListener : IsblBaseListener
    {
      public List<IsblParser.WhileStatementContext> PotentialInfiniteLoops { get; } = new List<IsblParser.WhileStatementContext>();
      public IContext Context;

      public override void EnterWhileStatement([NotNull] IsblParser.WhileStatementContext context)
      {
        var block = context.statementBlock();
        if (block == null)
          return;
        var condition = context.expression().GetText().Trim('(', ')');
        if ((condition.Equals("True", StringComparison.OrdinalIgnoreCase) || condition == "1" || condition == "-1")
            && !HasExit(block, context))
        {
          PotentialInfiniteLoops.Add(context);
        }
      }

      private bool HasExit(IParseTree node, IsblParser.WhileStatementContext targetLoop)
      {
        if (node is IsblParser.ExitforStatementContext exitfor && ExitsTargetWhile(exitfor, targetLoop)) return true;
        if (node is IsblParser.InvokeStatementContext invoke && invoke.invocationCall().Length == 0 &&
            invoke.function() is { } function)
        {
          var name = function.identifier().GetText();
          if (name.Equals("Exit", StringComparison.OrdinalIgnoreCase) && !Context.HasUserFunction(name))
            return true;
          if (name.Equals("Raise", StringComparison.OrdinalIgnoreCase) && !Context.HasUserFunction(name) &&
              ParameterListParser.GetExpressions(function.parameterList()) is [not null] &&
              !HasHandlerInsideLoop(invoke, targetLoop))
            return true;
        }
        for (var i = 0; i < node.ChildCount; i++)
          if (HasExit(node.GetChild(i), targetLoop)) return true;
        return false;
      }

      private static bool HasHandlerInsideLoop(IParseTree statement, IsblParser.WhileStatementContext loop)
      {
        for (var child = statement; child.Parent != null && !ReferenceEquals(child.Parent, loop); child = child.Parent)
          if (child.Parent is IsblParser.TryStatementContext attempt && attempt.EXCEPT() != null &&
              ReferenceEquals(child, attempt.statementBlock(0)))
            return true;
        return false;
      }

      private static bool ExitsTargetWhile(IsblParser.ExitforStatementContext exitfor, IsblParser.WhileStatementContext targetLoop)
      {
        var exitTarget = InvalidExitForRule.FindTarget(exitfor);
        if (exitTarget == null) return false;
        for (var parent = targetLoop.Parent; parent != null; parent = parent.Parent)
          if (ReferenceEquals(parent, exitTarget))
            return true;
        return false;
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new InfiniteLoopListener { Context = context };
      walker.Walk(listener, tree);

      foreach (var whileStmt in listener.PotentialInfiniteLoops)
      {
        report.Add(Severity.Warning, Code, "Условие WHILE постоянно истинно. В теле не найден распознаваемый выход из этого цикла.",
          document, whileStmt.Start.ToTextPosition(), new DiagnosticRecommendation(
            "Замените постоянное условие проверкой прогресса или окончания обработки. Если вычисление должно обработать конечную коллекцию, рассмотрите FOREACH. EXITFOR выходит из FOREACH и не создаёт самостоятельного выхода из WHILE.",
            "Проверьте пустой набор, последнюю строку и повторение с неизменными входными данными. Если это намеренный длительный процесс, проверьте, как он останавливается."));
      }
    }
  }
}
