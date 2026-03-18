using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// A custom VisualElement that renders an ability icon clipped to a diamond shape,
/// a cooldown overlay, and a diamond border.
///
/// For regular abilities (cooldown) — FillFromTop = false (default):
///   CooldownT = 0 → ready, no overlay.
///   CooldownT = 1 → on cooldown, overlay starts full and drains upward from the bottom point.
///
/// For the flight/energy ability — FillFromTop = true:
///   CooldownT = 0 → full energy, no overlay.
///   CooldownT = 1 → empty, overlay fills downward from the top point.
///   Drive it with: CooldownT = 1f - playerMovement.FlightEnergyRatio
///
/// Usage:
///   var diamond = new DiamondAbilityElement();
///   diamond.Icon         = myTexture2D;
///   diamond.CooldownT    = 0f;
///   diamond.FillFromTop  = false;       // true for flight energy bar
///   diamond.BorderColor  = Color.white;
///   diamond.BorderWidth  = 2f;
///   diamond.OverlayColor = new Color(0, 0, 0, 0.6f);
/// </summary>
[UxmlElement]
public partial class DiamondAbilityElement : VisualElement
{
    public new class UxmlFactory : UxmlFactory<DiamondAbilityElement, UxmlTraits> { }
    public new class UxmlTraits : VisualElement.UxmlTraits { }

    // ------------------------------------------------------------------ //
    //  Public properties – each setter triggers a repaint                 //
    // ------------------------------------------------------------------ //

    private Texture2D _icon;
    public Texture2D Icon
    {
        get => _icon;
        set { _icon = value; MarkDirtyRepaint(); }
    }

    /// <summary>
    /// 0 = ability ready (no overlay), 1 = fully covered by cooldown.
    /// The dark overlay starts full and drains upward from the bottom point.
    /// </summary>
    private float _cooldownT;
    public float CooldownT
    {
        get => _cooldownT;
        set { _cooldownT = Mathf.Clamp01(value); MarkDirtyRepaint(); }
    }

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

