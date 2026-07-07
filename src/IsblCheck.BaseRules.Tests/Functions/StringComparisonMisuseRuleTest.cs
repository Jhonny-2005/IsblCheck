using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class StringComparisonMisuseRuleTest
  {
    private readonly StringComparisonMisuseRule rule = new StringComparisonMisuseRule();

    [TestMethod]
    public void DoubleEquals_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "if x == y then\nabs(x)\nend");
      TestHelper.AssertSingleMessage(report, "F033", Severity.Warning);
    }

    [TestMethod]
    public void SingleEquals_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "if x = y then\nabs(x)\nend");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NotEquals_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "if x <> y then\nabs(x)\nend");
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
