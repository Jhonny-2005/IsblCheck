using IsblCheck.Core.Checker;
using System.Linq;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class FunctionTooBigRuleTest
  {
    private readonly FunctionTooBigRule rule = new FunctionTooBigRule();

    [TestMethod]
    public void SmallFunction_NoReport()
    {
      var code = "function MyFunc() : void\begin\nx = 1\ny = 2\nend";
      var report = TestHelper.ApplyRule(rule, code,
        componentType: ComponentType.Function);
      TestHelper.AssertNoMessages(report);
    }

    [TestMethod]
    public void LargeFunction_ShouldReport()
    {
      var sb = new StringBuilder();
      sb.AppendLine("function MyFunc() : void");
      sb.AppendLine("begin");
      for (int i = 0; i < 600; i++)
      {
        sb.AppendLine($"x{i} = {i}");
      }
      sb.AppendLine("end");

      var report = TestHelper.ApplyRule(rule, sb.ToString(),
        componentType: ComponentType.Function);
      Assert.IsTrue(report.Messages.Count() > 0, "Expected function too big warning");
    }
  }
}
