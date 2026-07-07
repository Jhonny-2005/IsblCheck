using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class ExceptionsOffOutsideTryExceptRuleTest
  {
    private readonly ExceptionsOffOutsideTryExceptRule rule = new ExceptionsOffOutsideTryExceptRule();

    [TestMethod]
    public void ExceptionsOffOutsideTry_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule, "ExceptionsOff()\nx = 1");
      TestHelper.AssertSingleMessage(report, "F040", Severity.Error);
    }

    [TestMethod]
    public void ExceptionsOffInsideTry_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "try\nExceptionsOff()\nx = 1\nexcept\nend");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoExceptionsOff_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "x = 1\ny = 2");
      TestHelper.AssertNoMessages(report);
    }
  }
}
