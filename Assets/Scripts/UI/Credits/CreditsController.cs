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

    private void DrawTree(MeshGenerationContext ctx, bool isRight)
    {
        var p = ctx.painter2D;
        float cx = 50f; // Center of the 100px wide tree

        // 1. Double-layered magical aura
        Color auraColor = isRight 
            ? new Color(0.67f, 0.85f, 1f, 0.15f) 
            : new Color(0.78f, 0.72f, 1f, 0.15f);
            
        p.fillColor = auraColor;
        p.BeginPath(); p.Arc(new Vector2(cx, 55f), 65f, 0f, 360f); p.Fill();
        
        p.fillColor = new Color(auraColor.r, auraColor.g, auraColor.b, 0.25f);
        p.BeginPath(); p.Arc(new Vector2(cx, 55f), 45f, 0f, 360f); p.Fill();

        // 2. Ancient Trunk with Flared Roots
        p.fillColor = new Color(0.15f, 0.10f, 0.25f, 1f); // Darker, richer wood
        p.BeginPath();
        p.MoveTo(new Vector2(cx - 18f, 140f)); // Left root
        p.BezierCurveTo(
            new Vector2(cx - 8f, 115f),
            new Vector2(cx - 4f, 80f),
            new Vector2(cx, 60f));
        p.BezierCurveTo(
            new Vector2(cx + 4f, 80f),
            new Vector2(cx + 8f, 115f),
            new Vector2(cx + 18f, 140f)); // Right root
        p.Fill();

        // 3. Glowing Sap Vein on Trunk
        p.lineWidth = 1.5f;
        p.strokeColor = isRight 
            ? new Color(0.66f, 0.85f, 1f, 0.6f) 
            : new Color(0.78f, 0.72f, 1f, 0.6f);
        p.BeginPath();
        p.MoveTo(new Vector2(cx - 2f, 135f));
        p.BezierCurveTo(new Vector2(cx + 4f, 110f), new Vector2(cx - 5f, 90f), new Vector2(cx + 1f, 70f));
        p.Stroke();

        // 4. Branches
        p.lineWidth = 3f;
        p.strokeColor = new Color(0.15f, 0.10f, 0.25f, 1f);
        p.BeginPath();
        p.MoveTo(new Vector2(cx, 80f));
        p.BezierCurveTo(new Vector2(cx - 15f, 75f), new Vector2(cx - 30f, 60f), new Vector2(cx - 35f, 45f));
        p.Stroke();
        p.BeginPath();
        p.MoveTo(new Vector2(cx, 90f));
        p.BezierCurveTo(new Vector2(cx + 15f, 85f), new Vector2(cx + 30f, 70f), new Vector2(cx + 35f, 55f));
        p.Stroke();

        // 5. Canopy Setup
        Color primaryCanopy = isRight 
            ? new Color(0.29f, 0.48f, 0.69f, 0.95f) 
            : new Color(0.48f, 0.37f, 0.75f, 0.95f);
            
        Color highlightCanopy = isRight 
            ? new Color(0.66f, 0.85f, 1f, 0.85f) 
            : new Color(0.78f, 0.72f, 1f, 0.85f);
            
        Color magicCore = new Color(0.99f, 0.94f, 0.65f, 1f); // Golden starlight

        // Back leaves
        p.fillColor = primaryCanopy;
        p.BeginPath(); p.Arc(new Vector2(cx - 28f, 55f), 26f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx + 28f, 60f), 24f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx, 35f), 32f, 0f, 360f); p.Fill();
        
        // Floating magical leaf clusters (disconnected from the tree)
        p.BeginPath(); p.Arc(new Vector2(cx - 45f, 40f), 6f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx + 40f, 30f), 8f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx, 5f), 7f, 0f, 360f); p.Fill();

        // Front/highlight leaves
        p.fillColor = highlightCanopy;
        p.BeginPath(); p.Arc(new Vector2(cx - 18f, 45f), 20f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx + 18f, 50f), 18f, 0f, 360f); p.Fill();
        p.BeginPath(); p.Arc(new Vector2(cx, 30f), 22f, 0f, 360f); p.Fill();

        // 6. Hanging magical vines
        p.lineWidth = 1.5f;
        p.strokeColor = highlightCanopy;
        p.BeginPath(); 
        p.MoveTo(new Vector2(cx - 25f, 75f)); 
        p.BezierCurveTo(new Vector2(cx - 27f, 85f), new Vector2(cx - 23f, 95f), new Vector2(cx - 26f, 105f)); 
        p.Stroke();
        
        p.BeginPath(); 
        p.MoveTo(new Vector2(cx + 22f, 80f)); 
        p.BezierCurveTo(new Vector2(cx + 20f, 90f), new Vector2(cx + 24f, 100f), new Vector2(cx + 21f, 110f)); 
        p.Stroke();

        // 7. Embedded glowing orbs/fruit inside the canopy
        Vector2[] orbs = {
            new Vector2(cx - 15f, 35f),
            new Vector2(cx + 12f, 42f),
            new Vector2(cx + 26f, 55f),
            new Vector2(cx - 25f, 62f),
            new Vector2(cx, 20f)
        };
        
        foreach(var pos in orbs) 
        {
            // Soft halo
            p.fillColor = new Color(magicCore.r, magicCore.g, magicCore.b, 0.4f);
            p.BeginPath(); p.Arc(pos, 4f, 0f, 360f); p.Fill();
            // Bright solid core
            p.fillColor = magicCore;
            p.BeginPath(); p.Arc(pos, 1.5f, 0f, 360f); p.Fill();
        }
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