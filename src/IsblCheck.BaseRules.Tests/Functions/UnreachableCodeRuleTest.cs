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
        "foreach x in arr\nexitfor\ny = 1\nendforeach");
      Assert.IsTrue(report.Messages.Count() > 0, "Expected unreachable code warning");
    }

    [TestMethod]
    public void ExitForWithoutUnreachableCode_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "foreach x in arr\nif x > 10\nexitfor\nendif\nendforeach");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoExitStatement_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "x = 1\ny = 2\nz = 3");
      TestHelper.AssertNoMessages(report);
    }
  }
}
