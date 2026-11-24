const venues = [
  {
    id: 'clock-square', image: 'ice-skating.webp', color: 'lavender',
    ko: ['시계탑 광장', '눈 내리는 성당 앞에서 사슴 세 마리를 만나고, 빙판을 가로지르는 8자 코스를 돌아요.', ['스케이트 타기', '트리플 악셀', '사슴 타기']],
    en: ['Clock Tower Square', 'Meet three deer outside the snowy cathedral, then glide across the rink on a figure-eight course.', ['Ice skating', 'Triple Axel', 'Deer rides']],
  },
  {
    id: 'pool', image: 'pool.webp', color: 'aqua',
    ko: ['수영장', '타워에서 빙글빙글 내려와 물보라를 일으켜요. 튜브를 끼고 둥근 물길을 한 바퀴 돌아보세요.', ['워터슬라이드', '수영', '튜브 타기']],
    en: ['Swimming Pool', 'Spiral down the slide into a splash, swim in the pool or float around the lazy river.', ['Water slide', 'Swimming', 'Tube rides']],
  },
  {
    id: 'amusement', image: 'amusement.webp', color: 'lavender',
    ko: ['놀이공원', '코끼리 기차로 한 바퀴 둘러보고, 대관람차와 회전목마에도 올라타요.', ['코끼리 기차', '대관람차', '회전목마']],
    en: ['Amusement Park', 'Circle the park on the Elephant Train, catch the view from the Ferris wheel or hop on the carousel.', ['Elephant Train', 'Ferris wheel', 'Carousel']],
  },
  {
    id: 'forest', image: 'forest.webp', color: 'leaf',
    ko: ['숲', '빵집에서 나온 친구와 그루터기를 깡충깡충 건너고 나무 사이로 짚라인을 타요.', ['그루터기', '구름사다리', '짚라인']],
    en: ['Forest', 'Meet a friend by the bakery, hop across stepping stumps and zip between the trees.', ['Stepping stumps', 'Log bridge', 'Zipline']],
  },
  {
    id: 'playground', image: 'playground.webp', color: 'peach',
    ko: ['놀이터', '봉 타워 위에서 인사한 뒤 봉을 타고 내려와요. 다시 탈 땐 오른쪽 사다리로 올라가요.', ['봉타기', '미끄럼틀', '방방', '시소', '뺑뺑이']],
    en: ['Playground', 'Wave from the tower, slide down the pole, then climb the ladder on the right to go again.', ['Pole slide', 'Slide', 'Trampoline', 'Seesaw', 'Roundabout']],
  },
  {
    id: 'desert', image: 'desert.webp', color: 'peach',
    ko: ['사막', '높은 모래언덕을 썰매로 내려오고, 오아시스 옆 모닥불에 둘러앉아 쉬어요.', ['모래썰매', '캠핑']],
    en: ['Desert', 'Sled down a tall dune, then take a break by the campfire next to the oasis.', ['Sand sledding', 'Camping']],
  },
  {
    id: 'park', image: 'park.webp', color: 'leaf',
    ko: ['공원', '꽃집과 과자집에서 나온 친구들이 손을 흔들고 분수 주변을 산책해요.', ['분수', '산책']],
    en: ['Park', 'Friends wave outside the flower and candy cottages, then wander around the fountain.', ['Fountain', 'Strolling']],
  },
];

