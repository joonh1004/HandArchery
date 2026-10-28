# HandArchery

Windows 웹캠의 양손 입력으로 활을 조준하고 당긴 뒤 손가락을 풀어 발사하는 동아리용 3D 양궁 체험 프로젝트입니다.

현재는 **Task 1: Unity Universal 3D 프로젝트 생성 및 Editor 기준선 확인 단계**입니다. 게임 기능·손 인식·Windows 실행본·실제 카메라 검증은 후속 작업입니다.

## 개발 환경
- Unity Editor: **6000.6.7f1** (Unity 6.6)
- 템플릿: 설치된 `com.unity.template.urp-blank-17.2.6.tgz`
- 패키지 버전은 `Packages/manifest.json`과 `Packages/packages-lock.json`을 기준으로 유지합니다.
- 원본 프로젝트: `D:/OneDrive/바탕 화면/준혁/semicolon/HandArchery`

## 프로젝트 열기
1. Unity Hub에서 **Add / Add project from disk**를 선택합니다.
2. 위 `HandArchery` 폴더를 선택합니다. `Assets` 하위 폴더를 선택하지 않습니다.
3. Editor 버전 **6000.6.7f1**로 엽니다. 첫 실행의 패키지 복원/import가 끝날 때까지 기다립니다.
4. 현재 단계에서는 기본 템플릿만 준비하며 실제 손 입력 게임은 아직 구현하지 않았습니다.

Hub 등록과 GUI Play 확인은 별도 검사입니다. 이 안내가 해당 검사의 완료를 뜻하지는 않습니다.

## 작업·검증 기록
- 협업 지침: [AGENTS.md](AGENTS.md), [CLAUDE.md](CLAUDE.md)
- 승인 기획/단계 보고: `D:/OneDrive/바탕 화면/준혁/AI agent/04_기록/설계`
- 실제 검사 로그: `D:/OneDrive/바탕 화면/준혁/AI agent/04_기록/검증/양궁`

`Assets`, `Packages`, `ProjectSettings`와 `.meta`를 함께 보존합니다. `Library`/`Temp` 등 캐시는 Git 관리 대상이 아닙니다. 원격 업로드는 별도 승인 후 진행하며 저장소는 Public 상태를 유지합니다.

## Task 1 확인 결과 (2026-10-28)

원래 프로젝트 경로에서 Unity 6000.6.7f1의 batch import와 스크립트 컴파일이 종료코드 0으로 완료됐습니다. 생성 결과 패키지는 URP 17.6.0, Input System 1.20.1, uGUI 2.6.0입니다. Input System은 설치 템플릿 원문 1.20.0과 달리 공식 Editor 생성 과정에서 1.20.1이 기록됐으며 수동 하향하지 않았습니다.

D3D12 진단 인터페이스와 URP Terrain shader 경고는 로그에 남아 있습니다. GUI 화면/Play, Windows 빌드, 실제 웹캠 입력은 아직 검증하지 않았습니다. 자세한 근거는 기록 폴더의 `양궁-task-1-report.md`를 참조합니다.
