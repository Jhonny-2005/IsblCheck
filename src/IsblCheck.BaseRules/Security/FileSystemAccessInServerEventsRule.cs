using System;
using System.Collections.Generic;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Security
{
  /// <summary>
  /// Правило обнаружения файловых операций в серверных событиях.
  /// </summary>
  internal class FileSystemAccessInServerEventsRule : AbstractRule
  {
    private const string Code = "S007";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(FileSystemAccessInServerEventsRule).Name,
        "Правило обнаружения файловых операций в серверных событиях."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> FileSystemMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "CreateFile", "DeleteFile", "CopyFile", "MoveFile", "FileExists",
      "CreateDirectory", "DeleteDirectory", "DirectoryExists",
      "СоздатьФайл", "УдалитьФайл", "СкопироватьФайл", "ПереместитьФайл",
      "ФайлСуществует", "СоздатьКаталог", "УдалитьКаталог", "КаталогСуществует"
    };

    private class FileSystemListener : IsblBaseListener
    {
      public List<IsblParser.FunctionContext> UnsafeCalls { get; } = new List<IsblParser.FunctionContext>();

      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var name = context.identifier().GetText();
        if (FileSystemMethods.Contains(name))
        {
          UnsafeCalls.Add(context);
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      if (!document.Path.StartsWith("Events.", StringComparison.OrdinalIgnoreCase))
        return;

      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new FileSystemListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.UnsafeCalls)
      {
        report.AddError(Code,
          "Файловая операция в серверном событии. Используйте специализированные API.",
          document, call.identifier().GetTextPosition());
      }
    }
  }
}
