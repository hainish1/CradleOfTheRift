using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class FallingLeavesEffect : MonoBehaviour
{
    [Header("References")]
    public UIDocument uiDocument;

    [Header("Leaf Settings")]
    [Tooltip("Optional leaf sprite with transparency. Leave empty to use the colored fallback.")]
    public Sprite leafSprite;
    [Tooltip("Maximum number of leaves on screen at once.")]
    public int maxLeaves = 25;
    [Tooltip("Seconds between each new leaf spawn.")]
    public float spawnInterval = 0.4f;
    [Tooltip("Min/max size of each leaf in UI pixels.")]
    public Vector2 leafSizeRange = new Vector2(20f, 45f);

    [Header("Movement")]
    public float minFallSpeed = 80f;
    public float maxFallSpeed = 180f;
    public float minDriftSpeed = 20f;
    public float maxDriftSpeed = 60f;
    public float minSwayAmount = 30f;
    public float maxSwayAmount = 80f;
    public float minSwaySpeed = 0.5f;
    public float maxSwaySpeed = 1.5f;
    public float minRotationSpeed = 30f;
    public float maxRotationSpeed = 120f;
    private const float CanvasWidth = 1920f;
    private const float CanvasHeight = 1080f;

    private VisualElement _leavesContainer;
    private readonly List<LeafData> _leaves = new List<LeafData>();
    private readonly List<LeafData> _toRemove = new List<LeafData>();
    private float _spawnTimer;

    // Pink/red petal palette
    private static readonly Color[] LeafColors =
    {
        new Color(0.95f, 0.40f, 0.50f, 1f), // hot pink
        new Color(0.85f, 0.20f, 0.30f, 1f), // deep rose red
        new Color(1.00f, 0.60f, 0.70f, 1f), // soft blush pink
        new Color(0.90f, 0.25f, 0.40f, 1f), // cherry red
        new Color(1.00f, 0.75f, 0.80f, 1f), // pale petal pink
        new Color(0.75f, 0.10f, 0.20f, 1f), // dark crimson
    };

    private class LeafData
    {
        public VisualElement Element;
        public float StartX;
        public float X, Y;
        public float FallSpeed;
        public float DriftSpeed;
        public float DriftDirection;
        public float SwayAmount;
        public float SwaySpeed;
        public float SwayOffset;
        public float Rotation;
        public float RotationSpeed;
        public float ElapsedTime;
        public float FadeInDuration; // seconds to fade in
    }

    void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[FallingLeavesEffect] UIDocument is not assigned.", this);
            return;
        }

        _leavesContainer = uiDocument.rootVisualElement.Q<VisualElement>("LeavesContainer");

        if (_leavesContainer == null)
        {
            Debug.LogError("[FallingLeavesEffect] Could not find 'LeavesContainer' in the UXML.", this);
            return;
        }

        // Pre-populate so the screen isn't empty at start
        int preSpawn = maxLeaves / 2;
        for (int i = 0; i < preSpawn; i++)
            SpawnLeaf(randomY: true);
    }

    void OnDisable()
    {
        foreach (var leaf in _leaves)
            leaf.Element?.RemoveFromHierarchy();

        _leaves.Clear();
    }

    void Update()
    {
        if (_leavesContainer == null) return;

        // Spawn new leaves
        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= spawnInterval && _leaves.Count < maxLeaves)
        {
            _spawnTimer = 0f;
            SpawnLeaf(randomY: false);
        }

        float dt = Time.deltaTime;
        _toRemove.Clear();

        foreach (var leaf in _leaves)
        {
            leaf.ElapsedTime += dt;

            // Fade in over FadeInDuration
            float opacity = leaf.ElapsedTime < leaf.FadeInDuration
                ? Mathf.Clamp01(leaf.ElapsedTime / leaf.FadeInDuration)
                : 1f;
            leaf.Element.style.opacity = opacity;

            // Sway horizontally using a sine wave
            float sway = Mathf.Sin(leaf.ElapsedTime * leaf.SwaySpeed + leaf.SwayOffset) * leaf.SwayAmount;
            leaf.X = leaf.StartX + sway + leaf.DriftSpeed * leaf.DriftDirection * leaf.ElapsedTime;
            leaf.Y += leaf.FallSpeed * dt;
            leaf.Rotation += leaf.RotationSpeed * dt;

            leaf.Element.style.left = leaf.X;
            leaf.Element.style.top = leaf.Y;
            leaf.Element.style.rotate = new StyleRotate(new Rotate(leaf.Rotation));

            // Recycle leaf once it falls off the bottom of the canvas
            if (leaf.Y > CanvasHeight + 60f)
                _toRemove.Add(leaf);
        }

        foreach (var leaf in _toRemove)
        {
            leaf.Element.RemoveFromHierarchy();
            _leaves.Remove(leaf);
        }
    }

    private void SpawnLeaf(bool randomY)
    {
        var element = new VisualElement();
        element.AddToClassList("leaf");

        float size = Random.Range(leafSizeRange.x, leafSizeRange.y);
        element.style.width = size;
        element.style.height = size;
        element.style.opacity = 0f; // starts transparent, fades in

        if (leafSprite != null)
        {
            element.style.backgroundImage = new StyleBackground(leafSprite);
        }
        else
        {
            // Leaf shape: tall and narrow with one pointed tip and one rounded base.
            // Width is roughly half the height to avoid the "lemon" look.
            element.style.width  = size * 0.55f;
            element.style.height = size;
            element.style.backgroundColor = LeafColors[Random.Range(0, LeafColors.Length)];
            // Top corners: one sharp tip (pointed), one softer curve
            element.style.borderTopLeftRadius     = size * 0.05f; // sharp tip side
            element.style.borderTopRightRadius    = size * 0.50f; // curved side
            // Bottom: wide rounded base
            element.style.borderBottomLeftRadius  = size * 0.50f;
            element.style.borderBottomRightRadius = size * 0.50f;
        }

        float startX = Random.Range(-50f, CanvasWidth + 50f);
        float startY = randomY
            ? Random.Range(-200f, CanvasHeight * 0.8f)  // spread across screen on init
            : Random.Range(-80f, -10f);                  // just above top on normal spawn

        var data = new LeafData
        {
            Element       = element,
            StartX        = startX,
            X             = startX,
            Y             = startY,
            FallSpeed     = Random.Range(minFallSpeed, maxFallSpeed),
            DriftSpeed    = Random.Range(minDriftSpeed, maxDriftSpeed),
            DriftDirection= Random.value > 0.5f ? 1f : -1f,
            SwayAmount    = Random.Range(minSwayAmount, maxSwayAmount),
            SwaySpeed     = Random.Range(minSwaySpeed, maxSwaySpeed),
            SwayOffset    = Random.Range(0f, Mathf.PI * 2f),
            Rotation      = Random.Range(0f, 360f),
            RotationSpeed = Random.Range(minRotationSpeed, maxRotationSpeed) * (Random.value > 0.5f ? 1f : -1f),
            ElapsedTime   = 0f,
            FadeInDuration= Random.Range(0.3f, 0.8f),
        };

        element.style.left = startX;
        element.style.top  = startY;

        _leavesContainer.Add(element);
        _leaves.Add(data);
    }
}
