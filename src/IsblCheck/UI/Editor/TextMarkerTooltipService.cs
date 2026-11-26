using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;

namespace IsblCheck.UI.Editor
{
  /// <summary>
  /// Сервис подсказок.
  /// </summary>
  public class TextMarkerTooltipService : IDisposable
  {
    #region Поля и свойства

    /// <summary>
    /// Редактор.
    /// </summary>
    private readonly TextEditor editor;
    private readonly TextBlock tooltipText;

    /// <summary>
    /// Маркеры.
    /// </summary>
    public TextSegmentCollection<TextMarker> Markers { get; set; }

    #endregion

    #region Методы

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void EditorToolTipOpeningHandler(object sender, ToolTipEventArgs e)
    {
      var point = Mouse.GetPosition(this.editor);
      var position = this.editor.GetPositionFromPoint(point);
      if (this.Markers == null || this.editor.Document == null || position == null)
      {
        e.Handled = true;
        return;
      }
      var offset = this.editor.Document.GetOffset(position.Value.Location);
      var marker = this.Markers.FindSegmentsContaining(offset).FirstOrDefault(candidate => candidate.ToolTip != null);
      e.Handled = marker == null;
      if (marker != null)
        this.tooltipText.Text = marker.ToolTip.ToString();
    }

    public void Dispose()
    {
      this.editor.TextArea.ToolTipOpening -= this.EditorToolTipOpeningHandler;
      this.editor.TextArea.ClearValue(FrameworkElement.ToolTipProperty);
    }

    #endregion

    #region Конструкторы

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="editor">Редактор.</param>
    public TextMarkerTooltipService(TextEditor editor)
    {
      this.editor = editor;
      this.tooltipText = new TextBlock { MaxWidth = 400, TextWrapping = TextWrapping.Wrap };
      this.editor.TextArea.ToolTip = new ToolTip { Content = this.tooltipText };
      this.editor.TextArea.ToolTipOpening += this.EditorToolTipOpeningHandler;
    }

    #endregion
  }
}
