using IsblCheck.Core.Checker;
using System.Linq;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class ExceptionClassNotSpecifiedRuleTest
  {
    private readonly ExceptionClassNotSpecifiedRule rule = new ExceptionClassNotSpecifiedRule();

    [TestMethod]
    public void ValidExceptionClass_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "CreateException(\"MyException\"; \"Error message\")");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void EmptyExceptionClass_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "CreateException(\"\"; \"Error message\")");
      Assert.IsTrue(report.Messages.Count() > 0, "Expected empty exception class info");
    }

    [TestMethod]
    public void RaiseWithClass_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "Raise(CreateException(\"MyException\"; \"msg\"))");
      TestHelper.AssertNoMessages(report);
    }
  }
}
