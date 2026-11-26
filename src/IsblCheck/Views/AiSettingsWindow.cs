using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using IsblCheck.Services;

namespace IsblCheck.Views
{
  internal static class AiSettingsWindow
  {
    public static bool ShowSettings()
    {
      var protocol = new ComboBox { Margin = new Thickness(0, 3, 0, 8) };
      protocol.Items.Add("Локальный API · /api/chat");
      protocol.Items.Add("Chat Completions · базовый адрес /v1");
      // Set selection after items are added.
      protocol.SelectedIndex = AiSettings.ChatCompletions ? 1 : 0;
      var endpoint = new TextBox { Text = AiSettings.Endpoint, Margin = new Thickness(0, 3, 0, 8) };
      var model = new TextBox { Text = AiSettings.Model, Margin = new Thickness(0, 3, 0, 8) };
      var key = new PasswordBox { Password = AiSettings.ApiKey, Margin = new Thickness(0, 3, 0, 8), Padding = new Thickness(8) };
      key.SetResourceReference(Control.BackgroundProperty, "SurfaceBrush");
      key.SetResourceReference(Control.ForegroundProperty, "InkBrush");
      key.SetResourceReference(Control.BorderBrushProperty, "LineBrush");
      var limit = new TextBox { Text = AiSettings.MaximumDocuments.ToString(), Margin = new Thickness(0, 3, 0, 8) };
      var timeout = new TextBox { Text = AiSettings.TimeoutSeconds.ToString(), Margin = new Thickness(0, 3, 0, 8) };
      var enabled = new CheckBox
      {
        Content = new TextBlock { Text = "Использовать ИИ при запуске проверки", TextWrapping = TextWrapping.Wrap },
        IsChecked = AiSettings.Enabled,
        Margin = new Thickness(0, 5, 0, 8)
      };
      enabled.SetResourceReference(Control.ForegroundProperty, "InkBrush");
      var panel = new StackPanel { Margin = new Thickness(16, 12, 16, 0) };
      static void AddField(Panel target, string label, Control control)
      {
        AutomationProperties.SetName(control, label);
        target.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
        target.Children.Add(control);
      }
      AddField(panel, "Протокол", protocol);
      AddField(panel, "Базовый адрес HTTP(S)", endpoint);
      AddField(panel, "Модель", model);
      AddField(panel, "Ключ API (если требуется)", key);
      var quota = new Grid();
      quota.ColumnDefinitions.Add(new ColumnDefinition());
      quota.ColumnDefinitions.Add(new ColumnDefinition());
      var documentField = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
      var timeoutField = new StackPanel();
      AddField(documentField, "Документов за проверку", limit);
      AddField(timeoutField, "Время ожидания, с", timeout);
      Grid.SetColumn(timeoutField, 1);
      quota.Children.Add(documentField);
      quota.Children.Add(timeoutField);
      panel.Children.Add(quota);
      panel.Children.Add(enabled);
      panel.Children.Add(new TextBlock { Text = "На указанный адрес отправляются код проверяемых документов и замечания. Ключ хранится до закрытия программы. Для удалённого API используйте HTTPS.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) });
      var save = new Button { Content = "Сохранить", IsDefault = true };
      var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(12, 4, 12, 8) };
      footer.Children.Add(new Button { Content = "Отмена", IsCancel = true });
      footer.Children.Add(save);
      DockPanel.SetDock(footer, Dock.Bottom);
      var layout = new DockPanel();
      layout.Children.Add(footer);
      layout.Children.Add(new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
      var window = new Window { Title = "Настройки ИИ", Content = layout, Width = 540, MinWidth = 330, MinHeight = 240, SizeToContent = SizeToContent.Height, Owner = Application.Current.MainWindow, WindowStartupLocation = WindowStartupLocation.CenterOwner };
      window.SourceInitialized += DarkTitleBar.Apply;
      window.SourceInitialized += WindowSizing.FitDialog;
      window.ContentRendered += WindowSizing.KeepInitialPositionVisible;
      save.Click += (_, __) => {
        if (!Uri.TryCreate(endpoint.Text, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https") || !string.IsNullOrEmpty(uri.UserInfo) || string.IsNullOrWhiteSpace(model.Text) || !int.TryParse(limit.Text, out var count) || count < 1 || count > 10000 || !int.TryParse(timeout.Text, out var seconds) || seconds < 10 || seconds > 3600)
        { MessageBox.Show(window, "Укажите корректный адрес без учётных данных, модель, число документов от 1 до 10000 и время ожидания от 10 до 3600 секунд.", "Настройки ИИ", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        AiSettings.Endpoint = endpoint.Text.Trim(); AiSettings.Model = model.Text.Trim(); AiSettings.ApiKey = key.Password;
        AiSettings.ChatCompletions = protocol.SelectedIndex == 1; AiSettings.MaximumDocuments = count; AiSettings.TimeoutSeconds = seconds; AiSettings.Enabled = enabled.IsChecked == true;
        window.DialogResult = true;
      };
      return window.ShowDialog() == true;
    }
  }
}
