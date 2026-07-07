using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Context.Development;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  /// <summary>
  /// Правило обнаружения пропуска обязательных параметров функции.
  /// </summary>
  internal class MandatoryParameterOmissionRule : AbstractRule
  {
    private const string Code = "F046";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(MandatoryParameterOmissionRule).Name,
        "Правило обнаружения пропуска обязательных параметров функции."), true);

    public static IRuleInfo Info => info.Value;

    private class MandatoryParamListener : IsblBaseListener
    {
      private readonly IReadOnlyDictionary<string, Function> allFunctions;

      public List<IsblParser.FunctionContext> MissingParams { get; } = new List<IsblParser.FunctionContext>();

      public MandatoryParamListener(IContext context)
      {
        var devFuncs = context.Development.Functions.ToDictionary(k => k.Name, StringComparer.OrdinalIgnoreCase);
        var appFuncs = context.Application.Functions.ToDictionary(k => k.Name, StringComparer.OrdinalIgnoreCase);
        var merged = new Dictionary<string, Function>(appFuncs, StringComparer.OrdinalIgnoreCase);
        foreach (var kv in devFuncs)
          merged[kv.Key] = kv.Value;
        allFunctions = merged;
      }

      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var funcName = context.identifier().GetText();
        if (!allFunctions.TryGetValue(funcName, out var func))
          return;

        var requiredParams = func.Arguments
          .Where(a => !a.HasDefaultValue)
          .OrderBy(a => a.Number)
          .ToList();

        if (requiredParams.Count == 0)
          return;

        var paramList = context.parameterList();
        int providedCount = paramList?.expression()?.Length ?? 0;

        if (providedCount < requiredParams.Count)
        {
          MissingParams.Add(context);
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new MandatoryParamListener(context);
      walker.Walk(listener, tree);

      foreach (var call in listener.MissingParams)
      {
        report.AddError(Code,
          $"Пропущен обязательный параметр функции '{call.identifier().GetText()}'.",
          document, call.identifier().GetTextPosition());
      }
    }
  }
}
