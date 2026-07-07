using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class InfiniteLoopRuleTest
  {
    private readonly InfiniteLoopRule rule = new InfiniteLoopRule();

    [TestMethod]
    public void WhileTrueWithoutExit_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "while True do\nx = 1\nend");
      Assert.IsTrue(report.Messages.Count > 0, "Expected infinite loop warning");
    }

    [TestMethod]
    public void WhileTrueWithExitFor_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "while True do\nif x > 10 then\nexitfor\nend\nx = x + 1\nend");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void WhileCondition_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "while x > 0 do\nx = x - 1\nend");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoWhileLoop_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = 1\ny = 2");
      TestHelper.AssertNoMessages(report);
    }
  }
}
