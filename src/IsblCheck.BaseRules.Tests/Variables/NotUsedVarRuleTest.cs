using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Variables;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Variables
{
  [TestClass]
  public class NotUsedVarRuleTest
  {
    private readonly NotUsedVarRule rule = new NotUsedVarRule();

    [TestMethod]
    public void UsedVariable_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "x = 3\nabs(x)");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void UnusedVariable_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule, "x = 3\ny = 2\nabs(y)");
      TestHelper.AssertSingleMessage(report, "A003", Severity.Warning);
    }

    [TestMethod]
    public void TwoUnusedVariables_ShouldReportTwo()
    {
      var report = TestHelper.ApplyRule(rule, "x = 3\ny = 2");
      TestHelper.AssertMessages(report, "A003", Severity.Warning, 2);
    }

    [TestMethod]
    public void VariableUsedInIf_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "x = 3\nif x > 0 then\nabs(x)\nend");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void VariableUsedInWhile_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "x = 3\nwhile x > 0 do\nx = x - 1\nend");
      TestHelper.AssertNoMessages(report);
    }
  }
}
