using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class MissingTryEditBeforeLockRuleTest
  {
    private readonly MissingTryEditBeforeLockRule rule = new MissingTryEditBeforeLockRule();

    [TestMethod]
    public void LockWithoutTryEdit_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "Doc.Lock()");
      TestHelper.AssertSingleMessage(report, "F054", Severity.Warning);
    }

    [TestMethod]
    public void LockWithTryEdit_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "if Doc.TryEdit()\nDoc.Lock()\nendif");
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
