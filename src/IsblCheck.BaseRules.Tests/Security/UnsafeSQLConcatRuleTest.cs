using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Security
{
  [TestClass]
  public class UnsafeSQLConcatRuleTest
  {
    private readonly UnsafeSQLConcatRule rule = new UnsafeSQLConcatRule();

    [TestMethod]
    public void ParameterizedQuery_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "SQL(\"SELECT * FROM users WHERE id = %s\"; id)");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void ConcatenatedQuery_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "SQL(\"SELECT * FROM users WHERE id = \" + id)");
      Assert.IsTrue(report.Messages.Count > 0, "Expected unsafe SQL concatenation warning");
    }

    [TestMethod]
    public void NoSQLCall_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = \"SELECT * FROM users\"");
      TestHelper.AssertNoMessages(report);
    }
  }
}
