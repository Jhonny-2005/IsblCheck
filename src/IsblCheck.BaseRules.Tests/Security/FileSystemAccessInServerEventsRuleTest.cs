using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Security
{
  [TestClass]
  public class FileSystemAccessInServerEventsRuleTest
  {
    private readonly FileSystemAccessInServerEventsRule rule = new FileSystemAccessInServerEventsRule();

    [TestMethod]
    public void FileSystemInEvent_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "CreateFile(\"test.txt\")",
        path: "Events.AfterUpdate");
      TestHelper.AssertSingleMessage(report, "S007", Severity.Error);
    }

    [TestMethod]
    public void FileSystemOutsideEvent_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "CreateFile(\"test.txt\")",
        path: "SomeOtherPath");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void RegularFunctionInEvent_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "abs(x)",
        path: "Events.AfterUpdate");
      TestHelper.AssertNoMessages(report);
    }
  }
}
