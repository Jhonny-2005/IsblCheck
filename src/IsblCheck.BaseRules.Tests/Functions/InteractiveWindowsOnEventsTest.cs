using IsblCheck.Core.Checker;
using System.Linq;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class InteractiveWindowsOnEventsTest
  {
    private readonly InteractiveWindowsOnEvents rule = new InteractiveWindowsOnEvents();

    [TestMethod]
    public void MessageBoxInNonEvent_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "MessageBox(\"Hello\")");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void MessageBoxInEvent_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "MessageBox(\"Hello\")",
        componentType: ComponentType.ReferenceType,
        componentName: "TestReference",
        path: "Events.AfterUpdate");
      Assert.IsTrue(report.Messages.Count() > 0, "Expected interactive windows warning");
    }

    [TestMethod]
    public void InputDialogInEvent_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "InputDialog(\"Enter value\")",
        componentType: ComponentType.ReferenceType,
        componentName: "TestReference",
        path: "Events.BeforeUpdate");
      Assert.IsTrue(report.Messages.Count() > 0, "Expected interactive windows warning");
    }
  }
}
