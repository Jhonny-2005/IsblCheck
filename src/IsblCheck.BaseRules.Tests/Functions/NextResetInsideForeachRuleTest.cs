using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class NextResetInsideForeachRuleTest
  {
    private readonly NextResetInsideForeachRule rule = new NextResetInsideForeachRule();

    [TestMethod]
    public void NextInsideForeach_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "foreach x in arr do\nx.Next()\nend");
      TestHelper.AssertSingleMessage(report, "F041", Severity.Error);
    }

    [TestMethod]
    public void NextOutsideForeach_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "arr.Next()");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoNextCall_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "foreach x in arr do\nabs(x)\nend");
      TestHelper.AssertNoMessages(report);
    }
  }
}
