const test = require('node:test');
const assert = require('node:assert/strict');
const { page, preferredLanguage } = require('./page');

test('browser language priority and English fallback', () => {
  for (const [header, expected] of [
    ['ko-KR,ko;q=0.9,en;q=0.8', 'ko'],
    ['en-US,en;q=0.9,ko;q=0.8', 'en'],
    ['en;q=0.4,ko-KR;q=0.9', 'ko'],
    ['KO-kr', 'ko'], ['ja-JP,ko;q=0.8', 'en'],
    ['ko;q=0,en;q=0.5', 'en'], ['', 'en'],
  ]) assert.equal(preferredLanguage(header), expected, header);
});

for (const language of ['ko', 'en']) {
  test(`${language}: title, six places, ten animals and current APK link`, () => {
    const html = page({ name: '1.0.47', code: '48' }, '/TenMinutesOfPeace.apk', language);
    const title = language === 'ko' ? '유아용 게임: 십분의 평화' : 'Ten Minutes of Peace';
    assert.ok(html.includes(`<html lang="${language}">`));
    assert.ok(html.includes(`<h1 id="game-title">${title}</h1>`));
    assert.equal((html.match(/class="place" /g) || []).length, 6);
    assert.match(html, /<img src="\/media\/theme-park\.jpg"/);
    assert.ok(html.includes(`<h3>${language === 'ko' ? '놀이공원' : 'Amusement Park'}</h3>`));
    assert.doesNotMatch(html, /\bbeach\b|해변가/i);
    assert.equal((html.match(/<li>/g) || []).length, 13); // Ten animals and three play steps.
    assert.equal((html.match(/href="\/TenMinutesOfPeace.apk" download/g) || []).length, 2);
    assert.ok(html.includes(language === 'ko' ? '버전 1.0.47 · 빌드 47' : 'Version 1.0.47 · Build 47'));
    assert.ok(!html.includes('undefined'));
    if (language === 'en') assert.doesNotMatch(html, /[가-힣]/);
    else assert.ok(html.includes('우는 아이, 화난 아이, 삐진 아이, 배고픈 아이, 졸린 아이, 모두가 하나되는 평화로운 가족을 지향합니다'));
    const missingApk = page(undefined, '/TenMinutesOfPeace.apk', language);
    assert.ok(!missingApk.includes('undefined'));
    assert.ok(missingApk.includes(language === 'ko' ? '버전 정보 확인 불가' : 'Version information unavailable'));
  });
}
