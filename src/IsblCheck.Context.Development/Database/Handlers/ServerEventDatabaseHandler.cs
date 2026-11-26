using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using IsblCheck.Core.Context.Development;
namespace IsblCheck.Context.Development.Database.Handlers
{
  internal class ServerEventDatabaseHandler : IDatabaseHandler<ServerEvent>
  {
    public IEnumerable<ServerEvent> Read(SqlConnection connection, Version platformVersion)
    {
      using var exists = new SqlCommand("SELECT OBJECT_ID(N'dbo.SBServerEvent', N'U')", connection);
      if (exists.ExecuteScalar() == DBNull.Value) yield break;
      using var command = new SqlCommand("SELECT e.[Name], m.[Name] AS [ModuleName] FROM dbo.SBServerEvent e LEFT JOIN dbo.MBRegUnit m ON m.RegUnit=e.ModuleID ORDER BY e.[Name]", connection);
      using var reader = command.ExecuteReader();
      while (reader.Read()) yield return new ServerEvent { Name = reader["Name"] as string, Title = reader["Name"] as string, ModuleName = reader["ModuleName"] as string };
    }
  }
}
