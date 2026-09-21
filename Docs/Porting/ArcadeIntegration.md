# Arcade WebGL 통합

Arcade 루트: `D:\Codex\Arcade`.

- Unity 버전: `/games/survivor-unity/play`
- 기존 웹 버전: `/games/survivor-arena/play`
- 라이브러리의 몰입 카테고리에 `Tiny Raiders · Survival Legend` 카드 추가.
- PC 키보드/마우스용. 기존 웹 버전과 서버 GameId/랭킹 규칙은 유지.
- Unity 실행 화면은 전체 화면, 라이브러리 복귀, 기존 웹 버전 링크, 로딩/오류/재시도를 제공.

## 빌드 갱신

1. Unity에서 Play Mode를 종료하고 `Survival Legend > Build Arcade WebGL` 실행.
2. `Logs/ArcadeWebBuild.txt`의 `Succeeded` 확인. 출력: `Builds/ArcadeWebGL`.
3. 프로젝트 루트에서 `python Tools/Arcade/package_webgl.py` 실행.
4. `Temp/ArcadeIntegration/apps/web/public/unity/survival-legend`를 Arcade의 같은 상대 경로에 복사. 이전 manifest에만 있는 교체된 청크 파일은 제거하여 중복 용량을 방지.
5. Arcade 루트에서 `npm run build --workspace=@ugph/web` 실행.

빌드는 `SurvivalLegendGameObjects` 씬을 사용하며 gzip과 Unity 압축 해제 fallback을 포함합니다. 빌드 후 에디터의 압축 및 화면 크기 설정은 원래 값으로 복원합니다.

자동화에서는 `Temp/ArcadeWebBuild.request` 파일을 생성하면 에디터 업데이트가 요청을 한 번 소비해 빌드를 실행합니다. 긴 동기 RPC 호출의 자동 재시도로 같은 빌드를 반복하지 않도록 하는 요청 큐입니다.

Arcade의 Cloudflare 정적 파일 제한(파일당 25 MiB)에 맞춰 큰 데이터/wasm은 20 MiB 단위로 나눕니다. 브라우저에서 순서대로 가져와 Blob URL로 합치고 Unity 기본 로더가 압축을 해제합니다. 각 조각의 파일명은 전체 데이터 해시를 포함하며 크기 검증으로 누락/잘못된 HTML 응답을 감지합니다. 원본 빌드 파일은 보존합니다. `build-manifest.json`에는 배포 파일의 크기와 SHA-256을 기록합니다.

제한 출처: https://developers.cloudflare.com/workers/platform/limits/

전체 데이터가 브라우저 메모리에 로드되므로 첫 실행은 다운로드 및 WebAssembly 초기화 시간이 필요합니다. iframe 내부 실행 상태는 같은 origin과 해당 frame의 메시지만 받습니다. 별도 서버 결과/랭킹 연동은 포함하지 않습니다.

이번 작업은 로컬 Arcade 프로젝트에 통합하며 원격 서비스 배포나 Git push를 수행하지 않습니다.
