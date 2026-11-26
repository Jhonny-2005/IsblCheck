using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;
using static IsblCheck.BaseRules.Functions.LocalQueryOrigin;

namespace IsblCheck.BaseRules.Functions
{
  public sealed class ScriptReloadInLoopRule : AbstractRule
  {
    public static IRuleInfo Info { get; } = new RuleInfo(nameof(ScriptReloadInLoopRule),
      "Повторная загрузка постоянного COM-скрипта в цикле.");

    private sealed class Listener(IReport report, IDocument document, IContext context) : IsblBaseListener
    {
      public override void EnterStatementBlock(IsblParser.StatementBlockContext block)
      {
        for (var i = 1; i < block.ChildCount; i++)
        {
          if (block.GetChild(i) is not IsblParser.InvokeStatementContext load ||
              load.variable()?.GetText() is not { } receiver || GetMember(load, receiver, "AddCode") is not { } code ||
              ParameterListParser.GetExpressions(code.parameterList()) is not [var source] ||
              Unwrap(source)?.operand()?.@string()?.GetText() is not { Length: > 2 } ||
              GetMember(block.GetChild(i - 1), receiver, "Reset") is not { } reset ||
              ParameterListParser.GetExpressions(reset.parameterList()).Length != 0)
            continue;

          if (SqlInLoopRule.FindRepeatedLocation(load) == null || !IsScriptControl(load, receiver))
            continue;

          report.Add(Severity.Information, "F043",
            $"«{receiver}.Reset» и «{receiver}.AddCode» повторяются в цикле. В AddCode каждый раз передаётся один и тот же текст.",
            document, code.identifier().GetTextPosition(), new DiagnosticRecommendation(
              "Оцените загрузку текста один раз до цикла и передачу очередного значения в функцию скрипта. Сохраните изоляцию состояния между записями и точный формат результата. Если Reset нужен для очистки переменных или COM-привязок, сначала проверьте способ передачи аргументов в установленной версии ScriptControl.",
              "Сравните результаты нескольких последовательных вызовов, включая пустое значение и исключение. Измерьте время на типовом количестве строк."));
        }
      }

      private static IsblParser.InvocationCallContext GetMember(IParseTree node, string receiver, string method) =>
        node is IsblParser.InvokeStatementContext call && NameEquals(call.variable()?.GetText(), receiver) &&
        call.indexer() == null && call.invocationCall() is [var member] && NameEquals(member.identifier().GetText(), method)
          ? member : null;

      private bool IsScriptControl(IsblParser.InvokeStatementContext use, string receiver)
      {
        var operand = Unwrap(FindAssignment(use, receiver)?.expression())?.operand();
        var call = operand?.function();
        var name = call?.identifier().GetText();
        if ((!NameEquals(name, "CreateObject") && !NameEquals(name, "СоздатьОбъект")) ||
            context.HasUserFunction(name) || operand.invocationCall().Length != 0 ||
            ParameterListParser.GetExpressions(call.parameterList()) is not [var progId])
          return false;
        var literal = Unwrap(progId)?.operand()?.@string()?.GetText();
        return literal?.Length > 2 && NameEquals(literal[1..^1], "MSScriptControl.ScriptControl");
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context) =>
      ParseTreeWalker.Default.Walk(new Listener(report, document, context), document.GetSyntaxTree());
  }
}
