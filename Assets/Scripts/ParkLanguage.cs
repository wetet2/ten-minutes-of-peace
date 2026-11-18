using System.Collections.Generic;
using UnityEngine;

namespace PeacePark
{
    // Keep scene names and LAN animal IDs stable; translate only displayed text.
    public static class ParkLanguage
    {
        public const string KoreanTitle = "유아용 게임: 십분의 평화";
        public const string EnglishTitle = "Ten Minutes of Peace";
        public static bool IsKorean => Application.systemLanguage == SystemLanguage.Korean;
        public static string Title => IsKorean ? KoreanTitle : EnglishTitle;
        public static string Text(string korean) => Translate(korean, IsKorean);
        public static string Translate(string text, bool korean) =>
            !korean && text != null && English.TryGetValue(text, out var english) ? english : text;
        public static string Arrival(string place, AnimalKind kind) => IsKorean
            ? place + "에서 " + AnimalKinds.Name(kind) + " 친구가 나와요!"
            : "Your " + AnimalKinds.Name(kind).ToLowerInvariant() + " friend is coming from " + Text(place) + "!";

        static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            { "곰", "Bear" }, { "호랑이", "Tiger" }, { "토끼", "Rabbit" }, { "개구리", "Frog" },
            { "기린", "Giraffe" }, { "코끼리", "Elephant" }, { "트리케라톱스", "Triceratops" },
            { "악어", "Crocodile" }, { "하마", "Hippo" }, { "고릴라", "Gorilla" },
            { "공원", "Park" }, { "숲", "Forest" }, { "놀이터", "Playground" }, { "사막", "Desert" },
            { "시계탑 광장", "Clock Tower Square" }, { "해변가", "Beach" },
            { "꽃집", "the flower shop" }, { "사탕집", "the candy shop" }, { "과자집", "the candy shop" }, { "빵집", "the bakery" },
            { "이끼 오두막", "the mossy cottage" }, { "해변 수풀", "the beach bushes" },
            { "시계 성당", "the cathedral" }, { "모래언덕", "the sand dunes" },
            { "미끄럼틀 꼭대기", "the slide" }, { "봉 타워", "the pole tower" },
            { "애들아 놀아라. 아빠 10분만 쉰다", "Daddy just wants ten minutes of peace. Pretty please!" },
            { "놀이 공간 열기", "Open a Play Space" }, { "이 기기에서 공간을 열어요", "Host on this device" },
            { "함께 놀기", "Play Together" }, { "동물 색칠하기", "Color an Animal" },
            { "색칠한 친구를 보내요", "Send your colorful friend" },
            { "자동", "Auto" }, { "앞모습", "Front" }, { "뒷모습", "Back" },
            { "보내는 중", "Sending" }, { "보내기", "Send" }, { "다시 색칠하기", "Color Again" },
            { "놀이 공간에 연결하기", "Join a Play Space" },
            { "호스트와 같은 Wi-Fi에 연결하세요.", "Connect to the same Wi-Fi as the host." },
            { "자동 찾기", "Find Automatically" },
            { "직접 연결하려면 호스트의 설정에서 IP를 확인하세요.", "For a direct connection, check the host's IP in Settings." },
            { "직접 연결", "Connect by IP" }, { "닫기", "Close" }, { "설정", "Settings" },
            { "내 IP 주소", "My IP Address" }, { "조작 방법", "Controls" },
            { "메뉴 접기", "Hide menu" }, { "메뉴 펼치기", "Show menu" },
            { "한 손가락 회전 · 두 손가락 이동·확대\n동물 터치  확대·따라가기\n같은 동물 다시 터치  원래 시점으로",
              "One finger: rotate · Two fingers: pan / zoom\nTap an animal to zoom in and follow\nTap it again to zoom out and return" },
            { "드래그 회전 · 오른쪽 드래그 이동 · 휠 확대\n동물 클릭  따라가기 / 다시 클릭  해제\n카메라 초기화 또는 Home  처음 시점",
              "Drag: rotate · Right drag: pan · Wheel: zoom\nClick an animal to follow; click again to zoom out\nReset View or Home: return to starting view" },
            { "음악 볼륨", "Music Volume" }, { "시작 화면으로", "Back to Start" },
            { "놀이 장소 고르기", "Choose a Place" }, { "카메라 초기화", "Reset View" },
            { "놀이 공간을 열 수 없어요. 다른 창을 확인해 주세요.", "Could not open a play space. Check if another window is hosting." },
            { "호스트 화면에 표시된 IPv4 주소를 입력하세요.", "Enter the IPv4 address shown on the host device." },
            { "같은 Wi-Fi에서 놀이 공간을 찾는 중…", "Looking for a play space on your Wi-Fi…" },
            { "놀이 공간에 연결하는 중…", "Connecting to the play space…" },
            { "놀이 공간에 그림을 보내는 중…", "Sending your drawing to the play space…" },
            { "놀이 공간에 연결됐어요", "Connected to the play space!" },
            { "놀이 공간을 찾지 못했어요. IP로도 연결할 수 있어요.", "No play space found. You can also connect by IP." },
            { "연결할 수 없어요. 주소와 호스트를 확인하세요.", "Could not connect. Check the address and host device." },
            { "놀이 공간에 보냈어요! 새 동물을 색칠해 보세요", "Friend sent! Let's color another animal." },
            { "이 호스트의 놀이 공간이 가득 찼어요", "This play space is full." },
            { "기다리는 친구가 많아요. 잠시 후 다시 적용해 주세요", "Lots of friends are waiting. Please try again soon." },
            { "전송을 확인하지 못했어요. 다시 적용해 주세요", "Could not confirm delivery. Please try sending again." }
        };
    }
}
