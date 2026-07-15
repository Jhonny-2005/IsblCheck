using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class MissingUnlockAfterLockRuleTest
  {
    private readonly MissingUnlockAfterLockRule rule = new MissingUnlockAfterLockRule();

    [TestMethod]
    public void LockWithoutUnlock_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "Doc.Lock()");
      TestHelper.AssertSingleMessage(report, "F055", Severity.Warning);
    }

    [TestMethod]
    public void LockWithUnlock_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "Doc.Lock()\nDoc.Unlock()");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoLock_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "abs(x)");
      TestHelper.AssertNoMessages(report);
    }
  }
}
