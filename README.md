# Ten Minutes of Peace — 색칠한 친구들의 공원

## 현재 개발 버전: 동물 일곱 종

곰, 호랑이, 토끼, 개구리, 기린, 코끼리, 트리케라톱스를 색칠해 공원에 보낼 수 있습니다. 통통한 사람형 몸과 걷기·인사 동작을 공유하고 머리와 꼬리는 동물별로 제작했습니다. 트리케라톱스의 정면과 옆면은 `Previews/triceratops-model.png`에서 볼 수 있습니다.

### 같은 Wi-Fi에서 함께 색칠하기

최신 Windows 실행 파일은 `Builds/Windows-LAN/TenMinutesOfPeace.exe`입니다. 같은 PC에서 실행 파일을 두 번 열어 연결을 확인할 수 있습니다.

1. 공원을 띄울 창에서 **기기 연결 → 이 기기에서 공원 열기**를 선택합니다.
2. 색칠할 창에서 **기기 연결**을 누르고 `127.0.0.1`을 입력한 뒤 **연결하고 색칠**을 누릅니다. 다른 PC라면 호스트에 표시된 같은 Wi-Fi의 IPv4 주소를 입력합니다.
3. 동물을 골라 앞뒷면을 색칠하고 **적용**을 누릅니다. 공원에서 동물이 등장하면 색칠 창에서 새 그림을 시작할 수 있습니다. 전송 확인에 실패하면 현재 그림이 남습니다.

공원 호스트는 TCP 포트 42731을 사용합니다. 원격 접수는 대기 포함 동물 24마리까지이고, 한꺼번에 기다리는 동물은 최대 8마리입니다. 새 동물을 보낼 때는 공원 창과 색칠 창을 모두 최신 버전으로 실행하세요.

### 둥근 현관 포치

꽃집은 연보라 테두리와 꽃무늬 돌바닥, 과자집은 비스킷 바닥과 작은 캔디 무늬를 사용합니다. 모서리가 둥근 포치는 산책로 높이로 완만하게 내려갑니다. 각 집의 `Rounded themed porch` 아래에서 편집할 수 있습니다. 생성 메뉴는 `Peace Park > Refine Cottage Entrances`입니다.

토끼의 등장 높이도 현관 경사와 맞췄습니다. Windows 플레이어 검증에서 양쪽 현관의 이전 사각 발판 제거 및 등장 경로 202개 샘플이 통과했습니다. 실제 렌더는 `Previews/porch-flower-1440p.png`, `Previews/porch-candy-1440p.png`입니다.

### Pretendard 및 애니메이션 공원 표현

Pretendard와 초원 배경은 `ParkPlayground` 장면에 반영되어 있습니다.

공원·드로잉·토끼 단독 화면은 `Assets/Resources/Fonts`의 Pretendard 1.3.9 Regular/Bold를 사용합니다. OS 설치 글꼴에 의존하지 않으며 글꼴 데이터를 빌드에 포함합니다. SIL OFL 원문을 폰트 폴더와 Windows 배포 폴더에 동봉합니다.

공원은 `PeacePark/Storybook` 셰이더와 선명한 팔레트를 사용합니다. 회색 반복 노이즈 텍스처를 제거하고 부드러운 명암과 실시간 그림자로 입체감을 만듭니다. 잔디에는 월드 좌표의 약한 넓은 색 변화만 사용합니다. 나무 수관은 큰 삼각 면 대신 매끄러운 굴곡이 있는 메시로 교체했습니다. 기존 얇은 섬 받침대 가장자리는 숨기고 초원, 외부 길, 중경 나무, 먼 언덕·구름을 연결했습니다. 배경에는 거리 안개를 사용하며 산책 영역은 기존 울타리 안에 유지됩니다.

새 아트 생성 코드는 `Assets/Editor/ParkArtDirection.cs`, 생성 에셋은 `Assets/StorybookGenerated`입니다. `Peace Park > Apply Storybook Art`는 현재 아트 방향을 다시 적용하므로 수동 아트 수정 후에는 실행 여부에 주의하세요. 원본 `ParkPolished` 장면과 원본 머티리얼은 보존했습니다. 현재 아트는 코드로 생성한 프로토타입이며 전문 제작된 동물의 숲 수준의 완성 에셋을 재현한 것은 아닙니다.

