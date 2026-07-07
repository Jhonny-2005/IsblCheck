using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Other;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Other
{
  [TestClass]
  public class TodoDoneCommentsRuleTest
  {
    private readonly TodoDoneCommentsRule rule = new TodoDoneCommentsRule();

    [TestMethod]
    public void TodoComment_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "// TODO: fix this later");
      Assert.IsTrue(report.Messages.Count > 0, "Expected TODO comment warning");
    }

    [TestMethod]
    public void DoneComment_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "// DONE: implemented");
      Assert.IsTrue(report.Messages.Count > 0, "Expected DONE comment warning");
    }

    [TestMethod]
    public void NormalComment_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "// this is a regular comment");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoComments_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = 1");
      TestHelper.AssertNoMessages(report);
    }
  }
}
