using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Other;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Other
{
  [TestClass]
  public class DeepNestingRuleTest
  {
    private readonly DeepNestingRule rule = new DeepNestingRule();

    [TestMethod]
    public void ShallowNesting_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "if x > 0 then\nif y > 0 then\nif z > 0 then\nabs(x)\nend\nend\nend");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void DeepNesting_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "if a > 0 then\nif b > 0 then\nif c > 0 then\nif d > 0 then\nif e > 0 then\nif f > 0 then\nabs(a)\nend\nend\nend\nend\nend\nend");
      Assert.IsTrue(report.Messages.Count > 0, "Expected deep nesting warning");
    }

    [TestMethod]
    public void NoNesting_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = 1\ny = 2\nz = 3");
      TestHelper.AssertNoMessages(report);
    }
  }
}
