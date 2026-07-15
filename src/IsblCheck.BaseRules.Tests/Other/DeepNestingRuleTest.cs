using IsblCheck.Core.Checker;
using System.Linq;
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
        "if x > 0\nif y > 0\nif z > 0\nabs(x)\nendif\nendif\nendif");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void DeepNesting_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "if a > 0\nif b > 0\nif c > 0\nif d > 0\nif e > 0\nif f > 0\nabs(a)\nendif\nendif\nendif\nendif\nendif\nendif");
      Assert.IsTrue(report.Messages.Count() > 0, "Expected deep nesting warning");
    }

    [TestMethod]
    public void NoNesting_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "x = 1\ny = 2\nz = 3");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void BoundaryFourLevels_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "if a > 0\nif b > 0\nif c > 0\nif d > 0\nabs(a)\nendif\nendif\nendif\nendif");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void BoundarySixLevels_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "if a > 0\nif b > 0\nif c > 0\nif d > 0\nif e > 0\nif f > 0\nabs(a)\nendif\nendif\nendif\nendif\nendif\nendif");
      Assert.IsTrue(report.Messages.Count() > 0, "Expected deep nesting warning at 6 levels");
    }
  }
}
