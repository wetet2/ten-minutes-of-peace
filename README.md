# Ten Minutes of Peace — 공원 둘러보기

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

`Peace Park > Build Windows Preview`로 `Builds/Windows/PeacePark.exe`를 만들 수 있습니다. 실행 파일과 함께 생성된 데이터 폴더도 필요합니다.

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
