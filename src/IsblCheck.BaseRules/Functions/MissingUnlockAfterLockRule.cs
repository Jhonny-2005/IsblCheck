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
  /// Правило обнаружения Lock без соответствующего Unlock.
  /// </summary>
  internal class MissingUnlockAfterLockRule : AbstractRule
  {
    private const string Code = "F055";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(MissingUnlockAfterLockRule).Name,
        "Правило обнаружения Lock без соответствующего Unlock."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> LockMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "Lock", "LockForServer", "Заблокировать", "ЗаблокироватьДляСервера"
    };

    private static readonly HashSet<string> UnlockMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "Unlock", "Разблокировать"
    };

    private class LockUnlockListener : IsblBaseListener
    {
      public List<IsblParser.InvocationCallContext> LeakedLocks { get; } = new List<IsblParser.InvocationCallContext>();

      public override void EnterInvocationCall(IsblParser.InvocationCallContext context)
      {
        var name = context.identifier().GetText();
        if (LockMethods.Contains(name))
        {
          LeakedLocks.Add(context);
        }
        else if (UnlockMethods.Contains(name))
        {
          if (LeakedLocks.Count > 0)
          {
            LeakedLocks.RemoveAt(LeakedLocks.Count - 1);
          }
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new LockUnlockListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.LeakedLocks)
      {
        report.AddWarning(Code,
          "Lock без соответствующего Unlock. Заблокированные документы недоступны другим пользователям.",
          document, call.identifier().GetTextPosition());
      }
    }
  }
}
