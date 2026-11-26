using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Context.Development;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;
using static IsblCheck.Core.Checker.WorkflowParameterAccess;

namespace IsblCheck.Core.Checker
{
  internal sealed class WorkflowRouteAnalysis
  {
    private readonly WorkflowDescription description;
    private readonly WorkflowSchemeDocument scheme;
    private readonly IReport report;
    private readonly HashSet<string> selected;
    private readonly Calculation[] calculations;
    private readonly Dictionary<string, WorkflowBlock> blocks;

    internal static void Check(StandardRoute route, IEnumerable<Document> documents, IReport report)
    {
      if (route.WorkflowDescription.HasParameterCatalog || route.WorkflowDescription.HasRouteSection)
        new WorkflowRouteAnalysis(route, documents, report).Check();
    }

    private WorkflowRouteAnalysis(StandardRoute route, IEnumerable<Document> documents, IReport report)
    {
      this.report = report;
      description = route.WorkflowDescription;
      var supplied = documents.ToLookup(d => d.Path, StringComparer.Ordinal);
      scheme = supplied["Scheme"].OfType<WorkflowSchemeDocument>().FirstOrDefault();
      selected = supplied.Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
      calculations = WorkflowDocumentProvider.GetDocuments(route).Cast<Document>()
        .Select(doc => new Calculation(supplied[doc.Path].FirstOrDefault() ?? doc)).ToArray();
      blocks = description.Blocks.ToDictionary(b => b.Id, StringComparer.Ordinal);
    }

    private void Check()
    {
      var parameters = description.Parameters.GroupBy(p => p.Name, Names).ToDictionary(g => g.Key, g => g.ToArray(), Names);
      if (description.HasParameterCatalog)
        CheckCatalog(parameters);
      if (scheme == null || !description.HasRouteSection)
        return;
      var graph = CheckGraph();
      if (graph != null && description.HasParameterCatalog)
        CheckValues(parameters, graph);
    }

    private void CheckCatalog(Dictionary<string, WorkflowParameter[]> parameters)
    {
      if (scheme != null)
      {
        foreach (var (name, declarations) in parameters)
        {
          if (string.IsNullOrWhiteSpace(name) || declarations.Length > 1)
          {
            ReportScheme(Severity.Error, "R004", string.IsNullOrWhiteSpace(name)
                ? "У объявления параметра отсутствует имя." : $"Имя параметра «{name}» объявлено несколько раз.",
              declarations.Last(), "Задайте каждому параметру уникальное имя и обновите его привязки в схеме и ISBL.",
              "Откройте список параметров маршрута и повторите проверку после сохранения.");
          }
        }
        foreach (var binding in description.ParameterBindings)
          if (string.IsNullOrWhiteSpace(binding.Name) || !parameters.ContainsKey(binding.Name))
            ReportScheme(Severity.Error, "R002", $"{binding.Location}. Параметр «{binding.Name}» отсутствует в списке параметров маршрута.",
              binding, "Исправьте имя в привязке или объявите параметр с нужным типом. Для необязательного свойства удалите ошибочную привязку.",
              "Проверьте заполнение свойства или запрос значения на соответствующем шаге.");
      }
      if (!calculations.Any(c => c.Access.MayChangeCatalog))
        foreach (var calculation in calculations.Where(c => selected.Contains(c.Document.Path)))
          foreach (var access in calculation.Access.Accesses.Where(a => a.RequiresDeclaration && !parameters.ContainsKey(a.Name)))
            report.Add(Severity.Warning, "R002",
              $"«{access.Name}» используется в WorkflowParams.ValueByName, но отсутствует в списке параметров маршрута.",
              calculation.Document, access.Position, new DiagnosticRecommendation(
                "Сверьте имя с объявлением в настройках маршрута. Исправьте опечатку или объявите параметр нужного типа и определите источник его значения.",
                "Повторите проверку и пройдите ветку, на которой вычисление обращается к параметру."));

      if (scheme == null || description.HasUnparsedBindings || calculations.Any(c => c.Undecodable || c.Access.HasOpaqueAccess))
        return;
      var knownUses = description.ParameterBindings.Select(b => b.Name)
        .Concat(calculations.SelectMany(c => c.Access.Literals))
        .Concat(calculations.SelectMany(c => c.Access.Accesses.Select(a => a.Name))).ToHashSet(Names);
      foreach (var (name, declarations) in parameters)
        if (declarations.Length == 1 && !string.IsNullOrWhiteSpace(name) && !knownUses.Contains(name))
          ReportScheme(Severity.Information, "R003", $"Для параметра «{name}» не найдено использования в загруженной схеме и вычислениях маршрута.",
            declarations[0], "Проверьте вычисляемые роли, внешние сценарии и параметры запуска. Если обращений действительно нет, удалите объявление из настроек маршрута.",
            "Проверьте обычный запуск, запуск из других маршрутов и все ветки, где этот параметр мог использоваться.");
    }

