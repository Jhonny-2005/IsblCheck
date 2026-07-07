using IsblCheck.Core.Checker;
using System.Linq;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Other;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Other
{
  [TestClass]
  public class UnconditionalExitForRuleTest
  {
    private readonly UnconditionalExitForRule rule = new UnconditionalExitForRule();

    [TestMethod]
    public void ConditionalExit_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "foreach x in arr do\nif x > 10 then\nexitfor\nend\nend");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void UnconditionalExit_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "foreach x in arr do\nexitfor\nend");
      Assert.IsTrue(report.Messages.Count() > 0, "Expected unconditional exit for warning");
    }

    [TestMethod]
    public void NoForeach_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = 1\ny = 2");
      TestHelper.AssertNoMessages(report);
    }
  }
}
