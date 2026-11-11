using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace vatSysToolbox.Controls;

public enum Direction
{
    Left, Up, Right, Down
};

/// <summary>
/// A chevron control with 3D beveled edges
/// </summary>
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

    public Direction Direction
    {
        get => (Direction) GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    /// <remarks>
    /// Use an odd width and an odd height. Then the points of the chevron are on the center of a pixel, and they reach
    /// the edges of the control.
    /// </remarks>
    public Chevron()
    {
        Width = 9;
        Height = 9;

        // vatSys draws its arrows with no anti-aliasing. With anti-aliasing, a pixel that the point covers only a
        // little is almost transparent. Then the points do not appear to reach the edges of the control.
        RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);

        // Put the control on full pixels, so that the points are on the center of a pixel
        UseLayoutRounding = true;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var width = ActualWidth;
        var height = ActualHeight;

        if (width <= 0 || height <= 0)
            return;

        DrawBeveledChevron(dc, width, height, Direction);
    }

    void DrawBeveledChevron(DrawingContext dc, double width, double height, Direction direction)
    {
        var foreground = Foreground ?? Brushes.Black;
        var bevelWidth = Bevel.LineWidth / 2;

        // The corners of the triangle. Edge i goes from corner i to the next corner. The light comes from the top
        // left, thus the edges that face up or left are light, and the other edges are dark.
        var (corners, edgeBrushes) = direction switch
        {
            Direction.Up => (
                new[] { new Point(width / 2, 0), new Point(width, height), new Point(0, height) },
                new[] { Bevel.DarkBrush, Bevel.DarkBrush, Bevel.LightBrush }),
            Direction.Down => (
                new[] { new Point(0, 0), new Point(width, 0), new Point(width / 2, height) },
                new[] { Bevel.LightBrush, Bevel.DarkBrush, Bevel.LightBrush }),
            Direction.Left => (
                new[] { new Point(0, height / 2), new Point(width, 0), new Point(width, height) },
                new[] { Bevel.LightBrush, Bevel.DarkBrush, Bevel.DarkBrush }),
            Direction.Right => (
                new[] { new Point(0, 0), new Point(width, height / 2), new Point(0, height) },
                new[] { Bevel.LightBrush, Bevel.DarkBrush, Bevel.LightBrush }),
            _ => throw new NotSupportedException()
        };

        var innerCorners = GetInnerCorners(corners, bevelWidth);

        dc.DrawGeometry(foreground, null, CreatePolygon(corners));

        // Each bevel is the area between an edge of the triangle and the same edge of the inner triangle
        for (var i = 0; i < 3; i++)
        {
            var next = (i + 1) % 3;
            dc.DrawGeometry(
                edgeBrushes[i],
                null,
                CreatePolygon([corners[i], corners[next], innerCorners[next], innerCorners[i]]));
        }
    }

    // Gives the corners of the triangle with each edge moved in by the bevel width, at a right angle to the edge. Thus,
    // the bevel has the same width on the sloped edges as on the straight edges.
    // The inner triangle has the same shape as the triangle, and the same incenter (the center of the largest circle
    // in the triangle). Thus, move each corner to the incenter, by the ratio of the bevel width to the radius of that
    // circle.
    static Point[] GetInnerCorners(Point[] corners, double bevelWidth)
    {
        // The length of the edge opposite each corner
        var a = (corners[1] - corners[2]).Length;
        var b = (corners[2] - corners[0]).Length;
        var c = (corners[0] - corners[1]).Length;
        var perimeter = a + b + c;

        var incenter = new Point(
            (a * corners[0].X + b * corners[1].X + c * corners[2].X) / perimeter,
            (a * corners[0].Y + b * corners[1].Y + c * corners[2].Y) / perimeter);

        var area = Math.Abs(Vector.CrossProduct(corners[1] - corners[0], corners[2] - corners[0])) / 2;
        var inradius = 2 * area / perimeter;

        // If the triangle is too small for the bevel, the bevels fill the triangle
        var scale = Math.Max(0, (inradius - bevelWidth) / inradius);

        return corners.Select(corner => incenter + (corner - incenter) * scale).ToArray();
    }

    static PathGeometry CreatePolygon(Point[] points)
    {
        var segments = points.Skip(1).Select(point => new LineSegment(point, true));
        return new PathGeometry([new PathFigure(points[0], segments, true)]);
    }
}