    private Graph CheckGraph()
    {
      var valid = true;
      foreach (var id in description.RouteBlockIds.GroupBy(id => id, StringComparer.Ordinal))
        if (!blocks.ContainsKey(id.Key) || id.Count() > 1)
        {
          ReportScheme(Severity.Error, "R001", !blocks.ContainsKey(id.Key)
            ? $"Запись переходов с ID «{id.Key}» ссылается на отсутствующий блок."
            : $"Для блока «{id.Key}» есть несколько записей переходов.", null,
            "Сверьте запись Link со списком блоков в редакторе схемы.",
            "Сохраните схему и проверьте переходы для всех результатов блока.");
          valid = false;
        }
      var edges = description.Edges.GroupBy(e => (e.From, e.To, e.ResultCode)).ToArray();
      foreach (var edge in edges)
      {
        var (from, to, _) = edge.Key;
        if (!blocks.ContainsKey(from) || !blocks.ContainsKey(to))
        {
          ReportScheme(Severity.Error, "R001", $"Переход «{from}» → «{to}» ссылается на отсутствующий блок.",
            edge.First(), "В редакторе схемы удалите неактуальный переход или свяжите его с существующим блоком.",
            "Повторно сохраните и проверьте схему. Пройдите ветку соответствующего результата.");
          valid = false;
        }
      }
      var starts = blocks.Values.Where(b => b.BaseBlockType == RouteBlockType.Start).ToArray();
      var hasBothDirections = edges.All(e => e.Any(item => item.IsInput) && e.Any(item => !item.IsInput));
      if (!valid || starts.Length != 1 || !hasBothDirections ||
          !blocks.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(description.RouteBlockIds))
        return null;

      var graph = new Graph(starts[0].Id, edges.Select(g => g.First()).ToLookup(e => e.From, StringComparer.Ordinal));
      var reachable = graph.PathsFrom(graph.Start, _ => true).Keys.ToHashSet(StringComparer.Ordinal);
      foreach (var block in blocks.Values.Where(b => !reachable.Contains(b.Id)))
        ReportScheme(Severity.Warning, "R006", $"Блок {BlockLabel(block.Id)} недостижим из начала по переходам схемы.",
          block, "Проверьте, должен ли блок запускаться из этой схемы. Восстановите входящий переход или уберите устаревший блок.",
          "Проверьте прохождение маршрута до этого блока и его следующего перехода.");
      return graph;
    }

    private void CheckValues(Dictionary<string, WorkflowParameter[]> parameters, Graph graph)
    {
      if (description.HasUnparsedBindings || parameters.Any(p => p.Value.Length != 1))
        return;
      var byBlock = calculations.Where(c => c.BlockId != null).ToLookup(c => c.BlockId, StringComparer.Ordinal);
      foreach (var name in calculations.SelectMany(c => c.Access.Accesses)
        .Where(a => a.Operation == Kind.Dereference).Select(a => a.Name).Distinct(Names))
      {
        if (!parameters.TryGetValue(name, out var declarations) ||
            description.ParameterBindings.Any(b => b.BlockId == null && (b.Requested || b.IsOutput) && Names.Equals(b.Name, name)) ||
            calculations.Any(c => c.BlockId == null && c.MayWrite(name)))
          continue;

        var facts = blocks.Values.ToDictionary(b => b.Id, b => ReadBlock(b, byBlock[b.Id], name), StringComparer.Ordinal);
        if (!facts.Values.Any(f => f.Read != null))
          continue;
        CheckPaths(graph, name, declarations[0].HasDefault, facts);
      }
    }

