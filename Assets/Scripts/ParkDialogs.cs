using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static PeacePark.ParkWelcome;

namespace PeacePark
{
    internal sealed class ParkDialogs : MonoBehaviour
    {
        const float CardWidth = 520, ConnectionHeight = 386, SettingsHeight = 432;
        static readonly Color Cream = new Color32(255, 249, 229, 255);
        static readonly Color Green = new Color32(187, 227, 140, 255);
        static readonly Color Neutral = new Color32(246, 232, 196, 255);
        ParkPlayground park;
        RectTransform connection, settings, venues;
        Text addressSummary, volumePercent;
        Slider musicVolume;
        Image[] venueCards;
        InputField address;
        Button connect, discover;
        Sprite rounded;
        Texture2D texture;
        internal Sprite ToastBackground=>rounded;
        Rect lastArea;
        int lastScreenHeight;

        internal void Build(ParkPlayground owner)
        {
            park = owner;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            gameObject.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)transform;
            var dim = Child("Modal Backdrop", root).gameObject.AddComponent<Image>();
            dim.color = new Color32(17, 32, 22, 143);
            dim.rectTransform.anchorMin = Vector2.zero;
            dim.rectTransform.anchorMax = Vector2.one;
            dim.rectTransform.offsetMin = dim.rectTransform.offsetMax = Vector2.zero;
            var dismiss=dim.gameObject.AddComponent<Button>();
            dismiss.transition=Selectable.Transition.None;
            dismiss.navigation=new Navigation{mode=Navigation.Mode.None};
            dismiss.onClick.AddListener(park.CloseDialog);
            rounded = RoundedSprite();

            connection = Card("Connection Popup", root);
            Caption(connection, "놀이 공간에 연결하기", 34, new Rect(28, 17, 464, 53), true);
            Caption(connection, "호스트와 같은 Wi-Fi에 연결하세요.", 20, new Rect(32, 72, 456, 34));
            discover = Action(connection, "자동 찾기", new Rect(32, 113, 456, 54), Green);
            discover.onClick.AddListener(() => park.Lan.BeginDiscovery());
            Caption(connection, "직접 연결하려면 호스트의 설정에서 IP를 확인하세요.",
                17, new Rect(32, 178, 456, 30));
            address = AddressField(connection);
            connect = Action(connection, "직접 연결", new Rect(32, 268, 456, 42), Neutral);
            connect.onClick.AddListener(() => park.Lan.BeginJoin(address.text));
            Action(connection, "닫기", new Rect(180, 345, 160, 32), Neutral).onClick.AddListener(park.CloseDialog);

            settings = Card("Park Settings Popup", root);
            Caption(settings, "설정", 34, new Rect(28, 17, 464, 53), true);
            Caption(settings, "내 IP 주소", 21, new Rect(36, 77, 448, 30), true, TextAnchor.MiddleLeft);
            Surface("IP Address Panel", settings, new Rect(32, 111, 456, 78), new Color32(240, 244, 215, 255));
            addressSummary = Caption(settings, "", 20, new Rect(48, 118, 424, 62), false, TextAnchor.MiddleLeft);
            addressSummary.verticalOverflow = VerticalWrapMode.Truncate;
            addressSummary.resizeTextForBestFit = true;
            addressSummary.resizeTextMinSize = 12;
            addressSummary.resizeTextMaxSize = 20;
            Caption(settings, "조작 방법", 21, new Rect(36, 199, 448, 31), true, TextAnchor.MiddleLeft);
            string cameraHelp=Application.isMobilePlatform
                ? "한 손가락 회전 · 두 손가락 이동·확대\n동물 터치  확대·따라가기\n같은 동물 다시 터치  원래 시점으로"
                : "드래그 회전 · 오른쪽 드래그 이동 · 휠 확대\n동물 클릭  따라가기 / 다시 클릭  해제\n카메라 초기화 또는 Home  처음 시점";
            Caption(settings, cameraHelp,
                19, new Rect(36, 232, 448, 65), false, TextAnchor.MiddleLeft);
            Caption(settings, "음악 볼륨", 21, new Rect(36, 306, 112, 48), true, TextAnchor.MiddleLeft);
            volumePercent = Caption(settings, "", 19, new Rect(438, 306, 46, 48), false, TextAnchor.MiddleRight);
            musicVolume = CreateVolumeSlider(settings);
            musicVolume.onValueChanged.AddListener(percent =>
            {
                park.SetMusicVolume(percent / 100f);
                volumePercent.text = $"{percent:0}%";
            });
            Action(settings, "시작 화면으로", new Rect(32, 369, 220, 46), Neutral).onClick.AddListener(park.ReturnToModeSelection);
            Action(settings, "닫기", new Rect(268, 369, 220, 46), Green).onClick.AddListener(park.CloseDialog);

