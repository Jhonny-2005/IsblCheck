using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using IsblCheck.Core.Context.Development;
namespace IsblCheck.Context.Development.Database.Handlers
{
  internal class DevelopmentModuleDatabaseHandler : IDatabaseHandler<DevelopmentModule>
  {
    public IEnumerable<DevelopmentModule> Read(SqlConnection connection, Version platformVersion)
    {
      using var command = new SqlCommand("SELECT [Name], [Kod] FROM dbo.MBRegUnit ORDER BY [Name]", connection);
      using var reader = command.ExecuteReader();
      while (reader.Read()) yield return new DevelopmentModule { Name = reader["Name"] as string, Title = reader["Kod"] as string };
    }
  }
}
