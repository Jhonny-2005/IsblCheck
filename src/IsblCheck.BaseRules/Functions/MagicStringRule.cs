using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  /// <summary>
  /// Правило обнаружения магических строк (хардкод идентификаторов).
  /// </summary>
  internal class MagicStringRule : AbstractRule
  {
    private const string Code = "F039";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(MagicStringRule).Name,
        "Правило обнаружения магических строк (хардкод идентификаторов)."), true);

    public static IRuleInfo Info => info.Value;

    private const int MinStringForWarning = 5;

    private static readonly Regex IdentifierPattern = new Regex(
      @"^[A-Z][a-zA-Z0-9_]{4,}$", RegexOptions.Compiled);

    private static readonly Regex PathPattern = new Regex(
      @"^[A-Z][a-zA-Z]+\.[A-Z][a-zA-Z]+$", RegexOptions.Compiled);

    private class MagicStringListener : IsblBaseListener
    {
      public List<IsblParser.StringContext> MagicStrings { get; } =
        new List<IsblParser.StringContext>();

      public override void EnterString(IsblParser.StringContext context)
      {
        var text = context.GetText().Trim('"', '\'');

        if (string.IsNullOrEmpty(text) || text.Length < MinStringForWarning)
          return;

        if (IdentifierPattern.IsMatch(text) || PathPattern.IsMatch(text))
        {
          MagicStrings.Add(context);
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new MagicStringListener();
      walker.Walk(listener, tree);

      foreach (var str in listener.MagicStrings)
      {
        var value = str.GetText();
        report.AddInformation(Code,
          $"Магическая строка {value}. Рекомендуется вынести в константу.",
          document, str.Start.ToTextPosition());
      }
    }
  }
}
