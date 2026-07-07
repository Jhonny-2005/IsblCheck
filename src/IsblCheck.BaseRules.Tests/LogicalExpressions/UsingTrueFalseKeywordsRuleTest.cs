using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.LogicalExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.LogicalExpressions
{
  [TestClass]
  public class UsingTrueFalseKeywordsRuleTest
  {
    private readonly UsingTrueFalseKeywordsRule rule = new UsingTrueFalseKeywordsRule();

    [TestMethod]
    public void TrueInExpression_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "if x = True then\nabs(x)\nend");
      TestHelper.AssertSingleMessage(report, "B003", Severity.Warning);
    }

    [TestMethod]
    public void FalseInExpression_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "if x = False then\nabs(x)\nend");
      TestHelper.AssertSingleMessage(report, "B003", Severity.Warning);
    }

    [TestMethod]
    public void ProperBoolUsage_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "if x then\nabs(x)\nend");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void ComparisonWithVariable_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "if x = y then\nabs(x)\nend");
      TestHelper.AssertNoMessages(report);
    }
  }
}