검증: Windows 플레이어에서 Pretendard 한글 글리프, 연속 배경 및 토끼 5마리의 등장·산책·발 접지가 통과했습니다. 실제 창에서도 공원/드로잉 전환과 글꼴 표시를 확인했습니다. 실제 2560×1440 카메라 렌더: `Previews/park-storybook-1440p.png`, `Previews/park-storybook-door-1440p.png`.

### Windows 실행 파일

`Builds/Windows/TenMinutesOfPeace.exe`를 실행하면 Unity Editor 없이 공원과 색칠 화면을 사용할 수 있습니다. 기본 창 해상도는 2560×1440이고 창 크기를 변경할 수 있습니다. **F11**은 모니터 해상도의 전체화면과 창 모드 전환, **F3**은 현재 해상도와 FPS 표시입니다. 종료는 창의 X 또는 Alt+F4입니다. 실행 파일을 다른 곳으로 옮길 때는 `Windows` 폴더 전체를 복사하세요.

GPU 스키닝과 고정 배경 정적 배칭을 사용합니다. 기본 모드는 2× MSAA와 2048 그림자를 사용하며, **F10**으로 4× MSAA와 4096 그림자를 사용하는 고품질 모드로 전환할 수 있습니다. **F3**은 현재 FPS와 품질 모드를 표시합니다. 색칠 화면에서는 공원의 3D 렌더링을 끕니다. 복잡한 토끼 메시와 어깨 보정은 `Assets/Resources/Rabbit/RabbitRig.asset`으로 미리 생성합니다. 토끼 형상을 수정하면 이 생성 에셋을 갱신해야 합니다.

산책 중 잔디와 산책로의 실제 표면 높이를 사용하고, 65개 걸음 샘플의 발바닥 높이를 보간해 몸 높이를 보정합니다. Windows 플레이어에서 129개 걸음 자세의 최대 접지 오차는 0.00006 월드 단위였습니다. 렌더 예: `Previews/living-park-grounded-friends-1440p.png`. 표시 장치의 실제 FPS는 F3으로 확인하세요. 자동 검증의 화면 밖 렌더링 속도는 창 표시를 포함한 실사용 FPS와 다릅니다.

Unity에서 `Assets/Scenes/ParkPlayground.unity`를 열고 Play를 누르세요. 메뉴 `Peace Park > Open Living Park`로도 열 수 있습니다. 기존 공원과 토끼 단독 실험 장면은 보존했습니다.

1. 공원 오른쪽 위의 **동물 색칠하기**를 누릅니다.
2. 동물 앞·뒷면을 색칠하고 **적용**을 누릅니다. 적용 버튼은 3초 동안 다시 누를 수 없습니다.
3. 공원으로 돌아오면 무작위로 선택된 꽃집 또는 과자집의 문이 열리고, 동물이 걸어 나와 집 앞에서 한 번 손을 흔든 뒤 산책합니다.
4. 다시 색칠하면 새 동물이 추가됩니다. 색칠 도중 **공원 보기**로 돌아갈 수도 있습니다.

같은 집에 도착 요청이 겹치면 순서대로 등장합니다. 동물마다 앞·뒷면 이미지와 머티리얼을 따로 보유하고, 같은 종은 보정된 캐릭터 메시를 공유합니다. 문은 실제 개구부와 회전 경첩을 사용합니다. 산책은 집과 울타리를 피하도록 정한 중앙 영역에서 진행하며, 가까운 동물을 피하려고 방향을 조정합니다.

한 기기에서 색칠하거나, 같은 네트워크의 공원 호스트로 그림을 보낼 수 있습니다. 작업과 공원은 실행 중에만 유지되며 저장하지 않습니다. 실제 태블릿 성능과 터치 입력은 아직 검증하지 않았습니다.

Windows 빌드는 `Peace Park > Build Living Park Windows`를 사용합니다. 결과는 `Builds/Windows/TenMinutesOfPeace.exe`이며, 함께 생성된 데이터 폴더도 필요합니다. 아래 기존 공원 안내는 이전 단계의 기록입니다.

