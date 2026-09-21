# Gothic HUD verification

2026-09-21 · Unity 6000.5.4f1 Windows Editor. 플랫폼 빌드는 생성하지 않았습니다.

## 적용

- 기본 씬 `SurvivalLegendGameObjects.unity`에 직렬화된 Canvas HUD를 적용했습니다.
- 궁극기 구체(R), 중앙 Q/W/E/D/F, 체력 구체, 회피 배지, 정예 체력 바를 원본 스크린샷 배치에 맞췄습니다.
- 내장 imagegen으로 1254×1254 RGBA 리소스 3개를 생성했습니다. 프레임은 Sprite Border 250px 및 Sliced로 사용하고 투명 구체와 가고일은 별도 Image로 표시합니다.
- 타이틀·선택·증강·결과·일시정지 패널 및 캐릭터/증강 카드에도 같은 프레임 테마를 적용했습니다.

## 검증 결과

- `SurvivalLegend.EditModeTests`: **61/61 통과**, 실패/건너뜀 0. 결과는 `gothic-hud-editmode-tests.json`.
- 실제 Canvas 이벤트 연결 확인: Q 자기 강화 시전, W 위치 조준, R 즉시 시전과 충전량 소모, 일시정지/계속하기, 즉시 시전 체크박스.
- 체력/정예 게이지 비율, 정예 사망 시 바 숨김, 만료된 보호막 제외, QWERDF 내부 바인딩 순서 확인.
- 34개 Sliced Image 참조, 9-slice Sprite Border, 수직 Filled 구체 확인.
- EventSystem 레이캐스트에서 스킬 버튼 입력과 전장 중앙의 입력 통과 확인. 장식은 Raycast Target을 끕니다.
- 타이틀, 캐릭터 선택, 실제 전투 Game View를 육안 확인했습니다. 1920×1080 및 1366×768에서 HUD가 화면 안에 유지되고 현재/최대 체력과 스킬 이름이 표시됩니다.

플레이 검증은 런타임 상태를 구성한 짧은 에디터 스모크 검사입니다. 스크린샷의 94 HP, LV.10, 적 배치 등은 화면 비교를 위한 실행 중 값이며 게임 데이터나 씬에 저장하지 않았습니다. 브라우저/배포 빌드와 장시간 성능 검증은 수행하지 않았습니다.

스크린샷: `gothic-hud-combat.png`, `gothic-hud-1366.png`, `gothic-hud-title.png`, `gothic-hud-loadout.png`.

제작 및 편집 안내와 프롬프트: `Docs/Porting/GothicHud.md`, `Assets/SurvivalLegend/Resources/Art/UI/Gothic/ArtDirection.md`.
