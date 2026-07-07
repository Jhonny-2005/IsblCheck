using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class InteractiveModeCheckRuleTest
  {
    private readonly InteractiveModeCheckRule rule = new InteractiveModeCheckRule();

    [TestMethod]
    public void FormAccessWithoutCheck_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule, "x = Object.Form.Actions");
      TestHelper.AssertSingleMessage(report, "F047", Severity.Warning);
    }

    [TestMethod]
    public void FormAccessWithCheck_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "if InteractiveMode() then\nx = Object.Form.Actions\nend");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoFormAccess_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "x = 1");
      TestHelper.AssertNoMessages(report);
    }
  }
}
