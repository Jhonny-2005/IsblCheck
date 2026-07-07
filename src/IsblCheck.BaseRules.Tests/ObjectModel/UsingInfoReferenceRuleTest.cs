using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.ObjectModel
{
  [TestClass]
  public class UsingInfoReferenceRuleTest
  {
    private readonly UsingInfoReferenceRule rule = new UsingInfoReferenceRule();

    [TestMethod]
    public void InfoDotReference_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = Info.Reference");
      TestHelper.AssertSingleMessage(report, "O001", Severity.Warning);
    }

    [TestMethod]
    public void InfoDotMethod_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = Info.Method()");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void OtherProperty_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = Info.Name");
      TestHelper.AssertNoMessages(report);
    }
  }
}
