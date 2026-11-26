using System.Collections.Generic;
using IsblCheck.Context.Development.Package.Handlers;
using IsblCheck.Context.Development.Package.Models;
using IsblCheck.Core.Context.Development;
namespace IsblCheck.Context.Development.Folder.Handlers
{
  internal class ServerEventFolderHandler : FolderHandlerBase<ServerEvent, ComponentModel>
  {
    protected override string FolderName => "ServerEvents";
    protected override string CardModelRootNode => "ServerEvent";
    protected override IEnumerable<ServerEvent> ReadComponents(ComponentModel model, string path)
    {
      yield return PackageHandlerUtils.CreateEntity<ServerEvent>(model);
    }
  }
}
