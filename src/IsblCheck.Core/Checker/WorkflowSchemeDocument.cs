using System;
using System.Collections.Generic;
using System.Text;
using IsblCheck.Core.Context.Development;
using IsblCheck.Core.Reports;

namespace IsblCheck.Core.Checker
{
  internal sealed class WorkflowSchemeDocument : Document
  {
    private readonly Dictionary<object, TextPosition> positions;
    internal static readonly TextPosition Start = new() { Line = 1, Column = 0 };

    internal WorkflowSchemeDocument(StandardRoute route, string title) : this(route, title, Render(route)) { }

    private WorkflowSchemeDocument(StandardRoute route, string title,
      (string Text, Dictionary<object, TextPosition> Positions) rendered) : base(title, rendered.Text)
    {
      WorkflowRoute = route;
      ComponentType = ComponentType.StandardRoute;
      ComponentName = route.Name;
      Path = "Scheme";
      positions = rendered.Positions;
    }

    internal TextPosition GetPosition(object fact) =>
      fact != null && positions.TryGetValue(fact, out var position) ? position : Start;

    private static (string, Dictionary<object, TextPosition>) Render(StandardRoute route)
    {
      var text = new StringBuilder();
      var positions = new Dictionary<object, TextPosition>(ReferenceEqualityComparer.Instance);
      var scheme = route.WorkflowDescription;
      var line = 1;
      Append(null, $"Схема маршрута «{route.Title}»");
      foreach (var parameter in scheme.Parameters)
        Append(parameter, $"Параметр «{parameter.Name}» (тип {parameter.Type})" +
          (parameter.HasDefault ? ", задано исходное значение" : ""));
      foreach (var binding in scheme.ParameterBindings)
        Append(binding, $"{binding.Location}: «{binding.Name}»" + (binding.Required ? " (обязательный)" : ""));
      foreach (var block in scheme.Blocks)
        Append(block, $"Блок {block.Id} «{block.Name}»");
      foreach (var edge in scheme.Edges)
        Append(edge, $"{(edge.IsInput ? "Вход" : "Выход")}: {edge.From} → {edge.To}" +
          (string.IsNullOrEmpty(edge.ResultCode) ? "" : $" (результат «{edge.ResultCode}»)"));
      return (text.ToString(), positions);

      void Append(object fact, string value)
      {
        var start = text.Length;
        text.Append("// ").Append(value.Replace('\r', ' ').Replace('\n', ' '));
        if (fact != null)
          positions[fact] = new TextPosition { Line = line, Column = 3, StartIndex = start + 3, EndIndex = text.Length - 1 };
        text.AppendLine();
        line++;
      }
    }
  }
}
