using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using IsblCheck.Core.Parser;
using IsblCheck.Core.Reports;

namespace IsblCheck.Core.Checker
{
  public class Document : IDocument
  {
    private readonly Lazy<IParseTree> syntaxTree;
    private readonly bool allowExpression;
    private readonly ConcurrentQueue<ParseDiagnostic> parseDiagnostics = new ConcurrentQueue<ParseDiagnostic>();

    public string Name { get; }

    public string Text { get; }

    public ComponentType ComponentType { get; set; }

    public string ComponentName { get; set; }

    public string Path { get; set; }

    public List<string> ContextVariables { get; }

    internal Context.Development.StandardRoute WorkflowRoute { get; set; }

    internal IEnumerable<ParseDiagnostic> ParseDiagnostics => this.parseDiagnostics;

    public IParseTree GetSyntaxTree()
    {
      return this.syntaxTree.Value;
    }

    public Document(string name, string text, bool allowExpression = false)
    {
      this.allowExpression = allowExpression;
      this.Name = name;
      this.Text = text ?? string.Empty;
      this.ContextVariables = new List<string>();
      this.syntaxTree = new Lazy<IParseTree>(this.Parse, true);
    }

    private IParseTree Parse()
    {
      // AvalonEdit and .NET string offsets use UTF-16, not Unicode code-point indexes.
      var lexer = new IsblLexer(new AntlrInputStream(this.Text));
      lexer.RemoveErrorListeners();
      lexer.AddErrorListener(new LexerDiagnosticListener(this.Text, this.parseDiagnostics));

      var tokenStream = new CommonTokenStream(lexer);
      // Lex the entire input, including any suffix the parser cannot consume.
      tokenStream.Fill();
      tokenStream.Seek(0);
      var statementDiagnostics = new ConcurrentQueue<ParseDiagnostic>();
      var tree = this.ParseAttempt(tokenStream, false, statementDiagnostics);
      if (this.allowExpression && statementDiagnostics.Count > 0)
      {
        var expressionDiagnostics = new ConcurrentQueue<ParseDiagnostic>();
        var expression = this.ParseAttempt(tokenStream, true, expressionDiagnostics);
        if (expressionDiagnostics.IsEmpty) return expression;
      }
      foreach (var diagnostic in statementDiagnostics) this.parseDiagnostics.Enqueue(diagnostic);
      return tree;
    }

    private IParseTree ParseAttempt(CommonTokenStream tokenStream, bool expression,
      ConcurrentQueue<ParseDiagnostic> diagnostics)
    {
      tokenStream.Seek(0);
      var parser = new IsblParser(tokenStream);
      parser.RemoveErrorListeners();
      parser.AddErrorListener(new ParserDiagnosticListener(this.Text, diagnostics));
      IParseTree tree = expression ? parser.expression() : parser.statementBlock();
      if (parser.CurrentToken.Type != TokenConstants.EOF)
      {
        var token = parser.CurrentToken;
        diagnostics.Enqueue(new ParseDiagnostic(
          $"Неожиданный токен после конца блока: {token.Text}",
          new TextPosition { StartIndex = Math.Max(0, token.StartIndex),
            EndIndex = Math.Max(token.StartIndex, token.StopIndex), Line = token.Line, Column = token.Column }));
      }
      return tree;
    }

    private sealed class LexerDiagnosticListener : IAntlrErrorListener<int>
    {
      private readonly string text;
      private readonly ConcurrentQueue<ParseDiagnostic> diagnostics;

      public void SyntaxError(System.IO.TextWriter output, IRecognizer recognizer, int offendingSymbol,
        int line, int charPositionInLine, string message, RecognitionException exception)
      {
        var index = GetIndex(this.text, line, charPositionInLine);
        this.diagnostics.Enqueue(new ParseDiagnostic("Недопустимый символ или незавершённая строка ISBL.",
          new TextPosition { StartIndex = index, EndIndex = index, Line = line, Column = charPositionInLine }));
      }

      public LexerDiagnosticListener(string text, ConcurrentQueue<ParseDiagnostic> diagnostics)
      {
        this.text = text;
        this.diagnostics = diagnostics;
      }
    }

    private sealed class ParserDiagnosticListener : IAntlrErrorListener<IToken>
    {
      private readonly string text;
      private readonly ConcurrentQueue<ParseDiagnostic> diagnostics;

      public void SyntaxError(System.IO.TextWriter output, IRecognizer recognizer, IToken offendingSymbol,
        int line, int charPositionInLine, string message, RecognitionException exception)
      {
        var startIndex = offendingSymbol?.StartIndex ?? this.text.Length;
        if (startIndex < 0)
          startIndex = this.text.Length;
        var endIndex = offendingSymbol?.StopIndex ?? startIndex;
        endIndex = Math.Max(startIndex, endIndex);
        var location = offendingSymbol == null || offendingSymbol.Type == TokenConstants.EOF
          ? "в конце вычисления" : $"перед «{ShortToken(offendingSymbol.Text)}»";
        var description = message.StartsWith("missing ", StringComparison.Ordinal)
          ? $"Пропущен элемент синтаксиса {location}."
          : message.StartsWith("extraneous input ", StringComparison.Ordinal) || message.StartsWith("mismatched input ", StringComparison.Ordinal)
            ? offendingSymbol?.Type == TokenConstants.EOF ? "Неожиданный конец вычисления." : $"Неожиданный элемент «{ShortToken(offendingSymbol?.Text)}»."
            : $"Не удалось разобрать конструкцию {location}.";
        var expectedIndex = message.IndexOf(" expecting ", StringComparison.Ordinal);
        if (expectedIndex >= 0)
          description += " Ожидается " + message[(expectedIndex + " expecting ".Length)..].Replace("<EOF>", "конец вычисления", StringComparison.Ordinal) + ".";
        else if (message.StartsWith("missing ", StringComparison.Ordinal))
        {
          var at = message.IndexOf(" at ", StringComparison.Ordinal);
          if (at > "missing ".Length) description += " Ожидается " + message["missing ".Length..at] + ".";
        }
        this.diagnostics.Enqueue(new ParseDiagnostic(description,
          new TextPosition { StartIndex = startIndex, EndIndex = endIndex, Line = line, Column = charPositionInLine }));
      }

      private static string ShortToken(string text) =>
        text == null ? "неизвестный токен" : (text.Length > 48 ? text[..48] + "…" : text)
          .Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);

      public ParserDiagnosticListener(string text, ConcurrentQueue<ParseDiagnostic> diagnostics)
      {
        this.text = text;
        this.diagnostics = diagnostics;
      }
    }

    private static int GetIndex(string text, int line, int column)
    {
      var currentLine = 1;
      var index = 0;
      while (index < text.Length && currentLine < line)
      {
        if (text[index++] == '\n')
          currentLine++;
      }
      return Math.Min(index + column, text.Length);
    }
  }

  internal sealed class ParseDiagnostic
  {
    public string Message { get; }
    public TextPosition Position { get; }

    public ParseDiagnostic(string message, TextPosition position)
    {
      this.Message = message;
      this.Position = position;
    }
  }
}
