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
  /// Правило обнаружения магических чисел в коде.
  /// </summary>
  internal class MagicNumberRule : AbstractRule
  {
    private const string Code = "F032";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(MagicNumberRule).Name, "Правило обнаружения магических чисел (хардкод числовых констант)."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> AllowedNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "0", "1", "-1", "0.0", "1.0", "-1.0"
    };

    private class MagicNumberListener : IsblBaseListener
    {
      public List<IsblParser.OperandContext> MagicNumbers { get; } = new List<IsblParser.OperandContext>();

      public override void EnterOperand(IsblParser.OperandContext context)
      {
        if (context.@string() != null || context.identifier() != null || context.function() != null)
          return;

        var text = context.GetText().Trim();
        if (AllowedNumbers.Contains(text))
          return;

        if (Regex.IsMatch(text, @"^-?\d+(\.\d+)?$"))
        {
          MagicNumbers.Add(context);
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new MagicNumberListener();
      walker.Walk(listener, tree);

      foreach (var operand in listener.MagicNumbers)
      {
        report.AddInformation(Code, $"Магическое число '{operand.GetText()}'. Рекомендуется вынести в именованную константу.", document, operand.Start.ToTextPosition());
      }
    }
  }
}