const heroImage = 'clock-square.webp';
const screenshots = [heroImage, ...venues.map(({ image }) => image)];
const animals = {
  ko: ['곰', '호랑이', '토끼', '개구리', '기린', '코끼리', '트리케라톱스', '악어', '하마', '고릴라'],
  en: ['Bear', 'Tiger', 'Rabbit', 'Frog', 'Giraffe', 'Elephant', 'Triceratops', 'Crocodile', 'Hippo', 'Gorilla'],
};
const copy = {
  ko: {
    title: '유아용 게임: 십분의 평화',
    meta: '내가 색칠한 동물이 살아 움직이는 가족용 3D 색칠 게임. 스케이트장이 있는 시계탑 광장과 수영장 등 7곳에서 함께 놀아요. Windows와 Android 다운로드.',
    skip: '본문으로 건너뛰기', menu: '페이지 메뉴', places: '놀이 공간', together: '함께 놀기', download: '다운로드',
    category: '가족용 3D 색칠 게임', headline: '내가 색칠한 동물과 함께 놀아요.',
    family: '우는 아이, 화난 아이, 삐진 아이, 배고픈 아이, 졸린 아이, 모두가 하나되는 평화로운 가족을 지향합니다',
    platforms: 'Windows 다운로드', seePlaces: '7개 놀이 공간 보기',
    heroAlt: '성당과 사슴 산책로 앞 스케이트장에서 동물들이 스케이트를 타는 실제 게임 화면',
    heroCaption: '시계탑 광장', heroLink: '스케이트장 보기',
    stats: [['7곳', '놀이 공간'], ['10종', '색칠할 동물'], ['최대 10마리', '한 놀이 공간에 함께']],
    placesTitle: '오늘은 어디에서 놀까요?', placesIntro: '눈 내리는 광장에서 스케이트를 타고, 햇살 아래 수영장에서 물놀이를 즐겨요.',
    actual: '실제 게임 화면', enlarge: '게임 화면 크게 보기 (새 탭)',
    friendsTitle: '칠한 색 그대로, 움직이는 내 친구',
    friendsDescription: '동물의 앞면과 뒷면을 색칠해 보내면 놀이 공간에서 인사하고 산책해요. 동물을 터치해 따라가거나, 발바닥 버튼으로 놀이 중인 동물을 자동으로 따라갈 수 있어요.',
    animalList: '색칠할 수 있는 동물 10종',
    togetherTitle: '같은 Wi-Fi에서 함께 놀아요',
    togetherDescription: '가족이나 친구가 각자 색칠한 동물을 한 화면에 모을 수 있어요.',
    steps: [
      ['놀이 공간 열기', '한 기기에서 놀이 공간 열기를 눌러 장소를 고르세요.'],
      ['다른 기기 연결하기', '같은 Wi-Fi의 다른 기기에서 함께 놀기 → 자동 찾기로 연결하세요.'],
      ['색칠한 동물 보내기', '동물의 앞뒤를 색칠하고 보내기를 누르면 같은 놀이 공간에 등장해요.'],
    ],
    installTitle: '기기에 맞는 파일을 다운로드하세요',
    installIntro: '스케이트장 등 최근 추가된 놀이는 Android APK와 Windows ZIP에서 이용할 수 있어요.',
    androidLabel: 'Android APK 다운로드', androidInstructions: '안드로이드 기기에서 APK를 열어 설치하세요.',
    androidVersion: '버전', androidBuild: '빌드', noVersion: '버전 정보 확인 불가',
    windowsLabel: 'Windows ZIP 다운로드', windowsInstructions: 'ZIP의 압축을 모두 푼 뒤 TenMinutesOfPeace.exe를 실행하세요.',
    installerLabel: 'Windows 설치 파일', installerInstructions: '설치 파일을 실행하고 안내를 따라주세요.',
    dateLabel: '파일 날짜', previousInstaller: '이전 설치 패키지', noFile: '파일 준비 중',
    fontNotices: '글꼴 안내', language: '언어', apply: '적용',
  },
  en: {
    title: 'Ten Minutes of Peace',
    meta: 'A family 3D coloring game where your animals come to life. Explore seven play spaces including Clock Tower Square and its ice rink, plus a swimming pool. Download for Windows or Android.',
    skip: 'Skip to content', menu: 'Page navigation', places: 'Play Spaces', together: 'Play Together', download: 'Download',
    category: 'A family 3D coloring game', headline: 'Color an animal. Play with the friend you made.',
    family: 'Crying, angry, sulking, hungry or sleepy — our hope is to bring the whole family together for a little peace.',
    platforms: 'Windows downloads', seePlaces: 'Explore all seven places',
    heroAlt: 'In-game view of two skaters on the rink in front of the deer promenade and cathedral',
    heroCaption: 'Clock Tower Square', heroLink: 'See the ice rink',
    stats: [['7', 'play spaces'], ['10', 'animals to color'], ['Up to 10', 'animals together']],
    placesTitle: 'Where shall we play today?', placesIntro: 'Skate in a snowy square or splash around at a sunny pool.',
    actual: 'in-game screenshot', enlarge: 'View game screenshot (new tab)',
    friendsTitle: 'A little friend in the colors you chose',
    friendsDescription: 'Color both sides of an animal and send it to the play space. Tap a friend to follow it with the camera, or use the paw button to follow friends as they play.',
    animalList: '10 animals to color',
    togetherTitle: 'Play together on the same Wi-Fi',
    togetherDescription: 'Bring the animals your family and friends color into one shared play space.',
    steps: [
      ['Open a play space', 'On one device, choose Open a Play Space and pick a place.'],
      ['Connect another device', 'On another device on the same Wi-Fi, choose Play Together → Find Automatically.'],
      ['Send your animal', 'Color both sides and press Send to bring your animal into the shared play space.'],
    ],
    installTitle: 'Download for your device',
    installIntro: 'Find the ice rink and other recent additions in the Android APK and Windows ZIP.',
    androidLabel: 'Download Android APK', androidInstructions: 'Open the APK on your Android device to install.',
    androidVersion: 'Version', androidBuild: 'Build', noVersion: 'Version information unavailable',
    windowsLabel: 'Download Windows ZIP', windowsInstructions: 'Extract the entire ZIP, then open TenMinutesOfPeace.exe.',
    installerLabel: 'Windows Installer', installerInstructions: 'Open the installer and follow the instructions.',
    dateLabel: 'File date', previousInstaller: 'Earlier installer package', noFile: 'File unavailable',
    fontNotices: 'Font notices', language: 'Language', apply: 'Apply',
  },
};

