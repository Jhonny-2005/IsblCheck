using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class ExceptionsOffBalanceRuleTest
  {
    private readonly ExceptionsOffBalanceRule rule = new ExceptionsOffBalanceRule();

    [TestMethod]
    public void UnbalancedExceptionsOff_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "ExceptionsOff()\nx = 1\nExceptionsOff()");
      Assert.IsTrue(report.Messages.Count > 0, "Expected unbalanced exceptions warning");
    }

    [TestMethod]
    public void BalancedExceptions_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "ExceptionsOff()\nx = 1\nExceptionsOn()");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoExceptionsOff_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = 1\ny = 2");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void SingleExceptionsOff_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "ExceptionsOff()\nx = 1");
      TestHelper.AssertNoMessages(report);
    }
  }
}
