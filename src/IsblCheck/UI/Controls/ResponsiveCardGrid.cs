using System.Windows;
using System.Windows.Controls.Primitives;

namespace IsblCheck.UI.Controls
{
  /// <summary>Four cards on a wide pane, two or one when there is less room.</summary>
  public sealed class ResponsiveCardGrid : UniformGrid
  {
    protected override Size MeasureOverride(Size availableSize)
    {
      var columnCount = availableSize.Width >= 680 ? 4 : availableSize.Width >= 336 ? 2 : 1;
      if (Columns != columnCount)
        Columns = columnCount;
      return base.MeasureOverride(availableSize);
    }
  }
}