    /// <summary>
    /// When false (default): overlay drains upward from the bottom point — used for cooldowns.
    /// When true: overlay grows downward from the top point — used for the flight energy bar.
    /// </summary>
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
    }

    // ------------------------------------------------------------------ //
    //  Drawing                                                            //
    // ------------------------------------------------------------------ //
    private void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        float w = resolvedStyle.width;
        float h = resolvedStyle.height;
        if (w <= 0 || h <= 0) return;

        // Inset so the stroke is never clipped by the element boundary.
        float pad = _borderWidth * 0.5f + 1f;

        Vector2 top    = new Vector2(w * 0.5f, pad);
        Vector2 right  = new Vector2(w - pad,  h * 0.5f);
        Vector2 bottom = new Vector2(w * 0.5f, h - pad);
        Vector2 left   = new Vector2(pad,      h * 0.5f);

        var painter = ctx.painter2D;

        // ---- 1. Icon — tessellated diamond mesh so the texture is naturally clipped ---- //
        // We allocate a textured mesh (4 verts, 2 tris) whose positions match the diamond
        // vertices exactly.  UV coordinates map each vertex to its proportional position
        // within the full element rect, giving a centred ScaleAndCrop-style fill.
        if (_icon != null)
        {
            DrawIconMesh(ctx, top, right, bottom, left, w, h);
        }
        else
        {
            // Fallback: dark fill so the shape is visible when no icon is assigned.
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
            // FillFromTop = false → cooldown: overlay drains upward from the bottom point
            // FillFromTop = true  → flight:   overlay fills downward from the top point
            if (_fillFromTop)
                DrawOverlayFromTop(painter, top, right, bottom, left);
            else
                DrawOverlayFromBottom(painter, top, right, bottom, left);
        }

        // ---- 3. Diamond border stroke ---- //
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

    /// <summary>
    /// Overlay starts full and drains upward from the bottom vertex — used for regular cooldowns (FillFromTop = false).
    /// At CooldownT=1 the whole diamond is covered. As CooldownT decreases toward 0,
    /// the overlay shrinks down toward the bottom point and disappears.
    /// </summary>
    private void DrawOverlayFromBottom(
        Painter2D painter,
        Vector2 top, Vector2 right, Vector2 bottom, Vector2 left)
    {
        if (_cooldownT >= 1f)
        {
            // Full coverage – whole diamond.
            painter.fillColor = _overlayColor;
            painter.BeginPath();
            painter.MoveTo(top);
            painter.LineTo(right);
            painter.LineTo(bottom);
            painter.LineTo(left);
            painter.ClosePath();
            painter.Fill();
            return;
        }

        // fillY is the top-edge of the remaining overlay band.
        // At CooldownT=1: fillY = top.y    → whole diamond covered.
        // At CooldownT=0: fillY = bottom.y → nothing covered.
        // As CooldownT decreases, fillY moves DOWN toward bottom.y,
        // so the overlay shrinks from the top — leaving only the bottom portion visible.
        float diamondH   = bottom.y - top.y;
        float fillHeight = _cooldownT * diamondH;
        float fillY      = bottom.y - fillHeight;  // top-edge of remaining overlay
        float midY       = (top.y + bottom.y) * 0.5f;

        painter.fillColor = _overlayColor;
        painter.BeginPath();

        if (fillY >= midY)
        {
            // Remaining overlay is only within the lower triangle (bottom point only).
            // tEdge=0 → cut at midY (widest point), tEdge=1 → cut at bottom tip.
            float tEdge   = (bottom.y - fillY) / (bottom.y - midY);
            Vector2 rEdge = Vector2.Lerp(bottom, right, tEdge);
            Vector2 lEdge = Vector2.Lerp(bottom, left,  tEdge);

            painter.MoveTo(lEdge);
            painter.LineTo(rEdge);
            painter.LineTo(bottom);
            painter.ClosePath();
        }
        else
        {
            // Remaining overlay covers the full lower triangle + partial upper triangle.
            // tEdge=0 → cut at midY, tEdge=1 → cut at top tip.
            float tEdge   = (midY - fillY) / (midY - top.y);
            Vector2 rEdge = Vector2.Lerp(right, top, tEdge);
            Vector2 lEdge = Vector2.Lerp(left,  top, tEdge);

            painter.MoveTo(lEdge);
            painter.LineTo(rEdge);
            painter.LineTo(right);
            painter.LineTo(bottom);
            painter.LineTo(left);
            painter.ClosePath();
        }

        painter.Fill();
    }

    /// <summary>
    /// Overlay grows downward from the top vertex — used for the flight energy bar (FillFromTop = true).
    /// </summary>
    private void DrawOverlayFromTop(
        Painter2D painter,
        Vector2 top, Vector2 right, Vector2 bottom, Vector2 left)
    {
        if (_cooldownT >= 1f)
        {
            painter.fillColor = _overlayColor;
            painter.BeginPath();
            painter.MoveTo(top);
            painter.LineTo(right);
            painter.LineTo(bottom);
            painter.LineTo(left);
            painter.ClosePath();
            painter.Fill();
            return;
        }

        // fillY is the bottom-edge of the filled band, growing downward from top.y.
        float diamondH   = bottom.y - top.y;
        float fillHeight = _cooldownT * diamondH;
        float fillY      = top.y + fillHeight;   // bottom-edge of the filled band
        float midY       = (top.y + bottom.y) * 0.5f;

        painter.fillColor = _overlayColor;
        painter.BeginPath();

        if (fillY <= midY)
        {
            // Fill is entirely within the upper triangle (top → right → left).
            float tEdge = (fillY - top.y) / (midY - top.y);
            Vector2 rEdge = Vector2.Lerp(top, right, tEdge);
            Vector2 lEdge = Vector2.Lerp(top, left,  tEdge);

            painter.MoveTo(top);
            painter.LineTo(rEdge);
            painter.LineTo(lEdge);
            painter.ClosePath();
        }
        else
        {
            // Fill crosses the midline: upper triangle fully covered + partial lower triangle.
            float tEdge = (fillY - midY) / (bottom.y - midY);
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

    /// <summary>
    /// Allocates a raw textured mesh for the icon, shaped exactly to the diamond.
    /// This avoids needing Clip()/SaveState()/DrawTexture() which are Unity 2023.2+ only.
    ///
    /// The diamond is split into two triangles sharing the left/right edge:
    ///   Tri 0:  top   → right → left
    ///   Tri 1:  right → bottom → left
    ///
    /// UV for each vertex is its normalised position within the element rect, so the
    /// texture fills the diamond the same way ScaleToFill would across the full rect,
    /// but only the diamond pixels are rendered.
    /// </summary>
    private void DrawIconMesh(
        MeshGenerationContext ctx,
        Vector2 top, Vector2 right, Vector2 bottom, Vector2 left,
        float w, float h)
    {
        // 4 vertices, 6 indices (2 triangles × 3 indices)
        var mesh = ctx.Allocate(4, 6, _icon);

        // Helper: build a Vertex with UV derived from position in the element rect.
        Vertex MakeVertex(Vector2 pos)
        {
            return new Vertex
            {
                position = new Vector3(pos.x, pos.y, Vertex.nearZ),
                tint     = Color.white,
                uv       = new Vector2(pos.x / w, 1f - (pos.y / h))
            };
        }

        // Vertex order:  0=top, 1=right, 2=bottom, 3=left
        mesh.SetNextVertex(MakeVertex(top));
        mesh.SetNextVertex(MakeVertex(right));
        mesh.SetNextVertex(MakeVertex(bottom));
        mesh.SetNextVertex(MakeVertex(left));

        // Triangle 0: top(0) → right(1) → left(3)
        // Triangle 1: right(1) → bottom(2) → left(3)
        mesh.SetNextIndex(0); mesh.SetNextIndex(1); mesh.SetNextIndex(3);
        mesh.SetNextIndex(1); mesh.SetNextIndex(2); mesh.SetNextIndex(3);
    }
}