using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class NumericStringComparisonRuleTest
  {
    private readonly NumericStringComparisonRule rule = new NumericStringComparisonRule();

    [TestMethod]
    public void StringCompareOnNumber_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "if x == 5 then\nabs(x)\nendif");
      TestHelper.AssertSingleMessage(report, "F050", Severity.Warning);
    }

    [TestMethod]
    public void NumericCompare_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "if x = 5 then\nabs(x)\nendif");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void StringCompareOnString_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "if x == \"hello\" then\nabs(x)\nendif");
      TestHelper.AssertNoMessages(report);
    }
  }
}
