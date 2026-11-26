using Microsoft.Data.SqlClient;
namespace IsblCheck.Context.Development.Database
{
  /// <summary>Supports SQL credentials and Windows authentication without changing connection options.</summary>
  public static class SqlConnectionFactory
  {
    public static SqlConnection Create(string connectionString, SqlCredential credential)
      => credential == null ? new SqlConnection(connectionString) : new SqlConnection(connectionString, credential);
  }
}
