using System;
using System.Collections.Generic;
using System.Linq;
using IsblCheck.Core.Context.Application;
using IsblCheck.Core.Context.Development;

namespace IsblCheck.Core.Context
{
  /// <summary>Shared lookup tables for one run of CodeChecker.</summary>
  internal sealed class AnalysisCatalog : IContext
  {
    public IApplicationContext Application { get; }
    public IDevelopmentContext Development { get; }
    internal Lazy<Dictionary<string, Function>> Signatures { get; }
    internal Lazy<HashSet<string>> UserFunctions { get; }
    internal Lazy<Dictionary<string, Dictionary<string, LocalizationString>>> Localizations { get; }
    internal Lazy<HashSet<string>> ReferenceNames { get; }

    internal AnalysisCatalog(IContext context)
    {
      Application = context.Application;
      Development = context.Development;
      Signatures = new(() =>
      {
        var functions = new Dictionary<string, Function>(StringComparer.OrdinalIgnoreCase);
        foreach (var function in (Development?.Functions ?? Enumerable.Empty<Function>())
          .Concat(Application?.Functions ?? Enumerable.Empty<Function>()))
          if (!string.IsNullOrWhiteSpace(function?.Name)) functions.TryAdd(function.Name, function);
        return functions;
      });
      UserFunctions = new(() => (Development?.Functions ?? Enumerable.Empty<Function>())
        .Where(f => f != null && !f.IsSystem).Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase));
      Localizations = new(() =>
      {
        var groups = new Dictionary<string, Dictionary<string, LocalizationString>>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in Development?.LocalizationStrings ?? Enumerable.Empty<LocalizationString>())
        {
          if (item?.Group == null || item.Name == null) continue;
          if (!groups.TryGetValue(item.Group, out var strings))
          {
            strings = new Dictionary<string, LocalizationString>(StringComparer.OrdinalIgnoreCase);
            groups.Add(item.Group, strings);
          }
          strings.TryAdd(item.Name, item);
        }
        return groups;
      });
      ReferenceNames = new(() => (Development?.ReferenceTypes ?? Enumerable.Empty<ReferenceType>())
        .Where(r => r?.Name != null).Select(r => r.Name.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase));
    }
  }

  // Keep direct rule calls and older context providers working without a run catalog.
  public static class AnalysisCatalogExtensions
  {
    public static Function FindFunctionSignature(this IContext context, string name)
    {
      if (string.IsNullOrWhiteSpace(name)) return null;
      if (context is AnalysisCatalog index)
        return index.Signatures.Value.GetValueOrDefault(name);
      return context.Development?.Functions?.FirstOrDefault(f => string.Equals(f?.Name, name, StringComparison.OrdinalIgnoreCase)) ??
        context.Application?.Functions?.FirstOrDefault(f => string.Equals(f?.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public static bool HasUserFunction(this IContext context, string name)
    {
      if (name == null) return false;
      return context is AnalysisCatalog index ? index.UserFunctions.Value.Contains(name) :
        context.Development?.Functions?.Any(f => f != null && !f.IsSystem && string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)) == true;
    }

    public static LocalizationString FindLocalization(this IContext context, string group, string name)
    {
      if (group == null || name == null) return null;
      if (context is AnalysisCatalog index)
        return index.Localizations.Value.TryGetValue(group, out var strings) ? strings.GetValueOrDefault(name) : null;
      return context.Development?.LocalizationStrings?.FirstOrDefault(s => s != null &&
        string.Equals(s.Group, group, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public static bool HasDevelopmentReference(this IContext context, string name) =>
      context is AnalysisCatalog index ? index.ReferenceNames.Value.Contains(name) :
        context.Development?.ReferenceTypes?.Any(r => string.Equals(r?.Name?.Trim(), name, StringComparison.OrdinalIgnoreCase)) == true;
  }
}
