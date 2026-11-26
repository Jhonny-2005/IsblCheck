using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using IsblCheck.Core.Context.Development;

namespace IsblCheck.Context.Development.Utils
{
  /// <summary>
  /// Парсер описания типового маршрута.
  /// </summary>
  public class WorkflowDescriptionParser
  {
    #region Константы

    /// <summary>
    /// Заголовки событий типового маршрута.
    /// </summary>
    private static readonly Dictionary<string, string> RouteEventTitles = new Dictionary<string, string>
    {
      ["InitScript"] = "Начало выбора",
      ["Script"] = "Завершение выбора",
      ["TaskStart"] = "Возможность старта",
      ["TaskAbortPossibility"] = "Возможность прекращения",
      ["TaskAbort"] = "Прекращение",
      ["OnTaskFormShow"] = "Показ формы-карточки",
      ["OnTaskFormHide"] = "Скрытие формы-карточки"
    };

    /// <summary>
    /// Имена свойств-событий для различных типов блоков.
    /// </summary>
    private static readonly Dictionary<RouteBlockType, string[]> BlockEventNames = new Dictionary<RouteBlockType, string[]>
    {
      [RouteBlockType.Start] = new[]
        {
          "BeforeStart",
          "AfterFinish"
        },
      [RouteBlockType.Finish] = new[]
      {
        "BeforeStart",
        "AfterFinish"
      },
      [RouteBlockType.Notice] = new[]
      {
        "BeforeStart",
        "AfterFinish",
        "OnCreateNotices",
        "OnFormShow",
        "OnFormHide",
        "OnPreview"
      },
      [RouteBlockType.Step] = new[]
      {
        "BeforeStart",
        "AfterFinish",
        "OnCreateJobs",
        "BeforeQueryParams",
        "AfterQueryParams",
        "OnFormShow",
        "OnFormHide",
        "OnPreview"
      },
      [RouteBlockType.Decision] = new[]
      {
        "BeforeStart",
        "AfterFinish"
      },
      [RouteBlockType.Wait] = new[]
      {
        "BeforeStart",
        "AfterFinish"
      },
      [RouteBlockType.Monitor] = new[]
      {
        "BeforeStart",
        "AfterFinish"
      },
      [RouteBlockType.Script] = new[]
      {
        "BeforeStart",
        "AfterFinish"
      },
      [RouteBlockType.Connector] = new[]
      {
        "BeforeStart",
        "AfterFinish"
      },
      [RouteBlockType.SubTask] = new[]
      {
        "BeforeStart",
        "AfterFinish",
        "SubTaskCreate",
        "SubtaskInit",
        "SubtaskStart"
      },
      [RouteBlockType.Pause] = new[]
      {
        "BeforeStart",
        "AfterFinish"
      },
      [RouteBlockType.LifeCycleStage] = new[] { "BeforeStart", "AfterFinish" }
    };

    /// <summary>
    /// Уникальный идентификатор-признак для маркировки текста, закодированного в MIME.
    /// </summary>
    private const string MimeEncodedTextMarker = "{5314B05F-CF9F-4F66-99EC-24992A5FB114}";

    #endregion

    #region Поля и свойства

    /// <summary>
    /// XML описания маршрута.
    /// </summary>
    private readonly XDocument description;

    static WorkflowDescriptionParser() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    #endregion


    #region Методы

    /// <summary>
    /// Распарсить описание типового маршрута.
    /// </summary>
    /// <param name="workflowDescriptionXml">XML описания маршрута.</param>
    /// <returns>Описание типового маршрута.</returns>
    public static WorkflowDescription Parse(string workflowDescriptionXml)
    {
      var workflowDescription = new WorkflowDescription();

      var parser = new WorkflowDescriptionParser(workflowDescriptionXml);
      workflowDescription.Actions.AddRange(parser.GetActions());
      workflowDescription.Blocks.AddRange(parser.GetBlocks());
      workflowDescription.Events.AddRange(parser.GetEvents());
      workflowDescription.IsblProperties.AddRange(GetProperties(parser.description.Root.Element("Properties")));
      parser.ReadScheme(workflowDescription);

      return workflowDescription;
    }

