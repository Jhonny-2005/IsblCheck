using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  public class DeepNestingRule : AbstractRule
  {
    private const string Code = "F017";
    private const int MaxNestingLevel = 4;

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(DeepNestingRule).Name, "Обнаружена чрезмерная вложенность блоков кода."), true);

    public static IRuleInfo Info => info.Value;

    private class NestingListener : IsblBaseListener
    {
      private int currentLevel;
      private int maximumLevel;
      private int excessRegions;
      private IToken deepestLocation;
      public List<(int Depth, int Regions, IToken Location)> Findings { get; } = new();

      private void Enter(IToken start)
      {
        currentLevel++;
        if (currentLevel == MaxNestingLevel + 1)
          excessRegions++;
        if (currentLevel > maximumLevel)
        {
          maximumLevel = currentLevel;
          deepestLocation = start;
        }
      }

      private void Exit()
      {
        if (--currentLevel != 0)
          return;
        if (excessRegions > 0)
          Findings.Add((maximumLevel, excessRegions, deepestLocation));
        maximumLevel = 0;
        excessRegions = 0;
        deepestLocation = null;
      }

      public override void EnterIfStatement([NotNull] IsblParser.IfStatementContext context) => Enter(context.Start);
      public override void ExitIfStatement([NotNull] IsblParser.IfStatementContext context) => Exit();
      public override void EnterWhileStatement([NotNull] IsblParser.WhileStatementContext context) => Enter(context.Start);
      public override void ExitWhileStatement([NotNull] IsblParser.WhileStatementContext context) => Exit();
      public override void EnterForeachStatement([NotNull] IsblParser.ForeachStatementContext context) => Enter(context.Start);
      public override void ExitForeachStatement([NotNull] IsblParser.ForeachStatementContext context) => Exit();
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var listener = new NestingListener();
      ParseTreeWalker.Default.Walk(listener, document.GetSyntaxTree());

      foreach (var (depth, regions, location) in listener.Findings)
        report.Add(Severity.Information, Code,
          $"Максимальная вложенность IF, WHILE и FOREACH в этом блоке — {depth}. Участков глубже {MaxNestingLevel} уровней — {regions}. Это ориентир для оценки читаемости.",
          document, location.ToTextPosition(), new DiagnosticRecommendation(
            "Начните с показанного участка. Рассмотрите выделение самостоятельного расчёта в функцию с явными параметрами. Сохраняйте порядок ветвей, область действия EXITFOR и выполнение FINALLY.",
            "Повторите сценарии для обеих ветвей, пустой коллекции и досрочного выхода. Сравните результаты и изменения объектов до и после переработки."));
    }
  }
}
