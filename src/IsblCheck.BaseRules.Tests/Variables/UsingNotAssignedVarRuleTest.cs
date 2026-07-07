using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Variables;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Variables
{
  [TestClass]
  public class UsingNotAssignedVarRuleTest
  {
    private readonly UsingNotAssignedVarRule rule = new UsingNotAssignedVarRule();

    [TestMethod]
    public void UndeclaredVariable_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule, "x = 3");
      TestHelper.AssertSingleMessage(report, "A001", Severity.Error);
    }

    [TestMethod]
    public void DeclaredVariable_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "x : int\nx = 3");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void AssignedVariable_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "x = 3\nabs(x)");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void VariableInForeach_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "foreach x in arr do\nabs(x)\nend");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void PredefinedVariable_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "abs(Object)");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void ContextVariable_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "abs(param1)",
        contextVariables: new System.Collections.Generic.List<string> { "PARAM1" });
      TestHelper.AssertNoMessages(report);
    }
  }
}