    private void ReadScheme(WorkflowDescription scheme)
    {
      var root = description.Root;
      var catalog = root.Element("Params");
      scheme.HasParameterCatalog = catalog != null && catalog.Elements().All(p => p.Name == "Param");
      foreach (var parameter in catalog?.Elements("Param") ?? Enumerable.Empty<XElement>())
      {
        var values = parameter.Elements("Value");
        var hasDefault = values.Any(value => value.Attributes().Any(a => !string.IsNullOrWhiteSpace(a.Value)) ||
          !string.IsNullOrWhiteSpace(value.Value) || value.HasElements);
        scheme.Parameters.Add(new WorkflowParameter((string)parameter.Attribute("Name") ?? "",
          (string)parameter.Attribute("Type"), hasDefault));
      }

      ReadRequested(root.Element("StartedAskableParams"), "Запрос при выборе маршрута", null);
      ReadProperties(root.Element("Properties"), null);
      foreach (var block in root.Element("Blocks")?.Elements("Block") ?? Enumerable.Empty<XElement>())
      {
        var id = (string)block.Attribute("ID");
        ReadProperties(block.Element("Properties"), id);
        ReadRequested(block.Element("AskableParams"), $"Запрос в блоке {id}", id);
        foreach (var result in block.Element("Results")?.Elements("Result") ?? Enumerable.Empty<XElement>())
        {
          var code = (string)result.Attribute("Code") ?? "";
          ReadRequested(result.Element("AskableParams"), $"Запрос в блоке {id}, результат «{code}»", id, code);
        }
      }

      var route = root.Element("Route");
      scheme.HasRouteSection = route != null;
      foreach (var link in route?.Elements("Link") ?? Enumerable.Empty<XElement>())
      {
        var id = (string)link.Attribute("ID") ?? "";
        scheme.RouteBlockIds.Add(id);
        foreach (var output in link.Element("Outputs")?.Elements("Output") ?? Enumerable.Empty<XElement>())
          scheme.Edges.Add(new WorkflowEdge(id, (string)output.Attribute("ID") ?? "", (string)output.Attribute("ResultCode") ?? "", false));
        foreach (var input in link.Element("Inputs")?.Elements("Input") ?? Enumerable.Empty<XElement>())
          scheme.Edges.Add(new WorkflowEdge((string)input.Attribute("ID") ?? "", id, (string)input.Attribute("ResultCode") ?? "", true));
      }

      void ReadRequested(XElement node, string location, string blockId, string resultCode = null)
      {
        foreach (var item in node?.Elements("AskableParam") ?? Enumerable.Empty<XElement>())
          scheme.ParameterBindings.Add(new WorkflowParameterBinding((string)item.Attribute("Name") ?? "",
            location, blockId, resultCode, Requested: true,
            Required: bool.TryParse((string)item.Attribute("Required"), out var required) && required));
      }

      void ReadProperties(XElement node, string blockId)
      {
        foreach (var property in node?.Elements("Property") ?? Enumerable.Empty<XElement>())
        {
          var title = (string)property.Attribute("Name") ?? "";
          var location = blockId == null ? $"Свойство задачи «{title}»" : $"Блок {blockId}, свойство «{title}»";
          // Output properties can store values in another named route parameter.
          if ((string)property.Attribute("LinkedParam") is { Length: > 0 } linked)
            scheme.ParameterBindings.Add(new WorkflowParameterBinding(linked, location + " (выход)", blockId, IsOutput: true));

          // ValueType=1 binds a property to a parameter by name. ValueType=2 means ISBL.
          if (!string.IsNullOrWhiteSpace(property.Element("ValueParamNames")?.Value) ||
              property.Element("ValueParamNames")?.HasElements == true)
            scheme.HasUnparsedBindings = true;
          if ((string)property.Attribute("ValueType") != "1")
            continue;
          var value = property.Element("Value");
          var name = (string)value?.Attribute("Value") ?? GetTextValue(value?.Element("Value")?.Value);
          if (string.IsNullOrWhiteSpace(name))
            continue;
          scheme.ParameterBindings.Add(new WorkflowParameterBinding(name, location, blockId));
        }
      }
    }

    private IEnumerable<WorkflowBlock> GetBlocks()
    {
      var blocks = this.description.Root?.Element("Blocks")?.Elements("Block");
      if (blocks != null)
      {
        foreach (var block in blocks)
        {
          yield return ParseBlock(block);
        }
      }
    }

    private IEnumerable<Core.Context.Development.Action> GetActions()
    {
      var actions = this.description.Root?.Element("RouteActions")?.Elements("Action");
      if (actions != null)
      {
        foreach (var action in actions)
        {
          yield return ParseAction(action);
        }
      }
    }

    private IEnumerable<WorkflowEvent> GetEvents()
    {
      var events = this.description.Root?.Element("Event")?.Elements();
      if (events != null)
      {
        foreach (var p in events)
        {
          var name = p.Name.LocalName;
          if (!RouteEventTitles.TryGetValue(name, out string title))
            title = name;
          var calculationText = GetTextValue(p.Value) ?? string.Empty;
          yield return new WorkflowEvent
          {
            Name = name,
            Title = title,
            CalculationText = calculationText
          };
        }
      }
    }

    private static Core.Context.Development.Action ParseAction(XElement actionElement)
    {
      var name = (string)actionElement.Attribute("Code");
      if (string.IsNullOrWhiteSpace(name)) throw new FormatException("У действия схемы отсутствует Code.");
      var isblText = actionElement.Element("ISBLText")?.Value;
      var calculation = isblText == null ? string.Empty : DecodeMimeText(isblText);
      return new Core.Context.Development.Action
      {
        Name = name,
        CalculationText = calculation
      };
    }

