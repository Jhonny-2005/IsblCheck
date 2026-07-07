using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Security
{
  [TestClass]
  public class ExecuteInServerEventsRuleTest
  {
    private readonly ExecuteInServerEventsRule rule = new ExecuteInServerEventsRule();

    [TestMethod]
    public void ExecuteInEvent_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "Execute(\"test\")",
        path: "Events.AfterUpdate");
      TestHelper.AssertSingleMessage(report, "S006", Severity.Error);
    }

    [TestMethod]
    public void ExecuteOutsideEvent_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "Execute(\"test\")",
        path: "SomeOtherPath");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoExecuteCall_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = 1",
        path: "Events.AfterUpdate");
      TestHelper.AssertNoMessages(report);
    }
  }
}
