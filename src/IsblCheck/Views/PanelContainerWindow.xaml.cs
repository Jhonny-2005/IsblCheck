using System.ComponentModel;
using System;
using System.IO;
using System.Windows;
using IsblCheck.Common.Panels;
using Xceed.Wpf.AvalonDock.Layout;
using Xceed.Wpf.AvalonDock.Layout.Serialization;

namespace IsblCheck.Views
{
  /// <summary>
  /// Представление главного окна.
  /// </summary>
  public partial class PanelContainerWindow
  {
    private void AiSettingsClick(object sender, RoutedEventArgs e) => AiSettingsWindow.ShowSettings();

    /// <summary>
    /// Файл конфигурации шаблонов.
    /// </summary>
    private static readonly string LayoutRootConfigurationFile = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IsblCheck", "IsblCheck.PanelContainer.config");

    /// <summary>
    /// Конструктор.
    /// </summary>
    public PanelContainerWindow()
    {
      this.SourceInitialized += DarkTitleBar.Apply;
      this.SourceInitialized += WindowSizing.FitMainWindow;
      this.ContentRendered += WindowSizing.KeepInitialPositionVisible;
      this.InitializeComponent();
      this.Loaded += this.MainWindowLoadedHandler;
      this.Closing += this.MainWindowClosingHandler;
    }

    /// <summary>
    /// Обработчик события загрузки окна.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void MainWindowLoadedHandler(object sender, RoutedEventArgs e)
    {
      var basePaneStyle = this.DockingManager.TryFindResource("AvalonDock_ThemeMetro_DocumentPaneControlStyle") as Style;
      if (basePaneStyle != null)
      {
        var paneStyle = new Style(typeof(Xceed.Wpf.AvalonDock.Controls.LayoutDocumentPaneControl), basePaneStyle);
        paneStyle.Setters.Add(new Setter(System.Windows.Controls.Control.BackgroundProperty, this.FindResource("CanvasBrush")));
        this.DockingManager.DocumentPaneControlStyle = paneStyle;
      }
      if (!File.Exists(LayoutRootConfigurationFile))
        return;
      try
      {
        var layoutSerializer = new XmlLayoutSerializer(this.DockingManager);
        layoutSerializer.LayoutSerializationCallback += LayoutSerializationCallbackHandler;
        layoutSerializer.Deserialize(LayoutRootConfigurationFile);
      }
      catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is System.Xml.XmlException || error is InvalidOperationException)
      {
        NLog.LogManager.GetCurrentClassLogger().Warn("Не удалось восстановить расположение панелей: {0}", error.GetType().Name);
      }
    }

    /// <summary>
    /// Обработчик события закрытия окна.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void MainWindowClosingHandler(object sender, CancelEventArgs e)
    {
      try
      {
        var layoutSerializer = new XmlLayoutSerializer(this.DockingManager);
        Directory.CreateDirectory(Path.GetDirectoryName(LayoutRootConfigurationFile));
        layoutSerializer.Serialize(LayoutRootConfigurationFile);
      }
      catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is InvalidOperationException)
      {
        NLog.LogManager.GetCurrentClassLogger().Warn("Не удалось сохранить расположение панелей: {0}", error.GetType().Name);
      }
    }

    /// <summary>
    /// Обработчик события загрузки 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private static void LayoutSerializationCallbackHandler(object sender, LayoutSerializationCallbackEventArgs e)
    {
      if (string.IsNullOrEmpty(e.Model.ContentId))
      {
        e.Cancel = true;
        return;
      }

      var panel = PanelManager.Instance.GetNamedPanel(e.Model.ContentId);
      if (panel == null)
        panel = PanelManager.Instance.CreateNamedPanel(e.Model.ContentId, e.Model is LayoutAnchorable);

      if (panel == null)
        e.Cancel = true;
      else
        e.Content = panel;
    }
  }
}