검증: Unity 6000.6.8f1에서 실제 Apply 및 3초 잠금, 두 집의 문 열림·닫힘, 같은 집의 대기열, 독립적인 색상 5마리, 열린 문을 통한 등장, 한 번 인사, 영역 내 산책, 화면 전환과 카메라 입력 전환이 통과했습니다. Windows 빌드와 독립 플레이어에서도 Apply, 5마리 생성, 등장·인사·산책, 실제 스킨 메시 및 색상 셰이더 렌더링을 확인했습니다. 마우스 제스처와 물리 디바이스 입력은 자동 검증 범위에 포함하지 않습니다. 에디터 렌더는 `Previews/living-park-welcome.png`, `Previews/living-park-friends.png`, Windows 플레이어 카메라 렌더는 `Previews/living-park-player-door.png`, `Previews/living-park-player-friends.png`에 있습니다.

## 이전 공원 프로토타입

Unity 6용 Built-in Render Pipeline 공원 프로토타입입니다. 외부 유료 에셋 없이 기본 3D 도형으로 제작하며, 최초 임포트 후 편집 가능한 장면과 머티리얼을 자동 생성합니다. 생성 이미지와 동일한 아트 품질을 재현한 완성본은 아닙니다.

## 실행

1. Unity Hub에서 Unity 6 Editor와 Windows Build Support를 설치합니다. 프로젝트의 기준 버전은 `6000.0.23f1`입니다. 설치된 Unity 6 버전으로 열 때 버전 변경 안내가 나올 수 있습니다.
2. Hub의 Add / Add project from disk로 이 폴더를 등록합니다.
3. 프로젝트를 열고 스크립트 임포트를 기다립니다. `Assets/Scenes/Park.unity`가 자동으로 만들어집니다.
4. 상단 Play를 누르고 Game 화면에서 조작합니다.
5. 자동 생성되지 않으면 `Peace Park > Create Park Scene`을 실행합니다.

## 조작

- 왼쪽 마우스 드래그: 공원을 중심으로 회전
- 오른쪽 또는 가운데 마우스 드래그: 시점의 중심 이동
- 마우스 휠: 확대·축소
- Home 또는 Reset view 버튼: 처음 시점으로 복귀

현재 카메라는 기존 Input Manager를 사용합니다. 새 프로젝트 설정으로 가져가는 경우 Player Settings의 Active Input Handling을 `Input Manager (Old)` 또는 `Both`로 설정하세요.

## 포함된 구성

꽃집, 과자집, 넓은 잔디, 순환 산책로, 나무, 울타리, 화단과 정적인 벤치. 동물·드로잉·네트워크·시설 이용 동작은 포함하지 않습니다. 모든 배치물은 Hierarchy의 Peace Park 아래에서 개별 편집할 수 있습니다.

`Peace Park > Build Windows Preview`로 `Builds/Windows/TenMinutesOfPeace.exe`를 만들 수 있습니다. 실행 파일과 함께 생성된 데이터 폴더도 필요합니다.

## 코드

- `Assets/Scripts/ParkBuilder.cs`: 공원 배치와 기본 모델 생성
- `Assets/Scripts/ParkCamera.cs`: 회전·이동·줌
- `Assets/Editor/ParkProjectSetup.cs`: 최초 장면 생성과 Windows 빌드 메뉴

## 검증 상태

작성 환경에서 Unity Editor 설치를 확인하지 못하여 Unity 컴파일, 실제 화면, 실행 파일 빌드는 아직 검증하지 못했습니다. 최초 실행 시 Console 오류 및 카메라 조작, 재실행 후 머티리얼 유지 여부를 확인해야 합니다.

## Reference image revision

Open `Assets/Scenes/ParkReference.unity` and press Play. The original `Park.unity` is preserved. Added faceted tree crowns, flared trunks, arched doors/windows, roof decorations and a continuous U-shaped path. See `Previews/park-reference.png` for an actual Unity render. Compilation, scene generation and rendering passed in Unity 6000.6.8f1 in a separate validation project. Interactive mouse controls were not tested in this pass.

