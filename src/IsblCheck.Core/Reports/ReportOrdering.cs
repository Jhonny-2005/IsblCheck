using System;
using System.Collections.Generic;
using System.Linq;

namespace IsblCheck.Core.Reports
{
  public static class ReportOrdering
  {
    public static IOrderedEnumerable<IReportMessage> ByImportance(this IEnumerable<IReportMessage> messages) =>
      messages.OrderBy(message => message.Severity)
        .ThenBy(message => message.Document?.Name, StringComparer.OrdinalIgnoreCase)
        .ThenBy(message => message.Document?.ComponentName, StringComparer.Ordinal)
        .ThenBy(message => message.Document?.Path, StringComparer.Ordinal)
        .ThenBy(message => message.Position.StartIndex)
        .ThenBy(message => message.Code, StringComparer.Ordinal)
        .ThenBy(message => message.Description, StringComparer.Ordinal);
  }
}
