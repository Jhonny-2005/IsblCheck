using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class MagicNumberRuleTest
  {
    private readonly MagicNumberRule rule = new MagicNumberRule();

    [TestMethod]
    public void MagicNumber_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = 42");
      Assert.IsTrue(report.Messages.Count > 0, "Expected magic number info");
    }

    [TestMethod]
    public void AllowedNumbers_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = 0\ny = 1\nz = -1");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoNumbers_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = \"hello\"");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void MultipleMagicNumbers_ShouldReportMultiple()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = 100\ny = 200\nz = 300");
      Assert.IsTrue(report.Messages.Count >= 3, "Expected at least 3 magic number warnings");
    }
  }
}
