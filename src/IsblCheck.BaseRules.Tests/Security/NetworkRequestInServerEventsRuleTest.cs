using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Security
{
  [TestClass]
  public class NetworkRequestInServerEventsRuleTest
  {
    private readonly NetworkRequestInServerEventsRule rule = new NetworkRequestInServerEventsRule();

    [TestMethod]
    public void NetworkRequestInEvent_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "HttpOpen(\"http://example.com\")",
        path: "Events.AfterUpdate");
      TestHelper.AssertSingleMessage(report, "S008", Severity.Warning);
    }

    [TestMethod]
    public void NetworkRequestOutsideEvent_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "HttpOpen(\"http://example.com\")",
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
