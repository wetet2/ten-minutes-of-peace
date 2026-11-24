using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PeacePark
{
    public sealed class ParkWelcome : MonoBehaviour
    {
        static readonly Color Ink = new Color32(22, 57, 25, 255);
        static readonly string[] AnimalNames = { "Bear", "Tiger", "Frog", "Giraffe", "Elephant" };
        // Match the reading order of the illustrated atlas.
        static readonly string[] CoverNames = { "공원", "숲", "놀이터", "사막", "시계탑 광장", "놀이공원" };
        static readonly Color32[] CoverColors = {
            new Color32(244, 248, 224, 255), new Color32(226, 242, 218, 255), new Color32(229, 245, 246, 255),
            new Color32(255, 239, 211, 255), new Color32(239, 235, 255, 255), new Color32(254, 233, 243, 255)
        };
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

        ParkPlayground park;
        RectTransform canvasRect, content, homeHeading, homeModes, venueChoices;
        RawImage meadow;
        Text wordmark;
        Text tagline;
        readonly RawImage[] animals = new RawImage[AnimalNames.Length];
        readonly RawImage[] groundShadows = new RawImage[AnimalNames.Length];
        ModeButton host, coloring;
        Button venueBack;
        RawImage backShadow;
        Text venueHeading, venueBackLabel;
        RectTransform backArrow;
        VenueTile[] venueTiles;
        Sprite buttonSurface, venueSurface;
        Texture2D buttonTexture, shadowTexture, venueTexture;
        Rect previousArea;
        Vector2Int previousScreen;

        internal void Build(ParkPlayground park)
        {
            this.park = park;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameObject.AddComponent<GraphicRaycaster>();
            canvasRect = (RectTransform)transform;
            meadow = Art("Park Illustration", canvasRect, "Meadow");
            content = Child("Safe Area", canvasRect);
            homeHeading = Child("Start Heading", content);
            wordmark = Label("App Title", homeHeading, ParkLanguage.Title, ParkFonts.Bold);
            wordmark.resizeTextForBestFit = true;
            wordmark.resizeTextMinSize = 10;
            wordmark.verticalOverflow = VerticalWrapMode.Truncate;
            tagline = Label("Tagline", homeHeading, "애들아 놀아라. 아빠 10분만 쉰다", ParkFonts.Bold);
            tagline.verticalOverflow = VerticalWrapMode.Truncate;
            shadowTexture = CreateGroundShadow();
            for (int i = 0; i < groundShadows.Length; i++)
            {
                var shadowImage = Child(AnimalNames[i] + " Ground Shadow", content).gameObject.AddComponent<RawImage>();
                shadowImage.texture = shadowTexture;
                shadowImage.color = new Color(1, 1, 1, 68f / 255f);
                shadowImage.raycastTarget = false;
                groundShadows[i] = shadowImage;
            }
            foreach (int index in new[] { 0, 1, 3, 4, 2 })
                animals[index] = Art(AnimalNames[index], content, AnimalNames[index]);
            homeModes = Child("Start Actions", content);
            buttonSurface = CreateButtonSurface();
            host = new ModeButton(this, "놀이 공간 열기", "이 기기에서 공간을 열어요", "Gate",
                new Color32(188, 239, 129, 255));
            coloring = new ModeButton(this, "함께 놀기", "색칠한 친구를 보내요", "PlayTogether",
                new Color32(255, 188, 110, 255));
            host.Button.onClick.AddListener(() => park.ChooseHostMode());
            coloring.Button.onClick.AddListener(park.ChooseDrawingMode);
            BuildVenueChoices(park);
            if (!EventSystem.current)
            {
                var events = new GameObject("Start Screen Input", typeof(EventSystem), typeof(StandaloneInputModule));
                events.transform.SetParent(park.transform, false);
            }
            RefreshLayout();
        }

        internal void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
            if (visible) RefreshLayout();
            else ShowVenueChoices(false);
        }

        internal bool IsChoosingVenue => venueChoices.gameObject.activeSelf;

        internal void ShowInGameVenueChoices(bool visible)
        {
            meadow.gameObject.SetActive(!visible);
            foreach (var animal in animals) animal.gameObject.SetActive(!visible);
            foreach (var shadow in groundShadows) shadow.gameObject.SetActive(!visible);
            SetVisible(visible);
            if (visible) ShowVenueChoices(true);
        }

        internal void ShowVenueChoices(bool visible)
        {
            for (int i = 0; i < venueTiles.Length; i++)
                venueTiles[i].SetCurrent(visible && park.IsVenuePickerOpen && i == park.VenueIndex);
            if (IsChoosingVenue == visible) return;
            venueChoices.gameObject.SetActive(visible);
            homeHeading.gameObject.SetActive(!visible);
            homeModes.gameObject.SetActive(!visible);
            if (EventSystem.current)
            {
                var selected = visible ? venueTiles[0].Button : host.Button;
                EventSystem.current.SetSelectedGameObject(isActiveAndEnabled ? selected.gameObject : null);
            }
        }

        void BuildVenueChoices(ParkPlayground park)
        {
            venueSurface = ParkDialogs.CreateRoundedSurface(out venueTexture);
            venueChoices = Child("Start Venue Tiles", content);
            venueHeading = Label("Venue Heading", venueChoices, "놀이 장소 고르기", ParkFonts.Bold);
            backShadow = LiftShadow(venueChoices, shadowTexture);
            venueBack = TileButton("뒤로", venueChoices, venueSurface, new Color32(250, 252, 236, 255));
            venueBack.onClick.AddListener(() =>
            {
                if (park.IsVenuePickerOpen) park.CloseDialog();
                else ShowVenueChoices(false);
            });
            var back = (RectTransform)venueBack.transform;
            venueBackLabel = Label("Label", back, "뒤로", ParkFonts.Bold);
            backArrow = Chevron(back, true);
            var covers = Resources.Load<Texture2D>("Welcome/VenueCovers");
            venueTiles = new VenueTile[park.Venues?.Length ?? 0];
            for (int i = 0; i < venueTiles.Length; i++)
            {
                int choice = i;
                var venue = park.Venues[i];
                var tile = new VenueTile(venueChoices, venue, covers, venueSurface, marksCurrent: true, shadowTexture: shadowTexture);
                tile.Button.onClick.AddListener(() =>
                {
                    if (park.IsVenuePickerOpen) park.SelectVenue(choice);
                    else park.ChooseStartingVenue(choice);
                });
                venueTiles[i] = tile;
            }
            venueChoices.gameObject.SetActive(false);
        }

        internal static Button TileButton(string name, RectTransform parent, Sprite sprite, Color color)
        {
            var root = Child(name, parent);
            var surface = root.gameObject.AddComponent<Image>();
            surface.sprite = sprite;
            surface.type = Image.Type.Sliced;
            surface.color = color;
            var shadow = root.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color32(44, 63, 32, 54);
            shadow.effectDistance = new Vector2(0, -4);
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = surface;
            var colors = button.colors;
            colors.highlightedColor = new Color32(207, 230, 180, 255);
            colors.selectedColor = new Color32(228, 240, 206, 255);
            colors.pressedColor = new Color32(181, 213, 157, 255);
            button.colors = colors;
            return button;
        }

        internal void SetModalOpen(bool open)
        {
            host.Button.interactable = coloring.Button.interactable = !open;
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
            wordmark.resizeTextMaxSize = Pixels(height * (portrait ? .062f : .09f));
            Place(wordmark.rectTransform, new Rect(center - titleWidth / 2, height * (portrait ? .034f : .037f),
                titleWidth, height * (portrait ? .089f : .128f)));
            Place(tagline.rectTransform, new Rect(left, height * (portrait ? .119f : .158f), width, height * .053f));
            tagline.resizeTextForBestFit = true;
            tagline.resizeTextMinSize = 8;
            tagline.resizeTextMaxSize = tagline.fontSize = Pixels(Mathf.Min(width * .052f, height * .038f));

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
                : (Mathf.Min(width * .91f, height * (ParkLanguage.IsKorean ? .836f : 1.13f)) - gap) / 2;
            float buttonTop = height * (portrait ? .746f : .799f);
            float buttonLeft = center - (portrait ? buttonWidth : buttonWidth * 2 + gap) / 2;
            host.Layout(new Rect(buttonLeft, buttonTop, buttonWidth, buttonHeight));
            coloring.Layout(new Rect(portrait ? buttonLeft : buttonLeft + buttonWidth + gap,
                portrait ? buttonTop + buttonHeight + gap : buttonTop, buttonWidth, buttonHeight));
            LayoutVenueChoices(left, width, height, portrait);
        }

        void LayoutVenueChoices(float left, float width, float height, bool portrait)
        {
            if (venueTiles.Length == 0) return;
            int columns = VenueColumns(venueTiles.Length);
            int rows = (venueTiles.Length + columns - 1) / columns;
            float gridWidth = Mathf.Min(width * (portrait ? .93f : .82f), height * 1.10f);
            float center = left + width * .5f;
            float gridLeft = center - gridWidth * .5f;
            float gap = Mathf.Min(width * .015f, height * .023f);
            float headingHeight = Mathf.Min(height * .086f, gridWidth * .142f);
            float headingGap = height * .044f;
            float backWidth = Mathf.Min(gridWidth * .18f, headingHeight * 1.68f);
            float rowGaps = gap * (rows - 1);
            float cellWidth = (gridWidth - gap * (columns - 1)) / columns;
            float cellHeight = Mathf.Min(cellWidth * (portrait ? 1.08f : .99f),
                (height * .746f - headingHeight - headingGap - rowGaps) / rows);
            float groupHeight = headingHeight + headingGap + rowGaps + cellHeight * rows;
            float headingTop = portrait ? (height - groupHeight) * .5f : height * .06f;
            float gridTop = headingTop + headingHeight + headingGap;
            FitLabel(venueHeading, new Rect(gridLeft + backWidth, headingTop, gridWidth - 2 * backWidth, headingHeight),
                headingHeight * .72f);
            float backHeight = headingHeight * .72f;
            var backBounds = new Rect(gridLeft, headingTop + (headingHeight - backHeight) * .5f, backWidth, backHeight);
            LayoutTileButton(venueBack, backBounds);
            PlaceLiftShadow(backShadow, backBounds, backHeight * .55f);
            venueBack.image.pixelsPerUnitMultiplier = 64f / backHeight;
            FitLabel(venueBackLabel, new Rect(backWidth * .25f, 0, backWidth * .65f, backHeight), backHeight * .40f);
            float backIcon = Mathf.Min(backWidth * .17f, backHeight * .32f);
            Place(backArrow, new Rect(backWidth * .13f, (backHeight - backIcon) * .5f, backIcon, backIcon));
            LayoutVenueTiles(venueTiles, columns, new Vector2(center, gridTop), new Vector2(cellWidth, cellHeight), gap);
        }

        internal static int VenueColumns(int count) => Mathf.Min(count, count <= 6 ? 3 : 4);

        internal static void LayoutVenueTiles(VenueTile[] tiles, int columns, Vector2 start, Vector2 cell, float gap)
        {
            for (int i = 0; i < tiles.Length; i++)
            {
                int row = i / columns;
                int rowCount = Mathf.Min(columns, tiles.Length - row * columns);
                float rowLeft = start.x - (rowCount * cell.x + (rowCount - 1) * gap) * .5f;
                tiles[i].Layout(new Rect(rowLeft + i % columns * (cell.x + gap),
                    start.y + row * (cell.y + gap), cell.x, cell.y));
            }
        }

        internal static void LayoutTileButton(Button button, Rect bounds)
        {
            Place((RectTransform)button.transform, bounds);
            button.image.pixelsPerUnitMultiplier = 128f / bounds.height;
        }

        internal static void FitLabel(Text label, Rect bounds, float size)
        {
            Place(label.rectTransform, bounds);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 8;
            label.resizeTextMaxSize = label.fontSize = Mathf.Max(8, Pixels(size));
            label.verticalOverflow = VerticalWrapMode.Truncate;
        }

        static RectTransform Chevron(RectTransform parent, bool left = false)
        {
            var icon = Child("Direction", parent);
            for (int arm = 0; arm < 2; arm++)
            {
                float y = arm == 0 ? .32f : .68f;
                IconStroke(icon, new Vector2(.49f, y), .58f, (arm == 0 ? 43 : -43) * (left ? -1 : 1));
            }
            return icon;
        }

        static void IconStroke(RectTransform icon, Vector2 center, float width, float angle)
        {
            var line = Child("Stroke", icon).gameObject.AddComponent<Image>();
            line.color = new Color32(76, 105, 62, 255);
            line.raycastTarget = false;
            var rect = line.rectTransform;
            rect.pivot = new Vector2(.5f, .5f);
            var half = new Vector2(width * .5f, .065f);
            rect.anchorMin = center - half;
            rect.anchorMax = center + half;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.Euler(0, 0, angle);
        }

        internal sealed class VenueTile
        {
            static readonly Color Paper = new Color32(255, 253, 241, 255);
            public readonly Button Button;
            readonly RawImage preview, shadow;
            readonly Text title;
            readonly Image frame, caption, currentMark;
            readonly RectTransform arrow;
            readonly Rect source = new Rect(0, 0, 1, 1);
            readonly float focusY = .5f;

            public VenueTile(RectTransform parent, ParkVenue venue, Texture2D covers, Sprite surface,
                bool marksCurrent = false, Texture2D shadowTexture = null)
            {
                if (shadowTexture) shadow = LiftShadow(parent, shadowTexture);
                Button = TileButton(venue.Name, parent, surface, Paper);
                var root = (RectTransform)Button.transform;
                frame = Child("Artwork Frame", root).gameObject.AddComponent<Image>();
                frame.sprite = surface;
                frame.type = Image.Type.Sliced;
                frame.raycastTarget = false;
                frame.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                preview = Child("Venue Preview", frame.rectTransform).gameObject.AddComponent<RawImage>();
                int cover = System.Array.IndexOf(CoverNames, venue.Name);
                if (covers && cover >= 0)
                {
                    float insetX = 2f / covers.width, insetY = 2f / covers.height;
                    source = new Rect(cover % 3 / 3f + insetX, (1 - cover / 3) * .5f + insetY,
                        1f / 3 - 2 * insetX, .5f - 2 * insetY);
                    focusY = cover == 5 ? .44f : cover == 4 ? .53f : .50f;
                    preview.texture = covers;
                }
                else preview.texture = venue.Preview;
                preview.raycastTarget = false;
                caption = Child("Caption Paper", frame.rectTransform).gameObject.AddComponent<Image>();
                caption.color = cover >= 0 ? CoverColors[cover] : new Color32(244, 248, 224, 255);
                caption.raycastTarget = false;
                title = Label("Label", frame.rectTransform, venue.Name, ParkFonts.Bold, TextAnchor.MiddleLeft);
                arrow = Chevron(frame.rectTransform);
                if (!marksCurrent) return;
                currentMark = Child("Current Place", frame.rectTransform).gameObject.AddComponent<Image>();
                currentMark.sprite = surface;
                currentMark.type = Image.Type.Sliced;
                currentMark.color = new Color32(242, 253, 223, 255);
                currentMark.raycastTarget = false;
                IconStroke(currentMark.rectTransform, new Vector2(.382f, .46f), .28f, -40);
                IconStroke(currentMark.rectTransform, new Vector2(.578f, .515f), .48f, 47);
                currentMark.gameObject.SetActive(false);
            }

            internal void SetCurrent(bool current)
            {
                Button.image.color = current ? new Color32(151, 194, 107, 255) : Paper;
                if (currentMark) currentMark.gameObject.SetActive(current);
            }

            public void Layout(Rect bounds)
            {
                LayoutTileButton(Button, bounds);
                if (shadow) PlaceLiftShadow(shadow, bounds, bounds.height * .22f);
                float border = Mathf.Max(2, bounds.height * .014f);
                float w = bounds.width - 2 * border, h = bounds.height - 2 * border;
                Place(frame.rectTransform, new Rect(border, border, w, h));
                frame.pixelsPerUnitMultiplier = 128f / bounds.height;
                float footer = h * .205f;
                var photo = new Rect(0, 0, w, h - footer);
                Place(preview.rectTransform, photo);
                if (preview.texture)
                {
                    float sourceRatio = source.width * preview.texture.width / (source.height * preview.texture.height);
                    float frameRatio = photo.width / photo.height;
                    float cropWidth = Mathf.Min(1, frameRatio / sourceRatio);
                    float cropHeight = Mathf.Min(1, sourceRatio / frameRatio);
                    preview.uvRect = new Rect(source.x + source.width * (1 - cropWidth) * .5f,
                        source.y + source.height * (1 - cropHeight) * focusY,
                        source.width * cropWidth, source.height * cropHeight);
                }
                Place(caption.rectTransform, new Rect(0, photo.height, w, footer));
                if (currentMark)
                {
                    float markerSize = Mathf.Clamp(h * .19f, 19, 39);
                    float inset = Mathf.Max(5, h * .047f);
                    Place(currentMark.rectTransform, new Rect(w - markerSize - inset, inset, markerSize, markerSize));
                    currentMark.pixelsPerUnitMultiplier = 64f / markerSize;
                }
                bool hasArrow = w >= 125;
                arrow.gameObject.SetActive(hasArrow);
                float margin = Mathf.Min(w * .085f, footer * .57f);
                float icon = footer * .30f;
                Place(arrow, new Rect(w - margin - icon, photo.height + (footer - icon) * .5f, icon, icon));
                FitLabel(title, new Rect(margin, photo.height, w - 2 * margin - (hasArrow ? icon * 1.75f : 0), footer),
                    Mathf.Min(footer * .52f, w * .090f));
            }
        }

        sealed class ModeButton
        {
            public readonly Button Button;
            readonly Image surface;
            readonly RawImage icon, shadow;
            readonly Text title, description;

            public ModeButton(ParkWelcome welcome, string text, string detail, string asset, Color color)
            {
                shadow = LiftShadow(welcome.homeModes, welcome.shadowTexture);
                var root = Child(text, welcome.homeModes);
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
                title.verticalOverflow = description.verticalOverflow = VerticalWrapMode.Truncate;
            }

            public void Layout(Rect bounds)
            {
                Place(surface.rectTransform, bounds);
                surface.pixelsPerUnitMultiplier = 144f / bounds.height;
                float w = bounds.width, h = bounds.height;
                PlaceLiftShadow(shadow, bounds, h * .58f);
                Fit(icon, new Rect(w * .062f, h * .13f, w * .275f, h * .67f));
                Place(title.rectTransform, new Rect(w * .355f, h * .128f, w * .59f, h * .39f));
                Place(description.rectTransform, new Rect(w * .355f, h * .52f, w * .59f, h * .25f));
                title.resizeTextForBestFit = description.resizeTextForBestFit = true;
                title.resizeTextMinSize = description.resizeTextMinSize = 8;
                title.resizeTextMaxSize = title.fontSize = Pixels(Mathf.Min(w * .092f, h * .32f));
                description.resizeTextMaxSize = description.fontSize = Pixels(Mathf.Min(w * .053f, h * .177f));
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

        static RawImage LiftShadow(RectTransform parent, Texture2D texture)
        {
            var shadow = Child("Lift Shadow", parent).gameObject.AddComponent<RawImage>();
            shadow.texture = texture;
            shadow.color = new Color(1, 1, 1, .55f);
            shadow.raycastTarget = false;
            return shadow;
        }

        static void PlaceLiftShadow(RawImage shadow, Rect bounds, float height)
        {
            Place(shadow.rectTransform, new Rect(bounds.x - bounds.width * .04f,
                bounds.yMax - height * .23f, bounds.width * 1.08f, height));
        }

        internal static Text Label(string name, RectTransform parent, string text, Font font, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var label = Child(name, parent).gameObject.AddComponent<Text>();
            label.text = ParkLanguage.Text(text);
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
                name = "Soft drop shadow", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var offset = new Vector2((x + .5f) / size * 2 - 1, (y + .5f) / size * 2 - 1);
                float alpha = Mathf.Clamp01(1 - offset.sqrMagnitude);
                pixels[y * size + x] = new Color32(35, 58, 17, (byte)(255 * alpha * alpha));
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
            Destroy(venueSurface);
            Destroy(venueTexture);
        }
    }
}
