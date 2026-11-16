const http = require('node:http');
const { open, readFile, readdir, stat } = require('node:fs/promises');
const { execFile } = require('node:child_process');
const path = require('node:path');
const { pipeline } = require('node:stream');
const { promisify } = require('node:util');

const ports = [...new Set((process.env.APK_SERVER_PORTS || process.env.APK_SERVER_PORT || '10282,80')
  .split(',').map(value => Number(value.trim())))];
if (ports.some(port => !Number.isInteger(port) || port < 1 || port > 65535)) {
  throw new Error('APK 서버 포트는 1~65535 사이의 정수를 쉼표로 구분해 지정하세요.');
}
const apkName = 'TenMinutesOfPeace.apk';
const apkPath = path.join(__dirname, 'Builds', 'Android', apkName);
const downloadPath = `/${apkName}`;
const run = promisify(execFile);
const pageAssets = new Map([
  ['/download.css', ['DownloadPage/styles.css', 'text/css; charset=utf-8']],
  ['/media/icon.png', ['Assets/Textures/AppIcon.png', 'image/png']],
  ['/media/bear.png', ['Assets/Resources/Welcome/Bear.png', 'image/png']],
  ['/media/frog.png', ['Assets/Resources/Welcome/Frog.png', 'image/png']],
  ['/media/elephant.png', ['Assets/Resources/Welcome/Elephant.png', 'image/png']],
  ...['meadow', 'park', 'forest', 'kids-cafe', 'coloring'].map(name =>
    [`/media/${name}.jpg`, [`DownloadPage/media/${name}.jpg`, 'image/jpeg']]),
]);
let cachedVersion;

function page(version) {
  const versionLabel = version ? `버전 ${version.name} · 빌드 ${version.code}` : '버전 정보 확인 불가';
  return `<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>유아용 게임: 십분의 평화 · Android 다운로드</title>
  <meta name="description" content="아이들이 색칠한 동물이 3D 놀이 공간에서 살아 움직이는 가족용 색칠 게임. Android APK를 다운로드하세요.">
  <link rel="icon" href="/media/icon.png">
  <link rel="stylesheet" href="/download.css">
</head>
<body>
  <header class="site-header">
    <div class="wrap header-row">
      <a class="brand" href="/" aria-label="십분의 평화 홈"><img src="/media/icon.png" alt="" width="42" height="42">십분의 평화</a>
      <nav aria-label="페이지 메뉴">
        <a href="#play">플레이 화면</a>
        <a href="#install">설치 안내</a>
      </nav>
    </div>
  </header>
  <main>
    <section class="hero wrap" aria-labelledby="game-title">
      <div class="hero-copy">
        <p class="game-kind">유아용 게임</p>
        <h1 id="game-title">십분의 평화</h1>
        <p class="hero-description">아이가 색칠한 동물이 3D 친구가 되어 걸어 나와요. 공원, 숲, 놀이터에서 우리만의 놀이 공간을 열어 보세요.</p>
        <div class="download-row">
          <a class="download" href="${downloadPath}" download><span aria-hidden="true">↓</span> Android APK 다운로드</a>
          <p class="version">${versionLabel}</p>
        </div>
      </div>
      <div class="hero-art" aria-hidden="true">
        <img class="meadow" src="/media/meadow.jpg" alt="" fetchpriority="high">
        <img class="bear" src="/media/bear.png" alt="">
        <img class="frog" src="/media/frog.png" alt="">
        <img class="elephant" src="/media/elephant.png" alt="">
      </div>
    </section>
    <section class="screens-section" id="play" aria-labelledby="screens-title">
      <div class="wrap">
        <h2 id="screens-title">색칠한 친구들과 놀아요</h2>
        <div class="screens">
          <figure>
            <img src="/media/coloring.jpg" alt="동물의 앞모습과 뒷모습을 색칠하는 게임 화면" loading="lazy" width="1280" height="720">
            <figcaption><h3>동물 색칠하기</h3><p>마음에 드는 동물을 골라 앞뒤를 색칠하고 놀이 공간에 보내요.</p></figcaption>
          </figure>
          <figure>
            <img src="/media/park.jpg" alt="분수와 두 채의 집이 있는 3D 공원" loading="lazy" width="1280" height="720">
            <figcaption><h3>공원</h3><p>꽃집과 사탕집의 문을 열고 나온 동물들이 손을 흔들어요.</p></figcaption>
          </figure>
          <figure>
            <img src="/media/forest.jpg" alt="나무 사이에 작은 집과 연못이 있는 숲" loading="lazy" width="1280" height="720">
            <figcaption><h3>숲</h3><p>나무가 빼곡한 숲에서 친구들이 작은 집을 나와 산책해요.</p></figcaption>
          </figure>
          <figure>
            <img src="/media/kids-cafe.jpg" alt="미끄럼틀과 봉 타워가 있는 놀이터" loading="lazy" width="1280" height="720">
            <figcaption><h3>놀이터</h3><p>친구들이 미끄럼틀이나 봉 타워에서 내려와 손을 흔들어요.</p></figcaption>
          </figure>
        </div>
      </div>
    </section>
    <section class="wrap install" id="install" aria-labelledby="install-title">
      <div>
        <h2 id="install-title">설치하고 함께 놀기</h2>
        <ol>
          <li>Android 기기에서 APK를 다운로드해 열어요. 설치 허용 안내가 뜨면 기기 설정에서 허용해 주세요.</li>
          <li>같은 Wi-Fi에 연결한 기기 한 대에서 <strong>놀이 공간 열기</strong>를 누르세요.</li>
          <li>다른 기기에서 <strong>동물 색칠하기</strong>를 누르고 열린 놀이 공간에 연결하세요.</li>
        </ol>
      </div>
      <a class="download" href="${downloadPath}" download><span aria-hidden="true">↓</span> Android APK 다운로드</a>
    </section>
  </main>
  <footer class="wrap site-footer">Ten Minutes of Peace</footer>
</body>
</html>`;
}

