//
// Author:
//   Michael Göricke
//
// Copyright (c) 2023
//
// This file is part of FocalMaster.
//
// ShapeConverter is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see<http://www.gnu.org/licenses/>.

using System.Windows.Media;

namespace ShapeConverter.BusinessLogic.Generators
{
    /// <summary>
    /// Class for creating a DrawingBrush out of a graphic visual
    /// </summary>
    public static class DrawingVisualGenerator
    {
        /// <summary>
        /// Generates a WPF brush from the specified drawing.
        /// </summary>
        public static DrawingVisual Generate(GraphicVisual visual)
        {
            var drawingBrush = new DrawingVisual();

            drawingBrush.Children.Add(GenerateDrawing(visual));

            return drawingBrush;
        }

        /// <summary>
        /// Generates a WPF drawing from the specified geometry tree.
        /// </summary>
        private static DrawingVisual GenerateDrawing(GraphicVisual visual)
        {
            DrawingVisual drawing = null;

            switch (visual)
            {
                case GraphicGroup group:
                {
                    var drawingGroup = new DrawingVisual();
                    drawing = drawingGroup;
                    drawingGroup.Opacity = group.Opacity;

                    if (group.Clip != null)
                    {
                        drawingGroup.Clip = GeometryGenerator.GenerateGeometry(group.Clip);
                    }

                    foreach (var childVisual in group.Children)
                    {
                        var childDrawing = GenerateDrawing(childVisual);
                        drawingGroup.Children.Add(childDrawing);
                    }

                    break;
                }

                case GraphicPath graphicPath:
                {
                    drawing = GeneratePath(graphicPath);

                    break;
                }
            }

            return drawing;
        }

        /// <summary>
        /// Create a GeometryDrawing from a single graphic path
        /// </summary>
        private static DrawingVisual GeneratePath(GraphicPath graphicPath)
        {
            var drawingVisual = new DrawingVisual();
            var drawingContext = drawingVisual.RenderOpen();

            var (fill, pen) = GetColors(graphicPath);
            drawingContext.DrawGeometry(fill, pen, GeometryGenerator.GenerateGeometry(graphicPath.Geometry));
            drawingContext.Close();

            return drawingVisual;
        }

        /// <summary>
        /// Set the fill and stroke colors
        /// </summary>
        private static (Brush, Pen) GetColors(GraphicPath graphicPath)
        {
            Brush fill = null;
            Pen pen = null;

            if (graphicPath.FillBrush != null)
            {
                fill = GenerateBrush(graphicPath.FillBrush);
            }

            if (graphicPath.StrokeBrush != null)
            {
                var brush = GenerateBrush(graphicPath.StrokeBrush);
                pen = new Pen(brush, graphicPath.StrokeThickness);
                pen.MiterLimit = graphicPath.StrokeMiterLimit;
            }

            return (fill, pen);
        }

        /// <summary>
        /// Generate a WPF brush
        /// </summary>
        private static Brush GenerateBrush(GraphicBrush graphicBrush)
        {
            Brush brush;

            if (graphicBrush is GraphicSolidColorBrush colorBrush)
            {
                brush = new SolidColorBrush(colorBrush.Color);
            }
            else
            {
                brush = new SolidColorBrush(Colors.Black);
            }

            return brush;
        }
    }
}
