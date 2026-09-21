# Gothic HUD

사용자 제공 첫 번째 스크린샷의 배치와 두 번째 스크린샷의 금속 조각 장식을 참고한 Canvas UI입니다. 전투 시야는 유지하고 하단에 궁극기, 5개 스킬, 체력을 배치합니다.

## 생성 리소스

내장 `imagegen` 도구로 생성한 PNG 원본을 프로젝트에 저장했습니다. 이미지에 텍스트를 구워 넣지 않고 Unity Text로 표시합니다.

- `Assets/SurvivalLegend/Resources/Art/UI/Gothic/PanelFrame.png`: 금속 프레임. Sprite Border와 Image Sliced로 모서리를 보존합니다.
- `Assets/SurvivalLegend/Resources/Art/UI/Gothic/gargoyle-guardian-left.png`: 투명 가고일 장식. 오른쪽 장식은 같은 Sprite를 좌우 반전합니다.
- `Assets/SurvivalLegend/Resources/Art/UI/Gothic/ivory-glass-orb.png`: 투명 구체. 체력과 궁극기 색상 및 채움량은 UI에서 제어합니다.

구체와 가고일은 형태를 보존해야 하므로 Simple/Filled로 표시하고 직사각형 패널과 버튼에 9-slice를 적용합니다. 가고일·구체 생성 프롬프트는 같은 리소스 폴더의 `ArtDirection.md`에 있습니다.

### PanelFrame 생성 프롬프트

> Use case: stylized-concept. Asset type: production Unity UI nine-slice sprite, ONE square framed dark panel, 1024x1024. Front-facing perfectly axis-aligned square. Thin aged brass double bevel around almost-black blue-green flat opaque center. Elegant dark fantasy engraved gunmetal and muted antique gold, restrained detailed corner flourishes completely confined to the outer 90 pixels of each corner. All four edge middle sections are perfectly straight uniform strips suitable for stretching; center is empty near-solid dark teal (#0d171b) with virtually no visible texture. Frame outer bounds at 16 pixels from canvas edges; frame maximum thickness 40 pixels except corner motifs within 90px. True transparent background only outside the framed square. Panel interior MUST be opaque dark. Clean sharp sprite, subtle metal highlights, no perspective, no drop shadow outside, no text, no letters, no numbers, no icons, no extra objects, no sample UI, no grid or guide lines. This is a reusable resizable game HUD panel frame, not a full interface.

## 편집

기본 씬 `Assets/SurvivalLegend/Scenes/SurvivalLegendGameObjects.unity`의 Canvas HUD 오브젝트에서 위치, 크기, 색상, Sprite 참조를 조정합니다. 텍스트와 게이지는 런타임 상태에 연결되어 있으므로 스킬/캐릭터 수치 자체는 기존 ScriptableObject 데이터에서 수정합니다.

`Survival Legend > UI > Apply Gothic HUD to Current Scene` 메뉴로 현재 씬에 기본 디자인을 다시 적용할 수 있습니다. 이 메뉴는 HUD 하위 오브젝트를 다시 생성하므로 HUD에서 수동으로 조정한 배치는 기본값으로 교체됩니다. 월드와 메뉴의 이벤트 연결은 유지합니다.

프레임 Sprite Border는 네 방향 모두 250px입니다. 1254×1254 원본의 장식 모서리를 보존하며, 패널 Image의 Pixels Per Unit Multiplier 9, 작은 버튼 18을 기본으로 사용합니다. 구체는 Vertical Filled이며 배경 구체와 겹쳐 잔량을 표시합니다. 가고일과 텍스트는 Raycast Target을 끄고 버튼·체력 구체 영역만 입력을 받습니다.

`SurvivorCanvasUI.Slots`는 화면 배치와 별개로 Q/W/E/R/D/F 순서를 유지합니다. 중앙에 Q/W/E/D/F를 표시하고 R은 왼쪽 구체에 연결합니다. 체력 현재값과 최대값은 별도 Text 오브젝트이므로 글꼴의 줄 높이와 관계없이 각각 편집할 수 있습니다.

플랫폼 빌드는 생성하지 않습니다.
