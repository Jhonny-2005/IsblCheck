using IsblCheck.Core.Checker;
using System.Linq;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class NestedTransactionRuleTest
  {
    private readonly NestedTransactionRule rule = new NestedTransactionRule();

    [TestMethod]
    public void NestedTransaction_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "StartTransaction()\nStartTransaction()\nCommitTransaction()\nCommitTransaction()");
      Assert.IsTrue(report.Messages.Count() > 0, "Expected nested transaction warning");
    }

    [TestMethod]
    public void BalancedTransaction_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "StartTransaction()\nx = 1\nCommitTransaction()");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoTransaction_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = 1\ny = 2");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void SequentialTransactions_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "StartTransaction()\nCommitTransaction()\nStartTransaction()\nCommitTransaction()");
      TestHelper.AssertNoMessages(report);
    }
  }
}
