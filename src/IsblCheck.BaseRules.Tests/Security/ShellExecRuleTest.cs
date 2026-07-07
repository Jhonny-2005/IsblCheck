using IsblCheck.Core.Checker;
using System.Linq;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Security
{
  [TestClass]
  public class ShellExecRuleTest
  {
    private readonly ShellExecRule rule = new ShellExecRule();

    [TestMethod]
    public void ShellInEvent_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "Shell(\"notepad.exe\")",
        path: "Events.BeforeUpdate");
      Assert.IsTrue(report.Messages.Count() > 0, "Expected shell exec error");
    }

    [TestMethod]
    public void ShellInNonEvent_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "Shell(\"notepad.exe\")",
        path: "");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void ShellExecuteInEvent_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "ShellExecute(\"open\"; \"test.exe\")",
        path: "Events.AfterInsert");
      Assert.IsTrue(report.Messages.Count() > 0, "Expected shell exec error");
    }

    [TestMethod]
    public void NoShellCall_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = 1",
        path: "Events.BeforeUpdate");
      TestHelper.AssertNoMessages(report);
    }
  }
}
