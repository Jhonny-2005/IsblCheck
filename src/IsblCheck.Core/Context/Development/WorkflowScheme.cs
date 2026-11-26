namespace IsblCheck.Core.Context.Development
{
  public sealed record WorkflowParameter(string Name, string Type, bool HasDefault);

  public sealed record WorkflowParameterBinding(string Name, string Location, string BlockId = null,
    string ResultCode = null, bool Requested = false, bool Required = false, bool IsOutput = false);

  public sealed record WorkflowEdge(string From, string To, string ResultCode, bool IsInput);
}
