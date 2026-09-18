using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GlyCounter.design
{
    public enum ShapeKind { Square, Circle, Diamond, Triangle }

    public class MonosaccharideDrawing
    {
        public ShapeKind Shape { get; set; }
        public Color FilledColor { get; set; }
        public Color OutlineColor { get; set; } = Color.DimGray;

        public static Dictionary<string, MonosaccharideDrawing> GetMonosaccharides()
        {
            var glcNAc = new MonosaccharideDrawing
            {
                Shape = ShapeKind.Square, FilledColor = ColorTranslator.FromHtml("#2E77B5"),
            };
            var fucose = new MonosaccharideDrawing
            {
                Shape = ShapeKind.Triangle, FilledColor = ColorTranslator.FromHtml("#E51E1F")
            };
            var mannose = new MonosaccharideDrawing
            {
                Shape = ShapeKind.Circle, FilledColor = ColorTranslator.FromHtml("#00A651")
            };
            var neuAc = new MonosaccharideDrawing
            {
                Shape = ShapeKind.Diamond, FilledColor = ColorTranslator.FromHtml("#804097")
            };
            var neuGc = new MonosaccharideDrawing
            {
                Shape = ShapeKind.Diamond, FilledColor = ColorTranslator.FromHtml("#8FCCE9")
            };
            var galactose = new MonosaccharideDrawing
            {
                Shape = ShapeKind.Circle, FilledColor = ColorTranslator.FromHtml("#FED405")
            };
            return new Dictionary<string, MonosaccharideDrawing>
            {
                { "GlcNAc", glcNAc },
                { "Fucose", fucose },
                { "Mannose", mannose },
                { "NeuAc", neuAc },
                { "NeuGc", neuGc },
                { "Galactose", galactose }
            };
        }
    }

    /// <summary>
    /// A progress bar made of a row of monosaccharides that appear as progress is made
    /// </summary>
    public class ShapeProgressBar : UserControl
    {
        private int _value = 0;
        private int _maximum = 100;
        private int _shapeCount = 10;
        private int _spacing = 6;

        [DefaultValue(100)]
        public int Maximum
        {
            get => _maximum;
            set { _maximum = Math.Max(1, value); Invalidate(); }
        }

        [DefaultValue(10)]
        public int ShapeCount
        {
            get => _shapeCount;
            set { _shapeCount = Math.Max(1, value); Invalidate(); }
        }

        [DefaultValue(6)]
        public int Spacing
        {
            get => _spacing;
            set { _spacing = Math.Max(0, value); Invalidate(); }
        }

        [DefaultValue(0)]
        public int Value
        {
            get => _value;
            set
            {
                _value = Math.Max(0, Math.Min(_maximum, value));
                Invalidate(); // trigger redraw
            }
        }

        public ShapeProgressBar()
        {
            DoubleBuffered = true; // prevents flicker while animating
            SetStyle(ControlStyles.ResizeRedraw, true);
            Height = 40;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int totalSpacing = _spacing * (_shapeCount - 1);
            float shapeSize = (Width - totalSpacing) / (float)_shapeCount;
            shapeSize = Math.Min(shapeSize, Height);

            // How many shapes should be drawn based on current value
            float progressFraction = _value / (float)_maximum;
            int filledCount = (int)Math.Round(progressFraction * _shapeCount);

            float y = (Height - shapeSize) / 2f;

            var monosaccharideShapes = MonosaccharideDrawing.GetMonosaccharides();
            var monosaccharideOrder = new List<string> { "GlcNAc", "Fucose", "GlcNAc", "Mannose", "Mannose", "Mannose", "GlcNAc", "Galactose", "NeuAc", "NeuGc" };

            for (int i = 0; i < filledCount; i++)
            {
                float x = i * (shapeSize + _spacing);
                var rect = new RectangleF(x, y, shapeSize, shapeSize);
                var monosaccharide = monosaccharideOrder[i];
                var shape = monosaccharideShapes[monosaccharide];
                Color fill = shape.FilledColor;

                using (var brush = new SolidBrush(fill))
                using (var pen = new Pen(shape.OutlineColor, 1f))
                {
                    DrawShape(g, shape.Shape, rect, brush, pen);
                }
            }
        }

        private void DrawShape(Graphics g, ShapeKind shape, RectangleF rect, Brush brush, Pen pen)
        {
            switch (shape)
            {
                case ShapeKind.Square:
                    g.FillRectangle(brush, rect);
                    g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                    break;

                case ShapeKind.Circle:
                    g.FillEllipse(brush, rect);
                    g.DrawEllipse(pen, rect);
                    break;

                case ShapeKind.Diamond:
                    {
                        PointF[] pts = {
                            new PointF(rect.X + rect.Width / 2, rect.Y),
                            new PointF(rect.X + rect.Width, rect.Y + rect.Height / 2),
                            new PointF(rect.X + rect.Width / 2, rect.Y + rect.Height),
                            new PointF(rect.X, rect.Y + rect.Height / 2)
                        };
                        g.FillPolygon(brush, pts);
                        g.DrawPolygon(pen, pts);
                    }
                    break;

                case ShapeKind.Triangle:
                    {
                        PointF[] pts = CreateEquilateralTriangle(rect);
                        g.FillPolygon(brush, pts);
                        g.DrawPolygon(pen, pts);
                    }
                    break;
            }
        }
        private PointF[] CreateEquilateralTriangle(RectangleF rect)
        {
            float side = rect.Height; // rect is always square (shapeSize x shapeSize)
            float triHeight = side * (float)(Math.Sqrt(3) / 2.0);

            float cx = rect.X + rect.Width / 2f;
            float cy = rect.Y + rect.Height / 2f;

            // Center the triangle horizontally within rect (triHeight <= rect.Width)
            float left = cx - triHeight / 2f;
            float right = cx + triHeight / 2f;

            return new PointF[]
            {
                new PointF(left, cy),                          // apex (pointing left)
                new PointF(right, rect.Y + side),               // bottom right
                new PointF(right, rect.Y)                       // top right
            };
        }
    }
}
