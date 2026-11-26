using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.SqlClient;
using IsblCheck.Core.Context.Development;

namespace IsblCheck.Context.Development.Database.Handlers
{
  /// <summary>Вычисления записей справочника ролей типовых маршрутов.</summary>
  internal sealed class WorkflowRoleDatabaseHandler : IDatabaseHandler<WorkflowRole>
  {
    public IEnumerable<WorkflowRole> Read(SqlConnection connection, Version platformVersion)
    {
      if (platformVersion < new Version(7, 7))
        throw new NotSupportedException($"Загрузка ролей маршрутов для версии платформы {platformVersion} не подтверждена.");
      var roles = new List<WorkflowRole>();
      using var command = new SqlCommand(@"SELECT a.Kod, a.NameAn, a.Sost, t.Event
        FROM dbo.MBAnalit a JOIN dbo.MBVidAn v ON v.Vid=a.Vid
        LEFT JOIN dbo.MBText t ON t.SrcRecID=a.XRecID WHERE v.Kod=N'РОЛ'", connection);
      using var reader = command.ExecuteReader();
      while (reader.Read())
      {
        var state = (reader["Sost"] as string)?.Trim();
        var text = reader["Event"] is byte[] bytes ? Encoding.GetEncoding(1251).GetString(bytes) : string.Empty;
        roles.Add(new WorkflowRole { Name = (reader["Kod"] as string)?.Trim(), Title = (reader["NameAn"] as string)?.Trim(),
          State = state == "Д" ? ComponentState.Active : ComponentState.Closed, CalculationText = text });
      }
      return roles;
    }
  }
}
