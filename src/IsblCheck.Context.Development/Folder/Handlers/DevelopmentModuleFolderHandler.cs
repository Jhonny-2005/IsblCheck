using System.Collections.Generic;
using IsblCheck.Context.Development.Package.Handlers;
using IsblCheck.Context.Development.Package.Models;
using IsblCheck.Core.Context.Development;
namespace IsblCheck.Context.Development.Folder.Handlers
{
  internal class DevelopmentModuleFolderHandler : FolderHandlerBase<DevelopmentModule, ComponentModel>
  {
    protected override string FolderName => "Modules";
    protected override string CardModelRootNode => "Module";
    protected override IEnumerable<DevelopmentModule> ReadComponents(ComponentModel model, string path)
    {
      yield return PackageHandlerUtils.CreateEntity<DevelopmentModule>(model);
    }
  }
}
