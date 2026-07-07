using IsblCheck.Core.Checker;
using System.Linq;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.ObjectModel
{
  [TestClass]
  public class RecoveryObjectStateRuleTest
  {
    private readonly RecoveryObjectStateRule rule = new RecoveryObjectStateRule();

    [TestMethod]
    public void PairedOpenClose_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "obj = CreateObject()\nobj.Open()\nx = 1\nobj.Close()");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void UnclosedObject_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "obj = CreateObject()\nobj.Open()");
      Assert.IsTrue(report.Messages.Count() > 0, "Expected unclosed object warning");
    }

    [TestMethod]
    public void NoOpenCall_NoReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "obj = CreateObject()\nx = 1");
      TestHelper.AssertNoMessages(report);
    }
  }
}
