namespace IsblCheck.Core.Context.Development
{
  /// <summary>
  /// Whether an imported catalog is available for missing-name checks.
  /// This does not assert that a development export is complete or matches the target system.
  /// </summary>
  public interface IMetadataAvailability
  {
    bool CanValidateReferences { get; }
    bool CanValidateLocalization { get; }
  }
}
