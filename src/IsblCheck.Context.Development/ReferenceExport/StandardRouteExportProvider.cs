using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using IsblCheck.Context.Development.Utils;
using IsblCheck.Core.Context.Development;

namespace IsblCheck.Context.Development.ReferenceExport
{
  /// <summary>Classic Directum reference-record export: ROOT/RecordRef plus sibling Base64 IMG fields.</summary>
  public sealed class StandardRouteExportProvider : IDevelopmentContextProvider, IMetadataAvailability
  {
    public bool CanValidateReferences => false;
    public bool CanValidateLocalization => false;
    private readonly string source;
    private readonly bool requireRoutes;
    private readonly object sync = new object();
    private ExportData data;

    public StandardRouteExportProvider(string path) : this(path, true) { }
    internal StandardRouteExportProvider(string path, bool requireRoutes)
    {
      source = Path.GetFullPath(path ?? throw new ArgumentNullException(nameof(path)));
      this.requireRoutes = requireRoutes;
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
      if (!File.Exists(source) && !Directory.Exists(source)) throw new FileNotFoundException("Выгрузка маршрутов не найдена.", source);
    }

    public IEnumerable<T> ReadComponents<T>() where T : Component
    {
      if (typeof(T) != typeof(StandardRoute) && typeof(T) != typeof(WorkflowRole)) return Enumerable.Empty<T>();
      lock (sync)
      {
        data ??= ReadExport();
        return (typeof(T) == typeof(StandardRoute) ? data.Routes.Cast<T>() : data.Roles.Cast<T>()).ToList();
      }
    }
    public void ResetCache() { lock (sync) data = null; }

