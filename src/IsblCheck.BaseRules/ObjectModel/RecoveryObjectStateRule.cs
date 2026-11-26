using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using IsblCheck.BaseRules.Properties;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.ObjectModel
{
  /// <summary>
  /// Правило поиска объектов, которые не восстанавливают свое состояние.
  /// </summary>
  internal class RecoveryObjectStateRule : AbstractRule
  {
    #region Константы

    /// <summary>
    /// Код правила.
    /// </summary>
    private const string Code = "I001";

    #endregion

    #region Внутренние классы

    /// <summary>
    /// Определение состояния объекта.
    /// </summary>
    private class ObjectStateDefinition
    {
      /// <summary>
      /// Имя объекта, вызывающего метод.
      /// </summary>
      public string InvokerName { get; set; }

      /// <summary>
      /// Имя вызываемого метода.
      /// </summary>
      public string CallMethodName { get; set; }
      public string CleanupMethodName { get; set; }

      /// <summary>
      /// Имя получаемого идентификатора состояния.
      /// </summary>
      public string IdentifierStateName { get; set; }

      /// <summary>
      /// Контекст вызова.
      /// </summary>
      public ParserRuleContext StatementContext { get; set; }
    }

    /// <summary>
    /// Поиск объектов с незавершенным состоянием, открываемые метод с параметром.
    /// </summary>
    private class CallMethodsWithSaveStateListener : IsblBaseListener
    {
      /// <summary>
      /// Определения объектов с не восстановленным состоянием.
      /// </summary>
      public readonly List<ObjectStateDefinition> NotRestoredObjectDefinitions = new List<ObjectStateDefinition>();

      /// <summary>
      /// Вызываемые методы.
      /// </summary>
      private static readonly Dictionary<string, string> callMethods = new Dictionary<string, string>
      {
        { "AddFrom", "DelFrom" },
        { "AddJoin", "DelJoin" },
        { "AddOrderBy", "DelOrderBy" },
        { "AddSelect", "DelSelect" },
        { "AddWhere", "DelWhere" }
      };

      /// <summary>
      /// Получить предложение означивания.
      /// </summary>
      /// <param name="node">Узел правил.</param>
      /// <returns>Предложение означивания.</returns>
      private static IsblParser.AssignStatementContext GetParentAssignStatementContext(IRuleNode node)
      {
        var parent = node.Parent;
        while (parent != null)
        {
          if (parent is IsblParser.AssignStatementContext)
            break;
          parent = parent.Parent;
        }
        return parent as IsblParser.AssignStatementContext;
      }

      /// <summary>
      /// Вход в COM вызов.
      /// </summary>
      /// <param name="context">Контекст.</param>
      public override void EnterInvocationCall(IsblParser.InvocationCallContext context)
      {
        var callMethod = context.GetChild<IsblParser.IdentifierContext>(0);
        if (callMethod == null)
          return;

        var callMethodName = callMethod.GetText();
        var isOpenMethod = callMethods
          .Any(m => m.Key.Equals(callMethodName, StringComparison.OrdinalIgnoreCase));
        var isCloseMethod = callMethods
          .Any(m => m.Value.Equals(callMethodName, StringComparison.OrdinalIgnoreCase));

        if (isOpenMethod)
        {
          var operand = context.Parent as IsblParser.OperandContext;
          if (operand == null)
            return;

          var invokerName = operand.children
           .TakeWhile(ctx => ctx != context)
           .Select(ctx => ctx.GetText())
           .Aggregate((accumulate, text) => accumulate + text);
          // Удаляем последнюю точку.
          invokerName = invokerName.Remove(invokerName.Length - 1, 1);

          var assingStatement = GetParentAssignStatementContext(context);
          if (assingStatement == null)
            return;

          var identifierState = assingStatement.GetChild<IsblParser.VariableContext>(0);
          if (identifierState == null)
            return;

          var identifierStateName = identifierState.GetText();

          var objectStateDefinition = new ObjectStateDefinition
          {
            CallMethodName = callMethodName,
            CleanupMethodName = callMethods.First(pair => pair.Key.Equals(callMethodName, StringComparison.OrdinalIgnoreCase)).Value,
            InvokerName = invokerName,
            IdentifierStateName = identifierStateName,
            StatementContext = assingStatement
          };
          this.NotRestoredObjectDefinitions.Add(objectStateDefinition);
        }
        else if (isCloseMethod)
        {
          var invokeStatement = context.Parent as IsblParser.InvokeStatementContext;
          if (invokeStatement == null)
            return;

          var invokerName = invokeStatement.children
           .TakeWhile(ctx => ctx != context)
           .Select(ctx => ctx.GetText())
           .Aggregate((accumulate, text) => accumulate + text);
          // Удаляем последнюю точку.
          invokerName = invokerName.Remove(invokerName.Length - 1, 1);

          var openMethodName = callMethods
            .First(m => m.Value.Equals(callMethodName, StringComparison.OrdinalIgnoreCase))
            .Key;

          var parameterList = context.GetChild<IsblParser.ParameterListContext>(0);
          if (parameterList == null)
            return;

          var identifierState = parameterList.GetChild<IsblParser.ExpressionContext>(0);
          if (identifierState == null)
            return;

          var identifierStateName = identifierState.GetText();

          var objectStateDefinition = this.NotRestoredObjectDefinitions
            .LastOrDefault(d => d.CallMethodName.Equals(openMethodName, StringComparison.OrdinalIgnoreCase) &&
              d.InvokerName.Equals(invokerName, StringComparison.OrdinalIgnoreCase) &&
              d.IdentifierStateName.Equals(identifierStateName, StringComparison.OrdinalIgnoreCase) &&
              !InOppositeBranches(d.StatementContext, invokeStatement));
          if (objectStateDefinition != null)
            this.NotRestoredObjectDefinitions.Remove(objectStateDefinition);
        }
      }
    }

    /// <summary>
    /// Поиск объектов с незавершенным состоянием, открываемые функциями без параметра.
    /// </summary>
    private class CallMethodsWithoutSaveStateListener : IsblBaseListener
    {
      public IContext Context;
      /// <summary>
      /// Определения объектов с не восстановленным состоянием.
      /// </summary>
      public readonly List<ObjectStateDefinition> NotRestoredObjectDefinitions = new List<ObjectStateDefinition>();

      /// <summary>
      /// Список переменных, созданных через CreateCachedReference/СоздатьКэшированныйСправочник.
      /// </summary>
      private readonly ISet<string> cachedReferenceVariableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      /// <summary>
      /// Вызываемые парные методы.
      /// </summary>
      private static readonly Dictionary<string, string> complementMethodNames = new Dictionary<string, string>
      {
        { "AddCheckPoint", "ReleaseCheckPoint" },
        { "DisableAll", "EnableAll" },
        { "DisableControls", "EnableControls" },
        { "EnterEditMode", "LeaveEditMode" },
        { "Open", "Close" },
        { "OpenRecord", "CloseRecord" }
      };

      public override void EnterAssignStatement([NotNull]IsblParser.AssignStatementContext context)
      {
        var variableContext = context.GetChild(0) as IsblParser.VariableContext;
        if (variableContext == null)
          return;

        var eq = context.GetChild(1) as TerminalNodeImpl;
        if (eq == null ||
          (eq.Symbol.Type != IsblParser.EQ &&
          eq.Symbol.Type != IsblParser.COLON))
          return;

        var variableName = variableContext.GetText();

        if (IsAssignedFromCreateCachedReferenceFunctionCall(context))
        {
          this.cachedReferenceVariableNames.Add(variableName);
        }
        else
        {
          this.cachedReferenceVariableNames.Remove(variableName);
        }
      }

      private bool IsAssignedFromCreateCachedReferenceFunctionCall(IsblParser.AssignStatementContext context)
      {
        if (context.expression()?.operand()?.ChildCount != 1)
          return false;

        var function = context.expression().operand().GetChild(0) as IsblParser.FunctionContext;
        if (function == null)
          return false;

        var functionName = function.identifier().GetText();
        return (functionName.Equals("CreateCachedReference", StringComparison.OrdinalIgnoreCase) ||
                functionName.Equals("СоздатьКэшированныйСправочник", StringComparison.OrdinalIgnoreCase)) &&
          !Context.HasUserFunction(functionName);
      }

      public override void EnterInvocationCall(IsblParser.InvocationCallContext context)
      {
        var callMethod = context.GetChild<IsblParser.IdentifierContext>(0);
        if (callMethod == null)
          return;

        var invokeStatement = context.Parent as IsblParser.InvokeStatementContext;
        if (invokeStatement == null)
          return;

        var callMethodName = callMethod.GetText();
        var isOpenMethod = complementMethodNames
          .Any(m => m.Key.Equals(callMethodName, StringComparison.OrdinalIgnoreCase));
        var isCloseMethod = complementMethodNames
          .Any(m => m.Value.Equals(callMethodName, StringComparison.OrdinalIgnoreCase));

        if (!isOpenMethod && !isCloseMethod)
          return;

        var invokerName = invokeStatement.children
         .TakeWhile(ctx => ctx != context)
         .Select(ctx => ctx.GetText())
         .Aggregate((accumulate, text) => accumulate + text);
        // Удаляем последнюю точку.
        invokerName = invokerName.Remove(invokerName.Length - 1, 1);

        if (isOpenMethod)
        {
          // A checkpoint on the same Events collection captures the prior state;
          // ReleaseCheckPoint restores it, so DisableAll does not need a separate EnableAll.
          var restoredByCheckpoint = callMethodName.Equals("DisableAll", StringComparison.OrdinalIgnoreCase) &&
            HasActiveCheckpointBefore(invokeStatement, invokerName);
          var isCachedReference = this.cachedReferenceVariableNames.Contains(invokerName);
          if (!restoredByCheckpoint && (!isCachedReference || !callMethodName.Equals("Open", StringComparison.OrdinalIgnoreCase)))
          {
            var objectStateDefinition = new ObjectStateDefinition
            {
              CallMethodName = callMethodName,
              CleanupMethodName = complementMethodNames.First(pair => pair.Key.Equals(callMethodName, StringComparison.OrdinalIgnoreCase)).Value,
              InvokerName = invokerName,
              StatementContext = invokeStatement
            };
            this.NotRestoredObjectDefinitions.Add(objectStateDefinition);
          }
        }
        else
        {
          var openMethodName = complementMethodNames
            .First(m => m.Value.Equals(callMethodName, StringComparison.OrdinalIgnoreCase))
            .Key;

          var objectStateDefinitions = this.NotRestoredObjectDefinitions
            .Where(d => d.CallMethodName.Equals(openMethodName, StringComparison.OrdinalIgnoreCase) &&
                        d.InvokerName.Equals(invokerName, StringComparison.OrdinalIgnoreCase) &&
                        !InOppositeBranches(d.StatementContext, invokeStatement))
            .ToList();
          foreach (var objectStateDefinition in objectStateDefinitions)
            this.NotRestoredObjectDefinitions.Remove(objectStateDefinition);
        }
      }

      private static bool HasActiveCheckpointBefore(IsblParser.InvokeStatementContext statement, string invokerName)
      {
        for (IParseTree node = statement; node != null; node = node.Parent)
        {
          if (node.Parent is not IsblParser.StatementBlockContext block) continue;
          var index = -1;
          for (var i = 0; i < block.ChildCount; i++)
            if (ReferenceEquals(block.GetChild(i), node)) { index = i; break; }
          if (index < 0) continue;
          for (var i = index - 1; i >= 0; i--)
          {
            var prior = block.GetChild(i);
            if (prior is IsblParser.InvokeStatementContext priorCall && TryGetCheckpointUse(priorCall.invocationCall().LastOrDefault(), out var method, out var priorInvoker) &&
                string.Equals(invokerName, priorInvoker, StringComparison.OrdinalIgnoreCase))
            {
              if (method.Equals("AddCheckPoint", StringComparison.OrdinalIgnoreCase)) return true;
              if (method.Equals("ReleaseCheckPoint", StringComparison.OrdinalIgnoreCase)) return false;
            }
            if (ContainsCheckpointUse(prior, invokerName)) return false;
          }
        }
        return false;
      }

      private static bool ContainsCheckpointUse(IParseTree node, string invokerName)
      {
        if (node is IsblParser.InvocationCallContext call && TryGetCheckpointUse(call, out _, out var receiver) &&
            string.Equals(invokerName, receiver, StringComparison.OrdinalIgnoreCase)) return true;
        for (var i = 0; i < node.ChildCount; i++)
          if (ContainsCheckpointUse(node.GetChild(i), invokerName)) return true;
        return false;
      }

      private static bool TryGetCheckpointUse(IsblParser.InvocationCallContext context, out string method, out string invoker)
      {
        method = null;
        invoker = null;
        if (context?.Parent is not IsblParser.InvokeStatementContext statement) return false;
        method = context.GetChild<IsblParser.IdentifierContext>(0)?.GetText();
        if (!string.Equals(method, "AddCheckPoint", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(method, "ReleaseCheckPoint", StringComparison.OrdinalIgnoreCase)) return false;
        var prefix = statement.children.TakeWhile(child => child != context).Select(child => child.GetText()).ToArray();
        if (prefix.Length == 0) return false;
        invoker = string.Concat(prefix);
        if (invoker.EndsWith(".", StringComparison.Ordinal)) invoker = invoker.Substring(0, invoker.Length - 1);
        return true;
      }
    }

    #endregion

    private static bool InOppositeBranches(IParseTree acquisition, IParseTree cleanup)
    {
      var cleanupBranches = new Dictionary<IsblParser.IfStatementContext, IsblParser.StatementBlockContext>();
      for (var node = cleanup; node != null; node = node.Parent)
        if (node is IsblParser.StatementBlockContext block && block.Parent is IsblParser.IfStatementContext condition)
          cleanupBranches.Add(condition, block);
      for (var node = acquisition; node != null; node = node.Parent)
        if (node is IsblParser.StatementBlockContext block && block.Parent is IsblParser.IfStatementContext condition &&
            cleanupBranches.TryGetValue(condition, out var cleanupBlock) && !ReferenceEquals(block, cleanupBlock))
          return true;
      return false;
    }

    #region Поля

    /// <summary>
    /// Инфо правила.
    /// </summary>
    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(RecoveryObjectStateRule).Name, Resources.RecoveryObjectStateRuleDescription), true);

    /// <summary>
    /// Инфо правила.
    /// </summary>
    public static IRuleInfo Info => info.Value;

    #endregion

    #region Методы

    /// <summary>
    /// Применить правило.
    /// </summary>
    /// <param name="report">Отчет.</param>
    /// <param name="document">Документ.</param>
    /// <param name="context">Контекст.</param>
    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var paramListener = new CallMethodsWithSaveStateListener();
      var noParamListener = new CallMethodsWithoutSaveStateListener { Context = context };

      walker.Walk(paramListener, tree);

      foreach (var notRestoredObjectDefinition in paramListener.NotRestoredObjectDefinitions)
      {
        AddFinding(report, document, notRestoredObjectDefinition);
      }


      walker.Walk(noParamListener, tree);

      foreach (var notRestoredObjectDefinition in noParamListener.NotRestoredObjectDefinitions)
      {
        AddFinding(report, document, notRestoredObjectDefinition);
      }
    }

    private static void AddFinding(IReport report, IDocument document, ObjectStateDefinition state)
    {
      var receiver = state.InvokerName;
      var cleanup = $"{receiver}.{state.CleanupMethodName}({state.IdentifierStateName})";
      var action = state.CleanupMethodName == "EnableAll"
        ? $"Проверьте исходное состояние «{receiver}». Для коллекции событий с поддержкой контрольных точек сохраните состояние через «{receiver}.AddCheckPoint()» до отключения и восстановите через «{receiver}.ReleaseCheckPoint()» при выходе."
        : $"Если состояние принадлежит «{receiver}» и его надо восстановить здесь, используйте «{cleanup}» после последнего использования. Если вызов вернул отдельный объект, сверяйте очистку с его контрактом. При передаче состояния через окружение найдите парную операцию в следующем событии и проверьте тот же объект и идентификатор.";
      report.Add(Severity.Warning, Code,
        $"В вычислении не найдена парная операция для {state.CallMethodName} объекта \"{receiver}\". Проверьте восстановление состояния с учётом ветвлений, алиасов и других вызовов.",
        document, state.StatementContext.GetTextPosition(), new DiagnosticRecommendation(action,
          $"Проверьте обычный выход и исключение после «{state.CallMethodName}». Для состояния между событиями проверьте отмену и выход до следующего события. Парный вызов должен относиться к тому же объекту и выполняться только после установки состояния."));
    }

    #endregion
  }
}
