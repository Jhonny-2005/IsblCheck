using IsblCheck.Core.Checker;
using IsblCheck.Core.Context.Development;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class UsingNotExistedReferenceRuleTest
  {
    private readonly UsingNotExistedReferenceRule rule = new UsingNotExistedReferenceRule();

    [TestMethod]
    public void ExistingReference_NoReport()
    {
      var refType = new ReferenceType { Name = "MyReference" };
      ContextHelper.Context.Development.ReferenceTypes.Add(refType);

      var report = TestHelper.ApplyRule(rule,
        "ref = CreateReference(\"MyReference\")");
      TestHelper.AssertNoMessages(report);

      ContextHelper.Context.Development.ReferenceTypes.Remove(refType);
    }

    [TestMethod]
    public void NonExistingReference_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "ref = CreateReference(\"InvalidRef\")");
      Assert.IsTrue(report.Messages.Count > 0, "Expected non-existing reference error");
    }
  }
}
