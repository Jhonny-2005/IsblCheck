using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Other;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Other
{
  [TestClass]
  public class DeprecatedEventVariableRuleTest
  {
    private readonly DeprecatedEventVariableRule rule = new DeprecatedEventVariableRule();

    [TestMethod]
    public void DocContext_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = DocContext");
      TestHelper.AssertSingleMessage(report, "F036", Severity.Warning);
    }

    [TestMethod]
    public void RefContext_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = RefContext");
      TestHelper.AssertSingleMessage(report, "F036", Severity.Warning);
    }

    [TestMethod]
    public void RegularVariable_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = y");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void PredefinedVariable_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "x = !Sender");
      TestHelper.AssertNoMessages(report);
    }
  }
}
