using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Variables;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Variables
{
  [TestClass]
  public class SelfAssignmentVarRuleTest
  {
    private readonly SelfAssignmentVarRule rule = new SelfAssignmentVarRule();

    [TestMethod]
    public void SelfAssignment_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule, "x = x");
      TestHelper.AssertSingleMessage(report, "A004", Severity.Warning);
    }

    [TestMethod]
    public void DifferentAssignment_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "x = y");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void PredefinedVariableSelfAssignment_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule, "!Sender = !Sender");
      TestHelper.AssertSingleMessage(report, "A004", Severity.Warning);
    }

    [TestMethod]
    public void MethodCallOnVariable_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "x = y\nabs(x)");
      TestHelper.AssertNoMessages(report);
    }
  }
}
