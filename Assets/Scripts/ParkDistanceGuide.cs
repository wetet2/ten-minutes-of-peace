using UnityEngine;
using UnityEngine.UI;
using static PeacePark.ParkWelcome;

namespace PeacePark
{
    internal sealed class ParkDistanceGuide : MonoBehaviour
    {
        internal const string Heading = "화면에서 한 걸음\n뒤로 가 볼까요?";
        internal const string ToolbarHint = "화면 거리 안내";

        RawImage illustration;
        Text heading;
        Rect lastSafeArea;
        int lastHeight;
        bool useKorean;

        internal void Build(ParkPlayground park)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            gameObject.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)transform;

            var cover = Child("Opaque screen cover", root).gameObject.AddComponent<Image>();
            cover.color = new Color32(255, 251, 240, 255);
            cover.rectTransform.anchorMin = Vector2.zero;
            cover.rectTransform.anchorMax = Vector2.one;
            cover.rectTransform.offsetMin = cover.rectTransform.offsetMax = Vector2.zero;
            var dismiss = cover.gameObject.AddComponent<Button>();
            dismiss.targetGraphic = cover;
            dismiss.transition = Selectable.Transition.None;
            dismiss.navigation = new Navigation { mode = Navigation.Mode.None };
            dismiss.onClick.AddListener(park.CloseDialog);

            illustration = Child("Bunny stepping back", root).gameObject.AddComponent<RawImage>();
            illustration.texture = Resources.Load<Texture2D>("Welcome/DistanceGuideRabbit");
            illustration.raycastTarget = false;

            heading = Label("Reminder heading", root, Heading, ParkFonts.Bold);
            heading.color = new Color32(37, 73, 55, 255);
            heading.verticalOverflow = VerticalWrapMode.Truncate;
            heading.resizeTextForBestFit = true;
            SetLanguage(ParkLanguage.IsKorean);
        }

        internal void SetLanguage(bool korean)
        {
            useKorean = korean;
            heading.text = ParkLanguage.Translate(Heading, korean);
            heading.font = ParkFonts.BoldFor(korean);
            lastSafeArea = default;
            Update();
        }

        void OnEnable() => Update();

        void Update()
        {
            var safe = Screen.safeArea;
            if (!heading || safe.width <= 0 || safe.height <= 0 || (lastSafeArea == safe && lastHeight == Screen.height)) return;
            lastSafeArea = safe;
            lastHeight = Screen.height;

            float scale = Mathf.Max(1, Mathf.Min(safe.width / 1280f, safe.height / 720f));
            float artWidth = Mathf.Min(460 * scale, safe.width * .83f, safe.height * .65f);
            float ratio = illustration.texture ? (float)illustration.texture.height / illustration.texture.width : 2f / 3f;
            float artHeight = artWidth * ratio;
            float gap = Mathf.Clamp(safe.height * .041f, 21 * scale, 33 * scale);
            float fontSize = useKorean
                ? Mathf.Clamp(safe.width * .05f, (safe.width <= 420 ? 31 : 36) * scale, 49 * scale)
                : Mathf.Clamp(safe.width * .068f, 23 * scale, 43 * scale);
            float textHeight = fontSize * 2.42f;
            float groupHeight = artHeight + gap + textHeight;
            float fit = Mathf.Min(1, safe.height * .90f / groupHeight);
            float top = Screen.height - safe.center.y - safe.height * .015f - groupHeight * fit / 2;

            Place(illustration.rectTransform,
                new Rect(safe.center.x - artWidth * fit / 2, top, artWidth * fit, artHeight * fit));
            float textWidth = safe.width * .91f;
            Place(heading.rectTransform,
                new Rect(safe.center.x - textWidth / 2, top + (artHeight + gap) * fit, textWidth, textHeight * fit));
            heading.fontSize = heading.resizeTextMaxSize = Mathf.Max(1, Mathf.RoundToInt(fontSize * fit));
            heading.resizeTextMinSize = Mathf.Min(14, heading.resizeTextMaxSize);
        }

        internal static Texture2D CreateToolbarIcon()
        {
            const int size = 96;
            var icon = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {name = "Step back reminder icon", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear};
            var pixels = new Color32[size * size];
            var stem = new Vector2(0, -5);
            var tip = new Vector2(0, -34);
            var left = new Vector2(-13, -21);
            var right = new Vector2(13, -21);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var point = new Vector2(x + .5f - size / 2, y + .5f - size / 2);
                var edge = new Vector2(Mathf.Abs(point.x) - 26, Mathf.Abs(point.y - 17) - 12);
                float screen = new Vector2(Mathf.Max(0, edge.x), Mathf.Max(0, edge.y)).magnitude
                    + Mathf.Min(0, Mathf.Max(edge.x, edge.y)) - 6;
                float arrow = Mathf.Min(Segment(point, stem, tip), Mathf.Min(Segment(point, left, tip), Segment(point, right, tip)));
                float opacity = Mathf.Clamp01(3.8f - Mathf.Min(Mathf.Abs(screen), arrow));
                pixels[y * size + x] = new Color32(80, 62, 47, (byte)Mathf.RoundToInt(opacity * 255));
            }
            icon.SetPixels32(pixels);
            icon.Apply(false, true);
            return icon;

            static float Segment(Vector2 point, Vector2 a, Vector2 b)
            {
                var line = b - a;
                var delta = point - a;
                return (delta - line * Mathf.Clamp01(Vector2.Dot(delta, line) / line.sqrMagnitude)).magnitude;
            }
        }
    }
}
