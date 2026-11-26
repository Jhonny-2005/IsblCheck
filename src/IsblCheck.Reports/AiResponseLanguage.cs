using System;
using System.IO;
using System.Text.RegularExpressions;

namespace IsblCheck.Reports
{
  public sealed class AiResponseLanguageException : InvalidOperationException
  {
    internal AiResponseLanguageException() : base("Не удалось получить пояснение ИИ на русском языке.") { }
  }

  internal static class AiResponseLanguage
  {
    private static readonly Regex InlineCodeOrLink = new(@"\x60[^\x60\r\n]+\x60|https?://[^\s)>]+",
      RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private static readonly Regex EnglishPhrase = new(@"\b(?:the|you|should|this|use|replace|can|then)\s+[a-z]+\s+[a-z]+\b",
      RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private static readonly Regex CodeIdentifier = new(
      @"\b(?:[A-Z]{2,}[0-9]*|[A-Za-z][a-z0-9_]*[A-Z][A-Za-z0-9_]*|[A-Za-z][A-Za-z0-9]*_[A-Za-z0-9_]+)\b",
      RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    public static bool IsRussian(string answer, out bool codeOnly)
    {
      codeOnly = false;
      var hasRussian = false;
      var hasCode = false;
      var inFence = false;
      var isCodeFence = false;
      using var reader = new StringReader(answer);
      while (reader.ReadLine() is { } line)
      {
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
          if (inFence && trimmed.TrimEnd(' ', '\t') == "```")
          {
            inFence = false;
            continue;
          }
          if (!inFence)
          {
            inFence = true;
            var language = trimmed[3..].Trim().ToLowerInvariant();
            isCodeFence = language is "isbl" or "sql" or "csharp" or "cs";
          }
          continue;
        }
        if (inFence && isCodeFence)
        {
          hasCode |= !string.IsNullOrWhiteSpace(line);
          continue;
        }

        string prose;
        try
        {
          prose = InlineCodeOrLink.Replace(line, "");
          if (EnglishPhrase.IsMatch(prose)) return false;
          prose = CodeIdentifier.Replace(prose, "");
        }
        catch (RegexMatchTimeoutException) { return false; }
        var letters = 0;
        var russian = 0;
        foreach (var ch in prose)
        {
          if (!char.IsLetter(ch)) continue;
          letters++;
          if (ch is >= 'А' and <= 'я' or 'Ё' or 'ё') russian++;
        }
        // Check prose line by line so a Russian title cannot hide English instructions.
        // Allow ISBL names and short technical terms in normal Russian sentences.
        if (letters > 0 && (russian == 0 || russian * 3 < letters)) return false;
        hasRussian |= russian > 0;
      }
      codeOnly = !hasRussian && hasCode;
      return hasRussian || hasCode;
    }
  }
}
