using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Security
{
  internal class HardcodedCredentialRule : AbstractRule
  {
    private const string Code = "S002";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(HardcodedCredentialRule).Name, "Правило поиска захардкоженных учётных данных и паролей в коде."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> CredentialFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "CreateConnection", "СоздатьСоединение", "RegWrite", "РеестрЗапись"
    };

    private static readonly HashSet<string> PasswordPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "password", "пароль", "passwd", "pwd", "secret", "credential", "token", "apikey", "api_key"
    };

    private class CredentialListener : IsblBaseListener
    {
      public List<IsblParser.FunctionContext> SuspiciousCalls { get; } = new List<IsblParser.FunctionContext>();

      public override void EnterFunction(IsblParser.FunctionContext context)
      {
        var funcName = context.identifier().GetText();
        if (!CredentialFunctions.Contains(funcName))
          return;

        var paramList = context.parameterList();
        if (paramList == null)
          return;

        foreach (var expr in paramList.expression())
        {
          var operand = expr?.operand();
          if (operand != null && operand.@string() != null)
          {
            var strValue = operand.@string().GetText().Trim('"', '\'');
            foreach (var pattern in PasswordPatterns)
            {
              if (strValue.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
              {
                SuspiciousCalls.Add(context);
                return;
              }
            }
          }
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new CredentialListener();
      walker.Walk(listener, tree);

      foreach (var call in listener.SuspiciousCalls)
      {
        var description = "Возможное наличие захардкоженных учётных данных. Рекомендуется хранить секреты в защищённых хранилищах.";
        report.AddWarning(Code, description, document, call.identifier().GetTextPosition());
      }
    }
  }
}
