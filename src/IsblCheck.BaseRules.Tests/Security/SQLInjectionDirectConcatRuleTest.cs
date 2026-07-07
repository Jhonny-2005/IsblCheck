using IsblCheck.Core.Checker;
using System.Linq;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Security
{
  [TestClass]
  public class SQLInjectionDirectConcatRuleTest
  {
    private readonly SQLInjectionDirectConcatRule rule = new SQLInjectionDirectConcatRule();

    [TestMethod]
    public void SafeQuery_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "SQL(\"SELECT * FROM users\")");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void UnsafeConcat_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "SQL(\"SELECT * FROM users WHERE name = \" + name)");
      Assert.IsTrue(report.Messages.Count() > 0, "Expected SQL injection error");
    }

    [TestMethod]
    public void UnsafeConcatWithVariable_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "CSQL(\"SELECT * FROM table WHERE id = \" + id)");
      Assert.IsTrue(report.Messages.Count() > 0, "Expected SQL injection error");
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
