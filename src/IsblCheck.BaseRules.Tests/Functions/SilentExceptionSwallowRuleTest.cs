using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class SilentExceptionSwallowRuleTest
  {
    private readonly SilentExceptionSwallowRule rule = new SilentExceptionSwallowRule();

    [TestMethod]
    public void OnlyFreeException_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "try\nx = 1\nexcept\nFreeException()\nendexcept");
      TestHelper.AssertSingleMessage(report, "F034", Severity.Warning);
    }

    [TestMethod]
    public void FreeExceptionWithLogging_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "try\nx = 1\nexcept\nlog(GetLastException())\nFreeException()\nendexcept");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void EmptyExceptBlock_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "try\nx = 1\nexcept\nendexcept");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoTryBlock_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "x = 1\ny = 2");
      TestHelper.AssertNoMessages(report);
    }
  }
}
