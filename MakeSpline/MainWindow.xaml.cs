using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Wpf;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MakeSpline
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        float speed = 0.05f;
        public MainWindow()
        {
            InitializeComponent();
            var settings = new GLWpfControlSettings
            {
                MajorVersion = 3,
                MinorVersion = 3
            };
            OpenTkControl.Start(settings);
            SplineRender.Instance.Initialize();
            Reset();
            DataContext = this;
            DataGridXYW.ItemsSource = SplineRender.Instance.P;
            StackPanel_ControlPointsToolPanel.DataContext = SplineRender.Instance;
        }

        private void Reset()
        {
            CheckBox_MakeBezierSpline.IsChecked = Default.DrawBezierSplineMode;
            Slider_BezierSplineWidth.Value = Default.BezierSplineWidth;
            ColorPicker_BezierSpline.SelectedColor = ColorFloatToByte(Default.ColorBezierSpline);
            Slider_BezierSplineWidth.Value = Default.BezierSplineWidth;

            CheckBox_MakeBezier2Spline.IsChecked = Default.DrawBezier2SplineMode;
            Slider_Bezier2SplineWidth.Value = Default.Bezier2SplineWidth;
            ColorPicker_Bezier2Spline.SelectedColor = ColorFloatToByte(Default.ColorBezier2Spline);
            Slider_Bezier2SplineWidth.Value = Default.Bezier2SplineWidth;

            CheckBox_MakeBezier3Spline.IsChecked = Default.DrawBezier3SplineMode;
            Slider_Bezier3SplineWidth.Value = Default.Bezier3SplineWidth;
            ColorPicker_Bezier3Spline.SelectedColor = ColorFloatToByte(Default.ColorBezier3Spline);
            Slider_Bezier3SplineWidth.Value = Default.Bezier3SplineWidth;

            CheckBox_MakeBSpline.IsChecked = Default.DrawBSplineMode;
            Slider_BSplineWidth.Value = Default.BSplineWidth;
            ColorPicker_BSpline.SelectedColor = ColorFloatToByte(Default.ColorBSpline);
            Slider_BSplineWidth.Value = Default.BSplineWidth;
            TextBox_BSplineM.Text = "2";

            CheckBox_MakeNURBS.IsChecked = Default.DrawNURBSMode;
            Slider_NURBSWidth.Value = Default.NURBSWidth;
            ColorPicker_NURBS.SelectedColor = ColorFloatToByte(Default.ColorNURBS);
            Slider_NURBSWidth.Value = Default.NURBSWidth;
            TextBox_NURBSM.Text = "2";
        }

        private void OpenTkControl_OnRender(TimeSpan obj)
        {
            GL.ClearColor(Color4.White);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            SplineRender.Instance.Draw();
            // Для времени
            TextBlock_BezierSplineTime.Text = $"Время (мс): {SplineRender.Instance.Splines[SplineTypes.BEZIER].Time_ms}";
            TextBlock_Bezier2SplineTime.Text = $"Время (мс): {SplineRender.Instance.Splines[SplineTypes.BEZIER2].Time_ms}";
            TextBlock_Bezier3SplineTime.Text = $"Время (мс): {SplineRender.Instance.Splines[SplineTypes.BEZIER3].Time_ms}";
            TextBlock_BSplineTime.Text = $"Время (мс): {SplineRender.Instance.Splines[SplineTypes.B_SPLINE].Time_ms}";
            TextBlock_NURBSTime.Text = $"Время (мс): {SplineRender.Instance.Splines[SplineTypes.NURBS].Time_ms}";
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SplineRender.Instance.CleanUp();
        }

        private void OpenTkControl_MouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(OpenTkControl);
            Point new_position = MouseMap(position);
            double x = new_position.X;
            double y = new_position.Y;
            TextBlock_XY.Text = $"X: {x.ToString("0.00")}, Y: {y.ToString("0.00")}";
        }

        private void OpenTkControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(OpenTkControl);
            Point new_position = MouseMap(position);
            float x = (float)new_position.X;
            float y = (float)new_position.Y;
            SplineRender.Instance.AddPoint(new Vector3f(x, y));

        }

        private void OpenTkControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((float)e.Delta > 0) 
                SplineRender.Instance.Scale *= (float)e.Delta / 100;
            else
                SplineRender.Instance.Scale /= -(float)e.Delta / 100;

            Vector4 v1 = new Vector4((float)1f, (float)1f, 0, 1);
            Vector4 v2 = new Vector4((float)-1f, (float)-1f, 0, 1);
            Matrix4 rtranslate = Matrix4.CreateTranslation(-SplineRender.Instance.Xoffset, -SplineRender.Instance.Yoffset, 0);
            Matrix4 rscale = Matrix4.CreateScale(1f / SplineRender.Instance.Scale, 1f / SplineRender.Instance.Scale, 0);
            Vector4 u1 = v1 * rscale * rtranslate;
            Vector4 u2 = v2 * rscale * rtranslate;
            speed = (u1.X - u2.X) / 40;
        }

        private void OpenTkControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left)
            {
                SplineRender.Instance.Xoffset -= speed;
            }
            else if (e.Key == Key.Right)
            {
                SplineRender.Instance.Xoffset += speed;
            }
            else if (e.Key == Key.Down)
            {
                SplineRender.Instance.Yoffset -= speed;
            }
            else if (e.Key == Key.Up)
            {
                SplineRender.Instance.Yoffset += speed;
            }
        }

        // Экранные коордианты в реальные
        private Point MouseMap(Point pos)
        {
            float left = -1f;
            float bottom = -1f;
            float right = 1f;
            float top = 1f;

            double width = OpenTkControl.ActualWidth;
            double height = OpenTkControl.ActualHeight;

            double x = pos.X * (right - left) / width + left;
            double y = pos.Y * (bottom - top) / height + top;
            Vector4 v = new Vector4((float)x, (float)y, 0, 1);
            Matrix4 rtranslate = Matrix4.CreateTranslation(-SplineRender.Instance.Xoffset, -SplineRender.Instance.Yoffset, 0);
            Matrix4 rscale = Matrix4.CreateScale(1f / SplineRender.Instance.Scale, 1f / SplineRender.Instance.Scale, 0);
            Vector4 u = v * rscale * rtranslate;
            return new Point(u.X, u.Y);
        }

        // Функции для перевода цветов
        static public Color ColorFloatToByte(Color4 color4)
        {
            Color color = new Color();
            color.R = (byte)(color4.R * 255);
            color.G = (byte)(color4.G * 255);
            color.B = (byte)(color4.B * 255);
            color.A = (byte)(color4.A * 255);
            return color;
        }

        static public Color4 ColorByteToFloat(Color color)
        {
            Color4 color4 = new Color4();
            color4.R = color.R / 255f;
            color4.G = color.G / 255f;
            color4.B = color.B / 255f;
            color4.A = color.A / 255f;
            return color4;
        }

        private void ValidateInputM(SplineTypes type, TextBox textBox ,TextBlock textBlock)
        {
            if (SplineRender.Instance.Splines == null) return;
            int m;
            bool success = int.TryParse(textBox.Text, out m) && m > 0;
            if (success)
            {
                SplineRender.Instance.Splines[type].M = m;
                textBlock.Text = "";
            }
            else
            {
                textBlock.Text = " невозможная m";
            }
        }

        private void CheckBox_MakeBezierSpline_Checked(object sender, RoutedEventArgs e)
        {
            if (SplineRender.Instance.Splines == null) return;
            SplineRender.Instance.Splines[SplineTypes.BEZIER].DrawSplineMode = true;
        }

        private void CheckBox_MakeBezierSpline_Unchecked(object sender, RoutedEventArgs e)
        {
            if (SplineRender.Instance.Splines == null) return;
            SplineRender.Instance.Splines[SplineTypes.BEZIER].DrawSplineMode = false;
        }

        private void ColorPicker_BezierSpline_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (SplineRender.Instance.Splines == null) return;
            SplineRender.Instance.Splines[SplineTypes.BEZIER].ColorSpline = ColorByteToFloat((Color)e.NewValue);
        }

        private void Slider_BezierSplineWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SplineRender.Instance.Splines == null) return;
            SplineRender.Instance.Splines[SplineTypes.BEZIER].SplineWidth = (float)e.NewValue;
        }

        private void CheckBox_MakeBezier2Spline_Checked(object sender, RoutedEventArgs e)
        {
            if (SplineRender.Instance.Splines == null) return;
            SplineRender.Instance.Splines[SplineTypes.BEZIER2].DrawSplineMode = true;
        }

        private void CheckBox_MakeBezier2Spline_Unchecked(object sender, RoutedEventArgs e)
        {
            if (SplineRender.Instance.Splines == null) return;
            SplineRender.Instance.Splines[SplineTypes.BEZIER2].DrawSplineMode = false;
        }

        private void ColorPicker_Bezier2Spline_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (SplineRender.Instance.Splines == null) return;
            SplineRender.Instance.Splines[SplineTypes.BEZIER2].ColorSpline = ColorByteToFloat((Color)e.NewValue);
        }

        private void Slider_Bezier2SplineWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SplineRender.Instance.Splines == null) return;
            SplineRender.Instance.Splines[SplineTypes.BEZIER2].SplineWidth = (float)e.NewValue;
        }

        private void CheckBox_MakeBezier3Spline_Checked(object sender, RoutedEventArgs e)
        {
            if (SplineRender.Instance.Splines == null) return;
            SplineRender.Instance.Splines[SplineTypes.BEZIER3].DrawSplineMode = true;
        }

        private void CheckBox_MakeBezier3Spline_Unchecked(object sender, RoutedEventArgs e)
        {
            if (SplineRender.Instance.Splines == null) return;
            SplineRender.Instance.Splines[SplineTypes.BEZIER3].DrawSplineMode = false;
        }

        private void ColorPicker_Bezier3Spline_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (SplineRender.Instance.Splines == null) return;
            SplineRender.Instance.Splines[SplineTypes.BEZIER3].ColorSpline = ColorByteToFloat((Color)e.NewValue);
        }

        private void Slider_Bezier3SplineWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SplineRender.Instance.Splines == null) return;
            SplineRender.Instance.Splines[SplineTypes.BEZIER3].SplineWidth = (float)e.NewValue;
        }

        private void CheckBox_MakeBSpline_Checked(object sender, RoutedEventArgs e)
        {
            if (SplineRender.Instance.Splines == null) return;
            SplineRender.Instance.Splines[SplineTypes.B_SPLINE].DrawSplineMode = true;
        }

        private void CheckBox_MakeBSpline_Unchecked(object sender, RoutedEventArgs e)
        {
            if (SplineRender.Instance.Splines == null) return;
            SplineRender.Instance.Splines[SplineTypes.B_SPLINE].DrawSplineMode = false;
        }

        private void ColorPicker_BSpline_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (SplineRender.Instance.Splines == null) return;
            SplineRender.Instance.Splines[SplineTypes.B_SPLINE].ColorSpline = ColorByteToFloat((Color)e.NewValue);
        }

        private void Slider_BSplineWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SplineRender.Instance.Splines == null) return;
            SplineRender.Instance.Splines[SplineTypes.B_SPLINE].SplineWidth = (float)e.NewValue;
        }

        private void TextBox_BSplineM_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateInputM(SplineTypes.B_SPLINE, TextBox_BSplineM ,TextBlock_BSplineMTip);
        }

        private void CheckBox_MakeNURBS_Checked(object sender, RoutedEventArgs e)
        {
            if (SplineRender.Instance.Splines == null) return;
            SplineRender.Instance.Splines[SplineTypes.NURBS].DrawSplineMode = true;
        }

        private void CheckBox_MakeNURBS_Unchecked(object sender, RoutedEventArgs e)
        {
            if (SplineRender.Instance.Splines == null) return;
            SplineRender.Instance.Splines[SplineTypes.NURBS].DrawSplineMode = false;
        }

        private void ColorPicker_NURBS_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (SplineRender.Instance.Splines == null) return;
            SplineRender.Instance.Splines[SplineTypes.NURBS].ColorSpline = ColorByteToFloat((Color)e.NewValue);
        }

        private void Slider_NURBSWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SplineRender.Instance.Splines == null) return;
            SplineRender.Instance.Splines[SplineTypes.NURBS].SplineWidth = (float)e.NewValue;
        }

        private void TextBox_NURBSM_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateInputM(SplineTypes.NURBS, TextBox_NURBSM, TextBlock_NURBSMTip);
        }

        private void DataGridXYW_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                SplineRender.Instance.Update();
        }

        private void DataGridXYW_CurrentCellChanged(object sender, EventArgs e)
        {
            SplineRender.Instance.Update();
        }

        private void DataGridXYW_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var selectedItems = DataGridXYW.SelectedItems;
            List<Vector3f> selectedPoints = new List<Vector3f>();
            foreach (var item in selectedItems)
            {
                selectedPoints.Add((Vector3f)item);

            }
            SplineRender.Instance.BuildSelectedPoints(selectedPoints);
        }

        private void DataGridXYW_LostFocus(object sender, RoutedEventArgs e)
        {
            SplineRender.Instance.CleanSelectedPoints();
        }
    }
}