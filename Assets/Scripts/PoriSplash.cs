using System;
using UnityEngine;
using UnityEngine.UI;

namespace PeacePark
{
    internal sealed class PoriSplash : MonoBehaviour
    {
        const float FadeInDuration = .60f;
        const int LeafCount = 4;
        const float FirstLeafStart = .50f;
        const float LeafOpeningDuration = .20f;
        const float LeafInterval = LeafOpeningDuration * .5f;
        const float WordmarkStart = FirstLeafStart + (LeafCount - 1) * LeafInterval + LeafOpeningDuration + .08f;
        const float WordmarkDuration = .30f;
        const float WordmarkHoldDuration = 1.34f;
        const float FadeOutStart = WordmarkStart + WordmarkDuration + WordmarkHoldDuration;
        const float FadeOutDuration = .85f;
        const float SplashDuration = FadeOutStart + FadeOutDuration;
        const float MenuFadeInDuration = .45f;
        static readonly Color Background = new Color32(251, 245, 229, 255);
        const float LogoWidth = 1000f;
        const float CloverSize = 668f;
        static readonly Vector2 LeafBase = new Vector2(.486f, .454f);

        readonly RawImage[] leaves = new RawImage[LeafCount];
        RectTransform content;
        RectTransform artwork;
        RawImage blackout;
        RawImage backdrop;
        RawImage wordmark;
        Action showMenu;
        bool revealingMenu;
        int createdOnFrame;
        float elapsed;

        internal static PoriSplash Play(Transform parent, Action showMenu)
        {
            var clover = Resources.Load<Texture2D>("PoriSplash/Clover");
            var wordmark = Resources.Load<Texture2D>("PoriSplash/Wordmark");
            if (!clover || !wordmark) return null;

            var screen = new GameObject("Pori Games Splash", typeof(RectTransform));
            screen.transform.SetParent(parent, false);
            var splash = screen.AddComponent<PoriSplash>();
            splash.Build(clover, wordmark, showMenu);
            return splash;
        }

        void Build(Texture2D clover, Texture2D lettering, Action showMenu)
        {
            this.showMenu = showMenu;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            gameObject.AddComponent<GraphicRaycaster>();

            content = new GameObject("Splash Artwork", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(transform, false);
            FillScreen(content);

            backdrop = AddImage("Ivory Backdrop", content, Texture2D.whiteTexture);
            FillScreen(backdrop.rectTransform);
            artwork = new GameObject("Logo", typeof(RectTransform)).GetComponent<RectTransform>();
            artwork.SetParent(content, false);

            leaves[0] = AddLeaf("Upper Left", clover, 0);
            leaves[1] = AddLeaf("Upper Right", clover, 1);
            leaves[2] = AddLeaf("Lower Right", clover, 2);
            leaves[3] = AddLeaf("Lower Left", clover, 3);
            wordmark = AddImage("Pori Games", artwork, lettering);
            wordmark.rectTransform.sizeDelta = new Vector2(LogoWidth, LogoWidth * lettering.height / lettering.width);

            // Fade the rendered artwork by drawing black over the entire composition.
            blackout = AddImage("Black Fade", transform, Texture2D.whiteTexture);
            FillScreen(blackout.rectTransform);
            blackout.raycastTarget = true;

            Layout();
            Animate(0);
            createdOnFrame = Time.frameCount;
        }

        RawImage AddLeaf(string name, Texture2D atlas, int cell)
        {
            var part = AddImage(name, artwork, atlas);
            // All four atlas cells use the same artboard. The last leaf carries the curved stem.
            part.uvRect = new Rect((cell % 2) * .5f, (1 - cell / 2) * .5f, .5f, .5f);
            var rect = part.rectTransform;
            rect.pivot = LeafBase;
            rect.sizeDelta = Vector2.one * CloverSize;
            rect.anchoredPosition = (LeafBase - Vector2.one * .5f) * CloverSize + Vector2.down * 8f;
            return part;
        }

        static RawImage AddImage(string name, Transform parent, Texture texture)
        {
            var image = new GameObject(name, typeof(RectTransform)).AddComponent<RawImage>();
            image.transform.SetParent(parent, false);
            image.texture = texture;
            image.raycastTarget = false;
            return image;
        }

        static void FillScreen(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        void Layout()
        {
            Rect area = Screen.safeArea;
            float width = Mathf.Min(area.width * .61f, area.height * 1.07f);
            artwork.anchoredPosition = area.center - new Vector2(Screen.width, Screen.height) * .5f;
            artwork.localScale = Vector3.one * (width / LogoWidth);
        }

        void Update()
        {
            if (Time.frameCount == createdOnFrame) return;
            // Keep long startup or resume frames from skipping most of a fade.
            elapsed += Mathf.Min(Time.unscaledDeltaTime, .05f);
            if (revealingMenu)
            {
                blackout.color = new Color(0, 0, 0, 1f - Ease(elapsed / MenuFadeInDuration));
                if (elapsed >= MenuFadeInDuration) Destroy(gameObject);
                return;
            }

            Layout();
            Animate(elapsed);
            if (elapsed < SplashDuration) return;

            // Keep one fully black frame between the two fades.
            content.gameObject.SetActive(false);
            showMenu();
            revealingMenu = true;
            elapsed = 0f;
        }

        void Animate(float elapsed)
        {
            backdrop.color = Color.Lerp(Color.black, Background, Ease(elapsed / FadeInDuration));
            for (int i = 0; i < leaves.Length; i++)
            {
                float opening = Ease((elapsed - FirstLeafStart - i * LeafInterval) / LeafOpeningDuration);
                var leaf = leaves[i];
                leaf.rectTransform.localScale = Vector3.one * Mathf.Lerp(.12f, 1f, opening);
                leaf.color = new Color(1, 1, 1, opening);
            }

            float reveal = Ease((elapsed - WordmarkStart) / WordmarkDuration);
            wordmark.color = new Color(1, 1, 1, reveal);
            wordmark.rectTransform.anchoredPosition = Vector2.down * (wordmark.rectTransform.rect.height * .10f * (1f - reveal));
            blackout.color = new Color(0, 0, 0, Ease((elapsed - FadeOutStart) / FadeOutDuration));
        }

        static float Ease(float progress) => Mathf.SmoothStep(0, 1, progress);
    }
}
