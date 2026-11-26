using System;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using IsblCheck.Core.Reports;
using IsblCheck.Reports.Properties;

namespace IsblCheck.Reports.Printers
{
  public class ExcelReportPrinter : IReportPrinter
  {
    public string FilePath { get; private set; }

    public string FontFamily { get; set; }

    public int FontSize { get; set; }

    public void Print(IReport report)
    {
      using (SpreadsheetDocument document = SpreadsheetDocument.Create(this.FilePath, SpreadsheetDocumentType.Workbook))
      {
        var workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();

        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        worksheetPart.Worksheet = new Worksheet();

        var sheets = workbookPart.Workbook.AppendChild(new Sheets());

        var sheet = new Sheet()
        {
          Id = workbookPart.GetIdOfPart(worksheetPart),
          SheetId = 1,
          Name = "Отчёт"
        };
        sheets.Append(sheet);

        var sheetData = worksheetPart.Worksheet.AppendChild(new SheetData());

        var row = new Row();
        row.Append(
          ConstructCell(Resources.Code, CellValues.String),
          ConstructCell(Resources.Type, CellValues.String),
          ConstructCell(Resources.Message, CellValues.String),
          ConstructCell(Resources.Element, CellValues.String),
          ConstructCell(Resources.Line, CellValues.String),
          ConstructCell(Resources.Column, CellValues.String));
        sheetData.AppendChild(row);

        foreach (var message in report.Messages.ByImportance())
        {
          row = new Row();
          row.Append(
            ConstructCell(message.Code, CellValues.String),
            ConstructCell(message.Severity.InRussian(), CellValues.String),
            ConstructCell(message.Description, CellValues.String),
            ConstructCell(message.Document?.Name ?? "", CellValues.String),
            ConstructCell(message.Position.Line.ToString(), CellValues.Number),
            ConstructCell(message.Position.Column.ToString(), CellValues.Number));
          sheetData.AppendChild(row);
        }

        workbookPart.Workbook.Save();
      }
    }

    private Cell ConstructCell(string value, CellValues dataType)
    {
      return new Cell()
      {
        CellValue = new CellValue(value),
        DataType = new EnumValue<CellValues>(dataType)
      };
    }

    public ExcelReportPrinter(string filePath)
    {
      this.FilePath = filePath;
    }
  }
}
