namespace IsblCheck.Core.Reports
{
  /// <summary>Advice derived from the analyzed code, with a way to verify the change.</summary>
  public sealed record DiagnosticRecommendation(string Action, string Verification);
}
