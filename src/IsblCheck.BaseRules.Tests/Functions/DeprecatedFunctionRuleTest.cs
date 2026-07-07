using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class DeprecatedFunctionRuleTest
  {
    private readonly DeprecatedFunctionRule rule = new DeprecatedFunctionRule();

    [TestMethod]
    public void NormalFunction_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "abs(1)");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void UnknownFunction_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "MyCustomFunc()");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void GetComponent_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule, "GetComponent(\"Test\")");
      TestHelper.AssertSingleMessage(report, "F025", Severity.Warning);
    }

    [TestMethod]
    public void ExecuteComponent_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule, "ExecuteComponent(\"Test\")");
      TestHelper.AssertSingleMessage(report, "F025", Severity.Warning);
    }

    [TestMethod]
    public void CreateComponentDescription_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule, "CreateComponentDescription(\"Test\")");
      TestHelper.AssertSingleMessage(report, "F025", Severity.Warning);
    }
  }
}
