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
        static readonly Color32[] LeafTints =
        {
            new Color32(188, 209, 174, 255), // Upper left, sage
            new Color32(250, 255, 244, 255), // Upper right, mint
            new Color32(156, 193, 164, 255), // Lower right, fern
            new Color32(250, 239, 207, 255)  // Lower left, pistachio
        };
        const float LogoWidth = 1000f;

        readonly RawImage[] leaves = new RawImage[LeafCount];
        CanvasGroup overlay;
        RectTransform artwork;
        RawImage blackout;
        RawImage backdrop;
        RawImage stem;
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

            var screen = new GameObject("Pori Custom Splash", typeof(RectTransform));
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

            blackout = AddImage("Black Backdrop", transform, Texture2D.whiteTexture);
            FillScreen(blackout.rectTransform);
            blackout.color = Color.black;
            blackout.raycastTarget = true;

            var content = new GameObject("Splash Artwork", typeof(RectTransform), typeof(CanvasGroup));
            var contentRect = (RectTransform)content.transform;
            contentRect.SetParent(transform, false);
            FillScreen(contentRect);
            overlay = content.GetComponent<CanvasGroup>();

            backdrop = AddImage("Ivory Backdrop", contentRect, Texture2D.whiteTexture);
            FillScreen(backdrop.rectTransform);
            artwork = new GameObject("Logo", typeof(RectTransform)).GetComponent<RectTransform>();
            artwork.SetParent(contentRect, false);

            // The atlas contains complete silhouettes. Each leaf opens around its own base.
            stem = AddPart("Stem", clover, 4, new Vector2(.59f, .88f), new Vector2(6, 9), 431, -4);
            leaves[0] = AddPart("Upper Left", clover, 0, new Vector2(.51f, .16f), new Vector2(-7, 5), 378, 51);
            leaves[1] = AddPart("Upper Right", clover, 1, new Vector2(.50f, .17f), new Vector2(6, 12), 364, -30);
            leaves[2] = AddPart("Lower Right", clover, 2, new Vector2(.49f, .16f), new Vector2(12, -3), 350, -119);
            leaves[3] = AddPart("Lower Left", clover, 3, new Vector2(.49f, .19f), new Vector2(-8, -6), 328, 145);
            foreach (var leaf in leaves)
            {
                var shadow = leaf.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color32(47, 72, 43, 88);
                // Keep the cast shadow down and to the right for every leaf orientation.
                shadow.effectDistance = Quaternion.Inverse(leaf.rectTransform.localRotation) * new Vector3(3, -4, 0);
            }
            wordmark = AddImage("Pori Custom", artwork, lettering);
            wordmark.rectTransform.sizeDelta = new Vector2(LogoWidth, LogoWidth * lettering.height / lettering.width);
            Layout();
            Animate(0);
            createdOnFrame = Time.frameCount;
        }

        RawImage AddPart(string name, Texture2D atlas, int cell, Vector2 pivot, Vector2 basePosition, float size, float angle)
        {
            var part = AddImage(name, artwork, atlas);
            part.uvRect = new Rect((cell % 3) / 3f, (1 - cell / 3) * .5f, 1f / 3f, .5f);
            var rect = part.rectTransform;
            rect.pivot = pivot;
            rect.anchoredPosition = basePosition;
            rect.sizeDelta = Vector2.one * size;
            rect.localRotation = Quaternion.Euler(0, 0, angle);
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
            overlay.gameObject.SetActive(false);
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
                Color tint = LeafTints[i];
                tint.a = opening;
                leaf.color = tint;
            }
            // Finish the stem with the last leaflet, before the lettering appears.
            stem.rectTransform.localScale = leaves[3].rectTransform.localScale;
            stem.color = new Color(1, 1, 1, leaves[3].color.a);

            float reveal = Ease((elapsed - WordmarkStart) / WordmarkDuration);
            wordmark.color = new Color(1, 1, 1, reveal);
            wordmark.rectTransform.anchoredPosition = Vector2.down * (wordmark.rectTransform.rect.height * .10f * (1f - reveal));
            overlay.alpha = 1f - Ease((elapsed - FadeOutStart) / FadeOutDuration);
        }

        static float Ease(float progress) => Mathf.SmoothStep(0, 1, progress);
    }
}
