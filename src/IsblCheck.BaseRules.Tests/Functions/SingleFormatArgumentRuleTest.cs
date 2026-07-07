using IsblCheck.Core.Checker;
using System.Linq;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class SingleFormatArgumentRuleTest
  {
    private readonly SingleFormatArgumentRule rule = new SingleFormatArgumentRule();

    [TestMethod]
    public void MultipleArgs_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "Format(\"%0:s %1:s\"; ArrayOf(a; b))");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void SingleArg_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "Format(\"%0:s\"; ArrayOf(a))");
      Assert.IsTrue(report.Messages.Count() > 0, "Expected single format argument info");
    }

    [TestMethod]
    public void NoFormatCall_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = 1");
      TestHelper.AssertNoMessages(report);
    }
  }
}