    private static WorkflowBlock ParseBlock(XElement blockElement)
    {
      var block = new WorkflowBlock { Id = blockElement.Attribute("ID")?.Value ?? string.Empty };

      if (!Enum.TryParse(blockElement.Attribute("SystemType")?.Value, true, out RouteBlockType baseBlockType) || !Enum.IsDefined(baseBlockType))
        baseBlockType = RouteBlockType.Unknown;
      block.BaseBlockType = baseBlockType;
      block.InputsJoinType = (string)blockElement.Attribute("InputsJoinType");
      foreach (var result in blockElement.Element("Results")?.Elements("Result") ?? Enumerable.Empty<XElement>())
        if ((string)result.Attribute("Code") is { } code)
          block.ExecutionResults.TryAdd(code, (string)result.Attribute("Name") ?? code);

      var properties = blockElement.Element("Properties")?.Elements("Property");
      if (properties != null)
      {
        var nameProperty = properties.FirstOrDefault(p => p.Attribute("Name")?.Value == "Name");
        if (nameProperty != null)
          block.Name = GetTextPropertyValue(nameProperty) ?? string.Empty;
        else
          block.Name = string.Empty;

        foreach (var p in properties.Where(p => p.Attribute("ValueType")?.Value == "2"))
        {
          var propName = RequiredName(p, "Name");
          var propTitle = (string)p.Attribute("Description") ?? propName;
          var calculationText = GetTextPropertyValue(p) ?? string.Empty;
          if (IsBlockEvent(propName, baseBlockType))
          {
            block.Events.Add(new WorkflowEvent
            {
              Name = propName,
              Title = propTitle,
              CalculationText = calculationText
            });
          }
          else
          {
            block.IsblProperties.Add(new WorklflowIsblProperty
            {
              Name = propName,
              Title = propTitle,
              CalculationText = calculationText
            });
          }
        }
      }

      var actions = blockElement.Elements().Where(e => e.Name == "Actions" || e.Name == "RouteActions").SelectMany(e => e.Elements("Action"));
      if (actions != null)
      {
        foreach (var action in actions)
        {
          block.Actions.Add(ParseAction(action));
        }
      }

      return block;
    }

    private static bool IsBlockEvent(string propertyName, RouteBlockType blockType)
    {
      if (BlockEventNames.TryGetValue(blockType, out string[] eventNames))
        return eventNames.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
      return false;
    }

    private static IEnumerable<WorklflowIsblProperty> GetProperties(XElement properties) =>
      (properties?.Elements("Property") ?? Enumerable.Empty<XElement>())
        .Where(p => (string)p.Attribute("ValueType") == "2")
        .Select(p => new WorklflowIsblProperty { Name = RequiredName(p, "Name"),
          Title = (string)p.Attribute("Description") ?? RequiredName(p, "Name"), CalculationText = GetTextPropertyValue(p) ?? string.Empty });

    private static string RequiredName(XElement node, string attribute)
    {
      var value = (string)node.Attribute(attribute);
      if (string.IsNullOrWhiteSpace(value)) throw new FormatException($"У узла {node.Name} отсутствует {attribute}.");
      return value;
    }

    private static void ValidateUnique(IEnumerable<XElement> nodes, Func<XElement, string> key, string section)
    {
      var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      foreach (var node in nodes)
        if (!names.Add(key(node))) throw new FormatException($"Повторное имя/код в разделе {section}: {key(node)}.");
    }

    private static string GetTextPropertyValue(XElement p)
    {
      return GetTextValue(p.Element("Value")?.Element("Value")?.Value);
    }

    private static string GetTextValue(string value)
    {
      return value != null && value.StartsWith(MimeEncodedTextMarker) ? DecodeMimeText(value.Substring(MimeEncodedTextMarker.Length)) : value;
    }

    private static string DecodeMimeText(string encodedValue)
    {
      return Encoding.GetEncoding(1251).GetString(Convert.FromBase64String(encodedValue));
    }

    #endregion

    #region Конструкторы

    /// <summary>
    /// Приватный конструктор.
    /// </summary>
    /// <param name="workflowDescriptionXml">XML описания маршрута.</param>
    private WorkflowDescriptionParser(string workflowDescriptionXml)
    {
      using var text = new StringReader(workflowDescriptionXml);
      using var reader = XmlReader.Create(text, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
      this.description = XDocument.Load(reader);
      var root = this.description.Root;
      if (root?.Name != "Settings") throw new FormatException("Ожидалась XML-схема Settings.");
      ValidateUnique(root.Element("Event")?.Elements() ?? Enumerable.Empty<XElement>(), n => n.Name.LocalName, "Event");
      ValidateUnique(root.Element("Blocks")?.Elements("Block") ?? Enumerable.Empty<XElement>(), n => RequiredName(n, "ID"), "Blocks");
      foreach (var owner in root.DescendantsAndSelf().Where(n => n.Name == "Settings" || n.Name == "Block"))
        ValidateUnique(owner.Elements().Where(n => n.Name == "Actions" || n.Name == "RouteActions").SelectMany(n => n.Elements("Action")), n => RequiredName(n, "Code"), "Actions");
      foreach (var properties in root.Descendants("Properties"))
        ValidateUnique(properties.Elements("Property"), n => RequiredName(n, "Name"), "Properties");
    }

    #endregion
  }
}
