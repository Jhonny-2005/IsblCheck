using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class FullReferenceLoadOnServerRuleTest
  {
    private readonly FullReferenceLoadOnServerRule rule = new FullReferenceLoadOnServerRule();

    [TestMethod]
    public void FindAllInForeach_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "foreach x in arr\nref.FindAll()\nendforeach");
      TestHelper.AssertSingleMessage(report, "P001", Severity.Warning);
    }

    [TestMethod]
    public void FindAllOutsideForeach_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "ref.FindAll()");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void NoFindAll_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "abs(x)");
      TestHelper.AssertNoMessages(report);
    }
  }
}
