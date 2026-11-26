using System;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  public class InvalidExitForRule : AbstractRule
  {
    public static IRuleInfo Info { get; } = new RuleInfo(nameof(InvalidExitForRule), "EXITFOR вне FOREACH.");

    internal static IsblParser.ForeachStatementContext FindTarget(IsblParser.ExitforStatementContext statement)
    {
      for (var parent = statement.Parent; parent != null; parent = parent.Parent)
        if (parent is IsblParser.ForeachStatementContext loop)
          return loop;
      return null;
    }

    private sealed class Listener : IsblBaseListener
    {
      public IReport Report;
      public IDocument Document;
      public override void EnterExitforStatement(IsblParser.ExitforStatementContext context)
      {
        if (FindTarget(context) != null) return;
        Report.Add(Severity.Error, "F026", "EXITFOR допустим только внутри цикла FOREACH.", Document, context.GetTextPosition(),
          new DiagnosticRecommendation(
            "Если нужно закончить WHILE, изменяйте состояние, от которого зависит его условие, и не выполняйте оставшуюся часть тела на пути выхода. В FOREACH расположите EXITFOR внутри того цикла, который должен завершиться.",
            "Проверьте выполнение операторов после цикла и отсутствие лишней итерации. Для вложенных циклов проверьте, какой цикл продолжает работу."));
      }
    }
    public override void Apply(IReport report, IDocument document, IContext context) =>
      ParseTreeWalker.Default.Walk(new Listener { Report = report, Document = document }, document.GetSyntaxTree());
  }
}
