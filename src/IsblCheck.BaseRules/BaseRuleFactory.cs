using System.ComponentModel.Composition;
using IsblCheck.BaseRules.Functions;
using IsblCheck.BaseRules.LogicalExpressions;
using IsblCheck.BaseRules.ObjectModel;
using IsblCheck.BaseRules.Other;
using IsblCheck.BaseRules.Variables;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules
{
  /// <summary>
  /// Фабрика базовых правил
  /// </summary>
  [Export(typeof(IRuleFactory))]
  public class BaseRuleFactory : AbstractRuleFactory
  {
    /// <summary>
    /// Конструктор.
    /// </summary>
    public BaseRuleFactory()
    {
      #region Functions

      this.Register<IncorrectFormatStringRule>(IncorrectFormatStringRule.Info);
      this.Register<SingleFormatArgumentRule>(SingleFormatArgumentRule.Info);
      this.Register<IncorrectFunctionParamsCountRule>(IncorrectFunctionParamsCountRule.Info);
      this.Register<FunctionWithoutHelpRule>(FunctionWithoutHelpRule.Info);
      this.Register<UsingNonExistingLocalizationStringRule>(UsingNonExistingLocalizationStringRule.Info);
      this.Register<ExceptionClassNotSpecifiedRule>(ExceptionClassNotSpecifiedRule.Info);
      this.Register<FunctionTooBigRule>(FunctionTooBigRule.Info);
      this.Register<UsingNotExistedReferenceRule>(UsingNotExistedReferenceRule.Info);
      this.Register<InteractiveWindowsOnEvents>(InteractiveWindowsOnEvents.Info);

      #endregion

      #region LogicalExpressions

      this.Register<UsingTrueFalseKeywordsRule>(UsingTrueFalseKeywordsRule.Info);
      this.Register<IncompatibleComparisonsRule>(IncompatibleComparisonsRule.Info);

      #endregion

      #region ObjectModel

      this.Register<RecoveryObjectStateRule>(RecoveryObjectStateRule.Info);
      this.Register<UsingInfoReferenceRule>(UsingInfoReferenceRule.Info);

      #endregion

      #region Variables

      this.Register<UsingNotAssignedVarRule>(UsingNotAssignedVarRule.Info);
      this.Register<UsingRedefinedVarRule>(UsingRedefinedVarRule.Info);
      this.Register<NotUsedVarRule>(NotUsedVarRule.Info);
      this.Register<SelfAssignmentVarRule>(SelfAssignmentVarRule.Info);

      #endregion

      #region Other

      this.Register<TodoDoneCommentsRule>(TodoDoneCommentsRule.Info);

      #endregion

      #region Security

      this.Register<SqlInjectionRiskRule>(SqlInjectionRiskRule.Info);
      this.Register<SqlFormattedValueRule>(SqlFormattedValueRule.Info);
      this.Register<HardcodedPasswordRule>(HardcodedPasswordRule.Info);

      #endregion

      #region ErrorHandling

      this.Register<EmptyCatchBlockRule>(EmptyCatchBlockRule.Info);
      this.Register<EventsRestoreOnExceptionRule>(EventsRestoreOnExceptionRule.Info);
      this.Register<ServerEventInteractiveRule>(ServerEventInteractiveRule.Info);
      this.Register<FinallyRaiseNewExceptionRule>(FinallyRaiseNewExceptionRule.Info);
      this.Register<EagerQueryFieldReadRule>(EagerQueryFieldReadRule.Info);

      #endregion

      #region Compatibility

      // Deprecation and return contracts depend on the installed Directum version.
      // Do not register unverified name-based heuristics as language diagnostics.

      #endregion

      #region Performance

      this.Register<InfiniteLoopRiskRule>(InfiniteLoopRiskRule.Info);
      this.Register<SqlInLoopRule>(SqlInLoopRule.Info);
      this.Register<QueryCursorProgressRule>(QueryCursorProgressRule.Info);
      this.Register<ScriptReloadInLoopRule>(ScriptReloadInLoopRule.Info);
      this.Register<RepeatedWorkflowParameterSaveRule>(RepeatedWorkflowParameterSaveRule.Info);

      #endregion

      #region CodeQuality

      this.Register<MagicNumbersRule>(MagicNumbersRule.Info);
      this.Register<DeepNestingRule>(DeepNestingRule.Info);
      this.Register<InvalidExitForRule>(InvalidExitForRule.Info);
      this.Register<DivisionByZeroRule>(DivisionByZeroRule.Info);
      this.Register<RatioComparisonRule>(RatioComparisonRule.Info);
      this.Register<IndexOfNameConditionRule>(IndexOfNameConditionRule.Info);
      this.Register<LateNullCheckRule>(LateNullCheckRule.Info);
      this.Register<LookupBeforeExistenceCheckRule>(LookupBeforeExistenceCheckRule.Info);
      this.Register<DelimitedListSubstringRule>(DelimitedListSubstringRule.Info);
      this.Register<IndexedConditionLoopRule>(IndexedConditionLoopRule.Info);
      this.Register<SqlRestoreKeyRule>(SqlRestoreKeyRule.Info);
      this.Register<WorklistProgressRule>(WorklistProgressRule.Info);
      this.Register<DeadCodeAfterExitRule>(DeadCodeAfterExitRule.Info);
      this.Register<ObjectReferenceCleanupRule>(ObjectReferenceCleanupRule.Info);

      #endregion
    }
  }
}
