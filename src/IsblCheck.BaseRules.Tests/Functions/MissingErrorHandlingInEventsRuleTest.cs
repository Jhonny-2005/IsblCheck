using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class MissingErrorHandlingInEventsRuleTest
  {
    private readonly MissingErrorHandlingInEventsRule rule = new MissingErrorHandlingInEventsRule();

    [TestMethod]
    public void EventWithTry_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "try\nx = 1\nexcept\nlog(\"error\")\nend",
        path: "Events.BeforeUpdate");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void EventWithoutTry_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = 1",
        path: "Events.BeforeUpdate");
      Assert.IsTrue(report.Messages.Count > 0, "Expected missing error handling info");
    }

    [TestMethod]
    public void NonEventCode_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = 1",
        path: "");
      TestHelper.AssertNoMessages(report);
    }
  }
}
