using System;
using System.IO;
using IsblCheck.Core.Context.Development;

namespace IsblCheck.Context.Development.Utils
{
  internal static class RecordStateParser
  {
    public static ComponentState Parse(string value)
    {
      var state = value?.Trim();
      if (string.IsNullOrEmpty(state) || state == "Д" || state.Equals("Действующая", StringComparison.OrdinalIgnoreCase) || state.Equals("Active", StringComparison.OrdinalIgnoreCase)) return ComponentState.Active;
      if (state == "З" || state.Equals("Закрытая", StringComparison.OrdinalIgnoreCase) || state.Equals("Closed", StringComparison.OrdinalIgnoreCase)) return ComponentState.Closed;
      throw new InvalidDataException($"Неизвестное состояние записи: {value}");
    }
  }
}
