using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Security
{
  /// <summary>
  /// Правило обнаружения хардкода строк подключения к БД.
  /// </summary>
  internal class HardcodedConnectionStringRule : AbstractRule
  {
    private const string Code = "S009";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(HardcodedConnectionStringRule).Name,
        "Правило обнаружения хардкода строк подключения к базе данных."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly Regex ConnectionStringPattern = new Regex(
      @"Provider=|Data Source=|Server=|Database=|Initial Catalog=|Server=",
      RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private class ConnectionStringListener : IsblBaseListener
    {
      public List<IsblParser.StringContext> HardcodedStrings { get; } = new List<IsblParser.StringContext>();

      public override void EnterString(IsblParser.StringContext context)
      {
        var text = context.GetText().Trim('"', '\'');
        if (ConnectionStringPattern.IsMatch(text) && text.Length > 20)
        {
          HardcodedStrings.Add(context);
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new ConnectionStringListener();
      walker.Walk(listener, tree);

      foreach (var str in listener.HardcodedStrings)
      {
        report.AddWarning(Code,
          "Хардкод строки подключения к базе данных. Используйте конфигурационные файлы.",
          document, str.Start.ToTextPosition());
      }
    }
  }
}
