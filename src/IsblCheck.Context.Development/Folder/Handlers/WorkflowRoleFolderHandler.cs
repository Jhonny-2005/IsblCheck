using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using IsblCheck.Context.Development.Folder.Models;
using IsblCheck.Context.Development.Utils;
using IsblCheck.Core.Context.Development;

namespace IsblCheck.Context.Development.Folder.Handlers
{
  /// <summary>Official DTU layout: Roles/{name}/Card.xml (Role) and Calculation.isbl.</summary>
  internal sealed class WorkflowRoleFolderHandler : FolderHandlerBase<WorkflowRole, RecordRefModel>
  {
    protected override string FolderName => "Roles";
    protected override string CardModelRootNode => "Role";
    protected override IEnumerable<WorkflowRole> ReadComponents(RecordRefModel model, string componentFolderPath)
    {
      if (string.IsNullOrWhiteSpace(model.Code)) throw new InvalidDataException($"У роли отсутствует Kod: {componentFolderPath}");
      var state = model.Requisites?.FirstOrDefault(r => r.Name == "Состояние")?.Value;
      var file = Path.Combine(componentFolderPath, "Calculation.isbl");
      yield return new WorkflowRole { Name = model.Code, Title = model.Name,
        State = RecordStateParser.Parse(state),
        CalculationText = File.Exists(file) ? File.ReadAllText(file, Encoding.GetEncoding(1251)) : string.Empty };
    }
  }
}
