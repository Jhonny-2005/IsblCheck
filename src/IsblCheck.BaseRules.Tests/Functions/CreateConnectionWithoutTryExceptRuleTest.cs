using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class CreateConnectionWithoutTryExceptRuleTest
  {
    private readonly CreateConnectionWithoutTryExceptRule rule = new CreateConnectionWithoutTryExceptRule();

    [TestMethod]
    public void CreateConnectionOutsideTry_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule, "conn = CreateConnection()");
      TestHelper.AssertSingleMessage(report, "F042", Severity.Warning);
    }

    [TestMethod]
    public void CreateConnectionInsideTry_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "try\nconn = CreateConnection()\nexcept\nend");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoCreateConnection_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "x = 1");
      TestHelper.AssertNoMessages(report);
    }
  }
}
