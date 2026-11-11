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