    private BlockFacts ReadBlock(WorkflowBlock block, IEnumerable<Calculation> items, string name)
    {
      var values = items.OrderBy(c => c.Phase).ToArray();
      if (block.BaseBlockType == RouteBlockType.Unknown ||
          (block.InputsJoinType != null && block.InputsJoinType != "0") ||
          description.ParameterBindings.Any(b => b.BlockId == block.Id && (b.Requested || b.IsOutput) && Names.Equals(b.Name, name)) ||
          values.Any(c => c.Undecodable || c.Access.HasOpaqueAccess || c.Access.HasEarlyTermination ||
            (c.Phase == Unordered && c.MayWrite(name))))
        return new BlockFacts(null, Effect.Unknown, Effect.Unknown);

      RoutedRead read = null;
      var before = Effect.Preserve;
      var effect = Effect.Preserve;
      foreach (var calculation in values.Where(c => c.Phase != Unordered))
        foreach (var access in calculation.Access.Accesses.Where(a => Names.Equals(a.Name, name))
          .OrderBy(a => a.Node is IsblParser.AssignStatementContext assignment ? assignment.Stop.StopIndex : a.Position.StartIndex))
        {
          if (access.Operation == Kind.Dereference && read == null && IsStraightLine(access.Node))
          {
            read = new RoutedRead(calculation.Document, access);
            before = effect;
          }
          if (access.Operation is Kind.Reference or Kind.Write or Kind.Clear)
            effect = !IsStraightLine(access.Node) || access.Operation == Kind.Reference
              ? Effect.Unknown : access.Operation == Kind.Write ? Effect.Set : Effect.Clear;
        }
      return new BlockFacts(read, before, effect);
    }

    private void CheckPaths(Graph graph, string name, bool hasDefault, Dictionary<string, BlockFacts> facts)
    {
      var entry = new State(graph.Start, hasDefault);
      var previous = new Dictionary<State, (State, WorkflowEdge)> { [entry] = default };
      var queue = new Queue<State>();
      queue.Enqueue(entry);
      var firstReadWithValue = new HashSet<string>(StringComparer.Ordinal);
      var withoutValue = new HashSet<string>(StringComparer.Ordinal);
      while (queue.TryDequeue(out var current))
      {
        var block = facts[current.Id];
        if (block.Read is { } read && selected.Contains(read.Document.Path))
        {
          var during = Apply(block.BeforeRead, current.HasValue);
          if (during == false && withoutValue.Add(current.Id))
          {
            var path = new List<WorkflowEdge>();
            for (var at = current; previous[at].Item2 != null; at = previous[at].Item1)
              path.Add(previous[at].Item2);
            path.Reverse();
            AddValueFinding("R005", read, $"На пути {PathLabel(graph.Start, path)} код использует объект из параметра «{name}». До обращения не видно исходного значения или записи на этом пути.",
              "Если параметр обязателен для этой ветки, заполните его до входа в блок. Если пустое значение допустимо, проверьте его перед обращением к свойствам объекта и обработайте этот вариант.",
              "Запустите маршрут по указанной ветке с пустым параметром и проверьте переход и результат вычисления.");
          }
          else if (during == true && block.BeforeRead == Effect.Preserve)
            firstReadWithValue.Add(current.Id);
        }

        if (Apply(block.AfterBlock, current.HasValue) is not bool outgoingValue)
          continue;
        foreach (var edge in graph.Outgoing[current.Id])
        {
          var next = new State(edge.To, outgoingValue);
          if (previous.TryAdd(next, (current, edge)))
            queue.Enqueue(next);
        }
      }

      // A value explicitly configured at selection can legitimately remain constant.
      if (hasDefault || !facts.Values.Any(f => f.AfterBlock == Effect.Set))
        return;
      foreach (var id in firstReadWithValue.Where(id => facts[id].AfterBlock == Effect.Preserve && !withoutValue.Contains(id)))
      {
        foreach (var edge in graph.Outgoing[id])
        {
          var paths = graph.PathsFrom(edge.To, node => facts[node].AfterBlock == Effect.Preserve);
          if (!paths.ContainsKey(id))
            continue;
          var path = new List<WorkflowEdge>();
          for (var at = id; paths[at] is { } item; at = item.From)
            path.Add(item);
          path.Reverse();
          path.Insert(0, edge);
          AddValueFinding("R007", facts[id].Read,
            $"Путь возврата {PathLabel(id, path)} повторно читает объект из параметра «{name}». На этом пути не видно обновления или очистки параметра.",
            "Уточните ожидаемое значение при возврате. Если значение нужно пересчитывать, очистите его на ветке возврата и заполните перед повторным использованием.",
            "Пройдите ветку дважды с разными исходными данными и сравните значение параметра при повторном входе.");
          break;
        }
      }
    }