## Polished 4K revision

Open `Assets/Scenes/ParkPolished.unity`, then Play. Actual render: `Previews/park-polished-4k.png` (3840 x 2160). Improved faceted crowns, rounded hard edges, painted surface textures, timber grain, larger flower decorations and softer high-resolution shadows. Runtime uses 8x MSAA and high-quality shadows; performance on tablets remains untested.

In Unity's Game tab, choose/add a Fixed Resolution of 2560 x 1440 or 3840 x 2160, and inspect at 1x scale. Free Aspect follows the panel size; a small Game panel is not a 4K view. Windows player default size is 2560 x 1440. Existing scenes remain unchanged.

Validation: Unity 6000.6.8f1 batch compilation, scene generation and 4K rendering succeeded. Interactive mouse controls and device performance were not measured.

## Drawing page

Open `Assets/Scenes/Drawing.unity` and press Play, or use `Peace Park > Open Drawing Page`. Use a landscape Game view (16:9 or 4:3). Choose one of five animals, one of ten colors and one of four brush sizes, then drag inside either outline. Paint is clipped to the silhouette; facial features remain on a separate overlay. Switching animals clears the previous drawing. Apply locks for three seconds and leaves the current drawing in place. There is no undo, eraser, zoom or saved-work gallery.

This version is local drawing only. `DrawingPage.Applied` provides the animal index and front/back PNG byte arrays for future networking integration; no host connection or animal spawn is implemented. Templates are procedural placeholders and are not yet UV-matched to final 3D animals.

Validation: Unity compilation, ten silhouette masks, stroke clipping, repeated Apply rejection, and selection reset passed. An actual Play-mode screenshot is in `Previews/drawing-page.png`. Touch input on physical tablets has not been verified.

## Drawing visual revision

The Drawing scene now uses generated toy-style animal head art, shaded rounded panels and glossy palette/brush/apply buttons. Templates use 960 x 1200 paint surfaces, smoother contours, smiling expressions and a separate facial-feature overlay. Open the same Drawing scene; rebuilding is unnecessary. Artwork is stored in `Assets/Resources/Drawing/AnimalHeads.png`. Built-in image generation was used only for the head atlas; controls, shadows and coloring behavior remain live Unity code. Prompt: `Docs/drawing-art-prompt.txt`.

### Final icon framing

Each animal head now uses its own measured atlas bounds and original aspect ratio instead of equal-width slices. Alpha-bound checks confirm that all five include transparent margins without clipped ears/horns. The earlier polished UI was rendered and coloring tests passed. The final framing-only render attempt was blocked by Unity Editor license error 198 (`No valid Unity Editor license found`). Reactivate the Editor license in Unity Hub, open Drawing, and press Play to inspect the latest version. `Previews/drawing-page-polished.png` currently shows the preceding visual revision.

## Upright animal templates

The five coloring animals now share chubby humanoid torsos, lowered arms with separate hands, separated legs and small standing feet. Animal faces and ears remain species-specific. Front/back silhouettes match. See `Docs/character-proportions.md` for the future 3D rig direction. This change updates the 2D coloring templates; 3D models and walking animations are not yet created.

The upright revision was compiled and rendered successfully in Unity 6000.6.8f1. Validation checked all ten masks, paint clipping, distinct left/right legs, arm-to-torso clearance, apply lock and reset on animal selection. Actual Play-mode preview: `Previews/drawing-page-upright.png`. Unity licensing was available again for this validation.

## Soft mascot body revision

The body silhouette now follows the user's reference: short rounded arms, a broad uninterrupted torso, two thick rounded legs and no narrow wrists, ankles or waist. All five species share this body; animal heads remain distinct. Actual verified Unity screenshot: `Previews/drawing-page-soft-body.png`. Compilation and ten-template paint/leg checks passed.

## Distinct animal heads

Five species-specific heads replace the shared generic face. The accepted body and limb outline is unchanged. Open the same Drawing scene and select each animal. `Previews/animal-species-templates.png` compares the five real generated template textures, in bear/tiger/rabbit/frog/giraffe order.

## Big-head character proportion

