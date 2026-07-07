using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Variables;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Variables
{
  [TestClass]
  public class UsingRedefinedVarRuleTest
  {
    private readonly UsingRedefinedVarRule rule = new UsingRedefinedVarRule();

    [TestMethod]
    public void RedefinedBeforeUse_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule, "x = 1\nx = 2");
      TestHelper.AssertSingleMessage(report, "A002", Severity.Warning);
    }

    [TestMethod]
    public void RedefinedAfterUse_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "x = 1\nabs(x)\nx = 2");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NullAssignment_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "x = null\nx = 1");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void DifferentVariables_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "x = 1\ny = 2\nabs(x)\nabs(y)");
      TestHelper.AssertNoMessages(report);
    }
  }
}
