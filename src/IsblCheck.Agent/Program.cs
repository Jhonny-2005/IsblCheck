using System;
using System.Linq;
using System.Reflection;
using NLog;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Reports;
using IsblCheck.Context.Application;
using IsblCheck.Context.Development;
using IsblCheck.Context.Development.ReferenceExport;
using IsblCheck.Reports.Printers;
using System.IO;

namespace IsblCheck.Agent
{
  internal class Program
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();
    internal static int Main(string[] args)
    {
      if (args.Length == 1 && args[0] == "--version")
      {
        Console.WriteLine(typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
        return 0;
      }
      if (args.Length == 1 && args[0] == "--help")
      {
        Console.WriteLine("ISBL Check Agent: запуск без аргументов использует IsblCheck.Agent.dll.config рядом с приложением.");
        Console.WriteLine("--version: версия; --help: справка. Коды выхода: 0 — проверка завершена без ошибок анализа; 1 — ошибки анализа; 2 — ошибка конфигурации/запуска.");
        Console.WriteLine("--routes <TipMarsh.xml или папка> [--report <report.csv|report.html>]: автономная проверка выгрузки маршрутов и ролей.");
        Console.WriteLine("--numeric-hints: включить необязательные подсказки о числовых литералах (F016), это не ошибки ISBL.");
        return 0;
      }
      var includeNumericHints = args.Count(a => a == "--numeric-hints");
      if (includeNumericHints > 1) { Console.Error.WriteLine("Параметр --numeric-hints задан повторно."); return 2; }
      args = args.Where(a => a != "--numeric-hints").ToArray();
      var routesMode = (args.Length == 2 || args.Length == 4) && args[0] == "--routes" &&
        (args.Length == 2 || args[2] == "--report");
      if (args.Length != 0 && !routesMode)
      {
        Console.Error.WriteLine("Неизвестные параметры. Используйте --help.");
        return 2;
      }
      try
      {
        LogManager.Setup().LoadConfigurationFromFile(Path.Combine(AppContext.BaseDirectory, "NLog.config"), optional: true);
        log.Info("Запуск агента");
        using var checker = new CodeChecker { IncludeOptionalRules = includeNumericHints == 1 };
        Console.WriteLine(includeNumericHints == 1 ? "Необязательные подсказки F016 о числовых литералах включены." : "Необязательные подсказки F016 о числовых литералах отключены; используйте --numeric-hints для включения.");
        if (routesMode)
        {
          checker.ContextManager.Load(new ApplicationContextFactory());
          checker.ContextManager.Load(new DevelopmentContextFactory(new[] { new StandardRouteExportProvider(args[1]) }));
          checker.RuleManager.LoadLibraries(Path.Combine(AppContext.BaseDirectory, "Rules"));
          checker.ReportManager.Printers.Add(args.Length != 4 ? new ConsoleReportPrinter() :
            Path.GetExtension(args[3]).Equals(".html", StringComparison.OrdinalIgnoreCase) ? new HtmlReportPrinter(args[3]) : new CsvReportPrinter(args[3]));
        }
        else checker.Configure();
        var context = checker.ContextManager.Context.Development;
        var documents = CodeCheckerCalculationProvider.GetDocuments(context).ToList();
        if (routesMode)
        {
          var schemes = documents.Count(d => d.ComponentType == ComponentType.StandardRoute && d.Path == "Scheme");
          Console.WriteLine($"Маршрутов: {context.StandardRoutes.Count}, ролей: {context.WorkflowRoles.Count}, вычислений: {documents.Count - schemes}, схем к проверке: {schemes}.");
          Console.WriteLine("Выгрузка маршрутов не содержит полного каталога справочников и локализаций; проверки отсутствующих зависимостей отключены.");
        }
        var report = checker.Check(documents).GetAwaiter().GetResult();
        report.Print();
        log.Info("Агент успешно завершил работу");
        return report.Messages.Any(m => m.Severity == Severity.Error) ? 1 : 0;
      }
      catch (Exception exception)
      {
        log.Error(exception, "Не удалось выполнить проверку ISBL");
        Console.Error.WriteLine("Не удалось выполнить проверку ISBL. Проверьте параметры и журнал агента.");
        return 2;
      }
    }
  }
}
