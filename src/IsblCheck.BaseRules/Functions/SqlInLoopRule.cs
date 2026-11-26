using System;
using System.Text.RegularExpressions;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  /// <summary>Identifies repeated database calls as profiling candidates, not confirmed bottlenecks.</summary>
  public sealed class SqlInLoopRule : AbstractRule
  {
    public static IRuleInfo Info { get; } = new RuleInfo(nameof(SqlInLoopRule), "SQL-запрос в цикле: возможная оптимизация.");
    private static readonly Regex SqlStart = new(@"^\s*(?<keyword>SELECT|INSERT|UPDATE|DELETE|MERGE|EXEC(?:UTE)?|(?:CREATE|ALTER|DROP|TRUNCATE))\b",
      RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex CountStart = new(@"^\s*SELECT\s+COUNT(?:_BIG)?\s*\(",
      RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex CountNullColumn = new(@"^\s*SELECT\s+COUNT(?:_BIG)?\s*\(\s*\*\s*\)\s+FROM\s+\S+\s+WHERE\s+\[?%(?:\d+:)?s\]?\s+IS\s+NULL\s*;?\s*\z",
      RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    private enum Operation { Unknown, Select, Count, CountNullColumn, Write, Interactive, Export }

    internal static string FindRepeatedLocation(IParseTree call)
    {
      for (var node = call; node.Parent != null; node = node.Parent)
      {
        if (node.Parent is IsblParser.WhileStatementContext loop && ReferenceEquals(node, loop.expression()))
          return "условии WHILE";
        if (node is IsblParser.StatementBlockContext block &&
            (block.Parent is IsblParser.WhileStatementContext || block.Parent is IsblParser.ForeachStatementContext))
          return "теле цикла";
      }
      return null;
    }

    private sealed class Listener : IsblBaseListener
    {
      public IReport Report;
      public IDocument Document;
      public IContext Context;
      public override void EnterFunction(IsblParser.FunctionContext function)
      {
        var name = function.identifier()?.GetText();
        if (name == null || !SqlInjectionRiskRule.TryGetQueryArgument(name, out var queryIndex) || Context.HasUserFunction(name))
          return;
        var location = FindRepeatedLocation(function);
        if (location == null) return;
        var arguments = ParameterListParser.GetExpressions(function.parameterList());
        if (arguments.Length <= queryIndex || arguments[queryIndex] == null) return;
        var operation = queryIndex != 0 ? Operation.Interactive :
          Equal(name, "SQL2XLSTAB") ? Operation.Export : Classify(ResolveText(arguments[queryIndex], function));
        AddFinding(name, location, function.identifier().GetTextPosition(), operation);
      }

      public override void EnterInvokeStatement(IsblParser.InvokeStatementContext statement)
      {
        if (statement.variable() == null || statement.indexer() != null || statement.invocationCall().Length != 1)
          return;
        var member = statement.invocationCall(0);
        var method = member.identifier().GetText();
        var execute = Equal(method, "Execute");
        if ((!execute && !Equal(method, "Open")) ||
            ParameterListParser.GetExpressions(member.parameterList()).Length != 0)
          return;
        var location = FindRepeatedLocation(statement);
        if (location == null)
          return;
        var receiver = statement.variable().GetText();
        if (LocalQueryOrigin.IsCreatedByBuiltInQuery(statement, receiver, Context))
          AddFinding($"{receiver}.{method}", location, member.identifier().GetTextPosition(),
            execute ? Operation.Write : Classify(FindCommandText(statement, receiver)));
      }

      private static IsblParser.ExpressionContext FindCommandText(IsblParser.InvokeStatementContext statement, string name)
      {
        // Only the immediately preceding statement in the same block is considered.
        if (statement.Parent is not IsblParser.StatementBlockContext block) return null;
        for (var i = 1; i < block.ChildCount; i++)
        {
          if (!ReferenceEquals(block.GetChild(i), statement)) continue;
          if (block.GetChild(i - 1) is not IsblParser.AssignStatementContext assignment ||
              !Equal(assignment.variable()?.GetText(), name) || assignment.indexer() != null ||
              assignment.invocationCall() is not [var property] ||
              !Equal(property.identifier().GetText(), "CommandText") || property.L_PAREN() != null)
            return null;
          return assignment.expression();
        }
        return null;
      }

      private static IsblParser.ExpressionContext ResolveText(IsblParser.ExpressionContext expression, IParseTree use)
      {
        var operand = LocalQueryOrigin.Unwrap(expression)?.operand();
        if (operand?.variable() is { } variable && operand.indexer() == null && operand.POINTER() == null &&
            operand.invocationCall().Length == 0)
          return LocalQueryOrigin.FindAssignment(use, variable.GetText())?.expression();
        return expression;
      }

      private Operation Classify(IsblParser.ExpressionContext expression)
      {
        expression = LocalQueryOrigin.Unwrap(expression);
        while (expression?.CONCAT() != null)
          expression = LocalQueryOrigin.Unwrap(expression.expression(0));
        var operand = expression?.operand();
        var literal = operand?.@string()?.GetText();
        if (literal == null && operand?.function() is { } format && operand.invocationCall().Length == 0)
        {
          var name = format.identifier().GetText();
          if ((Equal(name, "Format") || Equal(name, "Формат")) && !Context.HasUserFunction(name))
          {
            // Use the actual positional argument list, which can contain empty slots.
            if (ParameterListParser.GetExpressions(format.parameterList()) is [var first, ..])
              literal = LocalQueryOrigin.Unwrap(ResolveText(first, format))?.operand()?.@string()?.GetText();
          }
        }
        if (literal == null) return Operation.Unknown;
        var prefix = literal[1..^1];
        var keyword = SqlStart.Match(prefix).Groups["keyword"].Value;
        if (Equal(keyword, "SELECT"))
          return CountNullColumn.IsMatch(prefix) ? Operation.CountNullColumn :
            CountStart.IsMatch(prefix) ? Operation.Count : Operation.Select;
        return keyword.Length == 0 ? Operation.Unknown : Operation.Write;
      }

      private static bool Equal(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

      private void AddFinding(string name, string location, TextPosition position, Operation operation)
      {
        var action = operation switch
        {
          Operation.Write => "Для повторяющихся однотипных команд оцените одну операцию над набором строк или обработку ограниченными пакетами с параметрами. Порядок записи, транзакции и обработку ошибок каждой строки нужно сохранить.",
          Operation.Interactive => "Измерьте отдельно получение данных и ожидание выбора пользователя. При повторном открытии одного списка проверьте, можно ли предложить один выбор нескольких строк с сохранением прав, фильтров и отмены.",
          Operation.Export => "Проверьте, можно ли получить данные одной выборкой и сформировать отчёт из неё. Сохраните порядок строк, заголовки и границы отдельных выгрузок.",
          Operation.Count => "Если это запросы только на чтение, оцените GROUP BY для подсчёта по разным ключам или несколько агрегатов в одной выборке для проверки разных столбцов. Сохраните правила для NULL и результат для отсутствующих групп.",
          Operation.CountNullColumn => "Шаблон подставляет имя столбца в условие IS NULL. Если все проверки выполняются по одной таблице, используйте несколько агрегатов в одной выборке. Для условия «есть хотя бы одно значение» используйте COUNT_BIG([столбец]) > 0. Если нужно само число NULL, вычислите COUNT_BIG(*) - COUNT_BIG([столбец]). Подставляйте только имена из метаданных таблицы и экранируйте их как идентификаторы SQL Server.",
          Operation.Select => "Убедитесь, что команда только читает данные. При одинаковых параметрах и результате рассмотрите одну выборку. Для разных ключей оцените пакетную выборку с сопоставлением по ключу.",
          _ => "Уточните полный текст и назначение запроса. Для чтения оцените получение данных один раз или пакетную выборку по ключам. Для записи проверьте возможность обработки набором строк с сохранением порядка и транзакций."
        };
        if (location == "условии WHILE")
          action = "Результат запроса участвует в завершении WHILE. Проверьте, должен ли запрос видеть изменения после каждой итерации. " + action;
        Report.Add(Severity.Information, "F032", $"В {location} вызывается «{name}». При повторных итерациях запрос может выполняться многократно.",
          Document, position, new DiagnosticRecommendation(
            $"Измерьте число вызовов «{name}» и время на типовом объёме данных. {action}",
            "Сравните время и число запросов. Проверьте пустой набор, результат, порядок и права доступа. Для записи сравните сохранённые строки и поведение при сбое посередине операции."));
      }
    }
    public override void Apply(IReport report, IDocument document, IContext context) =>
      ParseTreeWalker.Default.Walk(new Listener { Report = report, Document = document, Context = context }, document.GetSyntaxTree());
  }
}
