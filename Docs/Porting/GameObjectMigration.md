# GameObject 및 데이터 구조 개편

2026-09-21 구현. 기본 씬은 `Assets/SurvivalLegend/Scenes/SurvivalLegendGameObjects.unity`이다. 별도 Windows/WebGL 빌드는 생성하지 않았다.

## 실행과 씬 구성

Unity에서 기본 씬을 열고 Play를 누른다. 타이틀 → 캐릭터/특수능력 선택 → 전투 → 증강/결과 흐름이 Canvas UI로 실행된다.

- `Systems/Game Session`: `SurvivorGame`. 데이터베이스, 맵 설정과 전투 상태를 관리한다.
- `Systems/Player Input Router`: 키보드·마우스 입력. Canvas 위 클릭과 전장 클릭을 구분한다.
- `Main Camera`: 원본 1280×720 투영과 화면 비율을 유지한다.
- `World`: `WorldPresenter`가 시뮬레이션의 ID와 실제 개체를 연결한다. 캐릭터·적·투사체·영역·FX를 풀링하고 재시작 때 초기화한다.
- `World/ArenaMap`: 바닥, 문양, 네 모서리 장식, 경계, 스폰 마커. 문양과 장식은 개별 SpriteRenderer이다. 체크 바닥 타일은 한 장의 텍스처로 유지된다.
- `UI`: 저장된 Canvas 패널과 EventSystem. 캐릭터·증강·기록처럼 개수가 변하는 목록만 템플릿 프리팹으로 생성한다.

전투 판정은 `GameSimulation`이 담당한다. 개체 표현은 실제 GameObject이지만 Transform을 움직이는 것으로 전투 판정 위치를 수정하지 않는다. 외형 크기는 `WorldVisualDatabase`의 displayScale, 스탯은 아래 데이터 에셋에서 편집한다.

## 데이터 편집

기준 에셋: `Assets/Resources/Data/SurvivalLegendGameDatabase.asset`

개별 정의: `Assets/Resources/Data/SurvivalLegend/` 아래 Characters, Skills, Specials, Enemies, NormalAugments, SpecialAugments.

1. **수정**: 정의 에셋을 선택하고 Inspector에서 스탯, 쿨타임, 효과 등을 수정한다. 스폰 단계·일반/정예 스폰 풀·공통 밸런스는 GameDatabase에서 편집한다.
2. **추가**: `Survival Legend > Data > Database Manager`를 열어 기본 DB를 불러오고 Create 또는 Duplicate를 사용한다. ID는 고유하게 유지한다. 새 캐릭터의 스킬 4개, 몬스터의 패턴, 스폰 풀 등 필요한 참조를 연결한다.
3. **삭제**: 관리 창의 Remove로 게임 등록을 해제한다. 사용 중인 참조와 기본 선택값도 수정한 뒤 Validate를 실행한다. 실제 파일 삭제는 Project 창에서 별도로 할 수 있다. 제거한 에셋은 Play 때 자동 재생성하지 않는다.
4. **저장·검증**: Save와 Validate를 사용한다. 잘못된 ID·참조·실행 수치는 시작 전에 차단한다. 전투 중에는 원본 에셋을 변경하지 않고 복사한 설정을 사용하므로 수정값은 다음 전투 시작부터 적용된다.

새 ID의 외형은 `WorldVisualDatabase`에 연결한다. 매핑이 없으면 기본 외형을 사용한다. 새 동작 방식 자체를 추가할 때는 해당 전투 효과 구현도 필요하며, 현재 지원하는 효과의 수치·조합 편집은 데이터만으로 가능하다.

## 에셋과 유지보수

- 월드 프리팹: `Assets/SurvivalLegend/Prefabs/World/`
- UI 프리팹: `Assets/SurvivalLegend/Prefabs/UI/`
- 방향 스프라이트/외형 DB: `Assets/SurvivalLegend/Resources/Art/World/`
- 각 캐릭터는 8방향 × 6프레임으로 구성한다. 몬스터도 8방향 리소스를 사용한다.
- 기존 `SurvivalLegend.unity`, IMGUI 표현, 정적 카탈로그는 비교/이관 이력으로 보존했다. 현재 실행 기준은 새 씬과 ScriptableObject DB이다.
- `Survival Legend > Migration` 메뉴는 명시적인 생성/열기 도구다. Play 또는 에디터 재로드 시 씬을 다시 만들지 않는다.

상세 데이터 규칙은 `GameDatabaseAuthoring.md`, 월드 제작 방법은 `WorldAuthoring.md`, 검증 범위는 `../Verification/GameObjectMigrationValidation.md`를 참고한다.
