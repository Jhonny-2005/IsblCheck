using System;
using System.Collections.Generic;
using System.Linq;
using IsblCheck.Core.Context;
using IsblCheck.Core.Context.Application;
using IsblCheck.Core.Context.Development;

namespace IsblCheck.BaseRules.Tests
{
  /// <summary>
  /// Мок контекста для тестирования правил.
  /// </summary>
  public static class ContextHelper
  {
    private static readonly Lazy<IContext> lazyContext = new Lazy<IContext>(() =>
    {
      var appContext = new MockApplicationContext();
      var devContext = new MockDevelopmentContext();
      return new MockContext(appContext, devContext);
    });

    public static IContext Context => lazyContext.Value;
  }

  /// <summary>
  /// Мок IContext.
  /// </summary>
  internal class MockContext : IContext
  {
    public IApplicationContext Application { get; }
    public IDevelopmentContext Development { get; }

    public MockContext(IApplicationContext application, IDevelopmentContext development)
    {
      Application = application;
      Development = development;
    }
  }

  /// <summary>
  /// Мок IApplicationContext.
  /// </summary>
  internal class MockApplicationContext : IApplicationContext
  {
    public IReadOnlyDictionary<string, object> Constants { get; }
    public IReadOnlyDictionary<string, int> Enums { get; }
    public IReadOnlyList<string> PredefinedVariables { get; }
    public IReadOnlyList<Function> Functions { get; }

    public MockApplicationContext()
    {
      Constants = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
      Enums = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
      PredefinedVariables = new List<string>
      {
        "This", "Self", "Object", "Sender", "Result",
        "Application", "ComponentTokens", "EDocuments", "Folders",
        "GlobalIDs", "Jobs", "References", "Reports", "Scripts",
        "Searches", "ServerEvents", "ServiceFactory", "SystemDialogs",
        "Tasks", "Wizards"
      };
      Functions = new List<Function>();
    }

    public bool IsExistConstant(string name)
    {
      return Constants.ContainsKey(name);
    }

    public string GetConstantValue(string name)
    {
      if (Constants.TryGetValue(name, out var value))
        return value?.ToString();
      return null;
    }

    public bool IsExistPredefinedVariable(string name)
    {
      return PredefinedVariables.Any(p =>
        string.Equals(p, name, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsExistEnumValue(string name)
    {
      return Enums.ContainsKey(name);
    }

    public bool IsExistsSysReference(string name, bool withOldReference)
    {
      return false;
    }
  }

  /// <summary>
  /// Мок IDevelopmentContext.
  /// </summary>
  internal class MockDevelopmentContext : IDevelopmentContext
  {
    public IList<CommonReport> CommonReports { get; }
    public IList<Constant> Constants { get; }
    public IList<DialogRequisite> DialogRequisites { get; }
    public IList<Dialog> Dialogs { get; }
    public IList<DocumentCardType> DocumentCardTypes { get; }
    public IList<DocumentRequisite> DocumentRequisites { get; }
    public IList<Function> Functions { get; }
    public IList<IntegratedReport> IntegratedReports { get; }
    public IList<LocalizationString> LocalizationStrings { get; }
    public IList<ManagedFolder> ManagedFolders { get; }
    public IList<ReferenceRequisite> ReferenceRequisites { get; }
    public IList<ReferenceType> ReferenceTypes { get; }
    public IList<RouteBlock> RouteBlocks { get; }
    public IList<Script> Scripts { get; }
    public IList<Viewer> Viewers { get; }
    public IList<StandardRoute> StandardRoutes { get; }
    public IList<Wizard> Wizards { get; }

    public MockDevelopmentContext()
    {
      CommonReports = new List<CommonReport>();
      Constants = new List<Constant>();
      DialogRequisites = new List<DialogRequisite>();
      Dialogs = new List<Dialog>();
      DocumentCardTypes = new List<DocumentCardType>();
      DocumentRequisites = new List<DocumentRequisite>();
      Functions = new List<Function>();
      IntegratedReports = new List<IntegratedReport>();
      LocalizationStrings = new List<LocalizationString>();
      ManagedFolders = new List<ManagedFolder>();
      ReferenceRequisites = new List<ReferenceRequisite>();
      ReferenceTypes = new List<ReferenceType>();
      RouteBlocks = new List<RouteBlock>();
      Scripts = new List<Script>();
      Viewers = new List<Viewer>();
      StandardRoutes = new List<StandardRoute>();
      Wizards = new List<Wizard>();
    }
  }
}
