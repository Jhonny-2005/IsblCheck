using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class WebRuntimeContextCheckRuleTest
  {
    private readonly WebRuntimeContextCheckRule rule = new WebRuntimeContextCheckRule();

    [TestMethod]
    public void DesktopDialogWithoutCheck_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule, "dlg = CreateOpenDialog()");
      TestHelper.AssertSingleMessage(report, "F048", Severity.Warning);
    }

    [TestMethod]
    public void DesktopDialogWithCheck_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "if not IsWebRuntimeContext() then\ndlg = CreateOpenDialog()\nend");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoDesktopFunction_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "x = 1");
      TestHelper.AssertNoMessages(report);
    }
  }
}
