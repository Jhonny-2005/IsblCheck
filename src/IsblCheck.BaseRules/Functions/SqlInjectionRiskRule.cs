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
  public class SqlInjectionRiskRule : AbstractRule
  {
    private const string Code = "F010";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(SqlInjectionRiskRule).Name, "Конкатенация вычисляемых значений в SQL-запросе: требуется проверка происхождения данных и API."), true);

    public static IRuleInfo Info => info.Value;

    // Query positions follow the IS-Builder signatures in Context.Application/Functions.cs.
    private static readonly Dictionary<string, int> SqlFunctions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
      { "SQL", 0 }, { "CSQL", 0 }, { "SQL2XLSTAB", 0 },
      { "SelectSQL", 2 }, { "ВыборSQL", 2 },
      { "CSelectSQL", 1 }, { "ПВыборSQL", 1 }
    };

    internal static bool TryGetQueryArgument(string name, out int index) =>
      SqlFunctions.TryGetValue(name, out index);

    internal static DiagnosticRecommendation RecommendSqlReview(string functionName)
    {
      var action = $"Установите источники подстановок в «{functionName}». Передавайте значения отдельно от текста запроса, если выбранный API это документирует. Для имён таблиц, полей и направлений сортировки используйте закрытый набор допустимых вариантов.";
      if (functionName.Equals("SQL", StringComparison.OrdinalIgnoreCase))
        action += " В каталоге ISBL второй аргумент SQL обозначает Alias. Для параметров выберите документированный способ целевой версии.";
      return new DiagnosticRecommendation(action,
        "В тестовой системе сравните выборку для обычного значения, строки с апострофом, пустого значения и числа. Проверьте, что меняются только значения параметров, а права доступа и результат запроса сохраняются.");
    }

    private class SqlInjectionListener : IsblBaseListener
    {
      public List<IsblParser.FunctionContext> SqlCallsWithConcat { get; } = new List<IsblParser.FunctionContext>();
      public IContext Context;

      public override void EnterFunction([NotNull] IsblParser.FunctionContext context)
      {
        var functionName = context.identifier().GetText();
        if (!TryGetQueryArgument(functionName, out var queryIndex) || IsOverridden(functionName))
          return;

        var paramList = context.parameterList();
        if (paramList == null)
          return;
        var expressions = ParameterListParser.GetExpressions(paramList);
        if (expressions.Length <= queryIndex)
          return;

        var firstParam = expressions[queryIndex];
        if (firstParam != null && ((HasStringConcatenation(firstParam) && !IsLiteralString(firstParam)) ||
            IsLocallyBuiltQuery(context, firstParam)))
          SqlCallsWithConcat.Add(context);
      }

      private bool IsOverridden(string name) => Context.HasUserFunction(name);

      private static bool IsLocallyBuiltQuery(IsblParser.FunctionContext call, IsblParser.ExpressionContext expression)
      {
        // Follow only a simple local variable to its latest assignment within the same
        // straight-line block. Cross-branch, alias and interprocedural flows are unknown.
        while (expression?.operand()?.expression() != null) expression = expression.operand().expression();
        var operand = expression?.operand();
        if (operand?.variable() == null || operand.indexer() != null || operand.invocationCall().Length != 0 || operand.POINTER() != null) return false;
        var name = operand.variable().GetText();
        IParseTree statement = call;
        while (statement?.Parent != null && statement.Parent is not IsblParser.StatementBlockContext) statement = statement.Parent;
        if (statement?.Parent is not IsblParser.StatementBlockContext block) return false;
        var foundStatement = false;
        for (var i = block.ChildCount - 1; i >= 0; i--)
        {
          var node = block.GetChild(i);
          if (!foundStatement) { if (ReferenceEquals(node, statement)) foundStatement = true; continue; }
          if (node is IsblParser.IfStatementContext || node is IsblParser.WhileStatementContext || node is IsblParser.ForeachStatementContext || node is IsblParser.TryStatementContext || node.GetText().Contains("^", StringComparison.Ordinal)) return false;
          if (node is IsblParser.AssignStatementContext assignment && assignment.variable() != null &&
              assignment.indexer() == null && assignment.invocationCall().Length == 0 &&
              string.Equals(assignment.variable().GetText(), name, StringComparison.OrdinalIgnoreCase))
          {
            var value = assignment.expression();
            return value != null && HasStringConcatenation(value) && !IsLiteralString(value);
          }
          if (node is IsblParser.DeclareVariableStatementContext declaration && string.Equals(declaration.variable()?.GetText(), name, StringComparison.OrdinalIgnoreCase)) return false;
        }
        return false;
      }

      private static bool IsLiteralString(IsblParser.ExpressionContext expression)
      {
        if (expression.operand()?.@string() != null) return true;
        if (expression.operand()?.expression() != null) return IsLiteralString(expression.operand().expression());
        return expression.CONCAT() != null && expression.expression().Length == 2 &&
          IsLiteralString(expression.expression(0)) && IsLiteralString(expression.expression(1));
      }

      private static bool HasStringConcatenation(IsblParser.ExpressionContext expr)
      {
        for (int i = 0; i < expr.ChildCount; i++)
        {
          var child = expr.GetChild(i) as TerminalNodeImpl;
          if (child != null && child.Symbol.Type == IsblParser.CONCAT)
            return true;
        }

        foreach (var child in expr.children)
        {
          if (child is IsblParser.ExpressionContext subExpr && HasStringConcatenation(subExpr))
            return true;
          if (child is IsblParser.OperandContext operand && operand.expression() != null &&
              HasStringConcatenation(operand.expression())) return true;
        }
        return false;
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new SqlInjectionListener { Context = context };
      walker.Walk(listener, tree);

      foreach (var funcCall in listener.SqlCallsWithConcat)
      {
        var name = funcCall.identifier().GetText();
        report.Add(Severity.Warning, Code, $"Запрос для «{name}» собирается конкатенацией вычисляемых значений. Риск зависит от источника и типа подстановок.",
          document, funcCall.identifier().GetTextPosition(), RecommendSqlReview(name));
      }
    }
  }
}