            venues = Card("Venue Picker", root);
            Caption(venues, "놀이 장소 고르기", 34, new Rect(28, 17, 464, 53), true);
            var choices = park.Venues;
            venueCards = new Image[choices?.Length ?? 0];
            for (int i = 0; i < venueCards.Length; i++)
            {
                int index = i;
                int row = i / 2;
                float x = i % 2 == 0 && i == venueCards.Length - 1 ? (CardWidth - 224) / 2 : 30 + (i % 2) * 236;
                var card = Surface(choices[i].Name, venues, new Rect(x, 86 + row * 178, 224, 164), Neutral);
                venueCards[i] = card;
                var button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = card;
                button.onClick.AddListener(() => park.SelectVenue(index));
                var art = Child("장소 그림", card.rectTransform).gameObject.AddComponent<RawImage>();
                art.texture = choices[i].Preview;
                art.enabled = art.texture != null;
                art.raycastTarget = false;
                Place(art.rectTransform, new Rect(9, 9, 206, 116));
                Caption(card.rectTransform, choices[i].Name, 25, new Rect(9, 124, 206, 35), true);
            }
            Action(venues, "닫기", new Rect(180, VenueHeight - 61, 160, 46), Neutral).onClick.AddListener(park.CloseDialog);
        }

        float VenueHeight => 150 + Mathf.Max(1, (venueCards.Length + 1) / 2) * 178;

        internal void Show(bool showConnection, bool showSettings, bool showVenues)
        {
            if (EventSystem.current && (gameObject.activeSelf || showConnection || showSettings || showVenues))
                EventSystem.current.SetSelectedGameObject(null);
            connection.gameObject.SetActive(showConnection);
            settings.gameObject.SetActive(showSettings);
            venues.gameObject.SetActive(showVenues);
            gameObject.SetActive(showConnection || showSettings || showVenues);
            if (showConnection) RefreshConnection();
            if (showSettings)
            {
                addressSummary.text = park.Lan.LocalIPv4();
                musicVolume.SetValueWithoutNotify(park.MusicVolume * 100f);
                volumePercent.text = $"{musicVolume.value:0}%";
            }
            if (showVenues)
                for (int i = 0; i < venueCards.Length; i++)
                    venueCards[i].color = i == park.VenueIndex ? Green : Neutral;
            if (gameObject.activeSelf) RefreshLayout();
        }

        void Update()
        {
            if (park.IsConnecting) RefreshConnection();
            RefreshLayout();
        }

        void OnApplicationFocus(bool focused)
        {
            if (focused && park && park.IsSettingsOpen)
                addressSummary.text = park.Lan.LocalIPv4();
        }

        void RefreshConnection()
        {
            address.interactable = connect.interactable = discover.interactable = !park.Lan.IsJoining;
        }

        Slider CreateVolumeSlider(RectTransform parent)
        {
            var area = Surface("음악 볼륨 슬라이더", parent, new Rect(150, 306, 282, 48), Color.clear);
            var rail = Surface("Volume Track", area.rectTransform, new Rect(14, 16, 254, 16), Neutral);
            rail.raycastTarget = false;
            var fill = Surface("Volume Fill", rail.rectTransform, Rect.zero, Green);
            fill.raycastTarget = false;
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;

            var handleArea = Child("Volume Handle Area", area.rectTransform);
            Place(handleArea, new Rect(14, 10, 254, 28));
            var handle = Surface("Volume Handle", handleArea, Rect.zero, new Color32(119, 167, 75, 255));
            handle.rectTransform.pivot = new Vector2(.5f, .5f);
            handle.rectTransform.sizeDelta = new Vector2(28, 0);

            var slider = area.gameObject.AddComponent<Slider>();
            slider.targetGraphic = handle;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.maxValue = 100;
            slider.wholeNumbers = true;
            return slider;
        }

