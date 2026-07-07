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
  /// Правило обнаружения записи в реестр в серверных событиях.
  /// </summary>
  internal class RegWriteRule : AbstractRule
  {
    private const string Code = "S005";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(RegWriteRule).Name,
        "Правило обнаружения записи в реестр в серверных событиях."), true);

    public static IRuleInfo Info => info.Value;

    private const string DocumentPathPrefix = "Events.";

    private static readonly HashSet<string> RegWriteMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "RegWriteValue", "ЗаписатьЗначениеВРеестр",
      "RegDeleteKey", "УдалитьКлючРеестра",
      "RegWriteString", "ЗаписатьСтрокуВРеестр"
    };

    private class RegWriteListener : IsblBaseListener
    {
      public List<IsblParser.FunctionContext> RegWriteCalls { get; } =
        new List<IsblParser.FunctionContext>();

      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var name = context.identifier().GetText();
        if (RegWriteMethods.Contains(name))
        {
          RegWriteCalls.Add(context);
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      if (!document.Path.StartsWith(DocumentPathPrefix, StringComparison.OrdinalIgnoreCase))
        return;

      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new RegWriteListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.RegWriteCalls)
      {
        report.AddError(Code,
          "Запись в реестр в серверном событии. Реестр — глобальный ресурс, изменения могут повлиять на другие сессии.",
          document, call.GetTextPosition());
      }
    }
  }
}
