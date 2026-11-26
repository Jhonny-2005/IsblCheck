using System.IO;
using Antlr4.Runtime;
using IsblCheck.Core.Reports;

namespace IsblCheck.Core.Checker
{
  internal class ReportErrorListener : IAntlrErrorListener<IToken>
  {
    private readonly IReport report;
    private readonly IDocument document;

    public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
      var start = offendingSymbol.ToTextPosition();
      report.AddError("", msg, document, start);
    }

    internal ReportErrorListener(IReport report, IDocument document)
    {
      this.report = report;
      this.document = document;
    }
  }
}
