using IsblCheck.Core.Checker;
using System.Linq;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class MagicStringRuleTest
  {
    private readonly MagicStringRule rule = new MagicStringRule();

    [TestMethod]
    public void IdentifierLikeString_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = \"MyReference\"");
      Assert.IsTrue(report.Messages.Count() > 0, "Expected magic string info");
    }

    [TestMethod]
    public void SimpleString_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = \"hello\"");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void EmptyString_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = \"\"");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void ShortString_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = \"OK\"");
      TestHelper.AssertNoMessages(report);
    }
  }
}
