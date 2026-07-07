using System.Collections.Generic;
using System.Linq;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsblCheck.BaseRules.Tests
{
  /// <summary>
  /// Утилиты для тестирования правил.
  /// </summary>
  internal static class TestHelper
  {
    /// <summary>
    /// Применить правило к ISBL-коду и вернуть отчет.
    /// </summary>
    internal static IReport ApplyRule(AbstractRule rule, string isblCode,
      ComponentType componentType = ComponentType.Script,
      string componentName = "TestScript",
      string path = "",
      List<string> contextVariables = null)
    {
      var reportManager = new ReportManager();
      var report = reportManager.Create();
      var document = new Document("test", isblCode)
      {
        ComponentType = componentType,
        ComponentName = componentName,
        Path = path
      };
      if (contextVariables != null)
      {
        document.ContextVariables.AddRange(contextVariables);
      }
      rule.Apply(report, document, ContextHelper.Context);
      return report;
    }

    /// <summary>
    /// Проверить, что отчет не содержит сообщений.
    /// </summary>
    internal static void AssertNoMessages(IReport report)
    {
      Assert.AreEqual(0, report.Messages.Count(),
        $"Expected no messages but found {report.Messages.Count()}: " +
        string.Join("; ", report.Messages.Select(m => $"{m.Code}: {m.Description}")));
    }

    /// <summary>
    /// Проверить, что отчет содержит ровно одно сообщение с указанным кодом.
    /// </summary>
    internal static IReportMessage AssertSingleMessage(IReport report,
      string expectedCode, Severity expectedSeverity)
    {
      Assert.AreEqual(1, report.Messages.Count(),
        $"Expected 1 message but found {report.Messages.Count()}: " +
        string.Join("; ", report.Messages.Select(m => $"{m.Code}: {m.Description}")));
      var msg = report.Messages.First();
      Assert.AreEqual(expectedCode, msg.Code, "Message code mismatch");
      Assert.AreEqual(expectedSeverity, msg.Severity, "Message severity mismatch");
      return msg;
    }

    /// <summary>
    /// Проверить, что отчет содержит сообщения с указанным кодом.
    /// </summary>
    internal static IEnumerable<IReportMessage> AssertMessages(IReport report,
      string expectedCode, Severity expectedSeverity, int expectedCount)
    {
      var messages = report.Messages
        .Where(m => m.Code == expectedCode && m.Severity == expectedSeverity)
        .ToList();
      Assert.AreEqual(expectedCount, messages.Count,
        $"Expected {expectedCount} messages with code {expectedCode} but found {messages.Count}");
      return messages;
    }
  }
}
