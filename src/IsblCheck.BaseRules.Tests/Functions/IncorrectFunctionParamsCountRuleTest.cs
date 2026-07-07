using System.Collections.Generic;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context.Development;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class IncorrectFunctionParamsCountRuleTest
  {
    private readonly IncorrectFunctionParamsCountRule rule = new IncorrectFunctionParamsCountRule();

    [TestMethod]
    public void CorrectParamCount_NoReport()
    {
      // Add a function with 2 params to development context
      var func = new Function { Name = "MyFunc" };
      func.Arguments.Add(new FunctionArgument { Number = 1, Name = "a" });
      func.Arguments.Add(new FunctionArgument { Number = 2, Name = "b" });
      ContextHelper.Context.Development.Functions.Add(func);

      var report = TestHelper.ApplyRule(rule, "MyFunc(1; 2)");
      TestHelper.AssertNoMessages(report);

      ContextHelper.Context.Development.Functions.Remove(func);
    }

    [TestMethod]
    public void TooManyParams_ShouldReport()
    {
      var func = new Function { Name = "MyFunc2" };
      func.Arguments.Add(new FunctionArgument { Number = 1, Name = "a" });
      ContextHelper.Context.Development.Functions.Add(func);

      var report = TestHelper.ApplyRule(rule, "MyFunc2(1; 2; 3)");
      TestHelper.AssertSingleMessage(report, "F001", Severity.Error);

      ContextHelper.Context.Development.Functions.Remove(func);
    }

    [TestMethod]
    public void ArrayOfExcluded_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "ArrayOf(1; 2; 3)");
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void UnknownFunction_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "UnknownFunc(1; 2; 3)");
      TestHelper.AssertNoMessages(report);
    }
  }
}
