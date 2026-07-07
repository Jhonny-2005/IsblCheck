using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class ExitInCriticalEventsRuleTest
  {
    private readonly ExitInCriticalEventsRule rule = new ExitInCriticalEventsRule();

    [TestMethod]
    public void ExitInBeforeUpdate_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "Exit()",
        path: "Events.BeforeUpdate");
      TestHelper.AssertSingleMessage(report, "F043", Severity.Warning);
    }

    [TestMethod]
    public void ExitOutsideEvent_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "Exit()",
        path: "SomeOtherPath");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoExitCall_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "x = 1",
        path: "Events.BeforeUpdate");
      TestHelper.AssertNoMessages(report);
    }
  }
}