    private ExportData ReadExport()
    {
      var result = new ExportData();
      var isFile = File.Exists(source);
      var directory = isFile ? Path.GetDirectoryName(source) : source;
      var files = Directory.EnumerateFiles(directory).Where(p =>
        Path.GetExtension(p).Equals(".xml", StringComparison.OrdinalIgnoreCase) ||
        Path.GetExtension(p).Equals(".dat", StringComparison.OrdinalIgnoreCase)).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
      if (isFile) { files.RemoveAll(f => string.Equals(f, source, StringComparison.OrdinalIgnoreCase)); files.Insert(0, source); }
      foreach (var file in files)
      {
        XDocument xml;
        try { xml = ReadXml(File.ReadAllBytes(file)); }
        catch (XmlException ex)
        {
          if ((isFile && file == source) || new[] { "TipMarsh", "RoleTM" }.Contains(Path.GetFileNameWithoutExtension(file), StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException($"Некорректный XML выгрузки: {file}", ex);
          continue; // Other DAT files may belong to unrelated export formats.
        }
        if (xml.Root?.Name.LocalName != "ROOT") continue;
        var recordKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var record in xml.Root.Elements("RecordRef"))
        {
          var reference = (string)record.Attribute("Vid");
          if ((reference == "ТМТ" && (!isFile || file == source)) || reference == "РОЛ")
          {
            var key = reference + ":" + RequiredAttribute(record, "Kod", file);
            if (!recordKeys.Add(key)) throw new InvalidDataException($"Повторная запись {key} в файле {file}.");
          }
          if (reference == "ТМТ" && (!isFile || file == source))
          {
            var code = RequiredAttribute(record, "Kod", file);
            var image = Requisite(record, "ISBSearchCondition");
            if (string.IsNullOrWhiteSpace(image)) throw new InvalidDataException($"Маршрут {code}: отсутствует ISBSearchCondition ({file}).");
            try
            {
              var schema = ReadXml(ReadTextField(directory, image));
              if (schema.Root?.Name.LocalName != "Settings") throw new InvalidDataException("Ожидалась XML-схема Settings.");
              var blockIds = new HashSet<string>(StringComparer.Ordinal);
              foreach (var block in schema.Root.Element("Blocks")?.Elements("Block") ?? Enumerable.Empty<XElement>())
                if (string.IsNullOrWhiteSpace((string)block.Attribute("ID")) || !blockIds.Add((string)block.Attribute("ID")))
                  throw new InvalidDataException("В схеме отсутствует ID блока или повторяется один и тот же ID.");
              result.Routes.Add(new StandardRoute {
                Name = code, Title = (string)record.Attribute("Name") ?? code, State = ReadState(record),
                WorkflowDescription = WorkflowDescriptionParser.Parse(schema.ToString(SaveOptions.DisableFormatting))
              });
            }
            catch (Exception ex) when (ex is XmlException || ex is FormatException || ex is InvalidDataException || ex is IOException)
            { throw new InvalidDataException($"Маршрут {code}, файл {image}: {ex.Message}", ex); }
          }
          else if (reference == "РОЛ")
          {
            var code = RequiredAttribute(record, "Kod", file);
            var image = Requisite(record, "ISBEvent");
            if (string.IsNullOrWhiteSpace(image)) continue;
            var text = DecodeText(ReadTextField(directory, image));
            result.Roles.Add(new WorkflowRole { Name = code, Title = (string)record.Attribute("Name") ?? code,
              State = ReadState(record), CalculationText = text });
          }
        }
      }
      result.Routes = result.Routes.GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList();
      result.Roles = result.Roles.GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList();
      if (requireRoutes && result.Routes.Count == 0)
        throw new InvalidDataException("В выбранной выгрузке нет записей ТМТ. Выберите TipMarsh.xml или папку с XML/DAT и связанными IMG.");
      return result;
    }

    private static string RequiredAttribute(XElement element, string name, string file)
    {
      var value = ((string)element.Attribute(name))?.Trim();
      if (string.IsNullOrEmpty(value)) throw new InvalidDataException($"В записи отсутствует {name}: {file}");
      return value;
    }
    private static string Requisite(XElement record, string name) =>
      (string)record.Elements("Requisite").FirstOrDefault(r => (string)r.Attribute("Name") == name)?.Attribute("Value");
    private static ComponentState ReadState(XElement record)
    {
      return RecordStateParser.Parse(Requisite(record, "Состояние"));
    }
    private static byte[] ReadTextField(string directory, string fileName)
    {
      // A reference export puts text-field files next to its index. Never resolve arbitrary paths from XML.
      if (Path.IsPathRooted(fileName) || fileName != Path.GetFileName(fileName) || fileName.Contains('\\') || fileName.Contains('/') || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        throw new InvalidDataException($"Недопустимое имя связанного файла: {fileName}");
      var path = Path.Combine(directory, fileName);
      if (!File.Exists(path)) throw new FileNotFoundException("Отсутствует связанное текстовое поле выгрузки.", path);
      if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Ссылки на внешние файлы IMG не поддерживаются.");
      var bytes = File.ReadAllBytes(path);
      // IMG in reference-record exports is Base64. Raw XML is also accepted for textual exports.
      using var reader = new StreamReader(new MemoryStream(bytes), Encoding.GetEncoding(1251), true);
      var text = reader.ReadToEnd();
      if (text.TrimStart().StartsWith("<", StringComparison.Ordinal)) return bytes;
      try { return Convert.FromBase64String(text); }
      catch (FormatException ex) { throw new InvalidDataException($"Некорректный Base64 в {path}", ex); }
    }
    private static XDocument ReadXml(byte[] bytes)
    {
      using var stream = new MemoryStream(bytes);
      using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
      return XDocument.Load(reader, LoadOptions.PreserveWhitespace);
    }
    private static string DecodeText(byte[] bytes)
    {
      if (bytes.Length >= 2 && ((bytes[0] == 0xff && bytes[1] == 0xfe) || (bytes[0] == 0xfe && bytes[1] == 0xff)) ||
          bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf)
      { using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, true); return reader.ReadToEnd(); }
      return Encoding.GetEncoding(1251).GetString(bytes);
    }
    private sealed class ExportData
    {
      public List<StandardRoute> Routes = new List<StandardRoute>();
      public List<WorkflowRole> Roles = new List<WorkflowRole>();
    }
  }
}
