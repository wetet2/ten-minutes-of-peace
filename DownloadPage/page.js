const venues = [
  ['공원', 'park.jpg', '꽃과 분수', '꽃집과 사탕집에서 나온 친구들이 분수 곁을 산책해요.'],
  ['숲', 'forest.jpg', '초록빛 산책', '나무가 빼곡한 숲속, 작은 빵집에서 친구를 만나요.'],
  ['놀이터', 'playground.png', '신나는 등장', '미끄럼틀을 타거나 봉을 타고 내려와 반갑게 손을 흔들어요.'],
  ['사막', 'desert.jpg', '모래 언덕 너머', '모래 언덕 너머로 나타난 친구와 시원한 오아시스를 둘러봐요.'],
  ['시계탑 광장', 'christmas.jpg', '눈 내린 마을', '커다란 시계가 있는 성당, 따뜻한 창문 불빛과 산책하는 사슴을 만나요.'],
  ['놀이공원', 'theme-park.jpg', '놀이기구를 타요', '코끼리 기차, 대관람차, 회전목마를 타고 놀이공원을 둘러봐요.'],
];
const animalNames = ['곰', '호랑이', '토끼', '개구리', '기린', '코끼리', '트리케라톱스', '악어', '하마', '고릴라'];
const english = {
  "십분의 평화": "Ten Minutes of Peace",
  "우리 가족의 색칠 놀이터": "A coloring playground for the whole family",
  "아이가 색칠한 동물이 살아 움직여요. 공원, 숲, 놀이터, 사막, 시계탑 광장, 놀이공원까지 여섯 놀이 공간에서 함께하는 가족용 색칠 게임. Android APK 다운로드.": "Bring your child’s colorful animals to life. Explore six play spaces together: Park, Forest, Playground, Desert, Clock Tower Square and Amusement Park. Download the Android APK.",
  "본문으로 건너뛰기": "Skip to content",
  "십분의 평화 홈": "Ten Minutes of Peace home",
  "페이지 메뉴": "Page navigation",
  "놀이 공간": "Play Spaces",
  "함께 놀기": "Play Together",
  "다운로드": "Download",
  "우리 가족의 작은 색칠 놀이터": "A little coloring playground for your family",
  "아이의 색으로 태어난 친구와": "A friend brought to life with your child’s colors.",
  "함께 떠나는 작은 여행.": "A little adventure to share.",
  "우는 아이, 화난 아이, 삐진 아이, 배고픈 아이, 졸린 아이, 모두가 하나되는 평화로운 가족을 지향합니다": "Crying, angry, sulking, hungry or sleepy — we hope to bring every child together in a peaceful, happy family.",
  "· Android용": "· For Android",
  "어디에서 놀아볼까요?": "Where shall we play?",
  "오늘의 여행지": "Today’s destination",
  "코끼리 기차와 대관람차가 있는 놀이공원 실제 게임 화면": "In-game Amusement Park view with the Elephant Train and Ferris wheel",
  "오늘은 놀이기구 타는 날": "A day of rides",
  "친구들과 함께, 놀이공원": "A trip to the Amusement Park",
  "놀이공원 소개 보기": "Explore the Amusement Park",
  "색칠한 친구가": "Your colorful friend",
  "빼꼼, 인사해요!": "pops out to say hello!",
  "게임 소개": "About the game",
  "10종의 동물": "10 Animals",
  "내 마음대로 색칠해요": "Color them your way",
  "6곳의 놀이 공간": "6 Play Spaces",
  "장소마다 다르게 등장해요": "A different entrance in every place",
  "같은 Wi-Fi로 함께": "Together on Wi-Fi",
  "가족의 친구들이 한자리에": "All your family’s friends in one place",
  "작은 친구들의 넓은 세상": "A big world for little friends",
  "오늘은 어디에서 놀까요?": "Where shall we play today?",
  "꽃이 피는 공원부터 눈 내린 광장까지.": "From flower-filled parks to snowy squares.",
  "좋아하는 장소에서 새로운 인사를 만나요.": "Discover a new hello in your favorite place.",
  "게임 화면 크게 보기 (새 탭)": "View game screenshot (new tab)",
  "실제 게임 화면": "in-game screenshot",
  "모든 장소 이미지는 실제 게임 화면입니다. 이미지를 누르면 크게 볼 수 있어요.": "Every location image is an actual game screenshot. Tap an image for a closer look.",
  "무슨 색이든 좋아!": "Every color is welcome!",
  "정답 없는 색칠 시간": "No wrong colors here",
  "내가 칠한 색 그대로,": "Your colors,",
  "살아 움직이는 친구들.": "brought to life.",
  "앞모습과 뒷모습을 색칠하고 놀이 공간에 보내세요. 문을 열고, 언덕을 넘고, 미끄럼틀을 타고 등장한 친구들이 손을 흔들고 산책해요.": "Color the front and back of your animal, then send it to the play space. Friends open doors, cross dunes or come down the slide before waving hello and taking a stroll.",
  "색칠할 수 있는 동물 10종": "10 animals to color",
  "색칠은 각자, 즐거움은 함께": "Your own colors. A shared adventure.",
  "온 가족의 친구들을 한자리에.": "Bring the whole family’s friends together.",
  "같은 Wi-Fi에 연결한 기기로": "Connect your devices to the same Wi-Fi",
  "하나의 놀이 공간을 함께 채워보세요.": "and fill one play space together.",
  "놀이 공간을 열어요": "Open a play space",
  "한 기기에서 ": "On one device, tap ",
  "놀이 공간 열기": "Open a Play Space",
  "를 누르고, 함께 놀 장소를 골라요.": " and choose a place to play.",
  "나만의 친구를 색칠해요": "Color your own friend",
  "다른 기기에서 ": "On another device, choose ",
  "함께 놀기 → 자동 찾기": "Play Together → Find Automatically",
  "로 연결하고, 좋아하는 동물을 색칠해요.": " to connect, then color your favorite animal.",
  "보내고, 반갑게 인사해요": "Send your friend and say hello",
  "보내기": "Send",
  "를 누르면 가족의 친구들이 같은 공간에 모여요. 화면을 움직이거나 확대해서 살펴보세요.": " brings your family’s friends into the same space. Move the view or zoom in to take a closer look.",
  "함께할 준비가 됐나요?": "Ready to play together?",
  "우리 가족의": "Make time for your family’s",
  "십분의 평화를 시작해요.": "ten minutes of peace.",
  "Android 기기에 APK를 다운로드해 설치하세요.": "Download and install the APK on your Android device.",
  "기기에 표시되는 설치 안내를 따라주시면 돼요.": "Follow the installation steps shown on your device.",
  "십분의 평화 앱 아이콘": "Ten Minutes of Peace app icon",
  "기존 앱이 있다면 같은 이름의 APK로 업데이트하세요.": "Already have the app? Use this APK to update it.",
  "함께 연결할 기기에는 같은 최신 버전을 설치해 주세요.": "Install the same latest version on every device you connect.",
  "색칠 한 장으로 시작하는, 우리 가족의 작은 평화.": "A little family peace, starting with a splash of color.",
  "글꼴 안내": "Font notices",
  "Android APK 다운로드": "Download Android APK",
  "버전 정보 확인 불가": "Version information unavailable",
  "버전": "Version",
  "빌드": "Build"
};
const englishVenues = [
  [
    "Park",
    "park.jpg",
    "Flowers & fountains",
    "Friends emerge from the flower and candy shops to stroll beside the fountain."
  ],
  [
    "Forest",
    "forest.jpg",
    "A walk in the woods",
    "Meet a friend at a little bakery nestled among the trees."
  ],
  [
    "Playground",
    "playground.png",
    "A playful hello",
    "Friends glide down a slide or a pole and greet you with a wave."
  ],
  [
    "Desert",
    "desert.jpg",
    "Beyond the dunes",
    "Meet a friend beyond the sand dunes and explore a refreshing oasis."
  ],
  [
    "Clock Tower Square",
    "christmas.jpg",
    "A snowy village",
    "Discover a grand clock cathedral, warmly lit windows and wandering deer."
  ],
  [
    "Amusement Park",
    "theme-park.jpg",
    "A day of rides",
    "Explore the amusement park on the Elephant Train, Ferris wheel or carousel."
  ]
];
const englishAnimals = ["Bear", "Tiger", "Rabbit", "Frog", "Giraffe", "Elephant", "Triceratops", "Crocodile", "Hippo", "Gorilla"];

