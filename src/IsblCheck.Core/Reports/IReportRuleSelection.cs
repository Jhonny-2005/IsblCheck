namespace IsblCheck.Core.Reports
{
  /// <summary>Rule selection captured when an analysis started; null means unavailable.</summary>
  public interface IReportRuleSelection
  {
    bool? OptionalRulesIncluded { get; }
  }
}
