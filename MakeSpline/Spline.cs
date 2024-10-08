using System.Collections.ObjectModel;
using System.Diagnostics;
using OpenTK.Mathematics;

namespace MakeSpline
{
    public class Vector3f
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float W { get; set; }

        public Vector3f(float x, float y, float w)
        {
            X = x;
            Y = y;
            W = w;
        }

        public Vector3f(float x, float y)
        {
            X = x;
            Y = y;
            W = 1f;
        }
    }
    public enum SplineTypes
    {
        BEZIER,
        BEZIER2,
        BEZIER3,
        B_SPLINE,
        NURBS
    }
    public interface ISpline
    {
        // Вычисленные точки сплайна
        public ReadOnlyCollection<Vector3f> S { get; }
        // Число контрольных точек
        public int N { get;}
        //  Степень сплайна
        public int M { get; set; }
        // Вычислить значения сплайна по контрольным точкам, возвращает время в миллисекундах
        public long MakeSpline();
    }

    // Сплайн Безье со степенью n - 1
    public class Bezier_spline : ISpline
    {
        List<Vector3f> s;
        IList<Vector3f> p; // контрольные точки и веса
        public int M { get { return N - 1; } set { } }
        public int N { get { return p.Count; } }
        public ReadOnlyCollection<Vector3f> S { get { return s.AsReadOnly(); } }
        public Bezier_spline(IList<Vector3f> p)
        {
            this.p = p;
            s = new List<Vector3f>();
        }
        // Формула стирлинга с добавкой
        // Источник: статья на хабре от GomelHawk
        private float factorial(int n)
        {
            if (n == 0) return 1;
            float left = MathF.Sqrt(2 * MathF.PI * n) * MathF.Pow(n / MathF.E, n);
            float right = 1 + 1f / (MathF.Sqrt(52 * MathF.E) * n);
            return left * right;
        }

        private float calculate_b(int k, float t)
        {
            float fact_n = factorial(M);
            float fact_k = factorial(k);
            float fact_n_k = factorial(M - k);
            int c = (int)(fact_n / (fact_k * fact_n_k));
            float b = c * MathF.Pow(t, k) * MathF.Pow(1 - t, M - k);
            return b;
        }

        public long MakeSpline()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            //if (N > 30) return 0;
            s.Clear();
            if (N < 2) return 0;
            int num = 100 * (N - 1);
            s = Enumerable.Repeat(new Vector3f(0, 0), num).ToList();
            // t пробеагает от 0 до последнего значения вектора T
            float t = 0;
            float h = 1f / (num - 1);
            for (int k = 0; k < num - 1; k++)
            {
                float x = 0; float y = 0;
                t = k * h;
                for (int i = 0; i <= M; i++)
                {
                    float b = calculate_b(i, t);
                    x += p[i].X * b;
                    y += p[i].Y * b;
                }
                s[k] = new Vector3f(x, y);
            }
            s[num - 1] = new Vector3f(p[N - 1].X, p[N - 1].Y);
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }
    }

    // Квадратичный Безье сплайн
    public class Bezier2_spline : ISpline
    {
        List<Vector3f> s;
        IList<Vector3f> p; // контрольные точки и веса
        public int M { get { return 2; } set { } }
        public int N { get { return p.Count;} }
        public ReadOnlyCollection<Vector3f> S { get { return s.AsReadOnly(); } }
        public Bezier2_spline(IList<Vector3f> p)
        {
            this.p = p;
            s = new List<Vector3f>();
        }

        public long MakeSpline()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            s.Clear();
            if (N < 3) return 0;
            // Создание расширенного массива контрольных точек
            List<Vector3f> p_new = new List<Vector3f> ();
            for (int i = 0; i < N; i++)
            {
                if (i != 1 && i % 2 != 0 || i >= 4)
                {
                    float x = 2 * p_new[p_new.Count - 1].X - p_new[p_new.Count - 2].X;
                    float y = 2 * p_new[p_new.Count - 1].Y - p_new[p_new.Count - 2].Y;
                    p_new.Add(new Vector3f(x, y));
                }
                p_new.Add(p[i]);
            }
            int d = 100;
            int count = (p_new.Count - 1) / 2; // количество элементарных кривых
            int num = d * count;
            s = Enumerable.Repeat(new Vector3f(float.MaxValue, float.MaxValue), num).ToList();
            // t пробегает от 0 до последнего значения вектора 1
            float t = 0;
            // ЦИКЛ ПО t
            for (int i = 0; i < count; i++)
            {
                float h = 1f / (d - 1);
                for (int k = 0; k < d; k++)
                {
                    t = k * h;
                    float x = (1 - t) * (1 - t) * p_new[2 * i].X + 2 * t * (1 - t) * p_new[2 * i + 1].X + t * t * p_new[2 * i + 2].X;
                    float y = (1 - t) * (1 - t) * p_new[2 * i].Y + 2 * t * (1 - t) * p_new[2 * i + 1].Y + t * t * p_new[2 * i + 2].Y;
                    s[k + (d - 1) * i] = new Vector3f(x, y);
                }
            }
            // Remove last elems
            while (MathF.Abs(s.Last().X - float.MaxValue) <= 1e-14 && MathF.Abs(s.Last().Y - float.MaxValue) <= 1e-14)
            {
                s.RemoveAt(s.Count - 1);
            }
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }
    }

    // Кубический Безье сплайн
    public class Bezier3_spline : ISpline
    {
        List<Vector3f> s;
        IList<Vector3f> p; // контрольные точки и веса
        public int M { get { return 3; } set { } }
        public int N { get { return p.Count; } }
        public ReadOnlyCollection<Vector3f> S { get { return s.AsReadOnly(); } }
        public Bezier3_spline(IList<Vector3f> p)
        {
            this.p = p;
            s = new List<Vector3f>();
        }

        public long MakeSpline()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            s.Clear();
            if (N < 4) return 0;
            //auto t_start = std::chrono::high_resolution_clock::now();
            // Создание расширенного массива контрольных точек
            List<Vector3f> p_new = new List<Vector3f>();
            for (int i = 0; i < N; i++)
            {
                if (i != 1 && (i >= 4 && i % 2 == 0))
                {
                    float x = 2 * p_new[p_new.Count() - 1].X - p_new[p_new.Count() - 2].X;
                    float y = 2 * p_new[p_new.Count - 1].Y - p_new[p_new.Count - 2].Y;
                    p_new.Add(new Vector3f(x, y));
                }
                p_new.Add(p[i]);
            }
            int d = 100;
            int count = (p_new.Count - 1) / 3; // количество элементарных кривых
            int num = d * count;
            s = Enumerable.Repeat(new Vector3f(float.MaxValue, float.MaxValue), num).ToList();
            // t пробеагает от 0 до последнего значения вектора 1
            float t = 0;
            for (int i = 0; i < count; i++)
            {
                float h = 1f / (d - 1);
                for (int k = 0; k < d; k++)
                {
                    t = k * h;
                    float x = (1 - t) * (1 - t) * (1 - t) * p_new[3 * i].X + 3 * t * (1 - t) * (1 - t) * p_new[3 * i + 1].X + 3 * t * t * (1 - t) * p_new[3 * i + 2].X + t * t * t * p_new[3 * i + 3].X;
                    float y = (1 - t) * (1 - t) * (1 - t) * p_new[3 * i].Y + 3 * t * (1 - t) * (1 - t) * p_new[3 * i + 1].Y + 3 * t * t * (1 - t) * p_new[3 * i + 2].Y + t * t * t * p_new[3 * i + 3].Y;
                    s[k + (d - 1) * i] = new Vector3f(x, y);
                }
            }
            // Remove last elems
            while (MathF.Abs(s.Last().X - float.MaxValue) <= 1e-14 && MathF.Abs(s.Last().Y - float.MaxValue) <= 1e-14)
            {
                s.RemoveAt(s.Count - 1);
            }
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }
    }

    public class B_spline : ISpline
    {
        int m;
        List<int> knots; // вектор параметров
        List<Vector3f> s;
        IList<Vector3f> p; // контрольные точки и веса
        Dictionary<Vector2i, float> b_map;
        public int M { get; set; }
        public int N { get { return p.Count; } }
        public ReadOnlyCollection<Vector3f> S { get { return s.AsReadOnly(); } }

        public B_spline(int m, IList<Vector3f> p)
        {
            this.m = m;
            this.p = p;
            s = new List<Vector3f>();
            b_map = new Dictionary<Vector2i, float>();
            knots = new List<int>();
        }

        private void MakeClampledKnonts()
        {
            knots.Clear();
            knots.Capacity = N + M + 1;
            //сформировать закрытый вектор параметров
            for (int i = 0; i < M + 1; i++)
                knots.Add(0);
            for (int i = 0; i < N - (M + 1); i++)
                knots.Add(i + 1);
            for (int i = N - M; i < N + 1; i++)
                knots.Add(N - M + 1);
        }

        // ФУНКЦИЯ ДЛЯ ПОДСЧЁТА КОЭЭФИЦИЕНТОВ СПЛАЙНА b_i,j(t), 
        private float calculate_b(int i, int j, float t)
        {
            // Проверяем, вычисляли ли мы уже значение b с этими индексами
            if (b_map.ContainsKey(new Vector2i(i, j)))
                return b_map[new Vector2i(i, j)];

            if (j == 0)
            {
                if (t >= knots[i] && t < knots[i + 1])
                    return 1f;
                else
                    return 0f;
            }

            // denominator - знаменатель
            float left_denominator = knots[i + j] - knots[i];
            float right_denominator = (knots[i + j + 1] - knots[i + 1]);
            float left_b = 0;
            float right_b = 0;
            if (left_denominator != 0)
            {
                left_b = (t - knots[i]) / left_denominator * calculate_b(i, j - 1, t);
            }
            if (right_denominator != 0)
            {
                right_b = (knots[i + j + 1] - t) / right_denominator * calculate_b(i + 1, j - 1, t);
            }

            float b = left_b + right_b;
            b_map.Add(new Vector2i(i, j), b);
            return b;
        }

        public long MakeSpline()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            s.Clear();
            if (N <= M) return 0;
            int num = 100 * (N - 1);
            MakeClampledKnonts();
            float h = MathF.Abs(knots.Last() - knots[0]) / (num - 1);
            float t = 0;
            s = Enumerable.Repeat(new Vector3f(0, 0), num).ToList();
            // t пробеагает от 0 до последнего значения вектора T
            for (int k = 0; k < num - 1; k++)
            {
                float x = 0; float y = 0;
                t = k * h;
                for (int i = 0; i < N; i++)
                {
                    float b = calculate_b(i, M, t);
                    x += p[i].X * b;
                    y += p[i].Y * b;
                }
                s[k] = new Vector3f(x, y);
                b_map.Clear();
            }
            s[num - 1] = new Vector3f(p[N - 1].X, p[N - 1].Y);
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }
    }

    public class NURBS : ISpline
    {
        int m;
        List<int> knots; // вектор параметров
        List<Vector3f> s; // точки сплайна
        IList<Vector3f> p; // контрольные точки и веса
        Dictionary<Vector2i, float> b_map;
        public int M { get; set; }
        public int N { get { return p.Count; } }
        public ReadOnlyCollection<Vector3f> S { get { return s.AsReadOnly(); } }

        public NURBS(int m, IList<Vector3f> p)
        {
            this.m = m;
            this.p = p;
            s = new List<Vector3f>();
            b_map = new Dictionary<Vector2i, float>();
            knots = new List<int>();
        }

        private void MakeClampledKnonts()
        {
            knots.Clear();
            knots.Capacity = N + M + 1;
            //сформировать закрытый вектор параметров
            for (int i = 0; i < M + 1; i++)
                knots.Add(0);
            for (int i = 0; i < N - (M + 1); i++)
                knots.Add(i + 1);
            for (int i = N - M; i < N + 1; i++)
                knots.Add(N - M + 1);
        }

        // ФУНКЦИЯ ДЛЯ ПОДСЧЁТА КОЭЭФИЦИЕНТОВ СПЛАЙНА b_i,j(t), 
        private float calculate_b(int i, int j, float t)
        {
            // Проверяем, вычисляли ли мы уже значение b с этими индексами
            if (b_map.ContainsKey(new Vector2i(i, j)))
                return b_map[new Vector2i(i, j)];

            if (j == 0)
            {
                if (t >= knots[i] && t < knots[i + 1])
                    return 1f;
                else
                    return 0f;
            }

            // denominator - знаменатель
            float left_denominator = knots[i + j] - knots[i];
            float right_denominator = (knots[i + j + 1] - knots[i + 1]);
            float left_b = 0;
            float right_b = 0;
            if (left_denominator != 0)
            {
                left_b = (t - knots[i]) / left_denominator * calculate_b(i, j - 1, t);
            }
            if (right_denominator != 0)
            {
                right_b = (knots[i + j + 1] - t) / right_denominator * calculate_b(i + 1, j - 1, t);
            }

            float b = left_b + right_b;
            b_map.Add(new Vector2i(i, j), b);
            return b;
        }

        public long MakeSpline()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            s.Clear();
            if (N <= M) return 0;
            int num = 100 * (N - 1);
            MakeClampledKnonts();
            float h = MathF.Abs(knots.Last() - knots[0]) / (num - 1);
            float t = 0;
            s = Enumerable.Repeat(new Vector3f(0, 0), num).ToList();
            // t пробеагает от 0 до последнего значения вектора T
            for (int k = 0; k < num - 1; k++)
            {
                float x = 0; float y = 0; float denominator = 0;
                t = k * h;
                for (int i = 0; i < N; i++)
                {
                    float b = calculate_b(i, M, t);
                    x += p[i].W * p[i].X * b;
                    y += p[i].W * p[i].Y * b;
                    denominator += p[i].W * b;
                }
                s[k] = new Vector3f(x / denominator, y / denominator);
                b_map.Clear();
            }
            s[num - 1] = new Vector3f(p[N - 1].X, p[N - 1].Y);
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }
    }
}