// Use the browser's highest-priority language. Other languages fall back to English.
function preferredLanguage(header = '') {
  const preferences = String(header).split(',').map((entry, index) => {
    const [tag, ...parameters] = entry.trim().split(';');
    const quality = parameters.find(value => value.trim().startsWith('q='));
    const q = quality ? Number(quality.trim().slice(2)) : 1;
    return { tag: tag.toLowerCase(), q, index };
  }).filter(({ tag, q }) => tag && Number.isFinite(q) && q > 0 && q <= 1)
    .sort((a, b) => b.q - a.q || a.index - b.index);
  return /^ko(?:-|$)/.test(preferences[0]?.tag || '') ? 'ko' : 'en';
}
function page(version, downloadPath, language = 'en') {
  language = language === 'ko' ? 'ko' : 'en';
  const t = text => language === 'ko' ? (text === '십분의 평화' ? '유아용 게임: 십분의 평화' : text) : english[text];
  const places = language === 'ko' ? venues : englishVenues;
  const animals = language === 'ko' ? animalNames : englishAnimals;
  const versionLabel = version ? `${t('버전')} ${version.name} · ${t('빌드')} ${version.name.split('.').at(-1)}` : t('버전 정보 확인 불가');
  const download = `<a class="button download" href="${downloadPath}" download><span aria-hidden="true">↓</span> ${t('Android APK 다운로드')}</a>`;
  return `<!doctype html>
<html lang="${language}">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <meta name="theme-color" content="#f8f6ed">
  <title>${t("십분의 평화")} · ${t("우리 가족의 색칠 놀이터")}</title>
  <meta name="description" content="${t("아이가 색칠한 동물이 살아 움직여요. 공원, 숲, 놀이터, 사막, 시계탑 광장, 놀이공원까지 여섯 놀이 공간에서 함께하는 가족용 색칠 게임. Android APK 다운로드.")}">
  <link rel="icon" href="/media/icon.png">
  <link rel="stylesheet" href="/download.css">
</head>
<body>
  <a class="skip-link" href="#main">${t("본문으로 건너뛰기")}</a>
  <header class="site-header"><div class="wrap header-row">
    <a class="brand" href="/" aria-label="${t("십분의 평화 홈")}"><img src="/media/icon.png" alt="" width="40" height="40"><span>${t("십분의 평화")}</span></a>
    <nav aria-label="${t("페이지 메뉴")}"><a href="#places">${t("놀이 공간")}</a><a href="#together">${t("함께 놀기")}</a><a class="nav-download" href="#install">${t("다운로드")} <span aria-hidden="true">↗</span></a></nav>
  </div></header>
  <main id="main">
    <section class="hero wrap" aria-labelledby="game-title">
      <div class="hero-copy">
        <p class="eyebrow"><span class="dot" aria-hidden="true"></span> ${t("우리 가족의 작은 색칠 놀이터")}</p>
        <h1 id="game-title">${t("십분의 평화")}</h1>
        <p class="hero-description">${t("아이의 색으로 태어난 친구와")}<br>${t("함께 떠나는 작은 여행.")}</p>
        <p class="family-message">${t("우는 아이, 화난 아이, 삐진 아이, 배고픈 아이, 졸린 아이, 모두가 하나되는 평화로운 가족을 지향합니다")}</p>
        <div class="download-row">${download}<p class="version">${versionLabel} <span>${t("· Android용")}</span></p></div>
        <a class="text-link" href="#places">${t("어디에서 놀아볼까요?")} <span aria-hidden="true">↓</span></a>
      </div>
      <figure class="hero-scene">
        <div class="scene-top"><span>${t("오늘의 여행지")}</span><span>06 / 06</span></div>
        <img src="/media/theme-park.jpg" alt="${t("코끼리 기차와 대관람차가 있는 놀이공원 실제 게임 화면")}" width="824" height="464" fetchpriority="high">
        <figcaption><div><span class="eyebrow">${t("오늘은 놀이기구 타는 날")}</span><h2>${t("친구들과 함께, 놀이공원")}</h2></div><a href="#place-6" aria-label="${t("놀이공원 소개 보기")}">↗</a></figcaption>
        <span class="scene-sticker">${t("색칠한 친구가")}<br>${t("빼꼼, 인사해요!")}</span>
      </figure>
    </section>
    <section class="intro-strip wrap" aria-label="${t("게임 소개")}">
      <p><strong>${t("10종의 동물")}</strong><span>${t("내 마음대로 색칠해요")}</span></p>
      <p><strong>${t("6곳의 놀이 공간")}</strong><span>${t("장소마다 다르게 등장해요")}</span></p>
      <p><strong>${t("같은 Wi-Fi로 함께")}</strong><span>${t("가족의 친구들이 한자리에")}</span></p>
    </section>
    <section class="places-section" id="places" aria-labelledby="places-title"><div class="wrap">
      <div class="section-heading"><div><p class="eyebrow">${t("작은 친구들의 넓은 세상")}</p><h2 id="places-title">${t("오늘은 어디에서 놀까요?")}</h2></div><p>${t("꽃이 피는 공원부터 눈 내린 광장까지.")}<br>${t("좋아하는 장소에서 새로운 인사를 만나요.")}</p></div>
      <div class="places">${places.map(([name, image, tag, description], i) => `
        <article class="place" id="place-${i + 1}">
          <a class="place-image" href="/media/${image}" target="_blank" rel="noopener" aria-label="${name} ${t("게임 화면 크게 보기 (새 탭)")}"><img src="/media/${image}" alt="${name} ${t("실제 게임 화면")}" width="824" height="464" loading="lazy"><span class="place-number">0${i + 1}</span><span class="image-expand" aria-hidden="true">↗</span></a>
          <div class="place-copy"><span class="place-tag">${tag}</span><h3>${name}</h3><p>${description}</p></div>
        </article>`).join('')}
      </div>
      <p class="image-note">${t("모든 장소 이미지는 실제 게임 화면입니다. 이미지를 누르면 크게 볼 수 있어요.")}</p>
    </div></section>
    <section class="friends wrap" aria-labelledby="friends-title">
      <div class="friends-art" aria-hidden="true"><span class="art-circle"></span><img class="friend-bear" src="/media/bear.png" alt="" loading="lazy"><img class="friend-frog" src="/media/frog.png" alt="" loading="lazy"><img class="friend-elephant" src="/media/elephant.png" alt="" loading="lazy"><span class="art-caption">${t("무슨 색이든 좋아!")}</span></div>
      <div class="friends-copy"><p class="eyebrow">${t("정답 없는 색칠 시간")}</p><h2 id="friends-title">${t("내가 칠한 색 그대로,")}<br>${t("살아 움직이는 친구들.")}</h2><p class="section-description">${t("앞모습과 뒷모습을 색칠하고 놀이 공간에 보내세요. 문을 열고, 언덕을 넘고, 미끄럼틀을 타고 등장한 친구들이 손을 흔들고 산책해요.")}</p><ul class="animal-list" aria-label="${t("색칠할 수 있는 동물 10종")}">${animals.map(name => `<li>${name}</li>`).join('')}</ul></div>
    </section>
    <section class="together-section" id="together" aria-labelledby="together-title"><div class="wrap">
      <div class="section-heading"><div><p class="eyebrow">${t("색칠은 각자, 즐거움은 함께")}</p><h2 id="together-title">${t("온 가족의 친구들을 한자리에.")}</h2></div><p>${t("같은 Wi-Fi에 연결한 기기로")}<br>${t("하나의 놀이 공간을 함께 채워보세요.")}</p></div>
      <ol class="steps"><li><span class="step-number">01</span><h3>${t("놀이 공간을 열어요")}</h3><p>${t("한 기기에서 ")}<strong>${t("놀이 공간 열기")}</strong>${t("를 누르고, 함께 놀 장소를 골라요.")}</p></li><li><span class="step-number">02</span><h3>${t("나만의 친구를 색칠해요")}</h3><p>${t("다른 기기에서 ")}<strong>${t("함께 놀기 → 자동 찾기")}</strong>${t("로 연결하고, 좋아하는 동물을 색칠해요.")}</p></li><li><span class="step-number">03</span><h3>${t("보내고, 반갑게 인사해요")}</h3><p><strong>${t("보내기")}</strong>${t("를 누르면 가족의 친구들이 같은 공간에 모여요. 화면을 움직이거나 확대해서 살펴보세요.")}</p></li></ol>
    </div></section>
    <section class="install wrap" id="install" aria-labelledby="install-title">
      <div class="install-copy"><p class="eyebrow">${t("함께할 준비가 됐나요?")}</p><h2 id="install-title">${t("우리 가족의")}<br>${t("십분의 평화를 시작해요.")}</h2><p class="section-description">${t("Android 기기에 APK를 다운로드해 설치하세요.")}<br>${t("기기에 표시되는 설치 안내를 따라주시면 돼요.")}</p></div>
      <div class="install-download"><img src="/media/icon.png" alt="${t("십분의 평화 앱 아이콘")}" width="72" height="72" loading="lazy">${download}<p class="version">${versionLabel}</p><p class="download-help">${t("기존 앱이 있다면 같은 이름의 APK로 업데이트하세요.")}<br>${t("함께 연결할 기기에는 같은 최신 버전을 설치해 주세요.")}</p></div>
    </section>
  </main>
  <footer class="site-footer" id="footer"><div class="wrap footer-row"><a class="brand" href="#game-title">${t("십분의 평화")}</a><p>${t("색칠 한 장으로 시작하는, 우리 가족의 작은 평화.")}</p><span class="font-notices">${t("글꼴 안내")}: <a href="/font-notice.txt">Baskin Robbins</a> · <a href="/comic-relief-license.txt">Comic Relief</a></span>
    <form class="language-picker" action="/" method="get">
      <label for="page-language">${language === 'ko' ? '언어' : 'Language'}</label>
      <select id="page-language" name="lang" onchange="this.form.requestSubmit()">
        <option value="en"${language === 'en' ? ' selected' : ''}>EN</option>
        <option value="ko"${language === 'ko' ? ' selected' : ''}>KR</option>
      </select>
      <noscript><button type="submit">${language === 'ko' ? '적용' : 'Apply'}</button></noscript>
    </form>
  </div></footer>
</body>
</html>`;
}
module.exports = { page, venues, preferredLanguage };
