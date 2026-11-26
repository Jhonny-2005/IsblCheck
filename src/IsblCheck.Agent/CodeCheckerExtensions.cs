using System;
using System.Configuration;
using System.IO;
using NLog;
using IsblCheck.Agent.Configuration;
using IsblCheck.Context.Application;
using IsblCheck.Context.Development;
using IsblCheck.Context.Development.Database;
using IsblCheck.Context.Development.Folder;
using IsblCheck.Context.Development.Package;
using IsblCheck.Core.Checker;
using IsblCheck.Reports.Printers;

namespace IsblCheck.Agent
{
  /// <summary>
  /// Методы расширения для CodeChecker.
  /// </summary>
  public static class CodeCheckerExtensions
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Сконфигурировать чекер.
    /// </summary>
    /// <param name="checker">Чекер.</param>
    public static void Configure(this ICodeChecker checker)
    {
      var section = (AgentConfigurationSection)ConfigurationManager.GetSection("agent");
      if (section == null)
        throw new ConfigurationErrorsException("Не найдена секция конфигурации <agent>.");
      if (section.ContextProviders == null || section.ContextProviders.Count == 0)
        throw new ConfigurationErrorsException("В секции <agent> не задан ни один источник контекста разработки.");
      if (section.ReportPrinters == null || section.ReportPrinters.Count == 0)
        throw new ConfigurationErrorsException("В секции <agent> не задан ни один способ вывода отчета.");

      log.Trace("Загрузка контекста приложения");
      var applicationContextFactory = new ApplicationContextFactory();
      checker.ContextManager.Load(applicationContextFactory);

      log.Trace("Загрузка контекста разработки");
      var developmentContextFactory = new DevelopmentContextFactory();
      foreach (ContextProviderElement element in section.ContextProviders)
      {
        switch (element.Provider)
        {
          case ContextProviderType.Package:
            log.Trace($"Найден загрузчик контекста из пакета {element.FilePath}");
            var packageProvider = new PackageProvider(element.FilePath);
            developmentContextFactory.Providers.Add(packageProvider);
            break;
          case ContextProviderType.Database:
            log.Trace($"Найден загрузчик контекста из базы данных {element.ConnectionString}");
            var databaseProvider = new DatabaseProvider(element.ConnectionString);
            developmentContextFactory.Providers.Add(databaseProvider);
            break;
          case ContextProviderType.Folder:
            log.Trace($"Найден загрузчик контекста из папки {element.FolderPath}");
            var folderProvider = new FolderProvider(element.FolderPath);
            developmentContextFactory.Providers.Add(folderProvider);
            break;
          case ContextProviderType.RouteExport:
            developmentContextFactory.Providers.Add(new IsblCheck.Context.Development.ReferenceExport.StandardRouteExportProvider(element.FilePath));
            break;
        }
      }
      checker.ContextManager.Load(developmentContextFactory);

      log.Trace("Загрузка сборок с правилами анализа");
      if (!string.IsNullOrEmpty(section.RuleLibraryPath))
      {
        var rulesPath = Path.IsPathRooted(section.RuleLibraryPath)
          ? section.RuleLibraryPath
          : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, section.RuleLibraryPath);
        if (Directory.Exists(rulesPath))
          checker.RuleManager.LoadLibraries(rulesPath);
        else
          throw new ConfigurationErrorsException($"Каталог сборок правил не найден: {rulesPath}");
      }

      log.Trace("Загрузка генераторов отчетов");
      if (section.ReportPrinters != null)
      {
        foreach (ReportPrinterElement element in section.ReportPrinters)
        {
          switch (element.Printer)
          {
            case ReportPrinterType.Console:
              var consoleReportPrinter = new ConsoleReportPrinter();
              checker.ReportManager.Printers.Add(consoleReportPrinter);
              break;
            case ReportPrinterType.CSV:
              var csvReportPrinter = new CsvReportPrinter(element.FilePath);
              checker.ReportManager.Printers.Add(csvReportPrinter);
              break;
          }
        }
      }
    }
  }
}
