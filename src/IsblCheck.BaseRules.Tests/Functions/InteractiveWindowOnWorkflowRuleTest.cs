using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class InteractiveWindowOnWorkflowRuleTest
  {
    private readonly InteractiveWindowOnWorkflowRule rule = new InteractiveWindowOnWorkflowRule();

    [TestMethod]
    public void MessageBoxInEvent_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "MessageBox",
        path: "Events.AfterUpdate");
      TestHelper.AssertSingleMessage(report, "F038", Severity.Warning);
    }

    [TestMethod]
    public void NonEventPath_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "MessageBox",
        path: "SomeOtherPath");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void RegularFunctionInEvent_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "abs(x)",
        path: "Events.AfterUpdate");
      TestHelper.AssertNoMessages(report);
    }
  }
}
