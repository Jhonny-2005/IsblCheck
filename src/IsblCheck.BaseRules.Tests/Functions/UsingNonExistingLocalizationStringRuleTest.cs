using IsblCheck.Core.Checker;
using IsblCheck.Core.Context.Development;
using IsblCheck.Core.Reports;
using IsblCheck.BaseRules.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests.Functions
{
  [TestClass]
  public class UsingNonExistingLocalizationStringRuleTest
  {
    private readonly UsingNonExistingLocalizationStringRule rule = new UsingNonExistingLocalizationStringRule();

    [TestMethod]
    public void ExistingString_NoReport()
    {
      var locString = new LocalizationString
      {
        Name = "TestString",
        Group = "MESSAGES"
      };
      locString.Values.Add(new LocalizationValue { Language = "RU", Value = "Test" });
      ContextHelper.Context.Development.LocalizationStrings.Add(locString);

      var report = TestHelper.ApplyRule(rule,
        "LoadString(\"TestString\"; \"MESSAGES\")");
      TestHelper.AssertNoMessages(report);

      ContextHelper.Context.Development.LocalizationStrings.Remove(locString);
    }

    [TestMethod]
    public void NonExistingString_ShouldReport()
    {
      var report = TestHelper.ApplyRule(rule,
        "LoadString(\"NonExisting\"; \"MESSAGES\")");
      Assert.IsTrue(report.Messages.Count > 0, "Expected non-existing localization string error");
    }
  }
}
