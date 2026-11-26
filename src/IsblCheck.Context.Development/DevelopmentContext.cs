using System.Collections.Generic;
using IsblCheck.Core.Context.Development;

namespace IsblCheck.Context.Development
{
  /// <summary>
  /// Контекст разработки.
  /// </summary>
  internal class DevelopmentContext : IDevelopmentContext, IMetadataAvailability
  {
    private bool referenceCatalogAvailable;
    private bool localizationCatalogAvailable;

    // The collections can also be populated by callers after context creation.
    // Availability means a catalog is present, not that an export is complete.
    public bool CanValidateReferences
    {
      get => this.referenceCatalogAvailable || this.ReferenceTypes?.Count > 0;
      internal set => this.referenceCatalogAvailable = value;
    }
    public bool CanValidateLocalization
    {
      get => this.localizationCatalogAvailable || this.LocalizationStrings?.Count > 0;
      internal set => this.localizationCatalogAvailable = value;
    }
    #region IDevelopmentContext

    public IList<CommonReport> CommonReports { get; internal set; }

    public IList<Constant> Constants { get; internal set; }

    public IList<DialogRequisite> DialogRequisites { get; internal set; }

    public IList<Dialog> Dialogs { get; internal set; }

    public IList<DocumentCardType> DocumentCardTypes { get; internal set; }

    public IList<DocumentRequisite> DocumentRequisites { get; internal set; }

    public IList<Function> Functions { get; internal set; }

    public IList<IntegratedReport> IntegratedReports { get; internal set; }

    public IList<LocalizationString> LocalizationStrings { get; internal set; }

    public IList<ManagedFolder> ManagedFolders { get; internal set; }

    public IList<ReferenceRequisite> ReferenceRequisites { get; internal set; }

    public IList<ReferenceType> ReferenceTypes { get; internal set; }

    public IList<RouteBlock> RouteBlocks { get; internal set; }

    public IList<Script> Scripts { get; internal set; }

    public IList<StandardRoute> StandardRoutes { get; internal set; }
    public IList<WorkflowRole> WorkflowRoles { get; internal set; }
    public IList<DevelopmentModule> Modules { get; internal set; }
    public IList<ServerEvent> ServerEvents { get; internal set; }

    public IList<Viewer> Viewers { get; internal set; }

    public IList<Wizard> Wizards { get; internal set; } 

    #endregion
  }
}
