using System.Windows;

namespace IsblCheck.Views.Dialogs
{
  /// <summary>
  /// Представление окно о программе.
  /// </summary>
  public partial class AboutWindow : Window
  {
    /// <summary>
    /// Конструктор.
    /// </summary>
    public AboutWindow()
    {
      this.SourceInitialized += DarkTitleBar.Apply;
      this.SourceInitialized += WindowSizing.FitDialog;
      this.ContentRendered += WindowSizing.KeepInitialPositionVisible;
      this.InitializeComponent();
    }
  }
}
