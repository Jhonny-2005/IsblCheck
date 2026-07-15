using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class ModifyCollectionDuringIterationRuleTest
  {
    private readonly ModifyCollectionDuringIterationRule rule = new ModifyCollectionDuringIterationRule();

    [TestMethod]
    public void ModifyDuringIteration_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "foreach x in arr\narr.Delete(x)\nendforeach");
      TestHelper.AssertSingleMessage(report, "F053", Severity.Error);
    }

    [TestMethod]
    public void NoModification_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "foreach x in arr\nabs(x)\nendforeach");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void ModifyOutsideForeach_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "arr.Delete(x)");
      TestHelper.AssertNoMessages(report);
    }
  }
}
