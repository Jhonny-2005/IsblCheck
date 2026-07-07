using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class TryFinallyWithoutExceptRuleTest
  {
    private readonly TryFinallyWithoutExceptRule rule = new TryFinallyWithoutExceptRule();

    [TestMethod]
    public void TryFinallyOnly_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "try\nx = 1\nfinally\ny = 2\nend");
      TestHelper.AssertSingleMessage(report, "F045", Severity.Information);
    }

    [TestMethod]
    public void TryExceptFinally_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "try\nx = 1\nexcept\ny = 2\nfinally\nz = 3\nend");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void TryExceptOnly_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "try\nx = 1\nexcept\ny = 2\nend");
      TestHelper.AssertNoMessages(report);
    }
  }
}
