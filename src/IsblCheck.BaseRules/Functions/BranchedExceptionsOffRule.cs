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
  /// Правило обнаружения асимметричного ExceptionsOff в ветвлениях IF.
  /// </summary>
  internal class BranchedExceptionsOffRule : AbstractRule
  {
    private const string Code = "F037";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(BranchedExceptionsOffRule).Name,
        "Правило обнаружения асимметричного ExceptionsOff в ветвлениях IF."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> ExceptionsOffMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "ExceptionsOff", "ОтключитьИсключения"
    };

    private static readonly HashSet<string> ExceptionsOnMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "ExceptionsOn", "ВключитьИсключения"
    };

    private class BranchedListener : IsblBaseListener
    {
      public List<IsblParser.IfStatementContext> AsymmetricIfs { get; } =
        new List<IsblParser.IfStatementContext>();

      public override void EnterIfStatement(IsblParser.IfStatementContext context)
      {
        if (context.ChildCount < 5)
          return;

        var thenBlock = FindStatementBlock(context, 0);
        var elseBlock = FindStatementBlock(context, 1);

        if (thenBlock == null || elseBlock == null)
          return;

        bool thenHasOff = BlockContainsCall(thenBlock, ExceptionsOffMethods);
        bool elseHasOff = BlockContainsCall(elseBlock, ExceptionsOffMethods);
        bool thenHasOn = BlockContainsCall(thenBlock, ExceptionsOnMethods);
        bool elseHasOn = BlockContainsCall(elseBlock, ExceptionsOnMethods);

        if ((thenHasOff && !elseHasOff && elseHasOn) ||
            (elseHasOff && !thenHasOff && thenHasOn))
        {
          AsymmetricIfs.Add(context);
        }
      }

      private IsblParser.StatementBlockContext FindStatementBlock(IsblParser.IfStatementContext context, int which)
      {
        int found = 0;
        for (int i = 0; i < context.ChildCount; i++)
        {
          if (context.GetChild(i) is IsblParser.StatementBlockContext block)
          {
            if (found == which)
              return block;
            found++;
          }
        }
        return null;
      }

      private bool BlockContainsCall(IsblParser.StatementBlockContext block, HashSet<string> methodNames)
      {
        return FindFunctions(block, methodNames);
      }

      private bool FindFunctions(IParseTree tree, HashSet<string> names)
      {
        if (tree is IsblParser.FunctionContext func)
        {
          if (names.Contains(func.identifier().GetText()))
            return true;
        }
        for (int i = 0; i < tree.ChildCount; i++)
        {
          if (FindFunctions(tree.GetChild(i), names))
            return true;
        }
        return false;
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new BranchedListener();
      walker.Walk(listener, tree);

      foreach (var ifStmt in listener.AsymmetricIfs)
      {
        report.AddWarning(Code,
          "Асимметричный ExceptionsOff: одна ветвь подавляет исключения, другая — восстанавливает без подавления.",
          document, ifStmt.Start.ToTextPosition());
      }
    }
  }
}
