using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// A custom VisualElement that renders an ability icon clipped to a diamond shape,
/// a cooldown overlay, and a diamond border.
///
/// FillFromTop = false (default, regular cooldowns):
///   CooldownT = 1 → dark overlay covers the entire diamond (ability just used).
///   As CooldownT decreases toward 0, the dark overlay retreats DOWNWARD from the top,
///   revealing the bright icon from the top down... 
///
///   WAIT — user wants bright reveal from BOTTOM UP.
///   So: dark overlay covers only the TOP portion (unrecharged area).
///   The BOTTOM portion is left clear (icon shows through = bright).
///   As CooldownT decreases, the dark shrinks downward toward the bottom tip,
///   so the bright area grows upward from the bottom. ✓
///
/// FillFromTop = true (flight energy bar):
///   Dark overlay grows DOWNWARD from the top point as energy depletes.
///   CooldownT = 1f - energyRatio.
/// </summary>
[UxmlElement]
public partial class DiamondAbilityElement : VisualElement
{
    // ------------------------------------------------------------------ //
    //  Public properties                                                  //
    // ------------------------------------------------------------------ //

    private Texture2D _icon;
    public Texture2D Icon
    {
        get => _icon;
        set { _icon = value; MarkDirtyRepaint(); }
    }

    private float _cooldownT;
    public float CooldownT
    {
        get => _cooldownT;
        set { _cooldownT = Mathf.Clamp01(value); MarkDirtyRepaint(); }
    }

    // Custom style property descriptors read from USS (--border-color, etc.)
    private static readonly CustomStyleProperty<Color> s_BorderColor  = new("--border-color");
    private static readonly CustomStyleProperty<float> s_BorderWidth  = new("--border-width");
    private static readonly CustomStyleProperty<Color> s_OverlayColor = new("--overlay-color");

    private Color _borderColor = Color.white;
    public Color BorderColor
    {
        get => _borderColor;
        set { _borderColor = value; MarkDirtyRepaint(); }
    }

    private float _borderWidth = 2f;
    public float BorderWidth
    {
        get => _borderWidth;
        set { _borderWidth = value; MarkDirtyRepaint(); }
    }

    private Color _overlayColor = new Color(0f, 0f, 0f, 0.6f);
    public Color OverlayColor
    {
        get => _overlayColor;
        set { _overlayColor = value; MarkDirtyRepaint(); }
    }

    private bool _fillFromTop = false;
    public bool FillFromTop
    {
        get => _fillFromTop;
        set { _fillFromTop = value; MarkDirtyRepaint(); }
    }

    // ------------------------------------------------------------------ //
    //  Constructor                                                        //
    // ------------------------------------------------------------------ //
    public DiamondAbilityElement()
    {
        generateVisualContent += OnGenerateVisualContent;
        RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
    }

    private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
    {
        if (customStyle.TryGetValue(s_BorderColor,  out var borderColor))  _borderColor  = borderColor;
        if (customStyle.TryGetValue(s_BorderWidth,  out var borderWidth))  _borderWidth  = borderWidth;
        if (customStyle.TryGetValue(s_OverlayColor, out var overlayColor)) _overlayColor = overlayColor;
        MarkDirtyRepaint();
    }

    // ------------------------------------------------------------------ //
    //  Drawing                                                            //
    // ------------------------------------------------------------------ //
    private void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        float w = resolvedStyle.width;
        float h = resolvedStyle.height;
        if (w <= 0 || h <= 0) return;

        float pad = _borderWidth * 0.5f + 1f;

        Vector2 top    = new Vector2(w * 0.5f, pad);
        Vector2 right  = new Vector2(w - pad,  h * 0.5f);
        Vector2 bottom = new Vector2(w * 0.5f, h - pad);
        Vector2 left   = new Vector2(pad,      h * 0.5f);

        var painter = ctx.painter2D;

        // ---- 1. Icon ---- //
        if (_icon != null)
        {
            DrawIconMesh(ctx, top, right, bottom, left, w, h);
        }
        else
        {
            painter.fillColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            painter.BeginPath();
            painter.MoveTo(top);
            painter.LineTo(right);
            painter.LineTo(bottom);
            painter.LineTo(left);
            painter.ClosePath();
            painter.Fill();
        }

