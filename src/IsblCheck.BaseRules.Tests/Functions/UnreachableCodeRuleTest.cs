using IsblCheck.Core.Checker;
using System.Linq;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class UnreachableCodeRuleTest
  {
    private readonly UnreachableCodeRule rule = new UnreachableCodeRule();

    [TestMethod]
    public void ExitForWithUnreachableCode_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "foreach x in arr do\nexitfor\ny = 1\nend");
      Assert.IsTrue(report.Messages.Count() > 0, "Expected unreachable code warning");
    }

    [TestMethod]
    public void ExitForWithoutUnreachableCode_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "foreach x in arr do\nif x > 10 then\nexitfor\nend\nend");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void ExitWithUnreachableCode_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "function Test() : void\nbegin\nexit()\nx = 1\nend");
      Assert.IsTrue(report.Messages.Count() > 0, "Expected unreachable code warning");
    }

    [TestMethod]
    public void NoExitStatement_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = 1\ny = 2\nz = 3");
      TestHelper.AssertNoMessages(report);
    }
  }
}