const androidMark = '<svg viewBox="0 0 24 24" aria-hidden="true" focusable="false"><path d="m5.7 6.1 2.1 3.5m10.5-3.5-2.1 3.5" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"/><path d="M2 18a10 10 0 0 1 20 0v1H2Zm5.9-3.8a1.1 1.1 0 1 0 2.2 0 1.1 1.1 0 0 0-2.2 0Zm6 0a1.1 1.1 0 1 0 2.2 0 1.1 1.1 0 0 0-2.2 0Z" fill="currentColor" fill-rule="evenodd"/></svg>';
const windowsMark = '<svg viewBox="0 0 24 24" aria-hidden="true" focusable="false"><path d="M2 2h9v9H2zm11 0h9v9h-9zM2 13h9v9H2zm11 0h9v9h-9z" fill="currentColor"/></svg>';

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

function page(version, downloadPaths, language = 'en', packages = {}) {
  language = language === 'ko' ? 'ko' : 'en';
  const t = copy[language];
  const [featured, ...places] = venues;
  const versionLabel = version ? `${t.androidVersion} ${version.name} · ${t.androidBuild} ${version.code}` : t.noVersion;
  const dateFormat = new Intl.DateTimeFormat(language === 'ko' ? 'ko-KR' : 'en-US', {
    dateStyle: 'medium', timeStyle: 'short', timeZone: 'Asia/Seoul',
  });
  const title = language === 'ko' ? '<span>유아용 게임:</span> 십분의 평화' : 'Ten Minutes<br>of Peace';
  const earlierInstaller = packages.windowsZip && packages.windowsInstaller && packages.windowsZip > packages.windowsInstaller;

  function releaseDate(key) {
    if (!packages[key]) return '';
    return `<p class="release-date">${t.dateLabel} <time datetime="${packages[key].toISOString()}">${dateFormat.format(packages[key])}</time></p>`;
  }
  function fileLink(key, label, mark, variant = '') {
    if (packages[key] === null) return `<span class="button unavailable" aria-disabled="true">${mark} ${label} · ${t.noFile}</span>`;
    return `<a class="button ${variant}" href="${downloadPaths[key]}" download>${mark}<span>${label}</span><span aria-hidden="true">↓</span></a>`;
  }
  function placeCard(venue, highlighted = false) {
    const [name, description, activities] = venue[language];
    return `<article class="place ${venue.color}${highlighted ? ' featured-place' : ''}" id="${venue.id}">
      <a class="place-image" href="/media/${venue.image}" target="_blank" rel="noopener" aria-label="${name} · ${t.enlarge}">
        <img src="/media/${venue.image}" width="1152" height="648" alt="${name} ${t.actual}" loading="lazy">
        <span class="expand" aria-hidden="true">↗</span>
      </a>
      <div class="place-info"><h3>${name}</h3><p>${description}</p>
        <ul class="activities">${activities.map(activity => `<li>${activity}</li>`).join('')}</ul>
      </div>
    </article>`;
  }

  return `<!doctype html>
<html lang="${language}">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <meta name="theme-color" content="#faf7ef">
  <meta name="description" content="${t.meta}">
  <title>${t.title}</title>
  <link rel="icon" href="/media/icon.png">
  <link rel="stylesheet" href="/download.css">
</head>
<body>
  <a class="skip-link" href="#main">${t.skip}</a>
  <header class="site-header">
    <div class="shell header-content">
      <a class="brand" href="/" aria-label="${t.title}"><img src="/media/icon.png" width="42" height="42" alt=""><span>${t.title}</span></a>
      <nav aria-label="${t.menu}">
        <a href="#places">${t.places}</a><a href="#together">${t.together}</a>
        <a class="nav-download" href="#install">${t.download} <span aria-hidden="true">↓</span></a>
      </nav>
    </div>
  </header>
  <main id="main">
    <section class="hero shell" aria-labelledby="game-title">
      <div class="hero-copy">
        <p class="category">${t.category}</p>
        <h1 id="game-title">${title}</h1>
        <p class="headline">${t.headline}</p>
        <p class="family-message">${t.family}</p>
        <div class="hero-actions">
          <a class="button" href="#install">${windowsMark}<span>${t.platforms}</span></a>
          ${fileLink('android', t.androidLabel, androidMark, 'button-outline')}
        </div>
        <a class="inline-link" href="#places">${t.seePlaces} <span aria-hidden="true">↓</span></a>
      </div>
      <figure class="hero-view">
        <img src="/media/${heroImage}" alt="${t.heroAlt}" width="1440" height="810" fetchpriority="high">
        <figcaption><span>${t.heroCaption}</span><a href="#clock-square">${t.heroLink} <span aria-hidden="true">↗</span></a></figcaption>
      </figure>
    </section>
    <dl class="stats shell">${t.stats.map(([number, label]) => `<div><dt>${label}</dt><dd>${number}</dd></div>`).join('')}</dl>
    <section id="places" class="places-section shell" aria-labelledby="places-title">
      <div class="section-head"><h2 id="places-title">${t.placesTitle}</h2><p>${t.placesIntro}</p></div>
      ${placeCard(featured, true)}
      <div class="place-grid">${places.map(venue => placeCard(venue)).join('')}</div>
    </section>
    <section class="friends shell" aria-labelledby="friends-title">
      <div class="friends-art" aria-hidden="true"><img src="/media/bear.png" alt="" loading="lazy"><img src="/media/frog.png" alt="" loading="lazy"><img src="/media/elephant.png" alt="" loading="lazy"></div>
      <div class="friends-copy"><h2 id="friends-title">${t.friendsTitle}</h2><p>${t.friendsDescription}</p><ul class="animals" aria-label="${t.animalList}">${animals[language].map(name => `<li>${name}</li>`).join('')}</ul></div>
    </section>
    <section class="together-section" id="together" aria-labelledby="together-title"><div class="shell">
      <div class="section-head"><h2 id="together-title">${t.togetherTitle}</h2><p>${t.togetherDescription}</p></div>
      <ol class="steps">${t.steps.map(([label, description], i) => `<li><span class="step-number" aria-hidden="true">${i + 1}</span><h3>${label}</h3><p>${description}</p></li>`).join('')}</ol>
    </div></section>
    <section class="install shell" id="install" aria-labelledby="install-title">
      <div class="section-head"><h2 id="install-title">${t.installTitle}</h2><p>${t.installIntro}</p></div>
      <div class="install-grid">
        <article class="install-card windows">
          <div class="device-icon" aria-hidden="true">${windowsMark}</div><h3>Windows</h3>
          <p>${t.windowsInstructions}</p>
          ${fileLink('windowsZip', t.windowsLabel, windowsMark)}
          ${releaseDate('windowsZip')}
          <div class="installer-row">
            ${fileLink('windowsInstaller', t.installerLabel, windowsMark, 'button-outline')}
            <p>${earlierInstaller ? t.previousInstaller : t.installerInstructions}</p>
            ${releaseDate('windowsInstaller')}
          </div>
        </article>
        <article class="install-card android">
          <div class="device-icon" aria-hidden="true">${androidMark}</div><h3>Android</h3>
          <p>${t.androidInstructions}</p>
          ${fileLink('android', t.androidLabel, androidMark)}
          <p class="version">${versionLabel}</p>
          ${releaseDate('android')}
        </article>
      </div>
    </section>
  </main>
  <footer class="site-footer"><div class="shell footer-content">
    <a class="brand" href="#game-title">${t.title}</a>
    <span class="font-notices">${t.fontNotices} <a href="/font-notice.txt">Baskin Robbins</a> · <a href="/comic-relief-license.txt">Comic Relief</a></span>
    <form class="language-picker" action="/" method="get">
      <label for="page-language">${t.language}</label>
      <select id="page-language" name="lang" onchange="this.form.requestSubmit()">
        <option value="en"${language === 'en' ? ' selected' : ''}>EN</option>
        <option value="ko"${language === 'ko' ? ' selected' : ''}>KR</option>
      </select>
      <noscript><button type="submit">${t.apply}</button></noscript>
    </form>
  </div></footer>
</body>
</html>`;
}

module.exports = { page, venues, preferredLanguage, screenshots };
