using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PeacePark
{
    public sealed class ParkWelcome : MonoBehaviour
    {
        static readonly Color Ink = new Color32(22, 57, 25, 255);
        static readonly string[] AnimalNames = { "Bear", "Tiger", "Frog", "Giraffe", "Elephant" };
        // Center, baseline, maximum width and height, relative to the illustrated stage.
        static readonly Vector4[] LandscapeAnimals = {
            new Vector4(.164f, .707f, .222f, .469f),
            new Vector4(.350f, .669f, .188f, .365f),
            new Vector4(.518f, .737f, .169f, .241f),
            new Vector4(.704f, .657f, .175f, .506f),
            new Vector4(.868f, .720f, .253f, .411f)
        };
        static readonly Vector4[] PortraitAnimals = {
            new Vector4(.20f, .492f, .35f, .277f),
            new Vector4(.29f, .715f, .39f, .211f),
            new Vector4(.69f, .718f, .36f, .180f),
            new Vector4(.51f, .477f, .29f, .309f),
            new Vector4(.81f, .508f, .36f, .225f)
        };

        RectTransform canvasRect, content;
        RawImage meadow, wordmark;
        Text tagline, status;
        readonly RawImage[] animals = new RawImage[AnimalNames.Length];
        readonly RawImage[] groundShadows = new RawImage[AnimalNames.Length];
        ModeButton host, coloring;
        Sprite buttonSurface;
        Texture2D buttonTexture, shadowTexture;
        Rect previousArea;
        Vector2Int previousScreen;

        internal void Build(ParkPlayground park)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameObject.AddComponent<GraphicRaycaster>();
            canvasRect = (RectTransform)transform;
            meadow = Art("Park Illustration", canvasRect, "Meadow");
            content = Child("Safe Area", canvasRect);
            wordmark = Art("Ten Minutes of Peace", content, "Title");
            tagline = Label("Tagline", content, "애들아 놀아라. 아빠 10분만 쉰다", ParkFonts.Bold);
            shadowTexture = CreateGroundShadow();
            for (int i = 0; i < groundShadows.Length; i++)
            {
                var shadowImage = Child(AnimalNames[i] + " Ground Shadow", content).gameObject.AddComponent<RawImage>();
                shadowImage.texture = shadowTexture;
                shadowImage.raycastTarget = false;
                groundShadows[i] = shadowImage;
            }
            foreach (int index in new[] { 0, 1, 3, 4, 2 })
                animals[index] = Art(AnimalNames[index], content, AnimalNames[index]);
            buttonSurface = CreateButtonSurface();
            host = new ModeButton(this, "놀이 공간 열기", "이 기기에서 공간을 열어요", "Gate",
                new Color32(188, 239, 129, 255));
            coloring = new ModeButton(this, "동물 색칠하기", "색칠한 친구를 보내요", "Crayons",
                new Color32(255, 188, 110, 255));
            host.Button.onClick.AddListener(() =>
            {
                if (!park.ChooseHostMode()) status.text = park.Lan.Message;
            });
            coloring.Button.onClick.AddListener(park.ChooseDrawingMode);
            status = Label("Connection Status", content, "", ParkFonts.Bold);
            status.color = new Color32(255, 252, 229, 255);
            var shadow = status.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color32(30, 61, 23, 230);
            shadow.effectDistance = new Vector2(1, -2);
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
            if (!EventSystem.current)
            {
                var events = new GameObject("Start Screen Input", typeof(EventSystem), typeof(StandaloneInputModule));
                events.transform.SetParent(park.transform, false);
            }
            RefreshLayout();
        }

        internal void SetVisible(bool visible)
        {
            if (visible) status.text = "";
            gameObject.SetActive(visible);
            if (visible) RefreshLayout();
        }

        internal void SetModalOpen(bool open)
        {
            host.Button.interactable = coloring.Button.interactable = !open;
            if (open) status.text = "";
        }

        void Update() => RefreshLayout();

        void RefreshLayout()
        {
            var screen = new Vector2Int(Screen.width, Screen.height);
            var area = Screen.safeArea;
            if (screen == previousScreen && area == previousArea) return;
            if (screen.x <= 0 || screen.y <= 0 || area.width <= 0 || area.height <= 0) return;
            previousScreen = screen;
            previousArea = area;

            float ratio = meadow.texture ? (float)meadow.texture.width / meadow.texture.height : 16f / 9f;
            float coverWidth = Mathf.Max(screen.x, screen.y * ratio);
            float coverHeight = coverWidth / ratio;
            Place(meadow.rectTransform, new Rect((screen.x - coverWidth) / 2, (screen.y - coverHeight) / 2, coverWidth, coverHeight));
            Place(content, new Rect(area.xMin, screen.y - area.yMax, area.width, area.height));
            float height = area.height;
            float width = Mathf.Min(area.width, height * 2f);
            float left = (area.width - width) / 2;
            float center = area.width / 2;
            bool portrait = width < height;
            float titleWidth = portrait ? width * .92f : Mathf.Min(width * .75f, height * .985f);
            Fit(wordmark, new Rect(center - titleWidth / 2, height * (portrait ? .034f : .037f),
                titleWidth, height * (portrait ? .089f : .128f)));
            Place(tagline.rectTransform, new Rect(left, height * (portrait ? .119f : .158f), width, height * .053f));
            tagline.fontSize = Pixels(Mathf.Min(width * .052f, height * .038f));

            var positions = portrait ? PortraitAnimals : LandscapeAnimals;
            for (int i = 0; i < animals.Length; i++)
            {
                var position = positions[i];
                float artWidth = width * position.z;
                float artHeight = height * position.w;
                Fit(animals[i], new Rect(left + width * position.x - artWidth / 2,
                    height * position.y - artHeight, artWidth, artHeight), bottom: true);
                float shadowWidth = animals[i].rectTransform.sizeDelta.x * .88f;
                Place(groundShadows[i].rectTransform, new Rect(left + width * position.x - shadowWidth / 2,
                    height * (position.y - .018f), shadowWidth, height * .034f));
            }

            float gap = height * (portrait ? .010f : .017f);
            float buttonHeight = height * (portrait ? .093f : .136f);
            float buttonWidth = portrait ? Mathf.Min(width * .86f, height * .47f)
                : (Mathf.Min(width * .91f, height * .836f) - gap) / 2;
            float buttonTop = height * (portrait ? .746f : .812f);
            float buttonLeft = center - (portrait ? buttonWidth : buttonWidth * 2 + gap) / 2;
            host.Layout(new Rect(buttonLeft, buttonTop, buttonWidth, buttonHeight));
            coloring.Layout(new Rect(portrait ? buttonLeft : buttonLeft + buttonWidth + gap,
                portrait ? buttonTop + buttonHeight + gap : buttonTop, buttonWidth, buttonHeight));
            Place(status.rectTransform, new Rect(left + width * .04f, height * .945f, width * .92f, height * .053f));
            status.fontSize = Pixels(Mathf.Min(width * .031f, height * .022f));
        }

        sealed class ModeButton
        {
            public readonly Button Button;
            readonly Image surface;
            readonly RawImage icon;
            readonly Text title, description;

            public ModeButton(ParkWelcome welcome, string text, string detail, string asset, Color color)
            {
                var root = Child(text, welcome.content);
                surface = root.gameObject.AddComponent<Image>();
                surface.sprite = welcome.buttonSurface;
                surface.type = Image.Type.Sliced;
                surface.color = color;
                Button = root.gameObject.AddComponent<Button>();
                Button.targetGraphic = surface;
                var colors = Button.colors;
                colors.highlightedColor = Color.white;
                colors.selectedColor = new Color32(255, 250, 226, 255);
                colors.pressedColor = new Color32(219, 219, 219, 255);
                Button.colors = colors;
                icon = Art(asset, root, asset);
                title = Label("Label", root, text, ParkFonts.Bold, TextAnchor.MiddleLeft);
                description = Label("Description", root, detail, ParkFonts.Regular, TextAnchor.MiddleLeft);
            }

            public void Layout(Rect bounds)
            {
                Place(surface.rectTransform, bounds);
                surface.pixelsPerUnitMultiplier = 144f / bounds.height;
                float w = bounds.width, h = bounds.height;
                Fit(icon, new Rect(w * .062f, h * .13f, w * .275f, h * .67f));
                Place(title.rectTransform, new Rect(w * .355f, h * .128f, w * .59f, h * .39f));
                Place(description.rectTransform, new Rect(w * .355f, h * .52f, w * .59f, h * .25f));
                title.fontSize = Pixels(Mathf.Min(w * .092f, h * .32f));
                description.fontSize = Pixels(Mathf.Min(w * .053f, h * .177f));
            }
        }

        static int Pixels(float size) => Mathf.Max(1, Mathf.RoundToInt(size));

        internal static RectTransform Child(string name, RectTransform parent)
        {
            var child = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)child.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            return rect;
        }

        static RawImage Art(string name, RectTransform parent, string asset)
        {
            var image = Child(name, parent).gameObject.AddComponent<RawImage>();
            image.texture = Resources.Load<Texture2D>("Welcome/" + asset);
            image.raycastTarget = false;
            image.enabled = image.texture != null;
            return image;
        }

        internal static Text Label(string name, RectTransform parent, string text, Font font, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var label = Child(name, parent).gameObject.AddComponent<Text>();
            label.text = text;
            label.font = font;
            label.color = Ink;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        internal static void Place(RectTransform target, Rect bounds)
        {
            target.anchoredPosition = new Vector2(bounds.x, -bounds.y);
            target.sizeDelta = bounds.size;
        }

        static void Fit(RawImage image, Rect bounds, bool bottom = false)
        {
            if (!image.texture) return;
            float factor = Mathf.Min(bounds.width / image.texture.width, bounds.height / image.texture.height);
            float width = image.texture.width * factor, height = image.texture.height * factor;
            Place(image.rectTransform, new Rect(bounds.center.x - width / 2,
                bottom ? bounds.yMax - height : bounds.center.y - height / 2, width, height));
        }

        Sprite CreateButtonSurface()
        {
            const int size = 144;
            buttonTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) {
                name = "Welcome button paper", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float edge = new Vector2(Mathf.Max(0, Mathf.Abs(x - 71.5f) - 8),
                    Mathf.Max(0, Mathf.Abs(y - 76) - 2)).magnitude - 54;
                float shade = Mathf.Exp(-Mathf.Max(0, edge + 3) / 4) * .23f;
                float fill = Mathf.Clamp01(.5f - edge);
                float grain = (((x * 17 + y * 31 + x * y * 7) % 29) / 28f - .5f) * .045f;
                grain *= Mathf.Clamp01((Mathf.Abs(x - 71.5f) - 9) / 12);
                float rim = Mathf.Exp(-Mathf.Abs(edge + 3.5f) / 1.75f);
                float upper = Mathf.InverseLerp(18, 131, y);
                float tone = Mathf.Clamp01(.88f + .12f * upper + (.49f * upper - .23f) * rim + grain);
                var pixel = Color.Lerp(new Color(.24f, .28f, .12f, shade), new Color(tone, tone, tone, 1), fill);
                pixels[y * size + x] = pixel;
            }
            buttonTexture.SetPixels32(pixels);
            buttonTexture.Apply(false, true);
            return Sprite.Create(buttonTexture, new Rect(0, 0, size, size), new Vector2(.5f, .5f),
                100, 0, SpriteMeshType.FullRect, new Vector4(65, 65, 65, 65));
        }

        static Texture2D CreateGroundShadow()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) {
                name = "Soft grass shadow", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var offset = new Vector2((x + .5f) / size * 2 - 1, (y + .5f) / size * 2 - 1);
                float alpha = Mathf.Clamp01(1 - offset.sqrMagnitude);
                pixels[y * size + x] = new Color32(35, 58, 17, (byte)(68 * alpha * alpha));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        void OnDestroy()
        {
            Destroy(buttonSurface);
            Destroy(buttonTexture);
            Destroy(shadowTexture);
        }
    }
}
