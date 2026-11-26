using System;
using System.Collections.Generic;
using IsblCheck.Core.Context.Development;

namespace IsblCheck.Context.Development.Database.Handlers
{
  // A localized string is identified by both group and code, not by code alone.
  internal sealed class LocalizationAccumulator
  {
    private readonly Dictionary<string, LocalizationString> entries = new(StringComparer.OrdinalIgnoreCase);
    internal IEnumerable<LocalizationString> Entries => entries.Values;
    internal void Add(string group, string name, string language, string value)
    {
      if (string.IsNullOrWhiteSpace(name)) return;
      var key = (group ?? string.Empty) + "\0" + name;
      if (!entries.TryGetValue(key, out var entry))
      {
        entry = new LocalizationString { Name = name, Title = name, Group = group };
        entries.Add(key, entry);
      }
      if (language != null || value != null)
        entry.Values.Add(new LocalizationValue { Language = language, Value = value });
    }
  }
}
