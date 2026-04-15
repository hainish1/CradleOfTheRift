using UnityEngine;
using UnityEngine.UIElements;

public enum SwapEffectType { None, RiftCrackle, ArcaneSpin, ManaPulse }

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
    private static readonly CustomStyleProperty<Color> s_EdgeColor      = new("--edge-color");
    private static readonly CustomStyleProperty<float> s_EdgeWidth      = new("--edge-width");
    private static readonly CustomStyleProperty<Color> s_BackgroundColor = new("--background-color");


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

    private Color _edgeColor = new Color(1f, 1f, 1f, 0.9f);
    public Color EdgeColor
    {
        get => _edgeColor;
        set { _edgeColor = value; MarkDirtyRepaint(); }
    }

    private float _edgeWidth = 2f;
    public float EdgeWidth
    {
        get => _edgeWidth;
        set { _edgeWidth = value; MarkDirtyRepaint(); }
    }

    private Color _backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set { _backgroundColor = value; MarkDirtyRepaint(); }
    }

    private bool _fillFromTop = false;
    public bool FillFromTop
    {
        get => _fillFromTop;
        set { _fillFromTop = value; MarkDirtyRepaint(); }
    }

    private float _iconScale = 1.5f;
    public float IconScale
    {
        get => _iconScale;
        set { _iconScale = value; MarkDirtyRepaint(); }
    }

    private Vector2 _iconOffset = Vector2.zero;
    public Vector2 IconOffset
    {
        get => _iconOffset;
        set { _iconOffset = value; MarkDirtyRepaint(); }
    }

    // Effect Logic
    private SwapEffectType _activeEffect = SwapEffectType.None;
    private float _effectIntensity = 0f;

    public DiamondAbilityElement()
    {
        generateVisualContent += OnGenerateVisualContent;
        RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
    }

    public void TriggerEffect(SwapEffectType effect, float durationMS)
    {
        _activeEffect = effect;
        this.experimental.animation.Start(1f, 0f, (int)durationMS, (element, val) => {
            _effectIntensity = val;
            MarkDirtyRepaint();
        }).OnCompleted(() => {
            _activeEffect = SwapEffectType.None;
            MarkDirtyRepaint();
        });
    }

    private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
    {
        if (customStyle.TryGetValue(s_BorderColor,     out var borderColor))     _borderColor     = borderColor;
        if (customStyle.TryGetValue(s_BorderWidth,     out var borderWidth))     _borderWidth     = borderWidth;
        if (customStyle.TryGetValue(s_OverlayColor,    out var overlayColor))    _overlayColor    = overlayColor;
        if (customStyle.TryGetValue(s_EdgeColor,       out var edgeColor))       _edgeColor       = edgeColor;
        if (customStyle.TryGetValue(s_EdgeWidth,       out var edgeWidth))       _edgeWidth       = edgeWidth;
        if (customStyle.TryGetValue(s_BackgroundColor, out var backgroundColor)) _backgroundColor = backgroundColor;
        MarkDirtyRepaint();
    }

    private void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        float w = resolvedStyle.width;
        float h = resolvedStyle.height;
        if (w <= 0 || h <= 0) return;

        float pad = _borderWidth * 0.5f + 1f;

        Vector2 top = new Vector2(w * 0.5f, pad);
        Vector2 right = new Vector2(w - pad, h * 0.5f);
        Vector2 bottom = new Vector2(w * 0.5f, h - pad);
        Vector2 left = new Vector2(pad, h * 0.5f);

        var painter = ctx.painter2D;

        // ---- Background fill (always drawn) ---- //
        painter.fillColor = _backgroundColor;
        painter.BeginPath();
        painter.MoveTo(top);
        painter.LineTo(right);
        painter.LineTo(bottom);
        painter.LineTo(left);
        painter.ClosePath();
        painter.Fill();
        
        // ---- Icon ---- //
        if (_icon != null)
        {
            DrawIconMesh(ctx, top, right, bottom, left, w, h);
        }

        // ---- Cooldown overlay ---- //
        if (_cooldownT > 0.001f)
        {
            if (_fillFromTop)
            {
                // Flight energy: dark wedge grows downward from the top as energy depletes.
                DrawWedgeFromTop(painter, top, right, bottom, left, _cooldownT, _overlayColor, _edgeColor, _edgeWidth);
            }
            else
            {
                // Regular cooldown:
                // The dark overlay covers ONLY the upper (unrecharged) portion of the diamond.
                // The lower portion is left undrawn so the icon shows through bright.
                // As CooldownT goes from 1→0, the dark wedge shrinks downward toward the
                // bottom tip — so the bright area grows upward from the bottom. ✓
                DrawWedgeFromTop(painter, top, right, bottom, left, _cooldownT, _overlayColor, _edgeColor, _edgeWidth);
            }
        }

        // ---- Border ---- //
        painter.strokeColor = _borderColor;
        painter.lineWidth = _borderWidth;
        painter.BeginPath();
        painter.MoveTo(top);
        painter.LineTo(right);
        painter.LineTo(bottom);
        painter.LineTo(left);
        painter.ClosePath();
        painter.Stroke();

        // Draw Fantasy Effects
        if (_activeEffect != SwapEffectType.None && _effectIntensity > 0)
        {
            HandleFantasyEffects(painter, w, h);
        }
    }

    private void HandleFantasyEffects(Painter2D painter, float w, float h)
    {
        Vector2 center = new Vector2(w * 0.5f, h * 0.5f);

        switch (_activeEffect)
        {
            case SwapEffectType.RiftCrackle:
                painter.strokeColor = new Color(0.4f, 0.9f, 1f, _effectIntensity);
                painter.lineWidth = 1.5f;
                for (int i = 0; i < 3; i++)
                {
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(w * 0.5f + Random.Range(-8, 8), Random.Range(-8, 8)));
                    painter.LineTo(new Vector2(w + Random.Range(-8, 8), h * 0.5f + Random.Range(-8, 8)));
                    painter.LineTo(new Vector2(w * 0.5f + Random.Range(-8, 8), h + Random.Range(-8, 8)));
                    painter.LineTo(new Vector2(Random.Range(-8, 8), h * 0.5f + Random.Range(-8, 8)));
                    painter.ClosePath();
                    painter.Stroke();
                }
                break;

            case SwapEffectType.ArcaneSpin:
                // FIX: Matrix transformations are handled via the contentMatrix in Painter2D
                float angle = (1f - _effectIntensity) * 180f;
                float s = (w * 0.5f) * 0.8f;
                
                // Save and Restore are actually called SaveContentMatrix and RestoreContentMatrix
                // However, UI Toolkit also supports a matrix approach
                Matrix4x4 rotationMatrix = Matrix4x4.TRS(center, Quaternion.Euler(0, 0, angle), Vector3.one) * Matrix4x4.Translate(-center);
                
                // Note: painter2D uses Save/Restore or similar if using specific versions, 
                // but setting the matrix directly is the most compatible way
                painter.strokeColor = new Color(1f, 0.85f, 0.3f, _effectIntensity);
                painter.lineWidth = 2f;
                
                // Drawing the rotating diamond
                painter.BeginPath();
                Vector2 p1 = rotationMatrix.MultiplyPoint3x4(new Vector2(center.x, center.y - s));
                Vector2 p2 = rotationMatrix.MultiplyPoint3x4(new Vector2(center.x + s, center.y));
                Vector2 p3 = rotationMatrix.MultiplyPoint3x4(new Vector2(center.x, center.y + s));
                Vector2 p4 = rotationMatrix.MultiplyPoint3x4(new Vector2(center.x - s, center.y));
                
                painter.MoveTo(p1);
                painter.LineTo(p2);
                painter.LineTo(p3);
                painter.LineTo(p4);
                painter.ClosePath();
                painter.Stroke();
                break;

            case SwapEffectType.ManaPulse:
                float burst = (w * 0.5f) + (20f * (1f - _effectIntensity));
                painter.fillColor = new Color(0.6f, 0.3f, 1f, _effectIntensity * 0.4f);
                painter.BeginPath();
                painter.MoveTo(new Vector2(center.x, center.y - burst));
                painter.LineTo(new Vector2(center.x + burst, center.y));
                painter.LineTo(new Vector2(center.x, center.y + burst));
                painter.LineTo(new Vector2(center.x - burst, center.y));
                painter.ClosePath();
                painter.Fill();
                break;
        }
    }

    // ------------------------------------------------------------------ //
    //  Wedge: grows DOWNWARD from the top point                          //
    //  t=0 → nothing. t=1 → full diamond.                               //
    // ------------------------------------------------------------------ //
    private void DrawWedgeFromTop(Painter2D painter, Vector2 top, Vector2 right,
    Vector2 bottom, Vector2 left, float t, Color color, Color edgeColor, float edgeWidth)
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
            return; // no edge line when fully covered
        }

        float diamondH = bottom.y - top.y;
        float cutY = top.y + (t * diamondH);
        float midY = (top.y + bottom.y) * 0.5f;

        painter.fillColor = color;
        painter.BeginPath();

        Vector2 rEdge, lEdge;

        if (cutY <= midY)
        {
            float tEdge = (cutY - top.y) / (midY - top.y);
            rEdge = Vector2.Lerp(top, right, tEdge);
            lEdge = Vector2.Lerp(top, left, tEdge);

            painter.MoveTo(top);
            painter.LineTo(rEdge);
            painter.LineTo(lEdge);
            painter.ClosePath();
        }
        else
        {
            float tEdge = (cutY - midY) / (bottom.y - midY);
            rEdge = Vector2.Lerp(right, bottom, tEdge);
            lEdge = Vector2.Lerp(left, bottom, tEdge);

            painter.MoveTo(top);
            painter.LineTo(right);
            painter.LineTo(rEdge);
            painter.LineTo(lEdge);
            painter.LineTo(left);
            painter.ClosePath();
        }

        painter.Fill();

        // ---- Edge line at the cooldown boundary ---- //
        painter.strokeColor = _edgeColor;
        painter.lineWidth = _edgeWidth;
        painter.BeginPath();
        painter.MoveTo(lEdge);
        painter.LineTo(rEdge);
        painter.Stroke();
    }

    private void DrawIconMesh(MeshGenerationContext ctx, Vector2 top, Vector2 right,
        Vector2 bottom, Vector2 left, float w, float h)
    {
        // The inner axis-aligned square inscribed in the diamond.
        // Its corners are at the midpoints between the diamond's vertices.
        float cx = w * 0.5f + _iconOffset.x;
        float cy = h * 0.5f + IconOffset.y;
        float halfSize = (right.x - left.x) * 0.5f * 0.5f; // half the diamond width / 2

        float scaledHalf = halfSize * _iconScale;

        Vector2 tlCorner = new Vector2(cx - scaledHalf, cy - scaledHalf); // top-left
        Vector2 trCorner = new Vector2(cx + scaledHalf, cy - scaledHalf); // top-right
        Vector2 brCorner = new Vector2(cx + scaledHalf, cy + scaledHalf); // bottom-right
        Vector2 blCorner = new Vector2(cx - scaledHalf, cy + scaledHalf); // bottom-left

        var mesh = ctx.Allocate(4, 6, _icon);

        Vertex MakeVertex(Vector2 pos, Vector2 uv) => new Vertex
        {
            position = new Vector3(pos.x, pos.y, Vertex.nearZ),
            tint = Color.white,
            uv = uv
        };

        mesh.SetNextVertex(MakeVertex(tlCorner, new Vector2(0, 1)));
        mesh.SetNextVertex(MakeVertex(trCorner, new Vector2(1, 1)));
        mesh.SetNextVertex(MakeVertex(brCorner, new Vector2(1, 0)));
        mesh.SetNextVertex(MakeVertex(blCorner, new Vector2(0, 0)));

        mesh.SetNextIndex(0); mesh.SetNextIndex(1); mesh.SetNextIndex(2);
        mesh.SetNextIndex(0); mesh.SetNextIndex(2); mesh.SetNextIndex(3);
    }
}