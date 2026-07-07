using IsblCheck.Core.Checker;
using System.Linq;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class IncorrectFormatStringRuleTest
  {
    private readonly IncorrectFormatStringRule rule = new IncorrectFormatStringRule();

    [TestMethod]
    public void CorrectFormat_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "Format(\"%0:s %1:s\"; ArrayOf(a; b))");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void MissingArg_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "Format(\"%0:s %1:s\"; ArrayOf(a))");
      // Should report F021 or F005 for missing argument
      Assert.IsTrue(report.Messages.Count() > 0, "Expected format error message");
    }

    [TestMethod]
    public void ExtraArg_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "Format(\"%0:s\"; ArrayOf(a; b))");
      // Should report F022 for extra argument
      Assert.IsTrue(report.Messages.Count() > 0, "Expected extra argument warning");
    }

    [TestMethod]
    public void NoFormatString_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "Format(\"just text\")");
      TestHelper.AssertNoMessages(report);
    }
  }
}