    private static bool? Apply(Effect effect, bool previous) => effect switch
    {
      Effect.Set => true,
      Effect.Clear => false,
      Effect.Preserve => previous,
      _ => null
    };

    private string BlockLabel(string id) => string.IsNullOrWhiteSpace(blocks[id].Name) ? id : $"{id} «{blocks[id].Name}»";

    private string PathLabel(string start, List<WorkflowEdge> path)
    {
      var steps = path.Take(10).Select(edge =>
        (edge.ResultCode.Length == 0 ? " → " : $" → [{blocks[edge.From].ExecutionResults.GetValueOrDefault(edge.ResultCode, edge.ResultCode)}] → ") +
        BlockLabel(edge.To));
      return BlockLabel(start) + string.Concat(steps) + (path.Count > 10 ? $" → … → {BlockLabel(path[^1].To)}" : "");
    }

    private void AddValueFinding(string code, RoutedRead read, string message, string action, string verification) =>
      report.Add(Severity.Information, code, message, read.Document, read.Access.Position,
        new DiagnosticRecommendation(action, verification));

    private void ReportScheme(Severity severity, string code, string message, object fact, string action, string verification) =>
      report.Add(severity, code, message, scheme, scheme.GetPosition(fact), new DiagnosticRecommendation(action, verification));

    private sealed record Graph(string Start, ILookup<string, WorkflowEdge> Outgoing)
    {
      public Dictionary<string, WorkflowEdge> PathsFrom(string start, Func<string, bool> canLeave)
      {
        var paths = new Dictionary<string, WorkflowEdge>(StringComparer.Ordinal) { [start] = null };
        var queue = new Queue<string>();
        queue.Enqueue(start);
        while (queue.TryDequeue(out var block))
          if (canLeave(block))
            foreach (var edge in Outgoing[block])
              if (paths.TryAdd(edge.To, edge))
                queue.Enqueue(edge.To);
        return paths;
      }
    }

    private const int Unordered = 3;
    private enum Effect { Preserve, Set, Clear, Unknown }
    private sealed record State(string Id, bool HasValue);
    private sealed record RoutedRead(Document Document, Access Access);
    private sealed record BlockFacts(RoutedRead Read, Effect BeforeRead, Effect AfterBlock);

    private sealed class Calculation
    {
      public Document Document { get; }
      public WorkflowParameterAccess Access { get; } = new();
      public string BlockId { get; }
      public int Phase { get; } = Unordered;
      public bool Undecodable { get; }

      public Calculation(Document document)
      {
        Document = document;
        var tree = document.GetSyntaxTree();
        Undecodable = document.ParseDiagnostics.Any();
        if (!Undecodable)
          ParseTreeWalker.Default.Walk(Access, tree);

        if (!document.Path.StartsWith("Blocks.", StringComparison.Ordinal))
          return;
        var parts = document.Path.Split('.', 3);
        BlockId = parts[1];
        Phase = parts[2].ToUpperInvariant() switch
        {
          "EVENTS.BEFORESTART" => 0,
          "PROPERTIES.SCRIPT" when document.WorkflowRoute.WorkflowDescription.Blocks
            .Any(b => b.Id == BlockId && b.BaseBlockType == RouteBlockType.Script) => 1,
          "EVENTS.AFTERFINISH" => 2,
          _ => Unordered
        };
      }

      public bool MayWrite(string parameter) => Undecodable || Access.HasOpaqueAccess ||
        Access.Accesses.Any(a => Names.Equals(a.Name, parameter) && a.Operation is Kind.Reference or Kind.Write or Kind.Clear);
    }
  }
}
