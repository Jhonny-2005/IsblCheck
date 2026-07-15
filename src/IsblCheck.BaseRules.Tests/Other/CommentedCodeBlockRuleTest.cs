using IsblCheck.Core.Checker;
using System.Linq;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Other;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Other
{
  [TestClass]
  public class CommentedCodeBlockRuleTest
  {
    private readonly CommentedCodeBlockRule rule = new CommentedCodeBlockRule();

    [TestMethod]
    public void LargeCommentBlock_ShouldReport()
    {
      var code = "var x = 1;" + System.Environment.NewLine +
                 "// if x > 0 then" + System.Environment.NewLine +
                 "// endif" + System.Environment.NewLine +
                 "// while True do" + System.Environment.NewLine +
                 "// x = 1" + System.Environment.NewLine +
                 "// endwhile" + System.Environment.NewLine +
                 "var y = 2;";
      var report = TestHelper.ApplyRule(rule, code);
      Assert.IsTrue(report.Messages.Count() > 0, "Expected commented code block warning");
    }

    [TestMethod]
    public void SmallCommentBlock_NoReport()
    {
      var code = "// this is a comment" + System.Environment.NewLine + "// another comment";
      var report = TestHelper.ApplyRule(rule, code);
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoComments_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "x = 1" + System.Environment.NewLine + "y = 2");
      TestHelper.AssertNoMessages(report);
    }
  }
}