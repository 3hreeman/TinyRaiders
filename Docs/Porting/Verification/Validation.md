# 첫 플레이 버전 검증 기록

검증일: 2026-09-20. Unity 6000.5.4f1. 원본 기준: `D:/Codex/Arcade`, Git `9452bb8`.

## 구현 범위

궁수 1종의 이동, 지정 공격/공격 이동, 회피, Q/W/E/R, D/F 특수능력, 일반/특별 증강, 엘리트, 사망 결과와 재도전을 구현했습니다. 원본 SD 그래픽 방향과 수치를 기준으로 재구성한 첫 플레이 버전이며 전체 게임의 픽셀 단위 동일성을 보장하는 최종 포팅은 아닙니다.

## 검증 결과

- 원본 웹 프로젝트: 26개 테스트 파일, 213개 테스트 통과.
- Unity EditMode: `SimulationParityTests` 21개 통과. 성장/피해/회복/명시적 공격, 스킬/회피, 증강 선택 시점과 보상 순서, 엘리트, 사망, 결정적 시뮬레이션, 활 발사 위치를 확인했습니다.
- Unity PlayMode: 타이틀 및 전투 화면을 직접 확인했습니다. 해당 검증 시 Console Error/Exception은 없었습니다. `unity-title.png`, `unity-combat.png`가 실제 캡처입니다.
- Windows: 빌드 성공. 실행 파일 시작 및 초기화 로그를 확인한 뒤 검증용 프로세스를 종료했습니다. 로그: `Logs/WindowsPlayer.log`.
- WebGL: 빌드 성공, 배치 종료 코드 0. 로그: `Logs/WebGLBuild.log`.
- WebGL 브라우저 직접 검증: 타이틀 → 궁수 선택 → 전투 시작, 우클릭 이동, 공격 이동 및 Q/W 입력 후 처치/쿨다운 변화, 사망 결과, 재도전, Escape 일시정지, 타이틀 복귀를 확인했습니다. 브라우저 입력만으로 엘리트까지 완주한 검증은 하지 않았으며 해당 진행 규칙은 위 시뮬레이션 테스트로 확인했습니다.
- 브라우저 Console: 실행 오류 0건. URP의 `Edge Adaptive Spatial Upsampling` 미지원 경고 1건이 있습니다. 현재 게임은 해당 후처리를 사용하지 않으며 타이틀/전투/결과 렌더링을 확인했습니다.

## 실행 결과물

- PC: `Builds/Windows/SurvivalLegend.exe` (같은 폴더의 데이터 파일 필요)
- Web: `Builds/WebGL/index.html`; 로컬 서버를 통해 실행
- 서버: 프로젝트 루트에서 `node Tools/serve-web.mjs`, 주소 `http://127.0.0.1:8765`
- 조작과 확인 항목: `Docs/Porting/PlaytestGuide.md`

현재 로컬 웹 서버는 플레이 확인을 위해 실행한 상태입니다. 추가 직업과 후속 확장은 사용자 플레이 피드백 및 승인 후 진행합니다.
