using System.Collections.Generic;
using IsblCheck.Context.Development.Package.Models;
using IsblCheck.Core.Context.Development;
namespace IsblCheck.Context.Development.Package.Handlers
{
  internal class ServerEventPackageHandler : IPackageHandler<ServerEvent>
  {
    public IEnumerable<ServerEvent> Read(ComponentsModel packageModel)
    {
      foreach (var model in packageModel.ServerEvents ?? new List<ComponentModel>())
        yield return PackageHandlerUtils.CreateEntity<ServerEvent>(model);
    }
  }
}
