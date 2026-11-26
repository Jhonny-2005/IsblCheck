using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Tree;
using IsblCheck.BaseRules.Properties;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Variables
{
  internal class UsingNotAssignedVarRule : AbstractRule
  {
    public static IRuleInfo Info { get; } = new RuleInfo(nameof(UsingNotAssignedVarRule), Resources.UsingNotAssignedVarRuleDescription);

    private sealed class State
    {
      public HashSet<string> Definite = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      public HashSet<string> Possible = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      public State Copy() => new State { Definite = new HashSet<string>(Definite, StringComparer.OrdinalIgnoreCase), Possible = new HashSet<string>(Possible, StringComparer.OrdinalIgnoreCase) };
      public void Assign(string name) { Definite.Add(name); Possible.Add(name); }
    }
    private sealed class Flow
    {
      public State Normal;
      public List<State> Breaks = new List<State>();
      public HashSet<string> Possible = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
    private sealed class Analysis
    {
      private readonly IReport report;
      private readonly IDocument document;
      private readonly IContext context;
      private readonly bool exitIsBuiltIn;
      private readonly bool raiseIsBuiltIn;
      private readonly HashSet<int> reported = new HashSet<int>();
      public Analysis(IReport report, IDocument document, IContext context)
      {
        this.report = report;
        this.document = document;
        this.context = context;
        this.exitIsBuiltIn = !HasUserFunction("Exit");
        this.raiseIsBuiltIn = !HasUserFunction("Raise");
      }

      private bool HasUserFunction(string name) => context.HasUserFunction(name);
      private static State Merge(IEnumerable<State> paths)
      {
        var states = paths.Where(s => s != null).ToList();
        if (states.Count == 0) return null;
        var result = states[0].Copy();
        foreach (var state in states.Skip(1)) { result.Definite.IntersectWith(state.Definite); result.Possible.UnionWith(state.Possible); }
        return result;
      }
      private void Read(IParseTree node, State state)
      {
        if (node == null || state == null) return;
        if (node is IsblParser.VariableContext variable)
        {
          var name = variable.GetText();
          if (!context.IsUserVariable(document, name) || state.Definite.Contains(name) || !reported.Add(variable.Start.StartIndex)) return;
          var uncertain = state.Possible.Contains(name);
          // The bundled platform catalog is version-dependent and incomplete. An unknown
          // SYSREQ name may be a newer system constant or a typo; do not claim certainty.
          var possibleSystemConstant = !uncertain && name.TrimStart('!').StartsWith("SYSREQ_", StringComparison.OrdinalIgnoreCase);
          var description = possibleSystemConstant
            ? $"Имя \"{name}\" не найдено в доступном каталоге. Это может быть системная константа; проверьте её в справке установленной версии Directum."
            : uncertain
              ? $"Переменная \"{name}\" может остаться без присваивания на некоторых путях. Проверьте ветвления и контекст вычисления."
              : $"Для имени \"{name}\" не найдено предшествующее локальное присваивание или известное контекстное определение. Проверьте параметры, системные имена и полноту контекста.";
          // An unknown name may come from runtime context or an incomplete platform catalog;
          // lack of a visible definition is not proof of an interpreter error.
          var recommendation = possibleSystemConstant
            ? new DiagnosticRecommendation(
              $"Сверьте написание «{name}» и наличие константы в справке версии Directum. Если имя задано верно, проверьте полноту каталога анализатора.",
              "Повторите анализ с каталогом целевой версии и проверьте вычисление в IS-Builder.")
            : uncertain
              ? new DiagnosticRecommendation(
                $"Найдите пути к чтению «{name}» без присваивания, включая пустой цикл и обработку исключения. Задайте на этих путях значение по смыслу вычисления или перенесите чтение в ветвь, где значение уже получено.",
                "Выполните обе ветви, случай пустых данных и обработку ошибки, если эти пути есть. Проверьте, что чтение получает ожидаемое значение на каждом пути.")
              : new DiagnosticRecommendation(
                $"Проверьте, откуда должно поступать «{name}». Для локальной переменной добавьте присваивание до первого чтения. Для параметра или переменной события проверьте объявление и загрузите метаданные этого вычисления.",
                "Запустите вычисление из его обычного компонента или события с типовым набором параметров и повторите анализ с доступным контекстом.");
          report.Add(Severity.Warning, "A001",
            description, document, variable.Start.ToTextPosition(), recommendation);
          return;
        }
        for (var i = 0; i < node.ChildCount; i++) Read(node.GetChild(i), state);
      }
      public Flow Block(IsblParser.StatementBlockContext block, State input)
      {
        var result = new Flow { Normal = input.Copy() };
        result.Possible.UnionWith(input.Possible);
        for (var i = 0; i < block.ChildCount && result.Normal != null; i++)
        {
          var next = Statement(block.GetChild(i), result.Normal);
          result.Normal = next.Normal;
          result.Breaks.AddRange(next.Breaks);
          result.Possible.UnionWith(next.Possible);
          if (next.Normal != null) result.Possible.UnionWith(next.Normal.Possible);
        }
        return result;
      }
      private Flow Statement(IParseTree node, State state)
      {
        if (node is IsblParser.IfStatementContext conditional)
        {
          Read(conditional.expression(), state);
          var yes = Block(conditional.statementBlock(0), state);
          var no = conditional.statementBlock().Length == 2 ? Block(conditional.statementBlock(1), state) : new Flow { Normal = state.Copy() };
          var result = new Flow { Normal = Merge(new[] { yes.Normal, no.Normal }), Breaks = yes.Breaks.Concat(no.Breaks).ToList() };
          result.Possible.UnionWith(yes.Possible); result.Possible.UnionWith(no.Possible);
          return result;
        }
        if (node is IsblParser.WhileStatementContext loop)
        {
          Read(loop.expression(), state);
          return Loop(Block(loop.statementBlock(), state), state, consumesBreaks: false);
        }
        if (node is IsblParser.ForeachStatementContext each)
        {
          Read(each.expression(), state); // The iterator does not exist while evaluating the collection.
          var bodyInput = state.Copy(); bodyInput.Assign(each.variable().GetText());
          return Loop(Block(each.statementBlock(), bodyInput), state, consumesBreaks: true);
        }
        if (node is IsblParser.TryStatementContext attempt)
        {
          var body = Block(attempt.statementBlock(0), state);
          var exceptional = state.Copy(); exceptional.Possible.UnionWith(body.Possible);
          if (attempt.EXCEPT() != null)
          {
            var handler = Block(attempt.statementBlock(1), exceptional);
            var result = new Flow { Normal = Merge(new[] { body.Normal, handler.Normal }), Breaks = body.Breaks.Concat(handler.Breaks).ToList() };
            result.Possible.UnionWith(body.Possible); result.Possible.UnionWith(handler.Possible);
            return result;
          }
          // FINALLY also executes when the body failed before completing any assignment.
          var finalInput = Merge(new[] { exceptional, body.Normal }.Concat(body.Breaks));
          var final = Block(attempt.statementBlock(1), finalInput);
          State Transfer(State path)
          {
            if (path == null || final.Normal == null) return null;
            var output = path.Copy();
            output.Definite.UnionWith(final.Normal.Definite.Except(finalInput.Definite));
            output.Possible.UnionWith(final.Normal.Possible);
            return output;
          }
          var finalResult = new Flow { Normal = Transfer(body.Normal), Breaks = body.Breaks.Select(Transfer).Where(s => s != null).Concat(final.Breaks).ToList() };
          finalResult.Possible.UnionWith(body.Possible); finalResult.Possible.UnionWith(final.Possible);
          return finalResult;
        }
        if (node is IsblParser.ExitforStatementContext)
          return new Flow { Breaks = new List<State> { state.Copy() } };
        if (node is IsblParser.DeclareVariableStatementContext declaration)
          state.Assign(declaration.variable().GetText());
        else if (node is IsblParser.AssignStatementContext assignment)
        {
          if (assignment.variable() != null && assignment.indexer() == null && assignment.invocationCall().Length == 0)
          {
            Read(assignment.expression(), state);
            state.Assign(assignment.variable().GetText());
          }
          else Read(assignment, state); // A member/index write needs an existing receiver.
        }
        else
        {
          Read(node, state);
          if (node is IsblParser.InvokeStatementContext invoke && invoke.invocationCall().Length == 0)
          {
            var name = invoke.function()?.identifier().GetText();
            if ((exitIsBuiltIn && string.Equals(name, "Exit", StringComparison.OrdinalIgnoreCase)) ||
                (raiseIsBuiltIn && string.Equals(name, "Raise", StringComparison.OrdinalIgnoreCase)))
              return new Flow();
          }
        }
        return new Flow { Normal = state };
      }
      private static Flow Loop(Flow body, State input, bool consumesBreaks)
      {
        // A WHILE may enclose EXITFOR, but only a FOREACH consumes that exit.
        var normalPaths = new[] { input, body.Normal }.AsEnumerable();
        if (consumesBreaks) normalPaths = normalPaths.Concat(body.Breaks);
        var result = new Flow { Normal = Merge(normalPaths) };
        if (!consumesBreaks) result.Breaks.AddRange(body.Breaks);
        result.Possible.UnionWith(body.Possible);
        return result;
      }
      public void Run(IParseTree tree)
      {
        if (tree is IsblParser.StatementBlockContext block) Block(block, new State());
        else Read(tree, new State());
      }
    }
    public override void Apply(IReport report, IDocument document, IContext context) =>
      new Analysis(report, document, context).Run(document.GetSyntaxTree());
  }
}
