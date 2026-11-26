namespace IsblCheck.Core.Context.Development
{
  /// <summary>Calculation of a role from the РОЛ reference exported alongside routes.</summary>
  public class WorkflowRole : Component
  {
    public ComponentState State { get; set; }
    public string CalculationText { get; set; }
  }
}
