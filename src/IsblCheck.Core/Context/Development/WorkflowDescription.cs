using System.Collections.Generic;

namespace IsblCheck.Core.Context.Development
{
  /// <summary>
  /// Описание маршрута.
  /// </summary>
  public class WorkflowDescription
  {
    /// <summary>
    /// Действия маршрута.
    /// </summary>
    public List<Action> Actions { get; } = new List<Action>();

    /// <summary>
    /// Блоки маршрута.
    /// </summary>
    public List<WorkflowBlock> Blocks { get; } = new List<WorkflowBlock>();

    /// <summary>
    /// События маршрута.
    /// </summary>
    public List<WorkflowEvent> Events { get; } = new List<WorkflowEvent>();

    /// <summary>Вычисляемые свойства самой задачи (Settings/Properties).</summary>
    public List<WorklflowIsblProperty> IsblProperties { get; } = new List<WorklflowIsblProperty>();

    public bool HasParameterCatalog { get; set; }
    public bool HasRouteSection { get; set; }
    public bool HasUnparsedBindings { get; set; }
    public List<WorkflowParameter> Parameters { get; } = new();
    public List<WorkflowParameterBinding> ParameterBindings { get; } = new();
    public List<WorkflowEdge> Edges { get; } = new();
    public List<string> RouteBlockIds { get; } = new();
  }
}
