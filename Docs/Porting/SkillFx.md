# 스킬 FX 제작 구조

레벨업 안내는 전체 화면 전환 패널 대신 캐릭터 주변의 금빛 파티클과 월드 텍스트로 표시합니다. 증강을 고르는 기능은 별개로 유지합니다.

FX는 전투 판정과 분리된 표현 계층입니다. 피해량, 범위, 재사용 시간, 난수와 스폰 규칙은 기존 게임 데이터를 사용하고, 빛의 색상과 크기·파티클 수·수명은 FX 리소스에서 편집합니다.

## 아트 리소스

`Assets/SurvivalLegend/Resources/Art/FX/`에는 부드러운 광원, 가는 불꽃, 초승달 모양의 베기, 소용돌이 텍스처와 공유 머티리얼을 저장합니다. 기본 텍스처는 Unity Editor 제작 도구가 수학적 알파 마스크로 생성합니다. 패키지 다운로드나 외부 이미지 서비스 없이 프로젝트 안에서 생성하고 편집할 수 있습니다.

`Assets/SurvivalLegend/Shaders/FXAdditive.shader`와 `FXAlpha.shader`는 파티클의 버텍스 색상과 알파를 사용하는 Unlit 셰이더입니다. 밝은 빛은 가산 합성, 소용돌이의 어두운 부분은 알파 합성으로 표시합니다.

`Survival Legend > Build FX Textures`는 누락된 리소스만 생성하며 기존에 수정한 텍스처와 머티리얼을 덮어쓰지 않습니다. 자세한 텍스처 규격은 리소스 폴더의 `ArtDirection.md`에 있습니다.

## 스킬 구분

| 계열 | 표현 방향 |
| --- | --- |
| 검사 | 금빛 검기, 회전 베기, 수호 파편, 지면 충격 |
| 궁수 | 녹색 집중, 퍼지는 화살 궤적, 청록 관통 궤적, 낙하 화살 |
| 마법사 | 화염 꼬리와 폭발, 서리 파편, 순간이동 잔광, 번개·과부하 |
| 특수 스킬 | 녹색 회복, 금빛 가속, 청록 보호막, 보라색 흡인 |
| 레벨업 | 캐릭터를 따라가는 금빛 상승 파티클과 LEVEL UP 텍스트 |

플랫폼 빌드는 생성하지 않습니다. 에디터에서 실제 스킬 발동과 화면을 확인하고, 전투 회귀 테스트 및 FX 연결 검증을 수행합니다.

## 프리팹과 데이터 편집

- `Assets/SurvivalLegend/Resources/FX/SkillFxCatalog.asset`: 전체 FX 목록과 동시 활성 개수 상한.
- `Assets/SurvivalLegend/Resources/FX/Profiles/`: 28개 `FxProfile` ScriptableObject. 프리팹, 색상, 크기, 지속 시간, 잔광 시간, 캐릭터 추적 여부를 편집합니다.
- `Assets/SurvivalLegend/Prefabs/FX/`: 28개 FX 프리팹. 자식 ParticleSystem에서 발사량, 속도, 모양, 크기와 색상 곡선을 편집합니다.
- `Survival Legend > FX > Apply Skill FX to Current Scene`: 리소스를 준비하고 현재 씬의 WorldPresenter에 연결합니다.

투사체는 TrailRenderer와 비행 파티클을 사용합니다. 충돌 이후에도 짧은 꼬리가 남으며, 일시정지·증강 선택 중에는 표현 시간이 멈춥니다. 재시작 시 파티클과 궤적을 비웁니다. FX 풀은 기본 활성 128개, 비활성 128개까지 보관하며 한 프로필의 비활성 보관은 48개까지입니다. 표현용 난수는 전투 난수와 분리합니다.
