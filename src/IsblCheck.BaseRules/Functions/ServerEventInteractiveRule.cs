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
  public sealed class ServerEventInteractiveRule : AbstractRule
  {
    public static IRuleInfo Info { get; } = new RuleInfo(nameof(ServerEventInteractiveRule), "Визуальный вызов в сценарии серверного события.");
    private static readonly HashSet<string> VisualFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { "MessageBox", "MessageBoxEx", "ShowMessage", "InputDialog", "InputDialogEx", "ShowDialog", "SelectSQL", "CSelectSQL", "Ввод", "ВводМеню", "ВыборSQL", "ДиалогДаНет", "Меню", "МенюРасш", "Окно", "ПВыборSQL", "РедТекст", "РедактироватьРТФ" };
    private sealed class Listener : IsblBaseListener
    {
      public IReport Report;
      public IDocument Document;
      public IContext Context;
      public override void EnterFunction(IsblParser.FunctionContext call)
      {
        var name = call.identifier()?.GetText();
        if (!VisualFunctions.Contains(name) || IsOverridden(name) || IsInteractiveGuarded(call)) return;
        Report.Add(Severity.Warning, "F035",
          $"Сценарий привязан к серверному событию и содержит визуальный вызов «{name}». Если ветвь достижима в службе обработки событий, окно будет недоступно.",
          Document, call.identifier().GetTextPosition(), new DiagnosticRecommendation(
            $"Уберите зависимость серверной ветви от «{name}». Ввод передавайте через параметры события. Ошибки и уведомления обрабатывайте средствами, доступными службе. Если сценарий общий с клиентом, отделите интерактивную ветвь и предоставьте рабочую серверную ветвь.",
            "Запустите обработчик через службу обработки событий и проверьте результаты успешного запуска и сбоя в истории событий. При совместном использовании отдельно проверьте клиентский вызов."));
      }
      private bool IsOverridden(string name) => Context.HasUserFunction(name);
      private bool IsInteractiveGuarded(IParseTree tree)
      {
        if (IsOverridden("InteractiveMode")) return false;
        for (var node = tree; node?.Parent != null; node = node.Parent)
          if (node.Parent is IsblParser.IfStatementContext branch && ReferenceEquals(node, branch.statementBlock(0)) && RequiresInteractive(branch.expression())) return true;
        return false;
      }
      private static bool RequiresInteractive(IsblParser.ExpressionContext expression)
      {
        while (expression?.operand()?.expression() != null) expression = expression.operand().expression();
        if (expression?.AND() != null && expression.expression().Length == 2)
          return RequiresInteractive(expression.expression(0)) || RequiresInteractive(expression.expression(1));
        var operand = expression?.operand();
        var function = operand?.function();
        return function != null && operand.invocationCall().Length == 0 && string.Equals(function.identifier()?.GetText(), "InteractiveMode", StringComparison.OrdinalIgnoreCase) && ParameterListParser.GetExpressions(function.parameterList()).Length == 0;
      }
    }
    public override void Apply(IReport report, IDocument document, IContext context)
    {
      if (document.ComponentType != ComponentType.Script || string.IsNullOrWhiteSpace(document.ComponentName) ||
          context.Development?.Scripts?.Any(s => string.Equals(s.Name, document.ComponentName, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(s.ServerEventName)) != true) return;
      ParseTreeWalker.Default.Walk(new Listener { Report = report, Document = document, Context = context }, document.GetSyntaxTree());
    }
  }
}
