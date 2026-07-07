using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Security
{
  [TestClass]
  public class HardcodedCredentialRuleTest
  {
    private readonly HardcodedCredentialRule rule = new HardcodedCredentialRule();

    [TestMethod]
    public void CreateConnectionWithPassword_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "conn = CreateConnection(\"server\"; \"password=secret123\")");
      Assert.IsTrue(report.Messages.Count > 0, "Expected hardcoded credential warning");
    }

    [TestMethod]
    public void CreateConnectionSafe_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "conn = CreateConnection(\"server\"; \"trusted=true\")");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoConnection_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = 1");
      TestHelper.AssertNoMessages(report);
    }
  }
}
