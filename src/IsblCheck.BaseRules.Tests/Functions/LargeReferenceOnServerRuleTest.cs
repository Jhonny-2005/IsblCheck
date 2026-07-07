using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class LargeReferenceOnServerRuleTest
  {
    private readonly LargeReferenceOnServerRule rule = new LargeReferenceOnServerRule();

    [TestMethod]
    public void CreateReferenceInEvent_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "ref = CreateReference(\"Test\")",
        path: "Events.AfterUpdate");
      TestHelper.AssertSingleMessage(report, "F051", Severity.Warning);
    }

    [TestMethod]
    public void CreateReferenceOutsideEvent_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "ref = CreateReference(\"Test\")",
        path: "SomeOtherPath");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoCreateReference_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = 1",
        path: "Events.AfterUpdate");
      TestHelper.AssertNoMessages(report);
    }
  }
}
