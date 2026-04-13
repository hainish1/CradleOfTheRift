using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class CreditsController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Scroll Settings")]
    [SerializeField] private float scrollSpeed = 60f;
    [SerializeField] private float fadeHeight  = 100f;

    [Header("Star Settings")]
    [SerializeField] private int   starCount       = 60;
    [SerializeField] private float starMinSize      = 1f;
    [SerializeField] private float starMaxSize      = 3f;
    [SerializeField] private float starMinTwinkle   = 1.5f;   
    [SerializeField] private float starMaxTwinkle   = 4.0f;

    [Header("Dust/Spore Settings")]
    [SerializeField] private float dustSpawnRate    = 0.4f;   
    [SerializeField] private float dustRiseDistance = 28f;
    [SerializeField] private float dustFadeDuration = 1.8f;
    [SerializeField] private int   dustPerBurst     = 2;  

    [Header("Scene Transition")]
    [SerializeField] private string nextSceneName = "MainMenu";    

    private ScrollView    _scrollView;
    private VisualElement _container;
    private VisualElement _fadeTop;
    private VisualElement _fadeBottom;
    private Button        _skipButton;
    private bool          _scrolling;
    private float         _maxScroll;

    private VisualElement _treeLeft;
    private VisualElement _treeRight;

    private VisualElement _creditsRoot;

    private List<VisualElement> _dividers = new();
    private List<StarData> _stars = new();
    private List<DustParticle> _dustParticles = new();
    private float              _dustTimer;

    private void OnEnable()
    {
        Time.timeScale = 1f;
        
        var root = uiDocument.rootVisualElement;

        _scrollView  = root.Q<ScrollView>("credits-scroll");
        _container   = root.Q<VisualElement>("credits-container");
        _fadeTop     = root.Q<VisualElement>("fade-top");
        _fadeBottom  = root.Q<VisualElement>("fade-bottom");
        _skipButton  = root.Q<Button>("skip-button");
        _treeLeft    = root.Q<VisualElement>("tree-left");
        _treeRight   = root.Q<VisualElement>("tree-right");
        _creditsRoot = root.Q<VisualElement>("credits-root");

        _fadeTop.generateVisualContent    += DrawFadeTop;
        _fadeBottom.generateVisualContent += DrawFadeBottom;
        _treeLeft.generateVisualContent   += ctx => DrawTree(ctx, false);
        _treeRight.generateVisualContent  += ctx => DrawTree(ctx, true);

        // Replace rune dividers — alternate twisted vine / ancient oak / twisted vine
        int dividerIndex = 0;
        root.Query<VisualElement>(className: "rune-divider").ForEach(div =>
        {
            foreach (var child in div.Children())
                child.style.display = DisplayStyle.None;

            if (dividerIndex % 2 == 0)
                div.generateVisualContent += DrawTwistedVineDivider;
            else
                div.generateVisualContent += DrawAncientOakDivider;

            _dividers.Add(div);
            dividerIndex++;
        });

        _skipButton.clicked += FinishCredits;

        _scrollView.RegisterCallback<GeometryChangedEvent>(OnLayoutReady);
    }

    private void OnDisable()
    {
        if (_fadeTop    != null) _fadeTop.generateVisualContent    -= DrawFadeTop;
        if (_fadeBottom != null) _fadeBottom.generateVisualContent -= DrawFadeBottom;
        if (_treeLeft   != null) _treeLeft.generateVisualContent   -= ctx => DrawTree(ctx, false);
        if (_treeRight  != null) _treeRight.generateVisualContent  -= ctx => DrawTree(ctx, true);
        if (_skipButton != null) _skipButton.clicked               -= FinishCredits;
        int cleanupIndex = 0;
        foreach (var div in _dividers)
        {
            if (div != null)
            {
                if (cleanupIndex % 2 == 0)
                    div.generateVisualContent -= DrawTwistedVineDivider;
                else
                    div.generateVisualContent -= DrawAncientOakDivider;
            }
            cleanupIndex++;
        }
        _dividers.Clear();

        foreach (var s in _stars)        s.Element?.RemoveFromHierarchy();
        foreach (var d in _dustParticles) d.Element?.RemoveFromHierarchy();
        _stars.Clear();
        _dustParticles.Clear();
    }

    private void OnLayoutReady(GeometryChangedEvent evt)
    {
        _scrollView.UnregisterCallback<GeometryChangedEvent>(OnLayoutReady);

        float panelHeight = _scrollView.layout.height;
        _container.style.paddingTop = panelHeight;
        // _container.style.paddingBottom = panelHeight; // Have it keep scrolling past the logo 

        SpawnStars();
        StartCoroutine(StartScrollNextFrame());
    }

    private IEnumerator StartScrollNextFrame()
    {
        yield return null;
        _scrollView.scrollOffset = Vector2.zero;
        float contentHeight = _container.layout.height;
        float panelHeight   = _scrollView.layout.height;
        _maxScroll = Mathf.Max(0f, contentHeight - panelHeight);
        _scrolling = true;
    }

    private void Update()
    {
        if (!_scrolling) return;

        UpdateScroll();
        UpdateDust();
        UpdateStarTwinkle();
    }

    private void UpdateScroll()
    {
        float newY = _scrollView.scrollOffset.y + scrollSpeed * Time.deltaTime;
        _scrollView.scrollOffset = new Vector2(0f, newY);
        if (_scrollView.scrollOffset.y >= _maxScroll)
            FinishCredits();
    }

    private void SpawnStars()
    {
        if (_creditsRoot == null) return;

        float panelW = _creditsRoot.layout.width;
        float panelH = _creditsRoot.layout.height;

        for (int i = 0; i < starCount; i++)
        {
            float size = Random.Range(starMinSize, starMaxSize);

            var el = new VisualElement();
            el.style.position                  = Position.Absolute;
            el.style.width                     = size;
            el.style.height                    = size;
            el.style.borderTopLeftRadius       = size / 2f;
            el.style.borderTopRightRadius      = size / 2f;
            el.style.borderBottomLeftRadius    = size / 2f;
            el.style.borderBottomRightRadius   = size / 2f;
            el.style.left                      = Random.Range(0f, panelW);
            el.style.top                       = Random.Range(0f, panelH * 0.85f);
            el.style.opacity                   = Random.Range(0.3f, 1f);

            Color[] starColors = {
                new Color(1f,    1f,    1f,    1f),   
                new Color(0.93f, 0.90f, 1f,    1f),   
                new Color(0.85f, 0.80f, 1f,    1f),   
            };
            el.style.backgroundColor = starColors[Random.Range(0, starColors.Length)];

            _creditsRoot.Insert(0, el);

            _stars.Add(new StarData
            {
                Element       = el,
                TwinkleSpeed  = Random.Range(starMinTwinkle, starMaxTwinkle),
                TwinkleOffset = Random.Range(0f, Mathf.PI * 2f),
                BaseOpacity   = Random.Range(0.25f, 0.9f),
            });
        }
    }

    private void UpdateStarTwinkle()
    {
        float t = Time.time;
        foreach (var star in _stars)
        {
            float phase   = Mathf.Sin((t / star.TwinkleSpeed) * Mathf.PI * 2f + star.TwinkleOffset);
            float opacity = Mathf.Lerp(star.BaseOpacity * 0.2f, star.BaseOpacity, (phase + 1f) * 0.5f);
            star.Element.style.opacity = opacity;
        }
    }

    private void UpdateDust()
    {
        _dustTimer += Time.deltaTime;
        if (_dustTimer >= dustSpawnRate)
        {
            _dustTimer = 0f;
            for (int i = 0; i < dustPerBurst; i++)
            {
                SpawnDust(_treeLeft,  false);
                SpawnDust(_treeRight, true);
            }
        }

        for (int i = _dustParticles.Count - 1; i >= 0; i--)
        {
            var d = _dustParticles[i];
            d.Age += Time.deltaTime;
            float progress = d.Age / dustFadeDuration;

            if (progress >= 1f)
            {
                d.Element.RemoveFromHierarchy();
                _dustParticles.RemoveAt(i);
                continue;
            }

            float rise    = Mathf.Lerp(0f, dustRiseDistance, EaseOut(progress));
            float sway    = Mathf.Sin(progress * Mathf.PI * 2f + d.SwayOffset) * 4f;
            float opacity = Mathf.Lerp(0.9f, 0f, progress);
            float scale   = Mathf.Lerp(1f, 0.2f, progress);

            d.Element.style.bottom  = new StyleLength(d.StartBottom + rise);
            d.Element.style.left    = new StyleLength(d.StartLeft   + sway);
            d.Element.style.opacity = opacity;
            d.Element.style.width   = d.BaseSize * scale;
            d.Element.style.height  = d.BaseSize * scale;
        }
    }

    private void SpawnDust(VisualElement tree, bool isRight)
    {
        if (tree == null || _creditsRoot == null) return;

        float size = Random.Range(2f, 5f);

        var el = new VisualElement();
        el.style.position                  = Position.Absolute;
        el.style.width                     = size;
        el.style.height                    = size;
        el.style.borderTopLeftRadius       = size / 2f;
        el.style.borderTopRightRadius      = size / 2f;
        el.style.borderBottomLeftRadius    = size / 2f;
        el.style.borderBottomRightRadius   = size / 2f;

        Color[] dustColors = {
            new Color(0.99f, 0.88f, 0.50f, 1f),   
            new Color(0.66f, 0.85f, 1.00f, 1f),   
            new Color(0.85f, 0.80f, 1.00f, 1f),   
            new Color(1.00f, 1.00f, 1.00f, 1f),   
        };
        el.style.backgroundColor = dustColors[Random.Range(0, dustColors.Length)];

        // Spawn within the tree canopy bounds
        float treeScreenLeft   = tree.layout.x;
        float treeScreenBottom = _creditsRoot.layout.height - tree.layout.yMax;

        float spawnLeft   = treeScreenLeft + Random.Range(20f, tree.layout.width - 20f);
        float spawnBottom = treeScreenBottom + Random.Range(60f, 110f); // Canopy height

        el.style.left   = spawnLeft;
        el.style.bottom = spawnBottom;

        _creditsRoot.Add(el);

        _dustParticles.Add(new DustParticle
        {
            Element     = el,
            StartLeft   = spawnLeft,
            StartBottom = spawnBottom,
            BaseSize    = size,
            SwayOffset  = Random.Range(0f, Mathf.PI * 2f),
        });
    }

    private void FinishCredits()
    {
        // Guard against being called more than once
        if (!_scrolling && !enabled) return;

        _scrolling = false;

        SceneManager.LoadScene(nextSceneName);
    }

    private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

    private void DrawTwistedVineDivider(MeshGenerationContext ctx)
    {
        var p  = ctx.painter2D;
        float cx = ctx.visualElement.layout.width  * 0.5f;
        float cy = ctx.visualElement.layout.height * 0.5f;

        Color vine    = new Color(0.42f, 0.34f, 0.60f, 0.85f);
        Color leafCol = new Color(0.24f, 0.50f, 0.33f, 0.90f);
        Color berry1  = new Color(0.55f, 0.23f, 0.43f, 1.00f);
        Color berry2  = new Color(0.63f, 0.27f, 0.43f, 1.00f);
        Color petal   = new Color(0.83f, 0.47f, 0.60f, 1.00f);
        Color stamen  = new Color(1.00f, 0.88f, 0.63f, 1.00f);

        // Main S-curve vine arms
        p.lineWidth   = 2.0f;
        p.strokeColor = vine;
        p.BeginPath();
        p.MoveTo(new Vector2(cx - 5f, cy));
        p.BezierCurveTo(new Vector2(cx - 18f, cy - 5f), new Vector2(cx - 35f, cy + 5f),
                        new Vector2(cx - 50f, cy - 3f));
        p.BezierCurveTo(new Vector2(cx - 60f, cy - 8f), new Vector2(cx - 68f, cy - 4f),
                        new Vector2(cx - 74f, cy));
        p.Stroke();
        p.BeginPath();
        p.MoveTo(new Vector2(cx + 5f, cy));
        p.BezierCurveTo(new Vector2(cx + 18f, cy - 5f), new Vector2(cx + 35f, cy + 5f),
                        new Vector2(cx + 50f, cy - 3f));
        p.BezierCurveTo(new Vector2(cx + 60f, cy - 8f), new Vector2(cx + 68f, cy - 4f),
                        new Vector2(cx + 74f, cy));
        p.Stroke();

        // Spiral curls at tips
        p.lineWidth   = 1.2f;
        p.strokeColor = new Color(vine.r, vine.g, vine.b, 0.70f);
        p.BeginPath();
        p.MoveTo(new Vector2(cx - 74f, cy));
        p.BezierCurveTo(new Vector2(cx - 80f, cy - 4f), new Vector2(cx - 82f, cy - 10f),
                        new Vector2(cx - 78f, cy - 13f));
        p.BezierCurveTo(new Vector2(cx - 74f, cy - 16f), new Vector2(cx - 70f, cy - 12f),
                        new Vector2(cx - 72f, cy - 8f));
        p.Stroke();
        p.BeginPath();
        p.MoveTo(new Vector2(cx + 74f, cy));
        p.BezierCurveTo(new Vector2(cx + 80f, cy - 4f), new Vector2(cx + 82f, cy - 10f),
                        new Vector2(cx + 78f, cy - 13f));
        p.BezierCurveTo(new Vector2(cx + 74f, cy - 16f), new Vector2(cx + 70f, cy - 12f),
                        new Vector2(cx + 72f, cy - 8f));
        p.Stroke();

        // Small leaves along vine
        void DrawVineLeaf(Vector2 pos, float angle, Color c)
        {
            float cos = Mathf.Cos(angle * Mathf.Deg2Rad);
            float sin = Mathf.Sin(angle * Mathf.Deg2Rad);
            p.fillColor = c;
            p.BeginPath();
            p.MoveTo(pos);
            p.BezierCurveTo(
                new Vector2(pos.x + cos * 6f - sin * 2.5f, pos.y + sin * 6f + cos * 2.5f),
                new Vector2(pos.x + cos * 10f - sin * 1f,  pos.y + sin * 10f + cos * 1f),
                new Vector2(pos.x + cos * 11f,             pos.y + sin * 11f));
            p.BezierCurveTo(
                new Vector2(pos.x + cos * 10f + sin * 1f,  pos.y + sin * 10f - cos * 1f),
                new Vector2(pos.x + cos * 6f  + sin * 2.5f, pos.y + sin * 6f - cos * 2.5f),
                pos);
            p.Fill();
        }
        DrawVineLeaf(new Vector2(cx - 22f, cy - 3f), -60f,  leafCol);
        DrawVineLeaf(new Vector2(cx - 48f, cy - 2f),  130f, leafCol);
        DrawVineLeaf(new Vector2(cx + 22f, cy - 3f), -120f, leafCol);
        DrawVineLeaf(new Vector2(cx + 48f, cy - 2f),  50f,  leafCol);

        // Berries on curls
        p.fillColor = berry1;
        p.BeginPath(); p.Arc(new Vector2(cx - 75f, cy - 9f),  2.5f, 0f, 360f); p.Fill();
        p.fillColor = berry2;
        p.BeginPath(); p.Arc(new Vector2(cx - 79f, cy - 12f), 2.0f, 0f, 360f); p.Fill();
        p.fillColor = berry1;
        p.BeginPath(); p.Arc(new Vector2(cx + 75f, cy - 9f),  2.5f, 0f, 360f); p.Fill();
        p.fillColor = berry2;
        p.BeginPath(); p.Arc(new Vector2(cx + 79f, cy - 12f), 2.0f, 0f, 360f); p.Fill();

        // Centre flower
        p.fillColor = new Color(0.11f, 0.07f, 0.20f, 1f);
        p.BeginPath(); p.Arc(new Vector2(cx, cy), 7f, 0f, 360f); p.Fill();
        p.lineWidth = 1f; p.strokeColor = new Color(0.61f, 0.41f, 0.53f, 1f);
        p.BeginPath(); p.Arc(new Vector2(cx, cy), 7f, 0f, 360f); p.Stroke();
        p.fillColor = petal;
        p.BeginPath(); p.Arc(new Vector2(cx,       cy - 4.5f), 2.0f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx + 3.9f, cy + 2.2f), 2.0f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx - 3.9f, cy + 2.2f), 2.0f, 0f, 360f); p.Fill();
        p.fillColor = stamen;
        p.BeginPath(); p.Arc(new Vector2(cx, cy), 1.5f, 0f, 360f); p.Fill();
    }

    private void DrawAncientOakDivider(MeshGenerationContext ctx)
    {
        var p  = ctx.painter2D;
        float cx = ctx.visualElement.layout.width  * 0.5f;
        float cy = ctx.visualElement.layout.height * 0.5f;

        Color bough  = new Color(0.23f, 0.18f, 0.12f, 1.00f);
        Color capDk  = new Color(0.42f, 0.18f, 0.09f, 1.00f);
        Color capMd  = new Color(0.61f, 0.33f, 0.16f, 1.00f);
        Color capLt  = new Color(0.80f, 0.40f, 0.22f, 1.00f);
        Color spot   = new Color(1.00f, 1.00f, 1.00f, 0.48f);
        Color moss   = new Color(0.24f, 0.38f, 0.25f, 1.00f);

        // Main gnarled boughs
        p.lineWidth   = 3.5f;
        p.strokeColor = bough;
        p.BeginPath();
        p.MoveTo(new Vector2(cx - 6f, cy + 2f));
        p.BezierCurveTo(new Vector2(cx - 20f, cy - 2f), new Vector2(cx - 42f, cy + 4f),
                        new Vector2(cx - 58f, cy - 2f));
        p.BezierCurveTo(new Vector2(cx - 66f, cy - 5f), new Vector2(cx - 70f, cy - 2f),
                        new Vector2(cx - 72f, cy));
        p.Stroke();
        p.BeginPath();
        p.MoveTo(new Vector2(cx + 6f, cy + 2f));
        p.BezierCurveTo(new Vector2(cx + 20f, cy - 2f), new Vector2(cx + 42f, cy + 4f),
                        new Vector2(cx + 58f, cy - 2f));
        p.BezierCurveTo(new Vector2(cx + 66f, cy - 5f), new Vector2(cx + 70f, cy - 2f),
                        new Vector2(cx + 72f, cy));
        p.Stroke();

        // Moss tufts at mushroom base
        p.lineWidth   = 0.8f;
        p.strokeColor = moss;
        p.BeginPath(); p.MoveTo(new Vector2(cx - 14f, cy + 3f)); p.BezierCurveTo(new Vector2(cx - 14f, cy + 1f), new Vector2(cx - 13f, cy),     new Vector2(cx - 12f, cy + 1f)); p.Stroke();
        p.BeginPath(); p.MoveTo(new Vector2(cx - 12f, cy + 3f)); p.BezierCurveTo(new Vector2(cx - 12f, cy),     new Vector2(cx - 11f, cy - 1f), new Vector2(cx - 10f, cy));     p.Stroke();
        p.BeginPath(); p.MoveTo(new Vector2(cx + 12f, cy + 3f)); p.BezierCurveTo(new Vector2(cx + 12f, cy + 1f), new Vector2(cx + 11f, cy),     new Vector2(cx + 10f, cy + 1f)); p.Stroke();
        p.BeginPath(); p.MoveTo(new Vector2(cx + 14f, cy + 3f)); p.BezierCurveTo(new Vector2(cx + 14f, cy),     new Vector2(cx + 13f, cy - 1f), new Vector2(cx + 12f, cy));     p.Stroke();

        // Left small mushroom
        p.fillColor = capDk;
        p.BeginPath(); p.MoveTo(new Vector2(cx - 10f, cy + 3f)); p.LineTo(new Vector2(cx - 6f, cy + 3f)); p.LineTo(new Vector2(cx - 8f, cy)); p.Fill();
        p.fillColor = capMd;
        p.BeginPath(); p.Arc(new Vector2(cx - 8f, cy), 4f, 0f, 360f); p.Fill();
        p.fillColor = spot;
        p.BeginPath(); p.Arc(new Vector2(cx - 9f,   cy - 0.5f), 0.7f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx - 6.5f, cy + 0.5f), 0.5f, 0f, 360f); p.Fill();

        // Centre large mushroom
        p.fillColor = capDk;
        p.BeginPath(); p.MoveTo(new Vector2(cx - 4f, cy + 3f)); p.LineTo(new Vector2(cx + 4f, cy + 3f)); p.LineTo(new Vector2(cx, cy - 2f)); p.Fill();
        p.fillColor = capMd;
        p.BeginPath(); p.Arc(new Vector2(cx, cy - 2f), 6.5f, 0f, 360f); p.Fill();
        p.fillColor = capLt;
        p.BeginPath(); p.Arc(new Vector2(cx, cy - 2.3f), 5.5f, 0f, 360f); p.Fill();
        p.fillColor = spot;
        p.BeginPath(); p.Arc(new Vector2(cx - 1.5f, cy - 3f),   1.0f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx + 2f,   cy - 1.8f), 0.7f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx - 0.5f, cy - 0.8f), 0.5f, 0f, 360f); p.Fill();

        // Right small mushroom
        p.fillColor = capDk;
        p.BeginPath(); p.MoveTo(new Vector2(cx + 6f, cy + 3f)); p.LineTo(new Vector2(cx + 10f, cy + 3f)); p.LineTo(new Vector2(cx + 8f, cy)); p.Fill();
        p.fillColor = capMd;
        p.BeginPath(); p.Arc(new Vector2(cx + 8f, cy), 4f, 0f, 360f); p.Fill();
        p.fillColor = spot;
        p.BeginPath(); p.Arc(new Vector2(cx + 7f,   cy - 0.5f), 0.7f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx + 9.5f, cy + 0.5f), 0.5f, 0f, 360f); p.Fill();
    }
    private void DrawTree(MeshGenerationContext ctx, bool isRight)
    {
        var p  = ctx.painter2D;
        float cx = 100f;  // Centre of 200px-wide canvas
        float by = 180f; // Bottom of 180px-tall canvas — crystals/trunk base sit here

        // ── Palette ──────────────────────────────────────────────────────────
        Color auraOuter  = isRight ? new Color(0.40f, 0.80f, 1.00f, 0.08f) : new Color(0.65f, 0.45f, 1.00f, 0.08f);
        Color auraMid    = isRight ? new Color(0.40f, 0.80f, 1.00f, 0.16f) : new Color(0.65f, 0.45f, 1.00f, 0.16f);
        Color auraInner  = isRight ? new Color(0.60f, 0.90f, 1.00f, 0.28f) : new Color(0.78f, 0.60f, 1.00f, 0.28f);

        Color trunkDark  = new Color(0.08f, 0.05f, 0.18f, 1f);
        Color trunkMid   = new Color(0.13f, 0.09f, 0.26f, 1f);

        Color canopyDeep = isRight ? new Color(0.28f, 0.52f, 0.80f, 0.95f) : new Color(0.48f, 0.34f, 0.80f, 0.95f);
        Color canopyMid  = isRight ? new Color(0.42f, 0.68f, 0.92f, 0.92f) : new Color(0.62f, 0.48f, 0.90f, 0.92f);
        Color canopyGlow = isRight ? new Color(0.65f, 0.88f, 1.00f, 0.88f) : new Color(0.82f, 0.70f, 1.00f, 0.88f);
        Color canopyTip  = isRight ? new Color(0.85f, 0.97f, 1.00f, 0.70f) : new Color(0.92f, 0.85f, 1.00f, 0.70f);

        Color orbGold    = new Color(1.00f, 0.95f, 0.60f, 1f);
        Color orbCyan    = new Color(0.55f, 1.00f, 0.90f, 1f);
        Color orbMagenta = new Color(1.00f, 0.55f, 0.90f, 1f);

        Color crystalA   = isRight ? new Color(0.45f, 0.85f, 1.00f, 0.90f) : new Color(0.75f, 0.50f, 1.00f, 0.90f);
        Color crystalB   = isRight ? new Color(0.25f, 0.60f, 0.90f, 0.80f) : new Color(0.55f, 0.30f, 0.85f, 0.80f);

        Color vineColor  = isRight ? new Color(0.45f, 0.85f, 1.00f, 0.60f) : new Color(0.78f, 0.60f, 1.00f, 0.60f);

        // ── 1. Wide ambient aura (3 rings) ───────────────────────────────────
        p.fillColor = auraOuter;
        p.BeginPath(); p.Arc(new Vector2(cx, by - 120f), 80f, 0f, 360f); p.Fill();
        p.fillColor = auraMid;
        p.BeginPath(); p.Arc(new Vector2(cx, by - 125f), 58f, 0f, 360f); p.Fill();
        p.fillColor = auraInner;
        p.BeginPath(); p.Arc(new Vector2(cx, by - 128f), 38f, 0f, 360f); p.Fill();

        // ── 2. Ground crystals at roots ───────────────────────────────────────
        void DrawCrystal(Vector2 base_, float w, float h, float lean, Color ca, Color cb)
        {
            float tipX = base_.x + lean;
            float tipY = base_.y - h;
            p.fillColor = ca;
            p.BeginPath();
            p.MoveTo(new Vector2(base_.x - w * 0.5f, base_.y));
            p.LineTo(new Vector2(base_.x + w * 0.5f, base_.y));
            p.LineTo(new Vector2(tipX, tipY));
            p.Fill();
            p.fillColor = cb;
            p.BeginPath();
            p.MoveTo(new Vector2(base_.x + w * 0.1f, base_.y));
            p.LineTo(new Vector2(base_.x + w * 0.5f, base_.y));
            p.LineTo(new Vector2(tipX, tipY));
            p.Fill();
        }

        DrawCrystal(new Vector2(cx - 32f, by),  7f, 22f, -4f, crystalA, crystalB);
        DrawCrystal(new Vector2(cx - 22f, by),  5f, 15f, -2f, crystalB, crystalA);
        DrawCrystal(new Vector2(cx + 28f, by),  7f, 20f,  3f, crystalA, crystalB);
        DrawCrystal(new Vector2(cx + 38f, by),  5f, 13f,  4f, crystalB, crystalA);
        DrawCrystal(new Vector2(cx -  8f, by),  4f, 10f,  1f, crystalA, crystalB);
        DrawCrystal(new Vector2(cx + 10f, by),  4f, 12f, -1f, crystalB, crystalA);

        // Flat perspective oval glows at crystal bases
        void DrawOvalGlow(Vector2 centre, float rx, float ry, Color c)
        {
            p.fillColor = c;
            p.BeginPath();
            p.MoveTo(new Vector2(centre.x - rx, centre.y));
            p.BezierCurveTo(new Vector2(centre.x - rx, centre.y - ry),
                            new Vector2(centre.x + rx, centre.y - ry),
                            new Vector2(centre.x + rx, centre.y));
            p.BezierCurveTo(new Vector2(centre.x + rx, centre.y + ry),
                            new Vector2(centre.x - rx, centre.y + ry),
                            new Vector2(centre.x - rx, centre.y));
            p.Fill();
        }
        Color glowCol = new Color(crystalA.r, crystalA.g, crystalA.b, 0.22f);
        DrawOvalGlow(new Vector2(cx - 26f, by - 1f), 22f,  6f, glowCol);
        DrawOvalGlow(new Vector2(cx + 30f, by - 1f), 18f,  5f, glowCol);

        // ── 3. Trunk — tapers naturally into canopy base ─────────────────────
        p.fillColor = trunkDark;
        p.BeginPath();
        p.MoveTo(new Vector2(cx - 18f, by));
        p.BezierCurveTo(new Vector2(cx - 10f, by - 40f), new Vector2(cx - 6f, by - 80f), new Vector2(cx - 5f, by - 110f));
        p.LineTo(new Vector2(cx + 5f, by - 110f));
        p.BezierCurveTo(new Vector2(cx + 6f, by - 80f), new Vector2(cx + 10f, by - 40f), new Vector2(cx + 18f, by));
        p.Fill();

        p.fillColor = trunkMid;
        p.BeginPath();
        p.MoveTo(new Vector2(cx - 6f, by - 5f));
        p.BezierCurveTo(new Vector2(cx - 3f, by - 40f), new Vector2(cx - 2f, by - 80f), new Vector2(cx - 2f, by - 108f));
        p.LineTo(new Vector2(cx + 2f, by - 108f));
        p.BezierCurveTo(new Vector2(cx + 2f, by - 80f), new Vector2(cx + 3f, by - 40f), new Vector2(cx + 6f, by - 5f));
        p.Fill();

        // ── 4. Branches (4 total) ─────────────────────────────────────────────
        p.lineWidth = 3.5f;
        p.strokeColor = trunkDark;
        p.BeginPath();
        p.MoveTo(new Vector2(cx, by - 98f));
        p.BezierCurveTo(new Vector2(cx - 14f, by - 102f), new Vector2(cx - 28f, by - 118f), new Vector2(cx - 38f, by - 134f));
        p.Stroke();
        p.BeginPath();
        p.MoveTo(new Vector2(cx, by - 92f));
        p.BezierCurveTo(new Vector2(cx + 14f, by - 96f), new Vector2(cx + 28f, by - 112f), new Vector2(cx + 38f, by - 128f));
        p.Stroke();
        p.lineWidth = 2f;
        p.BeginPath();
        p.MoveTo(new Vector2(cx - 10f, by - 108f));
        p.BezierCurveTo(new Vector2(cx - 18f, by - 118f), new Vector2(cx - 28f, by - 130f), new Vector2(cx - 20f, by - 150f));
        p.Stroke();
        p.BeginPath();
        p.MoveTo(new Vector2(cx + 10f, by - 104f));
        p.BezierCurveTo(new Vector2(cx + 20f, by - 116f), new Vector2(cx + 32f, by - 128f), new Vector2(cx + 22f, by - 148f));
        p.Stroke();

        // ── 6. Canopy — four depth layers ────────────────────────────────────
        p.fillColor = canopyDeep;
        p.BeginPath(); p.Arc(new Vector2(cx - 30f, by - 122f), 28f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx + 30f, by - 118f), 26f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx,       by - 144f), 34f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx - 48f, by - 138f),  8f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx + 44f, by - 148f), 10f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx + 10f, by - 176f),  7f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx - 15f, by - 182f),  6f, 0f, 360f); p.Fill();

        p.fillColor = canopyMid;
        p.BeginPath(); p.Arc(new Vector2(cx - 20f, by - 132f), 22f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx + 20f, by - 128f), 20f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx,       by - 148f), 24f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx - 38f, by - 146f), 11f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx + 34f, by - 156f), 13f, 0f, 360f); p.Fill();

        p.fillColor = canopyGlow;
        p.BeginPath(); p.Arc(new Vector2(cx - 10f, by - 140f), 14f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx + 12f, by - 136f), 12f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx,       by - 156f), 16f, 0f, 360f); p.Fill();

        p.fillColor = canopyTip;
        p.BeginPath(); p.Arc(new Vector2(cx,      by - 164f), 10f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx - 8f, by - 152f),  7f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx + 9f, by - 150f),  6f, 0f, 360f); p.Fill();

        // ── 7. Hanging vines (3 strands) ─────────────────────────────────────
        p.lineWidth = 1.2f;
        p.strokeColor = vineColor;
        p.BeginPath();
        p.MoveTo(new Vector2(cx - 26f, by - 106f));
        p.BezierCurveTo(new Vector2(cx - 29f, by - 94f), new Vector2(cx - 24f, by - 82f), new Vector2(cx - 28f, by - 68f));
        p.Stroke();
        p.BeginPath();
        p.MoveTo(new Vector2(cx + 24f, by - 102f));
        p.BezierCurveTo(new Vector2(cx + 21f, by - 90f), new Vector2(cx + 26f, by - 78f), new Vector2(cx + 22f, by - 64f));
        p.Stroke();
        p.BeginPath();
        p.MoveTo(new Vector2(cx,       by - 112f));
        p.BezierCurveTo(new Vector2(cx + 3f,   by - 100f), new Vector2(cx - 3f, by - 88f), new Vector2(cx + 1f, by - 74f));
        p.Stroke();

        void DrawVineLeaf(Vector2 pos, Color c)
        {
            p.fillColor = c;
            p.BeginPath();
            p.MoveTo(new Vector2(pos.x,      pos.y - 4f));
            p.LineTo(new Vector2(pos.x + 3f, pos.y));
            p.LineTo(new Vector2(pos.x,      pos.y + 4f));
            p.LineTo(new Vector2(pos.x - 3f, pos.y));
            p.Fill();
        }
        DrawVineLeaf(new Vector2(cx - 28f, by - 66f), canopyGlow);
        DrawVineLeaf(new Vector2(cx + 22f, by - 62f), canopyGlow);
        DrawVineLeaf(new Vector2(cx + 1f,  by - 72f), canopyTip);

        // ── 8. Glowing orbs — 3 colours, 3-ring halos ────────────────────────
        (Vector2 pos, Color col)[] orbDefs = {
            (new Vector2(cx - 14f, by - 146f), orbGold),
            (new Vector2(cx + 13f, by - 140f), orbCyan),
            (new Vector2(cx + 28f, by - 126f), orbGold),
            (new Vector2(cx - 26f, by - 120f), orbMagenta),
            (new Vector2(cx,       by - 162f), orbCyan),
            (new Vector2(cx - 5f,  by - 128f), orbGold),
        };

        foreach (var (pos, col) in orbDefs)
        {
            p.fillColor = new Color(col.r, col.g, col.b, 0.12f);
            p.BeginPath(); p.Arc(pos, 8f,   0f, 360f); p.Fill();
            p.fillColor = new Color(col.r, col.g, col.b, 0.30f);
            p.BeginPath(); p.Arc(pos, 4.5f, 0f, 360f); p.Fill();
            p.fillColor = new Color(col.r, col.g, col.b, 0.65f);
            p.BeginPath(); p.Arc(pos, 2.5f, 0f, 360f); p.Fill();
            p.fillColor = Color.white;
            p.BeginPath(); p.Arc(pos, 1.0f, 0f, 360f); p.Fill();
        }

        // ── 9. Sparkle cross-flares on brightest orbs ────────────────────────
        void DrawFlare(Vector2 pos, Color col, float len)
        {
            p.lineWidth = 0.8f;
            p.strokeColor = new Color(col.r, col.g, col.b, 0.70f);
            p.BeginPath(); p.MoveTo(new Vector2(pos.x - len, pos.y)); p.LineTo(new Vector2(pos.x + len, pos.y)); p.Stroke();
            p.BeginPath(); p.MoveTo(new Vector2(pos.x, pos.y - len)); p.LineTo(new Vector2(pos.x, pos.y + len)); p.Stroke();
            p.strokeColor = new Color(col.r, col.g, col.b, 0.35f);
            float d = len * 0.6f;
            p.BeginPath(); p.MoveTo(new Vector2(pos.x - d, pos.y - d)); p.LineTo(new Vector2(pos.x + d, pos.y + d)); p.Stroke();
            p.BeginPath(); p.MoveTo(new Vector2(pos.x + d, pos.y - d)); p.LineTo(new Vector2(pos.x - d, pos.y + d)); p.Stroke();
        }

        DrawFlare(new Vector2(cx,       by - 162f), orbCyan,    6f);
        DrawFlare(new Vector2(cx - 14f, by - 146f), orbGold,    5f);
        DrawFlare(new Vector2(cx - 26f, by - 120f), orbMagenta, 4f);
    }

    private void DrawFadeTop(MeshGenerationContext ctx)
    {
        DrawFade(ctx, _fadeTop.layout.width, _fadeTop.layout.height,
                 new Color(0.059f, 0.047f, 0.102f, 1f),
                 new Color(0.059f, 0.047f, 0.102f, 0f));
    }

    private void DrawFadeBottom(MeshGenerationContext ctx)
    {
        DrawFade(ctx, _fadeBottom.layout.width, _fadeBottom.layout.height,
                 new Color(0.059f, 0.047f, 0.102f, 0f),
                 new Color(0.059f, 0.047f, 0.102f, 1f));
    }

    private static void DrawFade(MeshGenerationContext ctx,
                                  float w, float h,
                                  Color colorTop, Color colorBottom)
    {
        var mesh = ctx.Allocate(4, 6);
        mesh.SetNextVertex(new Vertex { position = new Vector3(0, 0, Vertex.nearZ), tint = colorTop });
        mesh.SetNextVertex(new Vertex { position = new Vector3(w, 0, Vertex.nearZ), tint = colorTop });
        mesh.SetNextVertex(new Vertex { position = new Vector3(w, h, Vertex.nearZ), tint = colorBottom });
        mesh.SetNextVertex(new Vertex { position = new Vector3(0, h, Vertex.nearZ), tint = colorBottom });
        mesh.SetNextIndex(0); mesh.SetNextIndex(1); mesh.SetNextIndex(2);
        mesh.SetNextIndex(0); mesh.SetNextIndex(2); mesh.SetNextIndex(3);
    }

    private class StarData
    {
        public VisualElement Element;
        public float         TwinkleSpeed;
        public float         TwinkleOffset;
        public float         BaseOpacity;
    }

    private class DustParticle
    {
        public VisualElement Element;
        public float         StartLeft;
        public float         StartBottom;
        public float         BaseSize;
        public float         SwayOffset;
        public float         Age;
    }
}