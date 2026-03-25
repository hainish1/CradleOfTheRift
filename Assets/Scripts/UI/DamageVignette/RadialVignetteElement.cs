using UnityEngine;
using UnityEngine.UIElements;

public class RadialVignetteElement : VisualElement
{
    public Color vignetteColor = new Color(0.6f, 0f, 0f, 1f);

    // How far from the edge the vignette starts (0 = full screen, 1 = edges only)
    public float innerRadiusFraction = 0.35f;

    // Controls how sharply the vignette falls off toward the center. Higher = more concentrated at edges
    public float falloffPower = 2f;

    public RadialVignetteElement()
    {
        generateVisualContent += OnGenerateVisualContent;

        // Fill the screen, ignore mouse input
        style.position = Position.Absolute;
        style.left = 0; style.top = 0;
        style.right = 0; style.bottom = 0;
        style.width = new StyleLength(new Length(100, LengthUnit.Percent));
        style.height = new StyleLength(new Length(100, LengthUnit.Percent));
        pickingMode = PickingMode.Ignore;
    }

    private void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        var painter = ctx.painter2D;
        Rect r = contentRect;

        float cx = r.width  * 0.5f;
        float cy = r.height * 0.5f;

        // Use enough rings to keep the gradient smooth
        int rings = 40;
        float outerRadius = Mathf.Sqrt(cx * cx + cy * cy); // reaches all corners
        float innerRadius = outerRadius * Mathf.Clamp01(innerRadiusFraction); // clear center

        for (int i = 0; i < rings; i++)
        {
            float t0 = (float) i / rings;
            float t1 = (float)(i + 1) / rings;

            float r0 = Mathf.Lerp(innerRadius, outerRadius, t0);
            float r1 = Mathf.Lerp(innerRadius, outerRadius, t1);

            // Ease-in so the edge is dense and the center stays clear
            float a0 = Mathf.Pow(t0, falloffPower) * vignetteColor.a;
            float a1 = Mathf.Pow(t1, falloffPower) * vignetteColor.a;

            Color c0 = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, a0);
            Color c1 = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, a1);

            int segments = 64;
            float step = 360f / segments;

            for (int s = 0; s < segments; s++)
            {
                float angleA = Mathf.Deg2Rad * (s * step);
                float angleB = Mathf.Deg2Rad * ((s + 1) * step);

                // Quad: two triangles forming a ring slice
                painter.fillColor = c0;

                Vector2 p0 = new Vector2(cx + Mathf.Cos(angleA) * r0, cy + Mathf.Sin(angleA) * r0);
                Vector2 p1 = new Vector2(cx + Mathf.Cos(angleB) * r0, cy + Mathf.Sin(angleB) * r0);
                Vector2 p2 = new Vector2(cx + Mathf.Cos(angleB) * r1, cy + Mathf.Sin(angleB) * r1);
                Vector2 p3 = new Vector2(cx + Mathf.Cos(angleA) * r1, cy + Mathf.Sin(angleA) * r1);

                // Triangle 1
                painter.BeginPath();
                painter.MoveTo(p0); painter.LineTo(p1); painter.LineTo(p2);
                painter.ClosePath();
                painter.Fill();

                // Triangle 2 (outer color)
                painter.fillColor = c1;
                painter.BeginPath();
                painter.MoveTo(p0); painter.LineTo(p2); painter.LineTo(p3);
                painter.ClosePath();
                painter.Fill();
            }
        }
    }
}