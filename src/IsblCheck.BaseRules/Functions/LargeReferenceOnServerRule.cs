using System;
using System.Collections.Generic;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  /// <summary>
  /// Правило обнаружения открытия больших справочников на сервере.
  /// </summary>
  internal class LargeReferenceOnServerRule : AbstractRule
  {
    private const string Code = "F051";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(LargeReferenceOnServerRule).Name,
        "Правило обнаружения открытия больших справочников на сервере."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> ReferenceFactoryMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "CreateReference", "СоздатьСправочник"
    };

    private static readonly HashSet<string> FindAllMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "FindAll", "НайтиВсе", "Open", "Открыть"
    };

    private class LargeReferenceListener : IsblBaseListener
    {
      public List<IsblParser.FunctionContext> UnsafeReferences { get; } = new List<IsblParser.FunctionContext>();

      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var name = context.identifier().GetText();
        if (ReferenceFactoryMethods.Contains(name))
        {
          UnsafeReferences.Add(context);
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      if (!document.Path.StartsWith("Events.", StringComparison.OrdinalIgnoreCase))
        return;

      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new LargeReferenceListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.UnsafeReferences)
      {
        report.AddWarning(Code,
          "Создание справочника на сервере. Избегайте открытия больших справочников без фильтрации.",
          document, call.identifier().GetTextPosition());
      }
    }
  }
}
