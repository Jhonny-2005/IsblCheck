using System;
using System.Linq;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;

namespace IsblCheck.BaseRules
{
  /// <summary>
  /// Класс-расширение для IContext.
  /// </summary>
  public static class IContextExtensions
  {
    /// <summary>
    /// Префикс, с которого начинаются строки локализации группы CONST.
    /// </summary>
    private const string ConstLocalizationStringPrefix = "CONST";
    private static readonly System.Collections.Generic.HashSet<string> WorkflowScopedVariables =
      new System.Collections.Generic.HashSet<string>(new[] { "CreatedJobs", "CreatedNotices", "Job", "SubTask", "Role" }, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Проверить, является ли переменная пользовательской.
    /// </summary>
    /// <param name="context">Текущий контекст.</param>
    /// <param name="document">Документ для проверки.</param>
    /// <param name="name">Имя переменной.</param>
    /// <returns>Признак того, что переменная пользовательская.</returns>
    public static bool IsUserVariable(this IContext context, IDocument document, string name)
    {
      // These names have a documented event scope in classic Directum 5.8.
      // The legacy application-wide list must not hide a missing local in another route event.
      var scoped = document.ComponentType is ComponentType.StandardRoute or ComponentType.RouteBlock or ComponentType.WorkflowRole &&
        WorkflowScopedVariables.Contains(name.TrimStart('!'));
      return !(document.ContextVariables.Any(v => string.Equals(v, name, StringComparison.OrdinalIgnoreCase))
        || (document.ComponentType == ComponentType.Function && !string.IsNullOrEmpty(document.ComponentName) &&
            string.Equals(document.ComponentName, name.TrimStart('!'), StringComparison.OrdinalIgnoreCase))
        || context.Application.IsExistConstant(name)
        || (!scoped && context.Application.IsExistPredefinedVariable(name))
        || context.Application.IsExistEnumValue(name)
        || IsConstLocalizationString(context, name));
    }

    /// <summary>
    /// Проверить, является ли переменная строкой локализации группы CONST.
    /// </summary>
    /// <param name="context">Текущий контекст.</param>
    /// <param name="name">Имя переменной.</param>
    /// <returns>True, если является.</returns>
    private static bool IsConstLocalizationString(IContext context, string name)
    {
      if (name.StartsWith(ConstLocalizationStringPrefix + "_", StringComparison.OrdinalIgnoreCase) &&
          context.Development is IsblCheck.Core.Context.Development.IMetadataAvailability metadata && !metadata.CanValidateLocalization)
        return true; // The route export cannot establish whether this CONST localization exists.
      if (name.StartsWith(ConstLocalizationStringPrefix + "_", StringComparison.OrdinalIgnoreCase) &&
          context.FindLocalization(ConstLocalizationStringPrefix, name.Substring(ConstLocalizationStringPrefix.Length + 1)) != null)
      {
        return true;
      }
      return false;
    }
  }
}
