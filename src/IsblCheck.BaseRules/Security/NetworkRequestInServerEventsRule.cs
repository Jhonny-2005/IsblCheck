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
  /// Правило обнаружения сетевых запросов в серверных событиях.
  /// </summary>
  internal class NetworkRequestInServerEventsRule : AbstractRule
  {
    private const string Code = "S008";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(NetworkRequestInServerEventsRule).Name,
        "Правило обнаружения сетевых запросов в серверных событиях."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> NetworkMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "HttpOpen", "HttpSendRequest", "HttpClose",
      "ОткрытьHTTP", "ОтправитьЗапросHTTP", "ЗакрытьHTTP"
    };

    private class NetworkListener : IsblBaseListener
    {
      public List<IsblParser.FunctionContext> UnsafeCalls { get; } = new List<IsblParser.FunctionContext>();

      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var name = context.identifier().GetText();
        if (NetworkMethods.Contains(name))
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
      var listener = new NetworkListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.UnsafeCalls)
      {
        report.AddWarning(Code,
          "Сетевой запрос в серверном событии. Неконтролируемые запросы могут использоваться для эксфильтрации данных.",
          document, call.identifier().GetTextPosition());
      }
    }
  }
}
