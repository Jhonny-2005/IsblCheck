using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using IsblCheck.Context.Development.ReferenceExport;
using NLog;
using IsblCheck.Context.Development.Folder.Handlers;
using IsblCheck.Core.Context.Development;

namespace IsblCheck.Context.Development.Folder
{
  public class FolderProvider : IDevelopmentContextProvider, IMetadataAvailability
  {
    public bool CanValidateReferences => System.IO.Directory.Exists(System.IO.Path.Combine(workspacePath, "ReferenceTypes"));
    public bool CanValidateLocalization => System.IO.Directory.Exists(System.IO.Path.Combine(workspacePath, "LocalizationStrings"));
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    private readonly string workspacePath;
    private readonly StandardRouteExportProvider routeExports;

    public IEnumerable<T> ReadComponents<T>() where T : Component
    {
      var handlerType = ResolveComponentHandler<T>();
      if (handlerType == null)
        return this.routeExports.ReadComponents<T>();
      var handler = (IFolderHandler<T>)Activator.CreateInstance(handlerType);
      try
      {
        return handler.Read(this.workspacePath).Concat(this.routeExports.ReadComponents<T>()).ToList();
      }
      catch (Exception ex)
      {
        log.Error("Components reading error.", ex);
        throw;
      }
    }

    public void ResetCache()
    {
      this.routeExports.ResetCache();
    }

    public FolderProvider(string workspacePath)
    {
      this.workspacePath = workspacePath;
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
      this.routeExports = new StandardRouteExportProvider(workspacePath, false);
    }

    private static Type ResolveComponentHandler<T>() where T : Component
    {
      return Assembly.GetExecutingAssembly().DefinedTypes
        .FirstOrDefault(t => t.IsClass && !t.IsAbstract && t.ImplementedInterfaces.Contains(typeof(IFolderHandler<T>)));
    }
  }
}
