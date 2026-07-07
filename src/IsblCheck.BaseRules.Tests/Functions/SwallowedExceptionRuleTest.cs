using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class SwallowedExceptionRuleTest
  {
    private readonly SwallowedExceptionRule rule = new SwallowedExceptionRule();

    [TestMethod]
    public void ExceptionHandled_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "try\nx = 1\nexcept\nlog(GetLastException())\nend");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void ExceptionSwallowed_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "try\nx = 1\nexcept\nend");
      Assert.IsTrue(report.Messages.Count > 0, "Expected swallowed exception info");
    }

    [TestMethod]
    public void NoTryBlock_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = 1\ny = 2");
      TestHelper.AssertNoMessages(report);
    }
  }
}
