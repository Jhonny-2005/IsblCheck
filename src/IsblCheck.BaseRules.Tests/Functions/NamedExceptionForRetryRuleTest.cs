using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class NamedExceptionForRetryRuleTest
  {
    private readonly NamedExceptionForRetryRule rule = new NamedExceptionForRetryRule();

    [TestMethod]
    public void GenericExceptionInEvent_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "exc = CreateException(\"MyError\"; \"msg\")",
        path: "Events.AfterUpdate");
      TestHelper.AssertSingleMessage(report, "F052", Severity.Information);
    }

    [TestMethod]
    public void NamedExceptionInEvent_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "exc = CreateException(\"ESBEdmsComponentLockedError\"; \"msg\")",
        path: "Events.AfterUpdate");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void GenericExceptionOutsideEvent_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "exc = CreateException(\"MyError\"; \"msg\")",
        path: "SomeOtherPath");
      TestHelper.AssertNoMessages(report);
    }
  }
}
