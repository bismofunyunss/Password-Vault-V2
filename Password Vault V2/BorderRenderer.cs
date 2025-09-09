using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Password_Vault_V2;

internal static class BorderRenderer
{
    public static void DrawSmoothGradientBorder(Control control, Graphics g, float radius, float borderSize,
      Color topLeftColor, Color topRightColor, Color bottomRightColor, Color bottomLeftColor)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle rect = control.ClientRectangle;

        using (GraphicsPath path = GetRoundedPath(rect, radius))
        {
            // Create a path gradient brush to smoothly blend colors around the border
            using (PathGradientBrush brush = new PathGradientBrush(path))
            {
                // Set center color as transparent to create a fading effect
                brush.CenterColor = Color.Transparent;

                // Set colors for each corner of the path (order matters!)
                brush.SurroundColors = new Color[]
                {
                topLeftColor,
                topRightColor,
                bottomRightColor,
                bottomLeftColor
                };

                // Draw the gradient border with thick pen
                using (Pen pen = new Pen(brush, borderSize))
                {
                    pen.Alignment = PenAlignment.Center; // draw centered on the border path
                    g.DrawPath(pen, path);
                }
            }
        }

        // Optionally, set the control region to rounded for clipping
        control.Region = new Region(GetRoundedPath(rect, radius));
    }

    public static GraphicsPath GetRoundedPath(Rectangle rect, float radius)
    {
        GraphicsPath path = new GraphicsPath();
        float diameter = radius * 2f;

        path.StartFigure();

        // Top-left arc
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        // Top edge
        path.AddLine(rect.Left + radius, rect.Top, rect.Right - radius, rect.Top);
        // Top-right arc
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        // Right edge
        path.AddLine(rect.Right, rect.Top + radius, rect.Right, rect.Bottom - radius);
        // Bottom-right arc
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        // Bottom edge
        path.AddLine(rect.Right - radius, rect.Bottom, rect.Left + radius, rect.Bottom);
        // Bottom-left arc
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        // Left edge
        path.AddLine(rect.Left, rect.Bottom - radius, rect.Left, rect.Top + radius);

        path.CloseFigure();
        return path;
    }
}