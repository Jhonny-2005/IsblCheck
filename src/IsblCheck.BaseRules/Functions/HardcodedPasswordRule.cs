using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using IsblCheck.BaseRules.Properties;
using IsblCheck.Core.Checker;
using IsblCheck.Core.Context;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using IsblCheck.Core.Rules;

namespace IsblCheck.BaseRules.Functions
{
  public class HardcodedPasswordRule : AbstractRule
  {
    private const string Code = "F023";

    private static readonly Lazy<IRuleInfo> info = new Lazy<IRuleInfo>(() =>
      new RuleInfo(typeof(HardcodedPasswordRule).Name, "Обнаружен захардкоженный пароль."), true);

    public static IRuleInfo Info => info.Value;

    private static readonly HashSet<string> FunctionsToCheck = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "CreateConnection", "СоздатьСоединение"
    };

    private class HardcodedPasswordListener : IsblBaseListener
    {
      public List<IsblParser.FunctionContext> HardcodedPasswords { get; } = new List<IsblParser.FunctionContext>();
      public IContext Context;

      public override void EnterFunction([NotNull] IsblParser.FunctionContext context)
      {
        var functionName = context.identifier().GetText();
        if (!FunctionsToCheck.Contains(functionName) ||
            Context.HasUserFunction(functionName))
          return;

        var paramList = context.parameterList();
        if (paramList == null)
          return;

        var expressions = ParameterListParser.GetExpressions(paramList);
        if (expressions.Length < 2)
          return;

        // CreateConnection(SystemCode, ConnectParams, FormCaption)
        // ConnectParams is the second parameter (index 1), contains connection string
        var connectParams = expressions[1];
        if (connectParams?.operand()?.@string() != null &&
            System.Text.RegularExpressions.Regex.IsMatch(connectParams.GetText(),
              @"\b(?:Password|Pwd)\s*=\s*[^;\s'"" ]", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
          HardcodedPasswords.Add(context);
        }
      }
    }

    public override void Apply(IReport report, IDocument document, IContext context)
    {
      var tree = document.GetSyntaxTree();
      var walker = new ParseTreeWalker();
      var listener = new HardcodedPasswordListener { Context = context };
      walker.Walk(listener, tree);

      foreach (var funcCall in listener.HardcodedPasswords)
      {
        report.Add(Severity.Warning, Code, "В литерале параметров подключения найдено поле Password или Pwd со значением.",
          document, funcCall.identifier().GetTextPosition(), new DiagnosticRecommendation(
            "Если это действующий пароль, замените его и перенесите сведения для подключения в защищённую конфигурацию, доступную процессу. Используйте способ передачи учётных данных, предусмотренный целевой интеграцией.",
            "Проверьте подключение под рабочей учётной записью и убедитесь, что пароль отсутствует в исходнике, отчётах и журналах."));
      }
    }
  }
}
