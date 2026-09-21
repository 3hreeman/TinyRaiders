# GameObject / ScriptableObject migration verification

검증일: 2026-09-21. Unity 6000.5.4f1, Windows Editor. 별도 플레이어 빌드는 생성하지 않았다.

## 자동 테스트

`SurvivalLegend.EditModeTests`: **61/61 통과**, 실패/건너뜀 0. 원본 전투 회귀 테스트 46개와 스냅샷·에셋 통합 테스트 15개를 포함한다. 원시 결과: `gameobject-editmode-tests.json`.

- 에셋의 MonoScript 타입/파일 연결과 DB 참조 검증.
- 스킬 등록·수정·제거, 중복 ID, 연결이 끊긴 스킬 검출.
- 생성된 데이터의 디스크 저장과 재로드, 독립된 실행 스냅샷.
- 새 캐릭터/몬스터 ID, 스폰 구성, 맵 경계·마커, 궁극기 메타데이터.
- 시뮬레이션 RNG와 분리된 FX 식별자, 투영 좌표 왕복 변환.
- 실제 UI 프리팹 연결, 8방향 스프라이트, 공격 예고선/버프 표현 참조.

초기 생성 중 발견한 정의 에셋 dirty/save 누락을 수정했다. 처음 만든 빈 에셋만 GUID를 보존해 복구했으며, 재검증 시 저장된 ID·스탯·스킬 연결을 확인했다. 기존 사용자 데이터는 자동 시드로 덮어쓰지 않는다.

## 에디터 플레이 검증

새 씬에서 실제 Canvas 버튼 이벤트와 게임 세션을 연결해 확인했다. 전투 수치를 편집해 화면을 구성하거나 단계를 빠르게 전환한 검증용 실행이며 장시간 자연 플레이 테스트는 아니다. 검증용 HP/무적/경험치/페이즈 변경은 런타임 상태에만 적용했고 씬·데이터에 저장하지 않았다.

- 타이틀·선택 화면과 세 캐릭터의 시작 스탯/스킬/HUD 표시.
- 검사 E 이동·영역 공격, 마법사 Q 투사체·기본 연쇄 공격·R 버프.
- 플레이어·몬스터가 실제 ActorView로 생성됨. 기존 IMGUI 표현이 자동으로 중복 추가되지 않음.
- HUD/전장 좌표의 UI 레이캐스트 구분, 일시정지·계속하기 버튼.
- 레벨업에서 증강 화면으로 전환, 다시 뽑기 및 선택, 결과 화면과 기록 표시.
- 재도전에서 캐릭터 선택 유지, Generation 증가, 적·투사체·영역·기록 초기화.
- 정예 보상 카드에서 다시 뽑기 버튼 숨김.
- 마법사 버프 오브젝트 3개와 적 공격 예고선 연결.
- 1920×1080 및 720×1280 화면에서 Canvas 기준 프레임과 카메라 뷰포트의 경계 일치. 화면 변경 후 Canvas 레이아웃이 갱신된 다음 검사했다.

스크린샷: `gameobject-title.png`, `gameobject-loadout.png`, `gameobject-archer.png`, `gameobject-swordsman.png`, `gameobject-mage.png`, `gameobject-augment.png`, `gameobject-results.png`, `gameobject-buffs-windup.png`.

## 적용 상태와 범위

기본 씬: `Assets/SurvivalLegend/Scenes/SurvivalLegendGameObjects.unity`. 기존 씬은 빌드 목록에서 비활성화했다. 기본 데이터는 캐릭터 3, 몬스터 4, 스킬 18, 특수능력 4, 일반 증강 13, 특수 증강 22개다.

전투 판정은 기존 결정적 시뮬레이션이 유지한다. GameObject/Prefab은 표현과 제작 구조이며 Rigidbody 기반 전투로 변경하지 않았다. 맵 문양·장식은 개별 오브젝트이고 바닥 체크 타일은 베이크 텍스처다. PC/WebGL의 실제 플랫폼 빌드·성능 검증은 이번 요청에 따라 수행하지 않았다.
