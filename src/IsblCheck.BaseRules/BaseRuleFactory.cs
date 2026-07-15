using System.ComponentModel.Composition;
using IsblCheck.BaseRules.Functions;
using IsblCheck.BaseRules.ObjectModel;
using IsblCheck.BaseRules.Other;
using IsblCheck.BaseRules.Security;
using IsblCheck.BaseRules.Variables;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules
{
  /// <summary>
  /// Р¤Р°Р±СЂРёРєР° Р±Р°Р·РѕРІС‹С… РїСЂР°РІРёР»
  /// </summary>
  [Export(typeof(IRuleFactory))]
  public class BaseRuleFactory : AbstractRuleFactory
  {
    /// <summary>
    /// РљРѕРЅСЃС‚СЂСѓРєС‚РѕСЂ.
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
      this.Register<DeprecatedFunctionRule>(DeprecatedFunctionRule.Info);
      this.Register<TransactionSafetyRule>(TransactionSafetyRule.Info);
      this.Register<ConnectionLeakRule>(ConnectionLeakRule.Info);
      this.Register<LargeParameterCountRule>(LargeParameterCountRule.Info);
      this.Register<SwallowedExceptionRule>(SwallowedExceptionRule.Info);
      this.Register<MissingErrorHandlingInEventsRule>(MissingErrorHandlingInEventsRule.Info);
      this.Register<UnreachableCodeRule>(UnreachableCodeRule.Info);
      this.Register<InfiniteLoopRule>(InfiniteLoopRule.Info);
      this.Register<NestedTransactionRule>(NestedTransactionRule.Info);
      this.Register<ExceptionsOffBalanceRule>(ExceptionsOffBalanceRule.Info);
      this.Register<MagicNumberRule>(MagicNumberRule.Info);
      this.Register<SilentExceptionSwallowRule>(SilentExceptionSwallowRule.Info);
      this.Register<BranchedExceptionsOffRule>(BranchedExceptionsOffRule.Info);
      this.Register<InteractiveWindowOnWorkflowRule>(InteractiveWindowOnWorkflowRule.Info);
      this.Register<MagicStringRule>(MagicStringRule.Info);
      this.Register<ExceptionsOffOutsideTryExceptRule>(ExceptionsOffOutsideTryExceptRule.Info);
      this.Register<NextResetInsideForeachRule>(NextResetInsideForeachRule.Info);
      this.Register<CreateConnectionWithoutTryExceptRule>(CreateConnectionWithoutTryExceptRule.Info);
      this.Register<ExitInCriticalEventsRule>(ExitInCriticalEventsRule.Info);
      this.Register<SleepInServerEventsRule>(SleepInServerEventsRule.Info);
      this.Register<TryFinallyWithoutExceptRule>(TryFinallyWithoutExceptRule.Info);
      this.Register<MandatoryParameterOmissionRule>(MandatoryParameterOmissionRule.Info);
      this.Register<InteractiveModeCheckRule>(InteractiveModeCheckRule.Info);
      this.Register<WebRuntimeContextCheckRule>(WebRuntimeContextCheckRule.Info);
      this.Register<ShowMessageInReportRule>(ShowMessageInReportRule.Info);
      this.Register<NumericStringComparisonRule>(NumericStringComparisonRule.Info);
      this.Register<LargeReferenceOnServerRule>(LargeReferenceOnServerRule.Info);
            this.Register<NamedExceptionForRetryRule>(NamedExceptionForRetryRule.Info);
      this.Register<ModifyCollectionDuringIterationRule>(ModifyCollectionDuringIterationRule.Info);
      this.Register<MissingTryEditBeforeLockRule>(MissingTryEditBeforeLockRule.Info);
      this.Register<MissingUnlockAfterLockRule>(MissingUnlockAfterLockRule.Info);
      this.Register<FullReferenceLoadOnServerRule>(FullReferenceLoadOnServerRule.Info);

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
      this.Register<NullVsNilConfusionRule>(NullVsNilConfusionRule.Info);

      #endregion

      #region Other

      this.Register<TodoDoneCommentsRule>(TodoDoneCommentsRule.Info);
      this.Register<EmptyCatchBlockRule>(EmptyCatchBlockRule.Info);
      this.Register<DeepNestingRule>(DeepNestingRule.Info);
      this.Register<UnconditionalExitForRule>(UnconditionalExitForRule.Info);
            this.Register<DeprecatedEventVariableRule>(DeprecatedEventVariableRule.Info);
      this.Register<CommentedCodeBlockRule>(CommentedCodeBlockRule.Info);

      #endregion

      #region Security

      this.Register<HardcodedCredentialRule>(HardcodedCredentialRule.Info);
      this.Register<SQLInjectionDirectConcatRule>(SQLInjectionDirectConcatRule.Info);
      this.Register<ShellExecRule>(ShellExecRule.Info);
      this.Register<RegWriteRule>(RegWriteRule.Info);
            this.Register<ExecuteInServerEventsRule>(ExecuteInServerEventsRule.Info);
      this.Register<FileSystemAccessInServerEventsRule>(FileSystemAccessInServerEventsRule.Info);
      this.Register<NetworkRequestInServerEventsRule>(NetworkRequestInServerEventsRule.Info);
      this.Register<HardcodedConnectionStringRule>(HardcodedConnectionStringRule.Info);

      #endregion
    }
  }
}

