using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class CreditsController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Scroll Settings")]
    [SerializeField] private float scrollSpeed = 60f;   // px per second
    [SerializeField] private float fadeHeight  = 100f;  // px of fade at top/bottom

    public System.Action OnCreditsFinished;             // wire this up in your GameManager

    private ScrollView  _scrollView;
    private VisualElement _container;
    private VisualElement _fadeTop;
    private VisualElement _fadeBottom;
    private Button      _skipButton;

    private bool  _scrolling  = false;
    private float _maxScroll  = 0f;

    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        _scrollView  = root.Q<ScrollView>("credits-scroll");
        _container   = root.Q<VisualElement>("credits-container");
        _fadeTop     = root.Q<VisualElement>("fade-top");
        _fadeBottom  = root.Q<VisualElement>("fade-bottom");
        _skipButton  = root.Q<Button>("skip-button");

        // Register fade painters
        _fadeTop.generateVisualContent    += DrawFadeTop;
        _fadeBottom.generateVisualContent += DrawFadeBottom;

        // Skip button
        _skipButton.clicked += FinishCredits;

        // Wait one frame for layout to resolve before starting scroll
        _scrollView.RegisterCallback<GeometryChangedEvent>(OnLayoutReady);
    }

    private void OnDisable()
    {
        if (_fadeTop    != null) _fadeTop.generateVisualContent    -= DrawFadeTop;
        if (_fadeBottom != null) _fadeBottom.generateVisualContent -= DrawFadeBottom;
        if (_skipButton != null) _skipButton.clicked               -= FinishCredits;
    }

    // Called once the ScrollView has a real size
    private void OnLayoutReady(GeometryChangedEvent evt)
    {
        _scrollView.UnregisterCallback<GeometryChangedEvent>(OnLayoutReady);

        float panelHeight = _scrollView.layout.height;

        // Pad top so first line enters from bottom of screen
        _container.style.paddingTop = panelHeight;

        // One more frame to let that padding reflow
        StartCoroutine(StartScrollNextFrame());
    }

    private IEnumerator StartScrollNextFrame()
    {
        yield return null;

        // Scroll to the very top first (offset 0 = top of content)
        _scrollView.scrollOffset = Vector2.zero;

        // Max scroll = full content height minus visible panel height
        float contentHeight = _container.layout.height;
        float panelHeight   = _scrollView.layout.height;
        _maxScroll          = Mathf.Max(0f, contentHeight - panelHeight);

        _scrolling = true;
    }

    private void Update()
    {
        if (!_scrolling) return;

        float newY = _scrollView.scrollOffset.y + scrollSpeed * Time.deltaTime;
        _scrollView.scrollOffset = new Vector2(0f, newY);

        if (_scrollView.scrollOffset.y >= _maxScroll)
            FinishCredits();
    }

    private void FinishCredits()
    {
        _scrolling = false;
        OnCreditsFinished?.Invoke();
    }

    // ── Gradient fade painters ────────────────────────────────────────────
    // USS doesn't support gradients, so we paint them manually via the Mesh API

    private void DrawFadeTop(MeshGenerationContext ctx)
    {
        DrawFade(ctx, _fadeTop.layout.width, _fadeTop.layout.height,
                 new Color(0.039f, 0.039f, 0.071f, 1f),   // opaque at top
                 new Color(0.039f, 0.039f, 0.071f, 0f));  // transparent at bottom
    }

    private void DrawFadeBottom(MeshGenerationContext ctx)
    {
        DrawFade(ctx, _fadeBottom.layout.width, _fadeBottom.layout.height,
                 new Color(0.039f, 0.039f, 0.071f, 0f),   // transparent at top
                 new Color(0.039f, 0.039f, 0.071f, 1f));  // opaque at bottom
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
}