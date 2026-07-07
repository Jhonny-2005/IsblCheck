using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class ShowMessageInReportRuleTest
  {
    private readonly ShowMessageInReportRule rule = new ShowMessageInReportRule();

    [TestMethod]
    public void ShowMessageInReport_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "ShowMessage(\"test\")",
        componentType: ComponentType.CommonReport);
      TestHelper.AssertSingleMessage(report, "F049", Severity.Warning);
    }

    [TestMethod]
    public void ShowMessageOutsideReport_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "ShowMessage(\"test\")",
        componentType: ComponentType.Script);
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoShowMessage_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = 1",
        componentType: ComponentType.CommonReport);
      TestHelper.AssertNoMessages(report);
    }
  }
}
