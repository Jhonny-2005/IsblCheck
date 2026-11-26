using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Reflection;
using NLog;
using IsblCheck.Context.Development.Database.Handlers;
using IsblCheck.Core.Context.Development;

namespace IsblCheck.Context.Development.Database
{
  /// <summary>
  /// Провайдер базы данных.
  /// </summary>
  public class DatabaseProvider : IDevelopmentContextProvider, IMetadataAvailability
  {
    public bool CanValidateReferences => this.platformVersion != null;
    public bool CanValidateLocalization => this.platformVersion != null &&
      new LocalizationStringDatabaseHandler().GetLocalizationStringQuery(this.platformVersion) != null;

    #region IDevelopmentContextProvider

    public IEnumerable<T> ReadComponents<T>() where T : Component
    {
      if(this.platformVersion == null)
        this.platformVersion = this.ReadPlatformVersion();

      var handlerType = ResolveComponentHandler<T>();
      if (handlerType == null)
        return Enumerable.Empty<T>();
      var handler = (IDatabaseHandler<T>)Activator.CreateInstance(handlerType);

      using (var connection = SqlConnectionFactory.Create(this.connectionString, this.credential))
      {
        try
        {
          connection.Open();
          return handler.Read(connection, platformVersion).ToList();
        }
        catch (SqlException ex)
        {
          log.Error("Database connection error: {0}", ex.GetType().Name);
          throw;
        }
      }
    }

    /// <summary>
    /// Сбросить кэш.
    /// </summary>
    public void ResetCache()
    {
      this.platformVersion = null;
    }

    #endregion

    #region Константы

    /// <summary>
    /// Запрос версии платформы.
    /// </summary>
    private const string PlatformVersionQuery = @"
      SELECT
        [ValuePar]
      FROM
        [dbo].[XIni]
      WHERE
        [NamePar] = 'PlatformVersion'";

    #endregion

    #region Поля и свойства

    /// <summary>
    /// Логгер.
    /// </summary>
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Строка подключения.
    /// </summary>
    private readonly string connectionString;

    /// <summary>
    /// Учетные данные.
    /// </summary>
    private readonly SqlCredential credential;

    /// <summary>
    /// Версия платформы.
    /// </summary>
    private Version platformVersion;

    #endregion

    #region Методы

    /// <summary>
    /// Считать версию платформы.
    /// </summary>
    /// <returns>Версия платформы.</returns>
    private Version ReadPlatformVersion()
    {
      using (var connection = SqlConnectionFactory.Create(this.connectionString, this.credential))
      {
        try
        {
          connection.Open();

          var command = new SqlCommand(PlatformVersionQuery, connection);
          var platformVersionValue = command.ExecuteScalar() as string;
          if (!string.IsNullOrEmpty(platformVersionValue))
            return new Version(platformVersionValue);
          else
            throw new Exception("Wrong database version");
        }
        catch (SqlException ex)
        {
          log.Error("Database connection error: {0}", ex.GetType().Name);
          throw;
        }
      }
    }

    private static Type ResolveComponentHandler<T>() where T : Component
    {
      return Assembly.GetExecutingAssembly().DefinedTypes
        .FirstOrDefault(t => t.IsClass && t.ImplementedInterfaces.Contains(typeof(IDatabaseHandler<T>)));
    }

    #endregion

    #region Конструкторы

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="connectionString">Строка подключения.</param>
    public DatabaseProvider(string connectionString)
    {
      System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
      this.connectionString = connectionString;
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="connectionString">Строка подключения.</param>
    /// <param name="credential">Учетные данные аутентификации.</param>
    public DatabaseProvider(string connectionString, SqlCredential credential) : this(connectionString)
    {
      this.credential = credential;
    }

    #endregion
  }
}
