using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Variables;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Variables
{
  [TestClass]
  public class NullVsNilConfusionRuleTest
  {
    private readonly NullVsNilConfusionRule rule = new NullVsNilConfusionRule();

    [TestMethod]
    public void CompareWithNil_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "if x = nil then\nabs(x)\nend");
      TestHelper.AssertSingleMessage(report, "F035", Severity.Warning);
    }

    [TestMethod]
    public void CompareWithNULL_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "if x = NULL then\nabs(x)\nend");
      TestHelper.AssertSingleMessage(report, "F035", Severity.Warning);
    }

    [TestMethod]
    public void CompareWithVariable_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "if x = y then\nabs(x)\nend");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoComparison_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = 1\ny = 2");
      TestHelper.AssertNoMessages(report);
    }
  }
}
