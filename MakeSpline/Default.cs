using OpenTK.Mathematics;

namespace MakeSpline
{
    // Значения по умолчанию
    public static class Default
    {
        static public Color4 ColorPoints = new Color4(1f, 0f, 0f, 1f);
        static public float PointsSize = 8f;
        static public Color4 ColorChain  = new Color4(0.4f, 0.4f, 0.4f, 1f);
        static public float ChainWidth  = 2;
        static public bool DrawPointsMode = true;
        static public bool DrawChainMode = true;

        static public Color4 ColorBezierSpline = new Color4(0f, 1f, 1f, 1f);
        static public float BezierSplineWidth = 6;
        static public bool DrawBezierSplineMode = true;

        static public Color4 ColorBezier2Spline = new Color4(0f, 1f, 0f, 1f);
        static public float Bezier2SplineWidth = 5;
        static public bool DrawBezier2SplineMode = false;

        static public Color4 ColorBezier3Spline = new Color4(0f, 0f, 1f, 1f);
        static public float Bezier3SplineWidth = 4;
        static public bool DrawBezier3SplineMode = false;

        static public Color4 ColorBSpline = new Color4(0.5f, 0f, 0.5f, 1f);
        static public float BSplineWidth = 3;
        static public bool DrawBSplineMode = false;

        static public Color4 ColorNURBS = new Color4(1f, 0.55f, 0f, 1f);
        static public float NURBSWidth = 2;
        static public bool DrawNURBSMode = false;
    }
}