async function findAapt() {
  const executable = process.platform === 'win32' ? 'aapt.exe' : 'aapt';
  const sdkRoots = [process.env.ANDROID_HOME, process.env.ANDROID_SDK_ROOT];
  if (process.platform === 'win32') {
    const hub = path.join(process.env.ProgramFiles || 'C:/Program Files', 'Unity', 'Hub', 'Editor');
    const versions = await readdir(hub).catch(() => []);
    sdkRoots.push(...versions.sort().reverse().map(version => path.join(hub, version, 'Editor', 'Data',
      'PlaybackEngines', 'AndroidPlayer', 'SDK')));
  }
  for (const sdk of sdkRoots.filter(Boolean)) {
    const tools = path.join(sdk, 'build-tools');
    const versions = await readdir(tools).catch(() => []);
    for (const version of versions.sort((a, b) => b.localeCompare(a, undefined, { numeric: true }))) {
      const candidate = path.join(tools, version, executable);
      if ((await stat(candidate).catch(() => null))?.isFile()) return candidate;
    }
  }
  return executable;
}

async function apkVersion() {
  const file = await stat(apkPath);
  const key = `${file.size}:${file.mtimeMs}:${file.ctimeMs}`;
  if (cachedVersion?.key === key) return cachedVersion.value;
  const value = (async () => {
    const aapt = await findAapt();
    const { stdout } = await run(aapt, ['dump', 'badging', apkPath], { timeout: 5000, windowsHide: true });
    const match = stdout.match(/^package: .*?versionCode='(\d+)' versionName='(\d+(?:\.\d+)+)'/m);
    if (!match) throw new Error('APK version metadata is missing');
    return { name: match[2], code: match[1] };
  })();
  cachedVersion = { key, value };
  try {
    return await value;
  } catch (error) {
    if (cachedVersion?.value === value) cachedVersion = undefined;
    throw error;
  }
}

async function sendPage(request, response) {
  let version;
  try {
    version = await apkVersion();
  } catch (error) {
    if (error.code !== 'ENOENT') console.error('APK 버전 읽기 오류:', error.message);
  }
  response.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8', 'Cache-Control': 'no-store' });
  response.end(request.method === 'HEAD' ? undefined : page(version));
}

function sendText(response, status, message, headers = {}) {
  response.writeHead(status, { 'Content-Type': 'text/plain; charset=utf-8', ...headers });
  response.end(message);
}

async function sendPageAsset(request, response, [file, contentType]) {
  try {
    const bytes = await readFile(path.join(__dirname, file));
    response.writeHead(200, {
      'Content-Type': contentType,
      'Content-Length': bytes.length,
      'Cache-Control': 'no-cache',
    });
    response.end(request.method === 'HEAD' ? undefined : bytes);
  } catch (error) {
    if (error.code === 'ENOENT') {
      sendText(response, 404, '페이지 리소스를 찾을 수 없습니다.');
    } else {
      console.error('페이지 리소스 읽기 오류:', error.message);
      sendText(response, 500, '페이지 리소스를 읽을 수 없습니다.');
    }
  }
}

async function sendApk(request, response) {
  let apk;
  try {
    apk = await open(apkPath);
    const { size } = await apk.stat();
    response.writeHead(200, {
      'Content-Type': 'application/vnd.android.package-archive',
      'Content-Disposition': `attachment; filename="${apkName}"`,
      'Content-Length': size,
      'Cache-Control': 'no-store',
    });

    if (request.method === 'HEAD') {
      response.end();
      await apk.close();
      return;
    }

    pipeline(apk.createReadStream(), response, (error) => {
      if (error && error.code !== 'ERR_STREAM_PREMATURE_CLOSE') {
        console.error('APK 전송 오류:', error.message);
      }
    });
  } catch (error) {
    if (apk) await apk.close().catch(() => {});
    if (response.headersSent) {
      response.destroy();
    } else if (error.code === 'ENOENT') {
      sendText(response, 404, 'APK 파일이 없습니다. Android 빌드를 먼저 만들어 주세요.');
    } else {
      console.error('APK 읽기 오류:', error.message);
      sendText(response, 500, 'APK 파일을 읽을 수 없습니다.');
    }
  }
}

function handleRequest(request, response) {
  if (request.method !== 'GET' && request.method !== 'HEAD') {
    sendText(response, 405, '지원하지 않는 요청 방식입니다.', { Allow: 'GET, HEAD' });
    return;
  }

  const pathname = request.url.split('?', 1)[0];
  if (pathname === '/') {
    void sendPage(request, response);
  } else if (pathname === downloadPath) {
    void sendApk(request, response);
  } else if (pageAssets.has(pathname)) {
    void sendPageAsset(request, response, pageAssets.get(pathname));
  } else {
    sendText(response, 404, '페이지를 찾을 수 없습니다.');
  }
}

for (const port of ports) {
  const server = http.createServer(handleRequest);
  server.on('error', (error) => {
    console.error(`포트 ${port}에서 서버를 시작할 수 없습니다. ${error.message}`);
    process.exitCode = 1;
  });
  server.listen(port, '0.0.0.0', () => {
    console.log(`APK 다운로드 서버 실행 중 · http://localhost:${port}/`);
  });
}
console.log('같은 네트워크의 기기에서는 이 PC의 IPv4 주소로 접속하세요.');
