using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Security
{
  [TestClass]
  public class HardcodedConnectionStringRuleTest
  {
    private readonly HardcodedConnectionStringRule rule = new HardcodedConnectionStringRule();

    [TestMethod]
    public void HardcodedConnectionString_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = \"Provider=SQLOLEDB;Data Source=Server1;Initial Catalog=MyDB\"");
      TestHelper.AssertSingleMessage(report, "S009", Severity.Warning);
    }

    [TestMethod]
    public void ShortString_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = \"hello world\"");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoConnectionString_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = \"SELECT * FROM users\"");
      TestHelper.AssertNoMessages(report);
    }
  }
}
