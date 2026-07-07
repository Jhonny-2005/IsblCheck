using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class SleepInServerEventsRuleTest
  {
    private readonly SleepInServerEventsRule rule = new SleepInServerEventsRule();

    [TestMethod]
    public void SleepInEvent_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "Sleep(1000)",
        path: "Events.AfterUpdate");
      TestHelper.AssertSingleMessage(report, "F044", Severity.Warning);
    }

    [TestMethod]
    public void SleepOutsideEvent_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "Sleep(1000)",
        path: "SomeOtherPath");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoSleepCall_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = 1",
        path: "Events.AfterUpdate");
      TestHelper.AssertNoMessages(report);
    }
  }
}
