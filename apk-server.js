const http = require('node:http');
const { open, readdir, stat } = require('node:fs/promises');
const { execFile } = require('node:child_process');
const path = require('node:path');
const { pipeline } = require('node:stream');
const { promisify } = require('node:util');

const port = Number(process.env.APK_SERVER_PORT || 80);
const apkName = 'TenMinutesOfPeace.apk';
const apkPath = path.join(__dirname, 'Builds', 'Android', apkName);
const downloadPath = `/${apkName}`;
const run = promisify(execFile);
let cachedVersion;

function page(version) {
  const versionLabel = version ? `버전 ${version.name} · 빌드 ${version.code}` : '버전 정보 확인 불가';
  return `<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Ten Minutes of Peace · APK 다운로드</title>
  <style>
    * { box-sizing: border-box; }
    body {
      margin: 0;
      min-height: 100vh;
      display: grid;
      place-items: center;
      padding: 1.25rem;
      font: 1rem/1.6 system-ui, sans-serif;
      background: #eef3e9;
      color: #1d3024;
    }
    main {
      width: min(100%, 32rem);
      padding: clamp(1.5rem, 6vw, 3rem);
      background: #fcfdf9;
      border: 1px solid #d9e2d5;
      border-radius: 1.25rem;
    }
    h1 { margin: 0; font-size: clamp(1.75rem, 6vw, 2.75rem); line-height: 1.12; }
    p { margin: 0.75rem 0 1.5rem; }
    a {
      display: inline-block;
      padding: 0.75rem 1.25rem;
      border-radius: 0.6rem;
      background: #285841;
      color: #fff;
      font-weight: 650;
      text-decoration: none;
    }
    a:hover { background: #1d4230; }
    a:focus-visible { outline: 3px solid #285841; outline-offset: 4px; }
  </style>
</head>
<body>
  <main>
    <h1>Ten Minutes of Peace</h1>
    <p>Android에서 설치할 수 있는 APK 파일입니다.</p>
    <p>${versionLabel}</p>
    <a href="${downloadPath}" download>APK 다운로드</a>
  </main>
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

const server = http.createServer((request, response) => {
  if (request.method !== 'GET' && request.method !== 'HEAD') {
    sendText(response, 405, '지원하지 않는 요청 방식입니다.', { Allow: 'GET, HEAD' });
    return;
  }

  const pathname = request.url.split('?', 1)[0];
  if (pathname === '/') {
    void sendPage(request, response);
  } else if (pathname === downloadPath) {
    void sendApk(request, response);
  } else {
    sendText(response, 404, '페이지를 찾을 수 없습니다.');
  }
});

server.on('error', (error) => {
  console.error(`포트 ${port}에서 서버를 시작할 수 없습니다. ${error.message}`);
  process.exitCode = 1;
});

server.listen(port, '0.0.0.0', () => {
  console.log(`APK 다운로드 서버 실행 중 · http://localhost:${port}/`);
  console.log('같은 네트워크의 기기에서는 이 PC의 IPv4 주소로 접속하세요.');
});
