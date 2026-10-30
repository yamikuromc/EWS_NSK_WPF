using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GMap.NET;
using GMap.NET.WindowsPresentation;
using EWS_NSK_WPF.Models;

namespace EWS_NSK_WPF.Helpers
{
    public static class HydrantMarker
    {
        /// <summary>
        /// Создаёт маркер гидранта с цветовой индикацией статуса
        /// </summary>
        public static GMapMarker CreateMarker(Ewss hydrant, int currentZoom)
        {
            // Размер маркера зависит от зума
            double markerSize = GetMarkerSize(currentZoom);
            double strokeThickness = GetStrokeThickness(currentZoom);
            bool showText = ShouldShowText(currentZoom);
            double textScale = GetTextScale(currentZoom);

            // Получаем цвета по статусу
            var (fillColor, strokeColor) = MarkerColorHelper.GetColorsForStatus(hydrant.EwsStatusCod, hydrant.StatusName);

            // Создаём Canvas с маркером
            var canvas = CreateMarkerCanvas(
                markerSize, strokeThickness, showText, textScale,
                fillColor, strokeColor,
                hydrant.DisplayNumber, hydrant.PipeInfo);

            var marker = new GMapMarker(new PointLatLng(hydrant.LatitudeD, hydrant.LongitudeD))
            {
                Shape = canvas,
                Offset = new Point(-markerSize / 2, -markerSize / 2),
                Tag = hydrant.MarkerId
            };

            return marker;
        }

        /// <summary>
        /// Обновляет размер маркера при изменении зума
        /// </summary>
        public static void UpdateMarkerSize(GMapMarker marker, int zoomLevel)
        {
            if (marker?.Shape is not Canvas canvas) return;

            double markerSize = GetMarkerSize(zoomLevel);
            double textScale = GetTextScale(zoomLevel);
            bool showText = ShouldShowText(zoomLevel);
            double strokeThickness = GetStrokeThickness(zoomLevel);

            // Обновляем половину эллипса
            var halfEllipse = canvas.Children.OfType<Path>()
                .FirstOrDefault(p => p.Tag as string == "hydrant_half");
            if (halfEllipse != null)
                halfEllipse.Data = CreateHalfEllipseGeometry(markerSize);

            // Обновляем контур эллипса
            var ellipseOutline = canvas.Children.OfType<Ellipse>().FirstOrDefault();
            if (ellipseOutline != null)
            {
                ellipseOutline.Width = markerSize;
                ellipseOutline.Height = markerSize;
                ellipseOutline.StrokeThickness = strokeThickness;
            }

            // Обновляем линию
            var line = canvas.Children.OfType<Line>().FirstOrDefault();
            if (line != null)
            {
                line.X1 = -markerSize * 0.2;
                line.Y1 = markerSize / 2;
                line.X2 = markerSize * 1.2;
                line.Y2 = markerSize / 2;
                line.StrokeThickness = strokeThickness * 0.8;
            }

            // Обновляем текст
            var textBlock = canvas.Children.OfType<TextBlock>()
                .FirstOrDefault(tb => tb.Tag as string == "hydrant_text");
            if (textBlock != null)
            {
                if (showText)
                {
                    textBlock.FontSize = 10 * textScale;
                    textBlock.Padding = new Thickness(3 * textScale, 1 * textScale, 3 * textScale, 1 * textScale);
                    textBlock.Visibility = Visibility.Visible;
                    Canvas.SetLeft(textBlock, markerSize + 8 * textScale);
                    Canvas.SetTop(textBlock, markerSize / 2 - 6 * textScale);
                }
                else
                {
                    textBlock.Visibility = Visibility.Collapsed;
                }
            }

            marker.Offset = new Point(-markerSize / 2, -markerSize / 2);
        }

        /// <summary>
        /// Размер маркера в зависимости от зума
        /// </summary>
        private static double GetMarkerSize(int zoom)
        {
            return zoom switch
            {
                <= 12 => 10,
                13 => 12,
                14 => 14,
                15 => 16,
                16 => 18,
                17 => 22,
                _ => 26   
            };
        }