        // ---- 2. Cooldown overlay ---- //
        if (_cooldownT > 0.001f)
        {
            if (_fillFromTop)
            {
                // Flight energy: dark wedge grows downward from the top as energy depletes.
                DrawWedgeFromTop(painter, top, right, bottom, left, _cooldownT, _overlayColor);
            }
            else
            {
                // Regular cooldown:
                // The dark overlay covers ONLY the upper (unrecharged) portion of the diamond.
                // The lower portion is left undrawn so the icon shows through bright.
                // As CooldownT goes from 1→0, the dark wedge shrinks downward toward the
                // bottom tip — so the bright area grows upward from the bottom. ✓
                DrawWedgeFromTop(painter, top, right, bottom, left, _cooldownT, _overlayColor);
            }
        }

        // ---- 3. Border ---- //
        painter.strokeColor = _borderColor;
        painter.lineWidth   = _borderWidth;
        painter.BeginPath();
        painter.MoveTo(top);
        painter.LineTo(right);
        painter.LineTo(bottom);
        painter.LineTo(left);
        painter.ClosePath();
        painter.Stroke();
    }

    // ------------------------------------------------------------------ //
    //  Wedge: grows DOWNWARD from the top point                          //
    //  t=0 → nothing. t=1 → full diamond.                               //
    // ------------------------------------------------------------------ //
    private void DrawWedgeFromTop(Painter2D painter, Vector2 top, Vector2 right,
        Vector2 bottom, Vector2 left, float t, Color color)
    {
        if (t <= 0.001f) return;

        if (t >= 1f)
        {
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(top);
            painter.LineTo(right);
            painter.LineTo(bottom);
            painter.LineTo(left);
            painter.ClosePath();
            painter.Fill();
            return;
        }

        // cutY moves downward from top.y toward bottom.y as t increases.
        // At t=1: cutY = bottom.y → full diamond dark.
        // At t=0: cutY = top.y   → nothing dark.
        float diamondH = bottom.y - top.y;
        float cutY     = top.y + (t * diamondH);
        float midY     = (top.y + bottom.y) * 0.5f;

        painter.fillColor = color;
        painter.BeginPath();

        if (cutY <= midY)
        {
            // Dark wedge is entirely in the upper triangle.
            float   tEdge = (cutY - top.y) / (midY - top.y);
            Vector2 rEdge = Vector2.Lerp(top, right, tEdge);
            Vector2 lEdge = Vector2.Lerp(top, left,  tEdge);

            painter.MoveTo(top);
            painter.LineTo(rEdge);
            painter.LineTo(lEdge);
            painter.ClosePath();
        }
        else
        {
            // Dark wedge crosses midline into lower triangle.
            float   tEdge = (cutY - midY) / (bottom.y - midY);
            Vector2 rEdge = Vector2.Lerp(right, bottom, tEdge);
            Vector2 lEdge = Vector2.Lerp(left,  bottom, tEdge);

            painter.MoveTo(top);
            painter.LineTo(right);
            painter.LineTo(rEdge);
            painter.LineTo(lEdge);
            painter.LineTo(left);
            painter.ClosePath();
        }

        painter.Fill();
    }

    // ------------------------------------------------------------------ //
    //  Icon mesh                                                          //
    // ------------------------------------------------------------------ //
    private void DrawIconMesh(MeshGenerationContext ctx, Vector2 top, Vector2 right,
        Vector2 bottom, Vector2 left, float w, float h)
    {
        var mesh = ctx.Allocate(4, 6, _icon);

        Vertex MakeVertex(Vector2 pos) => new Vertex
        {
            position = new Vector3(pos.x, pos.y, Vertex.nearZ),
            tint     = Color.white,
            uv       = new Vector2(pos.x / w, 1f - (pos.y / h))
        };

        mesh.SetNextVertex(MakeVertex(top));
        mesh.SetNextVertex(MakeVertex(right));
        mesh.SetNextVertex(MakeVertex(bottom));
        mesh.SetNextVertex(MakeVertex(left));

        mesh.SetNextIndex(0); mesh.SetNextIndex(1); mesh.SetNextIndex(3);
        mesh.SetNextIndex(1); mesh.SetNextIndex(2); mesh.SetNextIndex(3);
    }
}