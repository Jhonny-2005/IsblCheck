using System.Collections.Generic;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context.Development;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class MandatoryParameterOmissionRuleTest
  {
    private readonly MandatoryParameterOmissionRule rule = new MandatoryParameterOmissionRule();

    [TestMethod]
    public void MissingRequiredParam_ShouldReport()
    {
      var func = new Function { Name = "TestFunc" };
      func.Arguments.Add(new FunctionArgument { Number = 1, Name = "a", HasDefaultValue = false });
      func.Arguments.Add(new FunctionArgument { Number = 2, Name = "b", HasDefaultValue = false });
      ContextHelper.Context.Development.Functions.Add(func);

      var report = TestHelper.ApplyRule(rule, "TestFunc(1)");
      TestHelper.AssertSingleMessage(report, "F046", Severity.Error);

      ContextHelper.Context.Development.Functions.Remove(func);
    }

    [TestMethod]
    public void AllParamsProvided_NoReport()
    {
      var func = new Function { Name = "TestFunc2" };
      func.Arguments.Add(new FunctionArgument { Number = 1, Name = "a", HasDefaultValue = false });
      func.Arguments.Add(new FunctionArgument { Number = 2, Name = "b", HasDefaultValue = false });
      ContextHelper.Context.Development.Functions.Add(func);

      var report = TestHelper.ApplyRule(rule, "TestFunc2(1; 2)");
      TestHelper.AssertNoMessages(report);

      ContextHelper.Context.Development.Functions.Remove(func);
    }

    [TestMethod]
    public void UnknownFunction_NoReport()
    {
      var report = TestHelper.ApplyRule(rule, "UnknownFunc(1)");
      TestHelper.AssertNoMessages(report);
    }
  }
}