        private static double GetStrokeThickness(int zoom)
        {
            return zoom >= 16 ? 2.5 : 1.5;
        }

        private static bool ShouldShowText(int zoom)
        {
            return zoom >= 14;
        }

        private static double GetTextScale(int zoom)
        {
            return zoom >= 17 ? 1.3 : (zoom >= 15 ? 1.0 : 0.8);
        }

        /// <summary>
        /// Создаёт Canvas с маркером (половина эллипса + контур + линия + текст)
        /// </summary>
        private static Canvas CreateMarkerCanvas(
            double markerSize, double strokeThickness, bool showText, double textScale,
            Color fillColor, Color strokeColor, string number, string truba)
        {
            var canvas = new Canvas
            {
                IsHitTestVisible = true,
                Width = 200,
                Height = 100
            };
            Panel.SetZIndex(canvas, 1000);

            // При наведении мыши — поднимаем маркер на передний план
            canvas.MouseEnter += (s, e) =>
            {
                Panel.SetZIndex(canvas, 10000); // Высокий ZIndex — маркер поверх всех
            };

            // Когда мышь уходит — возвращаем обычный ZIndex
            canvas.MouseLeave += (s, e) =>
            {
                Panel.SetZIndex(canvas, 1000);
            };

            // Половина эллипса (заполненная)
            var halfEllipse = new Path
            {
                Data = CreateHalfEllipseGeometry(markerSize),
                Fill = new SolidColorBrush(fillColor),
                Stroke = Brushes.Transparent,
                Tag = "hydrant_half"
            };
            Canvas.SetLeft(halfEllipse, 0);
            Canvas.SetTop(halfEllipse, 0);
            canvas.Children.Add(halfEllipse);

            // Контур эллипса
            var ellipseOutline = new Ellipse
            {
                Width = markerSize,
                Height = markerSize,
                Fill = Brushes.Transparent,
                Stroke = new SolidColorBrush(strokeColor),
                StrokeThickness = strokeThickness,
                IsHitTestVisible = true,
                Tag = "hydrant_outline"
            };
            Canvas.SetLeft(ellipseOutline, 0);
            Canvas.SetTop(ellipseOutline, 0);
            canvas.Children.Add(ellipseOutline);

            // Линия (привязка к трубе)
            var line = new Line
            {
                X1 = -markerSize * 0.2,
                Y1 = markerSize / 2,
                X2 = markerSize * 1.2,
                Y2 = markerSize / 2,
                Stroke = new SolidColorBrush(strokeColor),
                StrokeThickness = strokeThickness * 0.8,
                Tag = "hydrant_line"
            };
            Canvas.SetLeft(line, 0);
            Canvas.SetTop(line, 0);
            canvas.Children.Add(line);

            // Текст с номером и диаметром
            var textBlock = new TextBlock
            {
                Text = $"{number} {truba}".Trim(),
                FontSize = 10 * textScale,
                Foreground = Brushes.Black,
                Background = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)),
                Padding = new Thickness(4 * textScale, 2 * textScale, 4 * textScale, 2 * textScale),
                TextAlignment = TextAlignment.Left,
                Tag = "hydrant_text",
                Visibility = showText ? Visibility.Visible : Visibility.Collapsed
            };
            Canvas.SetLeft(textBlock, markerSize + 10 * textScale);
            Canvas.SetTop(textBlock, markerSize / 2 - 8 * textScale);
            canvas.Children.Add(textBlock);

            return canvas;
        }

        /// <summary>
        /// Создаёт геометрию половины эллипса (как в ZoneHydrantEditor)
        /// </summary>
        private static Geometry CreateHalfEllipseGeometry(double size)
        {
            var geometry = new PathGeometry();
            var figure = new PathFigure
            {
                StartPoint = new Point(size / 2, 0),
                IsClosed = true
            };
            figure.Segments.Add(new ArcSegment
            {
                Point = new Point(size / 2, size),
                Size = new Size(size / 2, size / 2),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = true
            });
            figure.Segments.Add(new LineSegment { Point = new Point(size / 2, size / 2) });
            geometry.Figures.Add(figure);
            return geometry;
        }
    }
}