using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Security
{
  [TestClass]
  public class RegWriteRuleTest
  {
    private readonly RegWriteRule rule = new RegWriteRule();

    [TestMethod]
    public void RegWriteInEvent_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "RegWriteValue(\"key\"; \"value\")",
        path: "Events.AfterUpdate");
      TestHelper.AssertSingleMessage(report, "S005", Severity.Error);
    }

    [TestMethod]
    public void RegWriteOutsideEvent_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "RegWriteValue(\"key\"; \"value\")",
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
