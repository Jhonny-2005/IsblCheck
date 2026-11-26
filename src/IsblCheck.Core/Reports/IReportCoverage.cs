using System.Collections.Generic;
using IsblCheck.Core.Checker;

namespace IsblCheck.Core.Reports
{
  /// <summary>Documents actually submitted to a completed analysis, including those with no findings.</summary>
  public interface IReportCoverage
  {
    IEnumerable<IDocument> Documents { get; }
    bool? CanValidateReferences => null;
    bool? CanValidateLocalization => null;
  }
}
