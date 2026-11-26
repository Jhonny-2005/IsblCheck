using System.Collections.Generic;
using IsblCheck.Context.Development.Package.Models;
using IsblCheck.Core.Context.Development;
namespace IsblCheck.Context.Development.Package.Handlers
{
  internal class DevelopmentModulePackageHandler : IPackageHandler<DevelopmentModule>
  {
    public IEnumerable<DevelopmentModule> Read(ComponentsModel packageModel)
    {
      foreach (var model in packageModel.Modules ?? new List<ComponentModel>())
        yield return PackageHandlerUtils.CreateEntity<DevelopmentModule>(model);
    }
  }
}
