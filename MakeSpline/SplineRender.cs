using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace MakeSpline
{
    // Сплайн + графика связанная с ним
    public class GraphSpline
    {
        ISpline spline;
        int vbo;
        bool drawSplineMode;
        public long Time_ms { get; private set; } = 0; // Время в миллисекундах
        public int N { get { return spline.N; } }
        public int M 
        { 
            get { return spline.M; } // Степень сплайна
            set 
            {
                spline.M = value;
                if (DrawSplineMode)
                    BuildSpline();
            } 
        }
        public int Vao { get; }
        public int Size { get { return spline.S.Count; } }
        public Color4 ColorSpline {  get; set; }
        public float SplineWidth {  get; set; }
        public bool DrawSplineMode { get { return drawSplineMode; }
            set 
            {
                drawSplineMode = value;
                if (DrawSplineMode) BuildSpline();
            } 
        }
        public GraphSpline(ISpline spline, Color4 colorSpline, float splineWidth, bool drawSplineMode)
        {
            vbo = GL.GenBuffer();
            Vao = GL.GenVertexArray();
            this.spline = spline;
            ColorSpline = colorSpline;
            SplineWidth = splineWidth;
            DrawSplineMode = drawSplineMode;
        }
        public void BuildSpline()
        {
            Time_ms = spline.MakeSpline();
            int k = 0;
            float[] v_spline = new float[spline.S.Count * 3];
            for (int i = 0; i < spline.S.Count; i++)
            {
                v_spline[k] = spline.S[i].X; v_spline[k + 1] = spline.S[i].Y; v_spline[k + 2] = 0;
                k += 3;
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, v_spline.Length * sizeof(float), v_spline, BufferUsageHint.StaticDraw);

            GL.BindVertexArray(Vao);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
        }

        public void Dispose()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.DeleteBuffer(vbo);
            GL.DeleteVertexArray(Vao);
        }
    }

    // thread safe singleton
    public sealed class SplineRender
    {
        private static readonly SplineRender instance = new SplineRender();

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static SplineRender() { }

        private SplineRender() { }

        public static SplineRender Instance
        {
            get { return instance; }
        }
        // Для координатных осей X и Y
        int vbo_axes;
        int vao_axes;
        Color4 color_axes = new Color4(0f, 0f, 0f, 1f);
        float axes_width = 3;
        // Для сетки
        int vbo_grid;
        int vao_grid;
        Color4 color_grid = new Color4(0.5f, 0.5f, 0.5f, 1f);
        float grid_width = 1;
        // Для контрольных точек и контрольной ломанной
        int vbo_points;
        int vao_points;
        // Выбранные контрольные точки
        int vbo_selected_points;
        int vao_selected_points;
        int num_selected_points = 0;
        Color4 color_selected_points = Color4.DarkBlue;
        // Количество интервалов на которые делится оси X и Y
        int x_d = 8;
        int y_d = 8;
        Matrix4 translate_mat = Matrix4.Identity;
        Matrix4 scale_mat = Matrix4.Identity;
        Matrix4 model = Matrix4.Identity;
        float xoffset = 0;
        float yoffset = 0;
        float scale = 1;
        Bezier_spline bezier_spline;
        Bezier2_spline bezier2_spline;
        Bezier3_spline bezier3_spline;
        B_spline b_spline;
        NURBS nurbs;
        Dictionary<SplineTypes, GraphSpline> splines;
        Shader shader;

        // public
        public ObservableCollection<Vector3f> P;
        public ReadOnlyDictionary<SplineTypes, GraphSpline> Splines { get { return splines?.AsReadOnly(); } }
        public Color ColorPoints { get; set; } = MainWindow.ColorFloatToByte(Default.ColorPoints);
        public float PointsSize { get; set; } = Default.PointsSize;
        public Color ColorChain { get; set; } = MainWindow.ColorFloatToByte(Default.ColorChain);
        public float ChainWidth { get; set; } = Default.ChainWidth;
        public bool DrawPointsMode { get; set; } = Default.DrawPointsMode;
        public bool DrawChainMode { get; set; } = Default.DrawChainMode;
        public float Xoffset { get { return xoffset; }
            set 
            {
                xoffset = value;
                translate_mat = Matrix4.CreateTranslation(xoffset, yoffset, 0);
                model = translate_mat * scale_mat;
            }
        }

        public float Yoffset
        {
            get { return yoffset; }
            set
            {
                yoffset = value;
                translate_mat = Matrix4.CreateTranslation(xoffset, yoffset, 0);
                model = translate_mat * scale_mat;
            }
        }

        public float Scale
        {
            get { return scale; }
            set
            {
                scale = value;
                scale_mat = Matrix4.CreateScale(scale, scale, 0);
                model = translate_mat * scale_mat;
            }
        }

        public void Initialize()
        {
            BuildAxes();
            BuildGrid();
            shader = new Shader("\\Shaders\\shader.vert", "\\Shaders\\shader.frag");
            shader.Use();
            vbo_points = GL.GenBuffer();
            vao_points = GL.GenVertexArray();
            vbo_selected_points = GL.GenBuffer();
            vao_selected_points = GL.GenVertexArray();
            P = new ObservableCollection<Vector3f>();
            bezier_spline = new Bezier_spline(P);
            bezier2_spline = new Bezier2_spline(P);
            bezier3_spline = new Bezier3_spline(P);
            b_spline = new B_spline(2, P);
            nurbs = new NURBS(2, P);
            splines = new Dictionary<SplineTypes, GraphSpline>
            {
                {SplineTypes.BEZIER, new GraphSpline(bezier_spline, Default.ColorBezierSpline, Default.BezierSplineWidth, Default.DrawBezierSplineMode) },
                {SplineTypes.BEZIER2, new GraphSpline(bezier2_spline, Default.ColorBezier2Spline, Default.Bezier2SplineWidth, Default.DrawBezier2SplineMode) },
                {SplineTypes.BEZIER3, new GraphSpline(bezier3_spline, Default.ColorBezier3Spline, Default.Bezier3SplineWidth, Default.DrawBezier3SplineMode) },
                {SplineTypes.B_SPLINE, new GraphSpline(b_spline, Default.ColorBSpline, Default.BSplineWidth, Default.DrawBSplineMode) },
                {SplineTypes.NURBS, new GraphSpline(nurbs, Default.ColorNURBS, Default.NURBSWidth, Default.DrawNURBSMode) }
            };
        }

        public void Update()
        {
            BuildPoints();
            foreach (GraphSpline spline in splines.Values)
                if (spline.DrawSplineMode) spline.BuildSpline();
        }

        public void AddPoint(Vector3f point)
        {
            P.Add(point);
            Update();
        }

        private void BuildAxes()
        {
            // optimaze
            float[] v_axes = { -100000f, 0f,  0f,
                                100000f,  0f,  0f,
                                0f,  -100000f, 0f,
                                0f,  100000f,  0f};
            vbo_axes = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_axes);
            GL.BufferData(BufferTarget.ArrayBuffer, v_axes.Length * sizeof(float), v_axes, BufferUsageHint.StaticDraw);
            vao_axes = GL.GenVertexArray();
            GL.BindVertexArray(vao_axes);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
        }

        // xd и yd количество интервалов, на которые будет делится сетка
        private void BuildGrid()
        {
            float x_h = 2f / x_d;
            float y_h = 2f / y_d;
            float[] v_grid = new float[(x_d + 1 + y_d + 1) * 6];
            int k = 0;
            for (int i = 0; i < x_d + 1; i++)
            {
                float x = -1 + x_h * i;
                v_grid[k] = x; v_grid[k + 1] = -1; v_grid[k + 2] = 0;
                v_grid[k + 3] = x; v_grid[k + 4] = 1; v_grid[k + 5] = 0;
                k += 6;
            }

            for (int i = 0; i < y_d + 1; i++)
            {
                float y = -1 + y_h * i;
                v_grid[k] = -1; v_grid[k + 1] = y; v_grid[k + 2] = 0;
                v_grid[k + 3] = 1; v_grid[k + 4] = y; v_grid[k + 5] = 0;
                k += 6;
            }
            vbo_grid = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_grid);
            GL.BufferData(BufferTarget.ArrayBuffer, v_grid.Length * sizeof(float), v_grid, BufferUsageHint.StaticDraw);
            vao_grid = GL.GenVertexArray();
            GL.BindVertexArray(vao_grid);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
        }

        private void BuildPoints()
        {
            int k = 0;
            float[] v_points = new float[P.Count * 3];
            for (int i = 0; i < P.Count; i++)
            {
                v_points[k] = P[i].X; v_points[k + 1] = P[i].Y; v_points[k + 2] = 0;
                k += 3;
            }
            
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_points);
            GL.BufferData(BufferTarget.ArrayBuffer, v_points.Length * sizeof(float), v_points, BufferUsageHint.StaticDraw);
           
            GL.BindVertexArray(vao_points);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
        }

        public void BuildSelectedPoints(IList<Vector3f> selectedPoints)
        {
            num_selected_points = selectedPoints.Count;
            int k = 0;
            float[] v_selected_points = new float[selectedPoints.Count * 3];
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                v_selected_points[k] = selectedPoints[i].X; v_selected_points[k + 1] = selectedPoints[i].Y; v_selected_points[k + 2] = 0;
                k += 3;
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_selected_points);
            GL.BufferData(BufferTarget.ArrayBuffer, v_selected_points.Length * sizeof(float), v_selected_points, BufferUsageHint.StaticDraw);

            GL.BindVertexArray(vao_selected_points);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
        }

        public void Draw()
        {
            DrawGrid();
            DrawAxis();
            DrawChain();
            DrawSplines();
            DrawPoints();
        }

        private void DrawAxis()
        {
            GL.LineWidth(axes_width);
            shader.SetColor4("current_color", color_axes);
            shader.SetMatrix4("model", ref model);
            GL.BindVertexArray(vao_axes);
            shader.Use();
            GL.DrawArrays(PrimitiveType.Lines, 0, 2);
            GL.DrawArrays(PrimitiveType.Lines, 2, 2);
        }

        private void DrawGrid()
        {
            GL.LineWidth(grid_width);
            shader.SetColor4("current_color", color_grid);
            Matrix4 transfrom_mat = Matrix4.Identity;
            shader.SetMatrix4("model", ref transfrom_mat);
            GL.BindVertexArray(vao_grid);
            shader.Use();
            GL.DrawArrays(PrimitiveType.Lines, 0, (x_d + 1 + y_d + 1) * 6);
        }

        private void DrawPoints()
        {
            if (DrawPointsMode)
            {
                shader.SetMatrix4("model", ref model);
                GL.BindVertexArray(vao_points);
                shader.Use();
                GL.PointSize(PointsSize);
                shader.SetColor4("current_color", MainWindow.ColorByteToFloat(ColorPoints));
                GL.DrawArrays(PrimitiveType.Points, 0, P.Count);
                // Выбранные точки
                if (num_selected_points != 0)
                {
                    GL.BindVertexArray(vao_selected_points);
                    shader.SetColor4("current_color", color_selected_points);
                    GL.DrawArrays(PrimitiveType.Points, 0, num_selected_points);
                }
            }
        }

        private void DrawChain()
        {
            if (DrawChainMode)
            {
                shader.SetMatrix4("model", ref model);
                GL.BindVertexArray(vao_points);
                shader.Use();
                GL.LineWidth(ChainWidth);
                shader.SetColor4("current_color", MainWindow.ColorByteToFloat(ColorChain));
                GL.DrawArrays(PrimitiveType.LineStrip, 0, P.Count);
            }
        }

        private void DrawSpline(GraphSpline graphSpline)
        {
            GL.LineWidth(graphSpline.SplineWidth);
            shader.SetColor4("current_color", graphSpline.ColorSpline);
            GL.BindVertexArray(graphSpline.Vao);
            GL.DrawArrays(PrimitiveType.LineStrip, 0, graphSpline.Size);
        }

        private void DrawSplines()
        {
            shader.Use();
            shader.SetMatrix4("model", ref model);
            foreach (GraphSpline spline in splines.Values)
                if (spline.DrawSplineMode) DrawSpline(spline);
        }

        public void CleanSelectedPoints()
        {
            num_selected_points = 0;
        }

        public void CleanUp()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.UseProgram(0);
            GL.DeleteBuffer(vbo_axes);
            GL.DeleteBuffer(vbo_grid);
            GL.DeleteVertexArray(vao_axes);
            GL.DeleteVertexArray(vao_grid);
            shader.Dispose();
        }
    }
}
