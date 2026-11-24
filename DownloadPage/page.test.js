const test = require('node:test');
const assert = require('node:assert/strict');
const { existsSync } = require('node:fs');
const { join } = require('node:path');
const { page, venues, preferredLanguage, screenshots } = require('./page');

const paths = {
  android: '/TenMinutesOfPeace.apk',
  windowsInstaller: '/TenMinutesOfPeace-Setup.exe',
  windowsZip: '/TenMinutesOfPeace-Windows.zip',
};
const dates = {
  android: new Date('2026-11-23T04:11:29Z'),
  windowsInstaller: new Date('2026-11-22T06:47:40Z'),
  windowsZip: new Date('2026-11-23T07:20:00Z'),
};
const apk = { name: '1.0.58', code: '59' };

test('browser language priority and English fallback', () => {
  for (const [header, expected] of [
    ['ko-KR,ko;q=0.9,en;q=0.8', 'ko'], ['en-US,en;q=0.9,ko;q=0.8', 'en'],
    ['en;q=0.4,ko-KR;q=0.9', 'ko'], ['KO-kr', 'ko'], ['ja-JP,ko;q=0.8', 'en'],
    ['ko;q=0,en;q=0.5', 'en'], ['', 'en'],
  ]) assert.equal(preferredLanguage(header), expected, header);
});

test('each published screenshot exists and has a unique URL', () => {
  assert.equal(new Set(screenshots).size, 8);
  for (const file of screenshots) assert.ok(existsSync(join(__dirname, 'media', file)), file);
});

for (const language of ['ko', 'en']) {
  test(language + ': seven places, current activities, and correct download links', () => {
    const html = page(apk, paths, language, dates);
    const korean = language === 'ko';
    assert.ok(html.includes('<html lang="' + language + '">'));
    assert.ok(html.includes(korean ? '<title>유아용 게임: 십분의 평화</title>' : '<title>Ten Minutes of Peace</title>'));
    assert.equal((html.match(/<article class="place /g) || []).length, 7);
    for (const venue of venues) {
      assert.ok(html.includes('<h3>' + venue[language][0] + '</h3>'), venue.id);
      assert.ok(html.includes('src="/media/' + venue.image + '"'), venue.id);
    }
    assert.ok(html.includes('src="/media/clock-square.webp"'));
    assert.ok(html.includes(korean ? '트리플 악셀' : 'Triple Axel'));
    assert.ok(html.includes(korean ? '8자 코스' : 'figure-eight course'));
    assert.ok(html.includes(korean ? '워터슬라이드' : 'Water slide'));
    assert.ok(html.includes(korean ? '봉타기' : 'Pole slide'));
    assert.ok(html.includes(korean ? '모래썰매' : 'Sand sledding'));
    assert.doesNotMatch(html, /\bbeach\b|해변가/i);
    assert.match(html, /class="animals"[^>]*>(?:<li>[^<]+<\/li>){10}<\/ul>/);
    for (const [href, count] of [[paths.android, 2], [paths.windowsZip, 1], [paths.windowsInstaller, 1]]) {
      assert.equal(html.split('href="' + href + '" download><svg').length - 1, count);
    }
    assert.ok(html.includes(korean ? '버전 1.0.58 · 빌드 59' : 'Version 1.0.58 · Build 59'));
    assert.ok(html.includes(korean ? 'Android APK와 Windows ZIP에서 이용할 수 있어요' : 'in the Android APK and Windows ZIP'));
    assert.ok(html.includes(korean ? '이전 설치 패키지' : 'Earlier installer package'));
    assert.ok(html.includes('<option value="' + language + '" selected>'));
    assert.equal((html.match(/<time datetime="/g) || []).length, 3);
    assert.doesNotMatch(html, /undefined/);
    if (korean) assert.ok(html.includes('우는 아이, 화난 아이, 삐진 아이, 배고픈 아이, 졸린 아이, 모두가 하나되는 평화로운 가족을 지향합니다'));
    else assert.doesNotMatch(html, /[가-힣]/);
  });

  test(language + ': unavailable files do not show dead download buttons', () => {
    const html = page(undefined, paths, language, { android: null, windowsInstaller: null, windowsZip: dates.windowsZip });
    assert.ok(html.includes(language === 'ko' ? '버전 정보 확인 불가' : 'Version information unavailable'));
    assert.ok(html.includes(language === 'ko' ? '파일 준비 중' : 'File unavailable'));
    assert.ok(!html.includes(paths.android));
    assert.ok(!html.includes(paths.windowsInstaller));
    assert.ok(html.includes(paths.windowsZip));
  });
}
