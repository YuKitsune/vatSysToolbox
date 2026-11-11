using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace vatSysToolbox.Controls;

public enum Direction
{
    Left, Up, Right, Down
};

/// <summary>
/// A chevron control with 3D beveled edges. The chevron fills the control.
/// </summary>
/// <remarks>
/// The chevron is drawn one pixel at a time, the same as vatSys draws its arrows with no anti-aliasing. Thus, the
/// points reach the edges of the control, and the bevel has the same width on all the edges.
/// </remarks>
public class Chevron : Control
{
    public static readonly DependencyProperty DirectionProperty =
        DependencyProperty.Register(
            nameof(Direction),
            typeof(Direction),
            typeof(Chevron),
            new FrameworkPropertyMetadata(
                Direction.Up,
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty BevelWidthProperty =
        DependencyProperty.Register(
            nameof(BevelWidth),
            typeof(double),
            typeof(Chevron),
            new FrameworkPropertyMetadata(
                Bevel.LineWidth / 2,
                FrameworkPropertyMetadataOptions.AffectsRender));

    public Direction Direction
    {
        get => (Direction) GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    /// <summary>
    /// The width of the bevel, at a right angle to each edge. Use a smaller value for a small chevron, so that the
    /// center of the chevron shows.
    /// </summary>
    public double BevelWidth
    {
        get => (double) GetValue(BevelWidthProperty);
        set => SetValue(BevelWidthProperty, value);
    }

    public Chevron()
    {
        Width = 9;
        Height = 9;

        // Put the control on full pixels, so that each pixel of the chevron is on a pixel of the screen
        UseLayoutRounding = true;
        RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        // Draw in the pixels of the screen, and not in WPF units, so that the chevron is correct at all scales
        var dpi = VisualTreeHelper.GetDpi(this);
        var pixelSize = new Size(1 / dpi.DpiScaleX, 1 / dpi.DpiScaleY);
        var width = (int) Math.Round(ActualWidth * dpi.DpiScaleX);
        var height = (int) Math.Round(ActualHeight * dpi.DpiScaleY);
        var bevelWidth = BevelWidth * dpi.DpiScaleX;

        if (width <= 0 || height <= 0)
            return;

        var foreground = Foreground ?? Brushes.Black;
        var (corners, edgeBrushes) = GetTriangle(width, height, Direction);

        foreach (var pixel in GetPixels(width, height, Direction))
        {
            var rect = new Rect(new Point(pixel.X * pixelSize.Width, pixel.Y * pixelSize.Height), pixelSize);
            dc.DrawRectangle(foreground, null, rect);

            // The pixel is in the bevel of the nearest edge, if it is nearer to that edge than the bevel width
            var center = new Point(pixel.X + 0.5, pixel.Y + 0.5);
            var edge = Enumerable.Range(0, 3)
                .OrderBy(i => GetDistance(center, corners[i], corners[(i + 1) % 3]))
                .First();
            if (GetDistance(center, corners[edge], corners[(edge + 1) % 3]) < bevelWidth)
                dc.DrawRectangle(edgeBrushes[edge], null, rect);
        }
    }

    // Gives the corners of the triangle, in pixels. Edge i goes from corner i to the next corner. The light comes from
    // the top left, thus the edges that face up or left are light, and the other edges are dark.
    static (Point[] Corners, Brush[] EdgeBrushes) GetTriangle(int width, int height, Direction direction)
    {
        return direction switch
        {
            Direction.Up => (
                new[] { new Point(width / 2.0, 0), new Point(width, height), new Point(0, height) },
                new[] { Bevel.DarkBrush, Bevel.DarkBrush, Bevel.LightBrush }),
            Direction.Down => (
                new[] { new Point(0, 0), new Point(width, 0), new Point(width / 2.0, height) },
                new[] { Bevel.LightBrush, Bevel.DarkBrush, Bevel.LightBrush }),
            Direction.Left => (
                new[] { new Point(0, height / 2.0), new Point(width, 0), new Point(width, height) },
                new[] { Bevel.LightBrush, Bevel.DarkBrush, Bevel.DarkBrush }),
            Direction.Right => (
                new[] { new Point(0, 0), new Point(width, height / 2.0), new Point(0, height) },
                new[] { Bevel.LightBrush, Bevel.DarkBrush, Bevel.LightBrush }),
            _ => throw new NotSupportedException()
        };
    }

    // Gives the pixels of the chevron. Each row (across the chevron) is centered, and gets wider from the point to the
    // base. The row at the point is 1 pixel (or 2 pixels if the base has an even number of pixels), and the row at the
    // base fills the control. Thus, the chevron reaches all the edges of the control.
    static IEnumerable<(int X, int Y)> GetPixels(int width, int height, Direction direction)
    {
        var isVertical = direction is Direction.Up or Direction.Down;
        var baseLength = isVertical ? width : height;
        var length = isVertical ? height : width;
        var parity = baseLength % 2;

        for (var row = 0; row < length; row++)
        {
            // The width of the row, with the same parity as the base, so that the row is centered
            var rowWidth = (int) Math.Round(((double) baseLength * (row + 1) / length - parity) / 2) * 2 + parity;
            rowWidth = Math.Max(rowWidth, parity == 1 ? 1 : 2);
            rowWidth = Math.Min(rowWidth, baseLength);

            var start = (baseLength - rowWidth) / 2;
            for (var across = start; across < start + rowWidth; across++)
            {
                yield return direction switch
                {
                    Direction.Up => (across, row),
                    Direction.Down => (across, height - 1 - row),
                    Direction.Left => (row, across),
                    Direction.Right => (width - 1 - row, across),
                    _ => throw new NotSupportedException()
                };
            }
        }
    }

    // Gives the distance from the point to the line through the edge
    static double GetDistance(Point point, Point edgeStart, Point edgeEnd)
    {
        var edge = edgeEnd - edgeStart;
        return Math.Abs(Vector.CrossProduct(edge, point - edgeStart)) / edge.Length;
    }
}
