using UnityEngine;
using UnityEngine.UIElements;

public enum SwapEffectType { 
    None, 
    RiftCrackle,
    ArcaneSpin,
     ManaPulse,
    PixieDust,       
    MoonbloomPetal, 
    FaeRipple,
    ChronoShift,
    EmberBurst,
    AbyssalBloom,
    StormSurge,
    GlitchRemnant,
    NebulaOverload,
    RiftShatter,
    SingularityPulse
}

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
        float pad = _borderWidth * 0.5f + 1f;

        // Define vertices locally so effects can reference them
        Vector2 top = new Vector2(w * 0.5f, pad);
        Vector2 right = new Vector2(w - pad, h * 0.5f);
        Vector2 bottom = new Vector2(w * 0.5f, h - pad);
        Vector2 left = new Vector2(pad, h * 0.5f);

        switch (_activeEffect)
        {
            case SwapEffectType.RiftCrackle:
            {
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
            }
            
            case SwapEffectType.ArcaneSpin:
            {
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
            }

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
            
            case SwapEffectType.PixieDust:
            {
                // Seed the positions once per TriggerEffect call so sparks don't
                // jitter each repaint.  We use a simple deterministic pattern
                // driven by the intensity bucket rather than Random each frame.
                int sparkCount = 14;
                float progress = 1f - _effectIntensity;   // 0 = just triggered, 1 = done

                for (int i = 0; i < sparkCount; i++)
                {
                    // Each spark has a fixed angle and a distance that grows with progress.
                    float angle = (i / (float)sparkCount) * Mathf.PI * 2f
                                + i * 0.31f;            // slight golden-ratio twist
                    float maxDist = (w * 0.55f);
                    float dist = maxDist * progress;

                    // Alternate warm gold / soft pink / white
                    Color sparkColor = (i % 3) switch
                    {
                        0 => new Color(1f,   0.88f, 0.35f, _effectIntensity),   // gold
                        1 => new Color(0.98f, 0.63f, 0.78f, _effectIntensity),  // pink
                        _ => new Color(1f,   1f,    1f,    _effectIntensity * 0.9f),
                    };

                    float sparkSize = Mathf.Lerp(4f, 1.2f, progress);
                    float sx = center.x + Mathf.Cos(angle) * dist;
                    float sy = center.y + Mathf.Sin(angle) * dist;

                    painter.fillColor = sparkColor;
                    painter.BeginPath();
                    // Draw a tiny diamond for each spark (more thematic than a circle)
                    painter.MoveTo(new Vector2(sx,              sy - sparkSize));
                    painter.LineTo(new Vector2(sx + sparkSize,  sy));
                    painter.LineTo(new Vector2(sx,              sy + sparkSize));
                    painter.LineTo(new Vector2(sx - sparkSize,  sy));
                    painter.ClosePath();
                    painter.Fill();
                }

                // Central flash that fades quickly
                painter.fillColor = new Color(1f, 0.97f, 0.7f, _effectIntensity * 0.5f);
                float flashR = w * 0.28f * _effectIntensity;
                painter.BeginPath();
                painter.MoveTo(new Vector2(center.x,           center.y - flashR));
                painter.LineTo(new Vector2(center.x + flashR,  center.y));
                painter.LineTo(new Vector2(center.x,           center.y + flashR));
                painter.LineTo(new Vector2(center.x - flashR,  center.y));
                painter.ClosePath();
                painter.Fill();
                break;
            }
            
            case SwapEffectType.MoonbloomPetal:
            {
                int petalCount = 7;
                float progress = 1f - _effectIntensity;
                float maxDist   = w * 0.58f;

                for (int i = 0; i < petalCount; i++)
                {
                    // Stagger petals so they don't all start at the same frame.
                    // Petals with a higher stagger index appear slightly later.
                    float stagger    = i / (float)petalCount * 0.45f;
                    float localProg  = Mathf.Clamp01((progress - stagger) / (1f - stagger));
                    if (localProg <= 0f) continue;

                    float baseAngle  = (i / (float)petalCount) * Mathf.PI * 2f;
                    float spinAngle  = baseAngle + localProg * 0.7f;   // gentle rotation

                    float dist       = maxDist * localProg;
                    float cx2        = center.x + Mathf.Cos(spinAngle) * dist;
                    float cy2        = center.y + Mathf.Sin(spinAngle) * dist;

                    float alpha      = Mathf.Sin(localProg * Mathf.PI) * _effectIntensity;
                    Color petalColor = Color.Lerp(
                        new Color(0.98f, 0.63f, 0.78f, alpha),   // warm pink
                        new Color(0.78f, 0.63f, 0.98f, alpha),   // soft lavender
                        (float)i / petalCount
                    );

                    // Build a petal outline: narrow elongated diamond rotated along
                    // the travel direction so it always points away from centre.
                    float petalLen  = Mathf.Lerp(9f, 5f, localProg);
                    float petalWid  = petalLen * 0.42f;

                    // Unit vector along travel direction
                    float dx = Mathf.Cos(spinAngle);
                    float dy = Mathf.Sin(spinAngle);
                    // Perpendicular
                    float px = -dy;
                    float py =  dx;

                    Vector2 tip  = new Vector2(cx2 + dx * petalLen,  cy2 + dy * petalLen);
                    Vector2 root = new Vector2(cx2 - dx * petalLen,  cy2 - dy * petalLen);
                    Vector2 lobe1= new Vector2(cx2 + px * petalWid,  cy2 + py * petalWid);
                    Vector2 lobe2= new Vector2(cx2 - px * petalWid,  cy2 - py * petalWid);

                    painter.fillColor = petalColor;
                    painter.BeginPath();
                    painter.MoveTo(tip);
                    painter.LineTo(lobe1);
                    painter.LineTo(root);
                    painter.LineTo(lobe2);
                    painter.ClosePath();
                    painter.Fill();
                }
                break;
            }

            case SwapEffectType.FaeRipple:
            {
                int ringCount = 3;
                float maxRadius = w * 0.62f;

                for (int i = 0; i < ringCount; i++)
                {
                    // Stagger: ring 0 leads, ring 2 lags.
                    float stagger    = i * 0.2f;
                    float progress   = Mathf.Clamp01((_effectIntensity - stagger) / (1f - stagger));
                    // _effectIntensity goes 1→0, so invert for expansion.
                    float expansion  = 1f - progress;
                    float alpha      = progress * (1f - expansion * 0.6f);
                    if (alpha <= 0.01f) continue;

                    float radius = maxRadius * expansion;

                    Color ringColor = i switch
                    {
                        0 => new Color(0.4f,  0.91f, 1f,   alpha),   // ice cyan
                        1 => new Color(0.63f, 0.51f, 0.98f,alpha),   // violet
                        _ => new Color(0.78f, 0.96f, 0.85f,alpha),   // mint shimmer
                    };

                    float strokeW = Mathf.Lerp(2.5f, 0.8f, expansion);

                    painter.strokeColor = ringColor;
                    painter.lineWidth   = strokeW;
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(center.x,            center.y - radius));
                    painter.LineTo(new Vector2(center.x + radius,   center.y));
                    painter.LineTo(new Vector2(center.x,            center.y + radius));
                    painter.LineTo(new Vector2(center.x - radius,   center.y));
                    painter.ClosePath();
                    painter.Stroke();

                    // Subtle inner fill for the innermost ring only
                    if (i == 0)
                    {
                        painter.fillColor = new Color(0.6f, 0.94f, 1f, alpha * 0.12f);
                        painter.BeginPath();
                        painter.MoveTo(new Vector2(center.x,            center.y - radius));
                        painter.LineTo(new Vector2(center.x + radius,   center.y));
                        painter.LineTo(new Vector2(center.x,            center.y + radius));
                        painter.LineTo(new Vector2(center.x - radius,   center.y));
                        painter.ClosePath();
                        painter.Fill();
                    }
                }
                break; 
            }

            case SwapEffectType.ChronoShift:
            {
                int ghostCount = 4;
                for (int i = 0; i < ghostCount; i++)
                {
                    // Stagger the ghosts based on intensity
                    float ghostProg = Mathf.Clamp01(_effectIntensity - (i * 0.15f));
                    if (ghostProg <= 0) continue;

                    float scale = 1f + (1f - ghostProg) * 0.5f;
                    painter.strokeColor = new Color(0.3f, 0.7f, 1f, ghostProg * 0.5f);
                    painter.lineWidth = 1.5f;

                    // Apply scale transformation manually for the ghosting effect
                    Vector2 gTop = center + (top - center) * scale;
                    Vector2 gRight = center + (right - center) * scale;
                    Vector2 gBottom = center + (bottom - center) * scale;
                    Vector2 gLeft = center + (left - center) * scale;

                    painter.BeginPath();
                    painter.MoveTo(gTop);
                    painter.LineTo(gRight);
                    painter.LineTo(gBottom);
                    painter.LineTo(gLeft);
                    painter.ClosePath();
                    painter.Stroke();
                }
                break;
            }

            case SwapEffectType.EmberBurst:
            {
                int sparkCount = 8;
                float p = 1f - _effectIntensity; // 0 to 1
                
                for (int i = 0; i < sparkCount; i++)
                {
                    float angle = (i * Mathf.PI * 2f) / sparkCount;
                    float speed = 40f + (i * 5f);
                    // Add a bit of "gravity" to the Y axis as it progresses
                    Vector2 velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed * p;
                    Vector2 gravity = new Vector2(0, p * p * 20f); 
                    Vector2 sparkPos = center + velocity + gravity;

                    painter.fillColor = Color.Lerp(new Color(1f, 0.6f, 0f), new Color(1f, 0.2f, 0f), p);
                    painter.fillColor = new Color(painter.fillColor.r, painter.fillColor.g, painter.fillColor.b, _effectIntensity);

                    float s = 3f * _effectIntensity;
                    painter.BeginPath();
                    painter.Arc(sparkPos, s, 0, 360);
                    painter.Fill();
                }
                break;
            }

            case SwapEffectType.AbyssalBloom:
            {
                float rot = _effectIntensity * 180f;
                float pulse = 0.4f + Mathf.Sin(_effectIntensity * Mathf.PI) * 0.4f;
                
                painter.fillColor = new Color(0.05f, 0.05f, 0.1f, _effectIntensity * 0.9f);
                
                Matrix4x4 bloomMatrix = Matrix4x4.TRS(center, Quaternion.Euler(0, 0, rot), new Vector3(pulse, pulse, 1)) 
                                    * Matrix4x4.Translate(-center);

                painter.BeginPath();
                painter.MoveTo(bloomMatrix.MultiplyPoint3x4(top));
                painter.LineTo(bloomMatrix.MultiplyPoint3x4(right));
                painter.LineTo(bloomMatrix.MultiplyPoint3x4(bottom));
                painter.LineTo(bloomMatrix.MultiplyPoint3x4(left));
                painter.ClosePath();
                painter.Fill();
                
                // Add a glowing rim to the inner bloom
                painter.strokeColor = new Color(0.6f, 0f, 1f, _effectIntensity);
                painter.lineWidth = 2f;
                painter.Stroke();
                break;
            }

            case SwapEffectType.StormSurge:
            {
                painter.strokeColor = new Color(0.5f, 0.8f, 1f, _effectIntensity);
                painter.lineWidth = 1.2f;
                // High-frequency jitter based on time/intensity
                Random.InitState((int)(_effectIntensity * 500)); 

                for (int i = 0; i < 5; i++)
                {
                    Vector2 startPos = center;
                    Vector2 endPos = i switch { 0 => top, 1 => right, 2 => bottom, 3 => left, _ => top + right * 0.5f };

                    painter.BeginPath();
                    painter.MoveTo(startPos);

                    // Create jagged segments
                    int segments = 4;
                    for (int j = 1; j <= segments; j++)
                    {
                        float t = j / (float)segments;
                        Vector2 point = Vector2.Lerp(startPos, endPos, t);
                        // Add perpendicular noise
                        Vector2 offset = new Vector2(Random.Range(-10, 10), Random.Range(-10, 10)) * _effectIntensity;
                        painter.LineTo(point + offset);
                    }
                    painter.Stroke();
                }
                break;
            }

            case SwapEffectType.GlitchRemnant:
            {
                float p = 1f - _effectIntensity;
                painter.fillColor = new Color(1f, 0.1f, 0.4f, _effectIntensity * 0.6f);
                
                for (int i = 0; i < 6; i++)
                {
                    float sliceY = Mathf.Lerp(0, h, (float)i / 6);
                    float sliceWidth = w * Random.Range(0.2f, 0.8f);
                    float xOffset = Mathf.Sin(p * 20f + i) * 15f;
                    float rectX = center.x - (sliceWidth * 0.5f) + xOffset;

                    painter.BeginPath();
                    painter.MoveTo(new Vector2(rectX, sliceY));
                    painter.LineTo(new Vector2(rectX + sliceWidth, sliceY));
                    painter.LineTo(new Vector2(rectX + sliceWidth, sliceY + 2f));
                    painter.LineTo(new Vector2(rectX, sliceY + 2f));
                    painter.ClosePath();
                    painter.Fill();
                    
                    if (i % 2 == 0) painter.fillColor = new Color(0.1f, 0.9f, 1f, _effectIntensity * 0.4f);
                }
                break;
            }

            case SwapEffectType.NebulaOverload:
            {
                painter.lineWidth = 2.5f;
                float rotationSpeed = (1f - _effectIntensity) * 720f;
                
                for (int i = 0; i < 3; i++)
                {
                    float ringRadius = (w * 0.3f) + (i * 8f * _effectIntensity);
                    float startAngle = rotationSpeed + (i * 120f);
                    
                    painter.strokeColor = i switch {
                        0 => new Color(0.6f, 0.2f, 1f, _effectIntensity), // Purple
                        1 => new Color(0.2f, 0.5f, 1f, _effectIntensity), // Blue
                        _ => new Color(1f, 1f, 1f, _effectIntensity * 0.5f) // White highlight
                    };

                    painter.BeginPath();
                    // Drawing an incomplete arc gives it that "spinning energy" look
                    painter.Arc(center, ringRadius, startAngle, startAngle + 90f);
                    painter.Stroke();
                }
                break;
            }

            case SwapEffectType.RiftShatter:
            {
                painter.strokeColor = new Color(0.8f, 1f, 1f, _effectIntensity);
                painter.lineWidth = 1.8f;
                Random.InitState((int)(_effectIntensity * 1000)); // Ultra-fast flicker

                for (int i = 0; i < 8; i++)
                {
                    // Pick a random spot on the border
                    float side = Random.value;
                    Vector2 start = side switch {
                        < 0.25f => Vector2.Lerp(top, right, Random.value),
                        < 0.5f => Vector2.Lerp(right, bottom, Random.value),
                        < 0.75f => Vector2.Lerp(bottom, left, Random.value),
                        _ => Vector2.Lerp(left, top, Random.value)
                    };

                    painter.BeginPath();
                    painter.MoveTo(start);
                    // Jagged line toward a slightly offset center
                    Vector2 mid = Vector2.Lerp(start, center, 0.5f) + (Random.insideUnitCircle * 10f);
                    painter.LineTo(mid);
                    painter.LineTo(center + (Random.insideUnitCircle * 5f));
                    painter.Stroke();
                }
                break;
            }

            case SwapEffectType.SingularityPulse:
            {
                float p = 1f - _effectIntensity;
                painter.strokeColor = new Color(0.4f, 0f, 1f, _effectIntensity);
                painter.lineWidth = 2.5f;

                for (int i = 0; i < 12; i++)
                {
                    float angle = (i * 30f) * Mathf.Deg2Rad;
                    float dist = Mathf.Lerp(w, 0, (p + (i * 0.1f)) % 1f);
                    Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
                    
                    // Draw a small "streak" pointing toward center
                    Vector2 streakEnd = pos + (center - pos).normalized * 15f;
                    
                    painter.BeginPath();
                    painter.MoveTo(pos);
                    painter.LineTo(streakEnd);
                    painter.Stroke();
                }
                break;
            }

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