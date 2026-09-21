# Arcade WebGL 통합 검증 (2026-09-22)

- Unity 6000.5.4f1 WebGL: Succeeded, errors 0, warnings 0.
- 포함 씬: SurvivalLegendGameObjects.
- Arcade `npm run build --workspace=@ugph/web`: TypeScript + Vite 통과. 기존 공통 번들의 500 kB 크기 안내는 남아 있음.
- Unity EditMode: 104/104 통과.
- 모든 배포 파일 HTTP 응답 길이/SHA-256 및 청크 재조립 결과 검증 통과 (`arcade-webgl-assets.txt`).
- 총 배포 데이터 약 58.1 MB. 가장 큰 파일 20 MiB. 데이터 청크 3개를 브라우저에서 합치며 gzip fallback 사용.
- 실제 Arcade 서버(로컬 3005) + 인앱 브라우저에서 라이브러리 카드, 모바일 필터, 기존 웹 버전 타이틀 보존 확인.
- Unity 타이틀 → 캐릭터 선택 → 전투 시작, 우클릭 이동, SPACE 추적, 전체 화면/해제 확인.
- 첫 실행에서 FX velocity curve 모드 오류 발견. SkillFxBuilder와 기존 FX 프리팹 19개를 수정. 활성 velocity 모듈 27개 모두 XYZ 모드 일치 검증 후 재빌드.
- 최종 빌드 재로딩 후 이동/스킬 입력에서 신규 파티클 오류 없음. 이전 빌드의 마지막 오류는 15:20:08 UTC, 최종 빌드 시작 15:22:16 UTC.
- 기존 서버 GameId/랭킹 API는 변경하지 않음. Unity 점수의 Arcade 랭킹 연동은 이번 범위에 포함하지 않음.
- 원격 배포, Git commit/push 미수행.
