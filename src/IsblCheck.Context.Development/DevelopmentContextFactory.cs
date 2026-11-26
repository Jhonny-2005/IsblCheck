using System.Collections.Generic;
using System.Linq;
using IsblCheck.Core.Context.Development;

namespace IsblCheck.Context.Development
{
  /// <summary>
  /// Фабрика контекста разработки. 
  /// </summary>
  public class DevelopmentContextFactory : IDevelopmentContextFactory
  {
    #region IDevelopmentContextFactory

    /// <summary>
    /// Список провайдеров.
    /// </summary>
    public IList<IDevelopmentContextProvider> Providers { get; }

    /// <summary>
    /// Создать контекст разработки.
    /// </summary>
    /// <returns>Контекст разработки.</returns>
    public IDevelopmentContext Create()
    {
      var context = new DevelopmentContext
      {
        CommonReports = this.ReadComponents<CommonReport>(),
        Constants = this.ReadComponents<Constant>(),
        DialogRequisites = this.ReadComponents<DialogRequisite>(),
        Dialogs = this.ReadComponents<Dialog>(),
        DocumentRequisites = this.ReadComponents<DocumentRequisite>(),
        DocumentCardTypes = this.ReadComponents<DocumentCardType>(),
        Functions = this.ReadComponents<Function>(),
        IntegratedReports = this.ReadComponents<IntegratedReport>(),
        LocalizationStrings = this.ReadComponents<LocalizationString>(),
        ManagedFolders = this.ReadComponents<ManagedFolder>(),
        ReferenceRequisites = this.ReadComponents<ReferenceRequisite>(),
        ReferenceTypes = this.ReadComponents<ReferenceType>(),
        RouteBlocks = this.ReadComponents<RouteBlock>(),
        Scripts = this.ReadComponents<Script>(),
        Modules = this.ReadComponents<DevelopmentModule>(),
        ServerEvents = this.ReadComponents<ServerEvent>(),
        StandardRoutes = this.ReadComponents<StandardRoute>(),
        WorkflowRoles = this.ReadComponents<WorkflowRole>(),
        Viewers = this.ReadComponents<Viewer>(),
        Wizards = this.ReadComponents<Wizard>()
      };

      // Some providers learn their capabilities only after loading the package or
      // database version. An absent provider/catalog is not an empty known catalog.
      context.CanValidateReferences = this.Providers.OfType<IMetadataAvailability>().Any(p => p.CanValidateReferences);
      context.CanValidateLocalization = this.Providers.OfType<IMetadataAvailability>().Any(p => p.CanValidateLocalization);

      return context;
    }

    #endregion

    #region Методы

    /// <summary>
    /// Прочитать компоненты.
    /// </summary>
    /// <typeparam name="T">Тип компоненты.</typeparam>
    /// <returns>Список компонент.</returns>
    private IList<T> ReadComponents<T>() where T : Component
    {
      var mergedComponents = new List<T>();
      var componentNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
      foreach (var provider in this.Providers)
      {
        foreach (var component in provider.ReadComponents<T>() ?? Enumerable.Empty<T>())
        {
          // Component uses reference equality, so Except does not remove the same
          // logical component when it was loaded from multiple providers.
          if (component == null || string.IsNullOrWhiteSpace(component.Name)) continue;
          var key = component is LocalizationString localized
            ? (localized.Group ?? string.Empty) + "\0" + localized.Name : component.Name;
          if (componentNames.Add(key))
            mergedComponents.Add(component);
        }
      }

      return mergedComponents;
    }

    #endregion

    #region Конструкторы

    /// <summary>
    /// Конструктор.
    /// </summary>
    public DevelopmentContextFactory()
    {
      this.Providers = new List<IDevelopmentContextProvider>();
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="providers">Список провайдеров.</param>
    public DevelopmentContextFactory(IEnumerable<IDevelopmentContextProvider> providers)
    {
      this.Providers = new List<IDevelopmentContextProvider>(providers);
    }

    #endregion
  }
}