The five templates now use a more animated character proportion: heads are 116% of the previous size, while the soft body is narrower and shorter. Feet remain on the same baseline, and rabbit ears and giraffe horns fit inside the canvas. Verified Play-mode screenshot: `Previews/drawing-page-big-head.png`.

The giraffe front muzzle was subsequently reduced and repositioned inside the lower face. `Previews/animal-species-templates.png` contains the latest verified five-animal comparison.

## Rabbit 2D-to-3D proof of concept

Open `Assets/Scenes/Rabbit3DPoc.unity` and press Play, or use `Peace Park > Open Rabbit 3D POC`. This scene locks the drawing page to the rabbit. Draw on the front and back templates and press Apply; the page changes to a 3D preview where the painted rabbit walks in, pauses, waves once and then strolls. `다시 색칠하기` returns to a clean rabbit canvas.

The rabbit now uses a continuous closed skin connecting its head, ears, torso, arms, legs, feet and tail. `RabbitMesh` extracts and welds that surface, adds UVs and blends skin weights across the shoulders, neck and hips. One mesh and one `SkinnedMeshRenderer` contain the skin and facial material regions. Facial details are separate surface islands inside the same mesh. Front/back painting is attached to rest-pose UVs, so marks move with the skin during animation. This is still a procedural prototype; shoulder deformation and final character sculpting can be refined further. Mobile performance has not been measured.

Validated in Unity 6000.6.8f1: compilation, paint shader, Play-mode Apply, one connected closed skin with 22,214 vertices / 44,424 triangles, normalized skin weights, one renderer and eleven baked animation poses passed. Actual renders: `Previews/rabbit-3d-poc.png`, `Previews/rabbit-connected-wave.png`, `Previews/rabbit-connected-back.png`, `Previews/rabbit-walk-front.png` and `Previews/rabbit-walk-three-quarter.png`. The validation Editor also reports an unrelated Unity Search startup exception; model checks and captures completed successfully.

### Shoulder and pelvis refinement

The arms are now bound in an A-pose and lowered by the idle animation. Shoulder support bones distribute rotation between the attachment and the upper arm; arm weights exclude the torso flanks. This reduces the stretched membrane under a raised arm. UVs are mapped from the lowered drawing pose. The pelvis centre remains on the body bone, with leg weights fading smoothly toward the inner thighs. A rounded U-shaped crotch opening and wider thigh spacing allow opposite leg swings without a sharp central pinch. Walking uses a gentler 15-degree swing. Validation includes both maximum-stride directions and multiple points during the wave, and checks that the pelvis centre stays within 0.06 units of its rest position (measured maximum 0.0408, including body bob and sway).

### Visible short neck

The head, ears and facial details are raised together by 0.23 units, down from 0.30 and originally 0.48. The neck keeps its 0.31-unit radius for a short, chubby silhouette. The arm roots and shoulder support pivots are lowered by 0.12 units; broad shallow shoulder surfaces connect the neck to the arms along a downward slope instead of a raised cap. Skin weights and paint UVs follow the revised pivots. Validation measures the central neck join (width 0.816 units at the revised sample height) and checks eleven animation poses. The latest front render is saved as `Previews/rabbit-soft-shoulders.png`.

### Clavicle-driven wave

The shoulders now pivot near the chest rather than sharing the upper-arm sockets. Each upper-arm joint is offset along its clavicle. During the wave, the clavicle begins rising slightly before the arm, rotates upward and translates gently; the upper arm supplies the remaining rotation. The entire shoulder slope blends onto the clavicle, while the central neck and chest remain stable. Idle and walking clavicles stay neutral, preserving the relaxed downward shoulder silhouette. The paint UVs follow this revised lowered pose.

Validation compares baked idle and wave surfaces: average right shoulder rise is 0.108 units and arm-socket rise is 0.156 units. It also checks that repeating a pose does not accumulate shoulder translation. All eleven pose checks and closed-surface checks passed. Latest wave render: `Previews/rabbit-clavicle-wave.png`. No external character model has been imported.

### Shoulder crease correction and side painting