        void RefreshLayout()
        {
            Rect safe = Screen.safeArea;
            if ((safe == lastArea && Screen.height == lastScreenHeight) || safe.width <= 0 || safe.height <= 0) return;
            lastArea = safe;
            lastScreenHeight = Screen.height;
            float scale = Mathf.Min(safe.width / (CardWidth + 36), safe.height / (Mathf.Max(SettingsHeight, VenueHeight) + 36),
                Mathf.Max(.82f, safe.height / 900f));
            Vector2 center = new Vector2(safe.center.x, Screen.height - safe.center.y);
            LayoutCard(connection, center, ConnectionHeight, scale);
            LayoutCard(settings, center, SettingsHeight, scale);
            LayoutCard(venues, center, VenueHeight, scale);
        }

        static void LayoutCard(RectTransform card, Vector2 center, float height, float scale)
        {
            card.localScale = new Vector3(scale, scale, 1);
            Place(card, new Rect(center.x - CardWidth * scale / 2, center.y - height * scale / 2, CardWidth, height));
        }

        RectTransform Card(string name, RectTransform parent)
        {
            var card = Surface(name, parent, Rect.zero, Cream);
            var shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color32(21, 27, 17, 110);
            shadow.effectDistance = new Vector2(0, -7);
            return card.rectTransform;
        }

        static Text Caption(RectTransform parent, string value, int size, Rect bounds, bool bold = false,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var text = Label(value, parent, value, bold ? ParkFonts.Bold : ParkFonts.Regular, alignment);
            text.fontSize = size;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Min(14, size);
            text.resizeTextMaxSize = size;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            Place(text.rectTransform, bounds);
            return text;
        }

        InputField AddressField(RectTransform parent)
        {
            var surface = Surface("IP Address", parent, new Rect(32, 210, 456, 50),
                new Color32(238, 242, 215, 255));
            var input = surface.gameObject.AddComponent<InputField>();
            input.targetGraphic = surface;
            var textBounds = new Rect(12, 0, 432, 50);
            input.textComponent = Caption(surface.rectTransform, "", 26, textBounds);
            var placeholder = Caption(surface.rectTransform, "192.168.", 26, textBounds);
            placeholder.color = new Color32(111, 110, 91, 255);
            placeholder.raycastTarget = false;
            input.placeholder = placeholder;
            input.textComponent.supportRichText = false;
            input.lineType = InputField.LineType.SingleLine;
            input.keyboardType = TouchScreenKeyboardType.URL;
            input.characterLimit = 45;
            return input;
        }

        Button Action(RectTransform parent, string label, Rect bounds, Color color)
        {
            var surface = Surface(label, parent, bounds, color);
            var button = surface.gameObject.AddComponent<Button>();
            button.targetGraphic = surface;
            Caption(surface.rectTransform, label, 22, new Rect(0, 0, bounds.width, bounds.height), true);
            return button;
        }

        Image Surface(string name, RectTransform parent, Rect bounds, Color color)
        {
            var image = Child(name, parent).gameObject.AddComponent<Image>();
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
            image.color = color;
            Place(image.rectTransform, bounds);
            return image;
        }

        Sprite RoundedSprite()
        {
            const int size = 64;
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                { name = "Rounded popup surface", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var edge = new Vector2(Mathf.Max(0, Mathf.Abs(x - 31.5f) - 15), Mathf.Max(0, Mathf.Abs(y - 31.5f) - 15));
                float distance = edge.magnitude - 16;
                byte tone = (byte)Mathf.Lerp(207, 255, Mathf.Clamp01(-distance / 2));
                pixels[y * size + x] = new Color32(tone, tone, tone, (byte)(Mathf.Clamp01(.5f - distance) * 255));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(18, 18, 18, 18));
        }

        void OnDestroy()
        {
            Destroy(rounded);
            Destroy(texture);
        }
    }
}
