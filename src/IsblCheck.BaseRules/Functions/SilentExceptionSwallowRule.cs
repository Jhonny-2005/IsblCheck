using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  /// <summary>
  /// Правило обнаружения тихого поглощения исключений (try/except с только FreeException).
  /// </summary>
  internal class SilentExceptionSwallowRule : AbstractRule
  {
    private const string Code = "F034";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(SilentExceptionSwallowRule).Name,
        "Правило обнаружения тихого поглощения исключений (try/except только с FreeException)."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> FreeExceptionMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "FreeException", "ОсвободитьИсключение"
    };

    private static readonly HashSet<string> ExceptionReadMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "GetLastException", "ОтлИнфДобавить", "ExceptionExists", "КонстЕсть"
    };

    private class SilentSwallowListener : IsblBaseListener
    {
      public List<IsblParser.TryStatementContext> SilentTryBlocks { get; } =
        new List<IsblParser.TryStatementContext>();

      public override void EnterTryStatement(IsblParser.TryStatementContext context)
      {
        if (context.ChildCount < 5)
          return;

        var exceptBlock = context.GetChild(2) as IsblParser.StatementBlockContext;
        if (exceptBlock == null)
          return;

        if (exceptBlock.ChildCount == 0)
          return;

        var functions = new List<string>();
        CollectFunctionNames(exceptBlock, functions);

        bool hasOnlyFreeException = functions.Count > 0 &&
          functions.All(f => FreeExceptionMethods.Contains(f)) &&
          !functions.Any(f => ExceptionReadMethods.Contains(f));

        if (hasOnlyFreeException)
        {
          SilentTryBlocks.Add(context);
        }
      }

      private void CollectFunctionNames(IParseTree tree, List<string> names)
      {
        if (tree is IsblParser.FunctionContext func)
        {
          names.Add(func.identifier().GetText());
        }
        for (int i = 0; i < tree.ChildCount; i++)
        {
          CollectFunctionNames(tree.GetChild(i), names);
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new SilentSwallowListener();
      walker.Walk(listener, tree);

      foreach (var tryStmt in listener.SilentTryBlocks)
      {
        report.AddWarning(Code,
          "Блок except содержит только FreeException без логирования. Исключения тихо поглощаются.",
          document, tryStmt.Start.ToTextPosition());
      }
    }
  }
}
