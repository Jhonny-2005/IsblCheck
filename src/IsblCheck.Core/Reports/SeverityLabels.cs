namespace IsblCheck.Core.Reports
{
  public static class SeverityLabels
  {
    public static string InRussian(this Severity severity) => severity switch
    {
      Severity.Error => "Ошибка",
      Severity.Warning => "Предупреждение",
      Severity.Information => "Информация",
      _ => "Неизвестный уровень"
    };
  }
}
