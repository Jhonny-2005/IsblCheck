using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Other;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Other
{
  [TestClass]
  public class EmptyCatchBlockRuleTest
  {
    private readonly EmptyCatchBlockRule rule = new EmptyCatchBlockRule();

    [TestMethod]
    public void EmptyCatch_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "try\nx = 1\nexcept\nend");
      Assert.IsTrue(report.Messages.Count > 0, "Expected empty catch block warning");
    }

    [TestMethod]
    public void CatchWithBody_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "try\nx = 1\nexcept\nlog(\"error\")\nend");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoTryBlock_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = 1");
      TestHelper.AssertNoMessages(report);
    }
  }
}
