using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class ConnectionLeakRuleTest
  {
    private readonly ConnectionLeakRule rule = new ConnectionLeakRule();

    [TestMethod]
    public void ConnectionClosed_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "conn = CreateConnection()\nconn.Open()\nconn.Close()");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void ConnectionLeak_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "conn = CreateConnection()\nconn.Open()");
      Assert.IsTrue(report.Messages.Count > 0, "Expected connection leak warning");
    }

    [TestMethod]
    public void NoConnection_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = 1\ny = 2");
      TestHelper.AssertNoMessages(report);
    }
  }
}
