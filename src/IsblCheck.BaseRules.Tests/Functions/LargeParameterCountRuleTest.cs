using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class LargeParameterCountRuleTest
  {
    private readonly LargeParameterCountRule rule = new LargeParameterCountRule();

    [TestMethod]
    public void NormalParamCount_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "MyFunc(1; 2; 3; 4; 5)");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void LargeParamCount_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "MyFunc(1; 2; 3; 4; 5; 6; 7; 8; 9; 10; 11)");
      Assert.IsTrue(report.Messages.Count > 0, "Expected large parameter count warning");
    }

    [TestMethod]
    public void BoundaryTenParams_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "MyFunc(1; 2; 3; 4; 5; 6; 7; 8; 9; 10)");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void BoundaryElevenParams_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "MyFunc(1; 2; 3; 4; 5; 6; 7; 8; 9; 10; 11)");
      Assert.IsTrue(report.Messages.Count > 0, "Expected large parameter count warning");
    }
  }
}
