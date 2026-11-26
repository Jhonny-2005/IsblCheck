using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using IsblCheck.Core.Context.Development;

namespace IsblCheck.Context.Development.Database.Handlers
{
  internal class LocalizationStringDatabaseHandler : IDatabaseHandler<LocalizationString>
  {
    public IEnumerable<LocalizationString> Read(SqlConnection connection, Version platformVersion)
    {
      var query = GetLocalizationStringQuery(platformVersion);
      var entries = new LocalizationAccumulator();
      if (query == null) return entries.Entries.ToList();
      using var command = new SqlCommand(query, connection);
      using var reader = command.ExecuteReader();
      while (reader.Read())
        entries.Add(reader["GroupCode"] as string, reader["Code"] as string,
          reader["LangCode"] as string, reader["String"] as string);
      return entries.Entries.ToList();
    }

    private const string LocalizationQuery = @"
      SELECT [GroupCode], [Code], [LangCode], [String]
      FROM [dbo].[SBLocalizedData]
      ORDER BY [GroupCode], [Code], [LangCode]";

    public string GetLocalizationStringQuery(Version platformVersion)
      => platformVersion >= new Version(7, 7) ? LocalizationQuery : null;
  }
}
