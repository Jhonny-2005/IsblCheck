namespace IsblCheck.Views.Dialogs
{
  /// <summary>
  /// Представление окно запроса данных подключения к базе.
  /// </summary>
  public partial class DatabaseCredentialWindow
  {
    /// <summary>
    /// Конструктор.
    /// </summary>
    public DatabaseCredentialWindow()
    {
      this.SourceInitialized += DarkTitleBar.Apply;
      this.SourceInitialized += WindowSizing.FitDialog;
      this.ContentRendered += WindowSizing.KeepInitialPositionVisible;
      this.InitializeComponent();
    }
  }
}
