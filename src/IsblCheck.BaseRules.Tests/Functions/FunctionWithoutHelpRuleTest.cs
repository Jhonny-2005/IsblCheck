using System.Collections.Generic;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context.Development;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class FunctionWithoutHelpRuleTest
  {
    private readonly FunctionWithoutHelpRule rule = new FunctionWithoutHelpRule();

    [TestMethod]
    public void FunctionWithHelp_NoReport()
    {
      var func = new Function { Name = "FuncWithHelp", Help = "Some help text" };
      ContextHelper.Context.Development.Functions.Add(func);

      var report = TestHelper.ApplyRule(rule, "FuncWithHelp()",
        componentType: ComponentType.Function,
        componentName: "FuncWithHelp");
      TestHelper.AssertNoMessages(report);

      ContextHelper.Context.Development.Functions.Remove(func);
    }

    [TestMethod]
    public void FunctionWithoutHelp_ShouldReport()
    {
      var func = new Function { Name = "FuncNoHelp", Help = null };
      ContextHelper.Context.Development.Functions.Add(func);

      var report = TestHelper.ApplyRule(rule, "FuncNoHelp()",
        componentType: ComponentType.Function,
        componentName: "FuncNoHelp");
      Assert.IsTrue(report.Messages.Count > 0, "Expected function without help warning");

      ContextHelper.Context.Development.Functions.Remove(func);
    }
  }
}
