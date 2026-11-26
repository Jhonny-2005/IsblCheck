using System.Collections.Generic;
using System.Linq;
using IsblCheck.Core.Context.Development;

namespace IsblCheck.Core.Checker
{
  /// <summary>Shared source-to-document mapping for the GUI and agent.</summary>
  public static class WorkflowDocumentProvider
  {
    public static IEnumerable<IDocument> GetDocuments(StandardRoute route) => GetDocuments(route, false);

    public static IEnumerable<IDocument> GetDocuments(StandardRoute route, bool includeScheme)
    {
      if (route?.WorkflowDescription == null || route.State != ComponentState.Active) yield break;
      var prefix = $"ТМ «{route.Title}» [{route.Name}]";
      if (includeScheme && (route.WorkflowDescription.HasRouteSection || route.WorkflowDescription.HasParameterCatalog))
        yield return new WorkflowSchemeDocument(route, $"{prefix}. Схема и параметры");
      foreach (var item in route.WorkflowDescription.Events.Where(e => !string.IsNullOrWhiteSpace(e.CalculationText)))
        yield return Create(route, $"{prefix}. Событие «{item.Title}»", item.CalculationText, $"Events.{item.Name}");
      foreach (var item in route.WorkflowDescription.Actions.Where(e => !string.IsNullOrWhiteSpace(e.CalculationText)))
        yield return Create(route, $"{prefix}. Действие «{item.Name}»", item.CalculationText, $"Actions.{item.Name}");
      foreach (var item in route.WorkflowDescription.IsblProperties.Where(e => !string.IsNullOrWhiteSpace(e.CalculationText)))
        yield return Create(route, $"{prefix}. Свойство задачи «{item.Title}»", item.CalculationText, $"Properties.{item.Name}", true);
      foreach (var block in route.WorkflowDescription.Blocks)
      {
        var label = $"{prefix}. Блок «{block.Name}» [{block.Id}]";
        foreach (var item in block.Events.Where(e => !string.IsNullOrWhiteSpace(e.CalculationText)))
        {
          var document = Create(route, $"{label}. Событие «{item.Title}»", item.CalculationText, $"Blocks.{block.Id}.Events.{item.Name}");
          AddBlockEventContext(document, item.Name);
          yield return document;
        }
        foreach (var item in block.Actions.Where(e => !string.IsNullOrWhiteSpace(e.CalculationText)))
          yield return Create(route, $"{label}. Действие «{item.Name}»", item.CalculationText, $"Blocks.{block.Id}.Actions.{item.Name}");
        foreach (var item in block.IsblProperties.Where(e => !string.IsNullOrWhiteSpace(e.CalculationText)))
          yield return Create(route, $"{label}. Свойство «{item.Title}»", item.CalculationText, $"Blocks.{block.Id}.Properties.{item.Name}", true);
      }
    }
    public static IEnumerable<IDocument> GetDocuments(WorkflowRole role)
    {
      if (role == null || role.State != ComponentState.Active || string.IsNullOrWhiteSpace(role.CalculationText)) yield break;
      var document = new Document($"Роль маршрута «{role.Title}» [{role.Name}]", role.CalculationText, true) {
        ComponentType = ComponentType.WorkflowRole, ComponentName = role.Name, Path = "Calculation"
      };
      // Directum 5.8, "Вычисления ролей": Role is the current role, scoped to this calculation.
      document.ContextVariables.AddRange(new[] { "Role", "!Role" });
      yield return document;
    }

    public static void AddBlockEventContext(IDocument document, string eventName)
    {
      var name = eventName?.ToUpperInvariant() switch
      {
        "ONCREATEJOBS" => "CreatedJobs",
        "ONCREATENOTICES" => "CreatedNotices",
        "BEFOREQUERYPARAMS" or "AFTERQUERYPARAMS" => "Job",
        "SUBTASKCREATE" or "SUBTASKINIT" or "SUBTASKSTART" => "SubTask",
        _ => null
      };
      if (name == null) return;
      document.ContextVariables.Add(name);
      document.ContextVariables.Add("!" + name);
    }

    private static Document Create(StandardRoute route, string title, string text, string path, bool expression = false) =>
      new Document(title, text, expression) { ComponentType = ComponentType.StandardRoute, ComponentName = route.Name,
        Path = path, WorkflowRoute = route };
  }
}