Shoulder weights are smoothed across the connected surface. At character creation, `RabbitShoulderCorrective` generates volume-preserving pose corrections for relaxed shoulders, the walking cycle and the wave. The raised junction is sculpted as a smooth union of the chest, moving shoulder and arm, then faired before baking the corrective blend shapes. Playback uses Unity's skinned blend shapes. This adds creation work and mesh memory; tablet performance remains unmeasured. The latest pose validation checks 12,892 shoulder faces across eleven poses without inversion or collapse. Average shoulder rise is now 0.127 units, with arm-socket rise unchanged at 0.156.

`RabbitPaintTexture` expands each silhouette's boundary colours into transparent pixels before assigning the front/back textures. Side UVs therefore receive the nearest silhouette colours instead of sampling empty canvas. Front and back colours blend around the sides; intentional white inside the animal and the original drawing are preserved. This remains a planar projection prototype, rather than an exact final UV-matched animal template.

`RabbitPaintValidation` now uses overlapping 64-pixel-radius strokes, matching the largest brush in DrawingPage, followed by the normal canvas PNG export. It checks every output texel and mesh UV for coverage, and checks that a deliberately white interior pixel survives. Actual two-colour renders: `Previews/rabbit-painted-side-right.png`, `Previews/rabbit-painted-side-left.png`, `Previews/rabbit-painted-front.png` and `Previews/rabbit-painted-back.png`.

### Actual-brush white-halo regression and narrower legs

The previous test recoloured every alpha >= 128 pixel directly. The real clipped brush leaves a few half-covered white outline pixels untouched; these incorrectly became expansion seeds and propagated white into side UVs. The old decoder failed when replayed using actual strokes. `RabbitPaintTexture` now seeds only fully opaque pixels, so the outside antialias fringe cannot create a halo. White pixels inside the drawing remain valid and are preserved.

`RabbitPocSceneSetup.CaptureBrushRegression` paints front and back yellow with the UI brush radius, calls `DrawingPage.ApplyDrawing`, and verifies the resulting material through `DrawingPage.Applied` and `RabbitCharacter`. The yellow front, back and both sides have full skin coverage. Front preview: `Previews/rabbit-yellow-fixed-front.png`; side previews: `Previews/rabbit-yellow-side-right.png` and `Previews/rabbit-yellow-side-left.png`.

Hip and foot centres are closer together, and the rounded crotch opening is reduced from about 0.36 to 0.171 units. The narrower silhouette passed the eleven-pose and shoulder-face checks; measured pelvis-centre drift is 0.0407 units. The test explicitly measures the narrow inner-leg gap to prevent the previous wide opening returning.

### Legs longer than arms

Hip joints are raised from 1.02 to 1.43 units, and the crotch opening now reaches about 1.235 units. The torso is shortened from below, with its upper shoulder and head landmarks retained, so the character gains visible leg length without growing taller. The narrow 0.171-unit leg gap is preserved. Leg skin weights, tail placement and lower-body paint coordinates follow the new proportions.

Validation measures the actual idle surface: visible leg length is 1.234 units, versus 1.057 from the arm socket to the hand tip. It asserts that the visible leg is at least 10% longer than the arm. Eleven poses, 13,222 shoulder-face checks and the real-brush side-paint regression pass; pelvis-centre drift is 0.0377 units. Latest front and walking renders: `Previews/rabbit-longer-legs-front.png` and `Previews/rabbit-longer-legs-walk.png`.

### Legs shortened by 10% (current)

The equal-limb revision measured a 1.059-unit visible leg against a 1.057-unit arm. The current revision scales lower-body vertex heights by 0.9, with a smooth transition back to the unchanged upper torso. A horizontal mask excludes the hands in their bind A-pose. Normals use the deformation Jacobian, UVs remain attached to their original surface locations, and hip joints follow the shortened legs at 1.134 units.

Measured visible leg length is now 0.953 units (90% of the preceding revision), with arm length unchanged at 1.057. The inner-leg gap remains about 0.17 units (sampled 0.174). Eleven poses, 13,222 shoulder-face checks and the actual-brush paint regressions passed; pelvis-centre drift is 0.0393 units. Latest front render: `Previews/rabbit-legs-minus-ten-front.png`.
