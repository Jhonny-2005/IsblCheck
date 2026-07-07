using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class BranchedExceptionsOffRuleTest
  {
    private readonly BranchedExceptionsOffRule rule = new BranchedExceptionsOffRule();

    [TestMethod]
    public void AsymmetricOffOn_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "if x = 1 then\nExceptionsOff()\nabs(x)\nelse\nExceptionsOn()\nabs(y)\nendif");
      TestHelper.AssertSingleMessage(report, "F037", Severity.Warning);
    }

    [TestMethod]
    public void SymmetricOffOn_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "if x = 1 then\nExceptionsOff()\nabs(x)\nelse\nExceptionsOff()\nabs(y)\nendif");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoExceptionsInEither_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "if x = 1 then\nabs(x)\nelse\nabs(y)\nendif");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoElseBranch_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "if x = 1 then\nExceptionsOff()\nabs(x)\nendif");
      TestHelper.AssertNoMessages(report);
    }
  }
}
