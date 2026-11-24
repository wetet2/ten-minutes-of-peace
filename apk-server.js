const http = require('node:http');
const { open, readFile, readdir, stat } = require('node:fs/promises');
const { execFile } = require('node:child_process');
const path = require('node:path');
const { pipeline } = require('node:stream');
const { promisify } = require('node:util');
const { page: renderPage, preferredLanguage, screenshots } = require('./DownloadPage/page');

const port = 10282;
const downloadPaths = {
  android: '/TenMinutesOfPeace.apk',
  windowsInstaller: '/TenMinutesOfPeace-Setup.exe',
  windowsZip: '/TenMinutesOfPeace-Windows.zip',
};
const apkPath = path.join(__dirname, 'Builds', 'Android', 'TenMinutesOfPeace.apk');
const windowsBuildPath = path.join(__dirname, 'Builds', 'Windows');
const downloadFiles = new Map([
  [downloadPaths.android, [apkPath, 'application/vnd.android.package-archive']],
  [downloadPaths.windowsInstaller, [path.join(windowsBuildPath, 'TenMinutesOfPeace-Setup.exe'), 'application/octet-stream']],
  [downloadPaths.windowsZip, [path.join(windowsBuildPath, 'Windows.zip'), 'application/zip']],
  ['/TenMinutesOfPeace.exe', [path.join(windowsBuildPath, 'TenMinutesOfPeace.exe'), 'application/octet-stream']],
]);
const run = promisify(execFile);
const pageAssets = new Map([
  ['/download.css', ['DownloadPage/styles.css', 'text/css; charset=utf-8']],
  ['/media/icon.png', ['Assets/Textures/AppIcon.png', 'image/png']],
  ['/media/bear.png', ['Assets/Resources/Welcome/Bear.png', 'image/png']],
  ['/media/frog.png', ['Assets/Resources/Welcome/Frog.png', 'image/png']],
  ['/media/elephant.png', ['Assets/Resources/Welcome/Elephant.png', 'image/png']],
  ['/media/playground.png', ['Assets/Textures/Venues/KidsCafe.png', 'image/png']],
  ['/media/desert.jpg', ['Assets/Textures/Venues/Desert.jpg', 'image/jpeg']],
  ['/media/christmas.jpg', ['Assets/Textures/Venues/Christmas.jpg', 'image/jpeg']],
  ['/media/theme-park.jpg', ['Assets/Textures/Venues/ThemePark.jpg', 'image/jpeg']],
  ['/fonts/game-title.otf', ['Assets/Resources/Fonts/BaskinRobbins-Bold.otf', 'font/otf']],
  ['/fonts/comic-relief-regular.ttf', ['Assets/Resources/Fonts/ComicRelief-Regular.ttf', 'font/ttf']],
  ['/fonts/comic-relief-bold.ttf', ['Assets/Resources/Fonts/ComicRelief-Bold.ttf', 'font/ttf']],
  ['/comic-relief-license.txt', ['Assets/Resources/Fonts/ComicRelief-OFL.txt', 'text/plain; charset=utf-8']],
  ['/font-notice.txt', ['Assets/Resources/Fonts/BaskinRobbins-NOTICE.txt', 'text/plain; charset=utf-8']],
  ...screenshots.map(name => [`/media/${name}`, [`DownloadPage/media/${name}`, 'image/webp']]),
  ...['meadow', 'park', 'forest', 'kids-cafe', 'coloring'].map(name =>
    [`/media/${name}.jpg`, [`DownloadPage/media/${name}.jpg`, 'image/jpeg']]),
]);
let cachedVersion;

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

function requestLanguage(request, choice) {
  if (choice === 'en' || choice === 'ko') return choice;
  const saved = (request.headers.cookie || '').split(';').map(value => value.trim())
    .find(value => /^peace_language=(en|ko)$/.test(value))?.split('=')[1];
  return saved || preferredLanguage(request.headers['accept-language']);
}

async function sendPage(request, response) {
  const choice = new URL(request.url, 'http://localhost').searchParams.get('lang');
  const validChoice = choice === 'en' || choice === 'ko';
  const language = requestLanguage(request, choice);
  const version = apkVersion().catch(error => {
    if (error.code !== 'ENOENT') console.error('APK 버전 읽기 오류:', error.message);
  });
  const fileDates = Object.entries(downloadPaths).map(async ([key, url]) => {
    const [file] = downloadFiles.get(url);
    const modified = await stat(file).then(info => info.isFile() ? info.mtime : null).catch(error => {
      if (error.code !== 'ENOENT') console.error('다운로드 파일 정보 읽기 오류:', error.message);
      return null;
    });
    return [key, modified];
  });
  const [apk, dates] = await Promise.all([version, Promise.all(fileDates)]);
  response.writeHead(200, {
    'Content-Type': 'text/html; charset=utf-8', 'Cache-Control': 'no-store',
    'Content-Language': language, 'Vary': 'Accept-Language, Cookie',
    ...(validChoice ? { 'Set-Cookie': `peace_language=${choice}; Path=/; Max-Age=31536000; SameSite=Lax; HttpOnly` } : {}),
  });
  response.end(request.method === 'HEAD' ? undefined : renderPage(apk, downloadPaths, language, Object.fromEntries(dates)));
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

async function sendDownload(request, response, filename, [filePath, contentType]) {
  let file;
  try {
    file = await open(filePath);
    const { size } = await file.stat();
    response.writeHead(200, {
      'Content-Type': contentType,
      'Content-Disposition': `attachment; filename="${filename}"`,
      'Content-Length': size,
      'Cache-Control': 'no-store',
    });

    if (request.method === 'HEAD') {
      response.end();
      await file.close();
      return;
    }

    pipeline(file.createReadStream(), response, (error) => {
      if (error && error.code !== 'ERR_STREAM_PREMATURE_CLOSE') {
        console.error(`다운로드 전송 오류 (${filename}):`, error.message);
      }
    });
  } catch (error) {
    if (file) await file.close().catch(() => {});
    if (response.headersSent) {
      response.destroy();
      return;
    }
    const language = requestLanguage(request);
    if (error.code === 'ENOENT') {
      sendText(response, 404, language === 'ko' ? '다운로드 파일을 찾을 수 없습니다.' : 'The download file could not be found.');
      return;
    }
    console.error(`다운로드 읽기 오류 (${filename}):`, error.message);
    sendText(response, 500, language === 'ko' ? '다운로드 파일을 읽을 수 없습니다.' : 'The download file could not be read.');
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
  } else if (downloadFiles.has(pathname)) {
    void sendDownload(request, response, path.posix.basename(pathname), downloadFiles.get(pathname));
  } else if (pageAssets.has(pathname)) {
    void sendPageAsset(request, response, pageAssets.get(pathname));
  } else {
    sendText(response, 404, '페이지를 찾을 수 없습니다.');
  }
}

const server = http.createServer(handleRequest);
server.on('error', (error) => {
  console.error(`포트 ${port}에서 서버를 시작할 수 없습니다. ${error.message}`);
  process.exitCode = 1;
});
server.listen(port, '0.0.0.0', () => {
  console.log(`Android 및 Windows 다운로드 서버 실행 중 · http://localhost:${port}/`);
  console.log('같은 네트워크의 기기에서는 이 PC의 IPv4 주소로 접속하세요.');
});
