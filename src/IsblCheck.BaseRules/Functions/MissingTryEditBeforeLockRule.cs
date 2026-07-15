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
  /// Правило обнаружения Lock без предварительной проверки TryEdit.
  /// </summary>
  internal class MissingTryEditBeforeLockRule : AbstractRule
  {
    private const string Code = "F054";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(MissingTryEditBeforeLockRule).Name,
        "Правило обнаружения Lock без предварительной проверки TryEdit."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> LockMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "Lock", "LockForServer", "Заблокировать", "ЗаблокироватьДляСервера"
    };

    private static readonly HashSet<string> TryEditMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "TryEdit", "ПопыткаРедактирования"
    };

    private class LockListener : IsblBaseListener
    {
      public List<IsblParser.InvocationCallContext> UnsafeLocks { get; } = new List<IsblParser.InvocationCallContext>();
      private bool hasTryEdit = false;

      public override void EnterInvocationCall(IsblParser.InvocationCallContext context)
      {
        var name = context.identifier().GetText();
        if (TryEditMethods.Contains(name))
        {
          hasTryEdit = true;
        }
        else if (LockMethods.Contains(name) && !hasTryEdit)
        {
          UnsafeLocks.Add(context);
        }
      }

      public override void ExitIfStatement(IsblParser.IfStatementContext context)
      {
        hasTryEdit = false;
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new LockListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.UnsafeLocks)
      {
        report.AddWarning(Code,
          "Lock без предварительной проверки TryEdit может привести к необработанным исключениям.",
          document, call.identifier().GetTextPosition());
      }
    }
  }
}
