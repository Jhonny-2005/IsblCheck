using IsblCheck.Core.Checker;
using System.Linq;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class TransactionSafetyRuleTest
  {
    private readonly TransactionSafetyRule rule = new TransactionSafetyRule();

    [TestMethod]
    public void BalancedTransaction_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "StartTransaction()\nx = 1\nCommitTransaction()");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void UnbalancedTransaction_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "StartTransaction()\nx = 1");
      Assert.IsTrue(report.Messages.Count() > 0, "Expected transaction safety error");
    }

    [TestMethod]
    public void NoTransaction_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = 1\ny = 2");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void CommitWithoutStart_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "CommitTransaction()");
      TestHelper.AssertNoMessages(report);
    }
  }
}
