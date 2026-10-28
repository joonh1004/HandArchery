# HandArchery

Windows 웹캠의 양손 입력으로 활을 조준하고 당긴 뒤 손가락을 풀어 발사하는 동아리용 3D 양궁 체험 프로젝트입니다.

5발 라운드, 점수, 조준·핀치·당김·놓기, 손 소실 시 취소, 한국어 화면과 Windows 실행본을 구현했습니다. 실제 사람의 양손 발사와 시연 노트북 검증은 아직 남아 있습니다.

## 지금 실행하기
`Builds/Windows/HandArchery.exe`를 실행하고 **체험 시작**을 누르세요. 다른 컴퓨터로 옮길 때는 **Windows 폴더 전체**를 복사합니다. Unity나 Python 설치 없이 실행하는 Windows 빌드입니다.

양손을 펼쳐 준비하고, 왼손으로 조준합니다. 오른 엄지·검지를 붙인 채 오른손을 바깥으로 당겼다가 손가락을 벌리면 발사됩니다. 좌우가 맞지 않으면 **좌우손 교환**, 방향은 **거울**, 왼손잡이는 **활을 잡는 손**으로 조정합니다. 영상은 표시·저장·전송하지 않습니다.

자세한 설정은 [팀원 시작 안내](Docs/팀원시작.md), 발표 준비는 [시연 점검표](Docs/시연점검표.md), 검사 범위는 [검증 기록](Docs/검증기록.md)을 확인하세요.

## 개발 환경
- Unity Editor: **6000.6.7f1** (Unity 6.6)
- 템플릿: 설치된 `com.unity.template.urp-blank-17.2.6.tgz`
- 패키지 버전은 `Packages/manifest.json`과 `Packages/packages-lock.json`을 기준으로 유지합니다.
- 원본 프로젝트: `D:/OneDrive/바탕 화면/준혁/semicolon/HandArchery`

## 프로젝트 열기
명령은 `HandArchery` 루트 폴더에서 PowerShell을 열고 실행합니다.

1. Unity Hub에서 **Add / Add project from disk**를 선택합니다.
2. 위 `HandArchery` 폴더를 선택합니다. `Assets` 하위 폴더를 선택하지 않습니다.
3. 새로 받은 프로젝트는 먼저 PowerShell에서 `./Tools/PrepareMediaPipe.ps1`을 실행해 공식 고정 버전 패키지와 모델을 준비합니다. 약 290MB의 패키지 해시를 확인하며 캐시는 Git에 넣지 않습니다.
4. Editor 버전 **6000.6.7f1**로 엽니다. 패키지 복원/import가 끝나면 `Assets/HandArchery/Scenes/HandArchery.unity`를 열고 Play를 누릅니다. 실행 중 양궁장이 생성됩니다.
5. Windows 빌드는 **HandArchery → Build Windows Archery** 메뉴 또는 Editor를 닫고 `./Tools/BuildWindows.ps1`로 만듭니다. 씬을 재생성하므로 수동 씬 변경은 먼저 보존하세요.

`./Tools/RunArchery.ps1`은 실제 카메라 모드입니다. `-SyntheticCheck`를 붙이면 카메라를 쓰지 않는 별도 자동 검사를 실행하며, 창을 클릭해 활성화해야 합니다. 합성 검사 결과로 실제 손 인식 성공을 판단하지 않습니다.

## 작업·검증 기록
- 협업 지침: [AGENTS.md](AGENTS.md), [CLAUDE.md](CLAUDE.md)
- 승인 기획/단계 보고: `D:/OneDrive/바탕 화면/준혁/AI agent/04_기록/설계`
- 실제 검사 로그: `D:/OneDrive/바탕 화면/준혁/AI agent/04_기록/검증/양궁`

`Assets`, `Packages`, `ProjectSettings`와 `.meta`를 함께 보존합니다. `Library`/`Temp` 등 캐시는 Git 관리 대상이 아닙니다. 원격 업로드는 별도 승인 후 진행하며 저장소는 Public 상태를 유지합니다.

## 확인 결과 (2026-10-28)

원래 프로젝트 경로에서 Unity 6000.6.7f1의 batch import와 스크립트 컴파일이 종료코드 0으로 완료됐습니다. 생성 결과 패키지는 URP 17.6.0, Input System 1.20.1, uGUI 2.6.0입니다. Input System은 설치 템플릿 원문 1.20.0과 달리 공식 Editor 생성 과정에서 1.20.1이 기록됐으며 수동 하향하지 않았습니다.

손 입력 계약 17개, 실제 Unity Core 규칙 89개와 게임 계약 11개가 통과했습니다. Windows 빌드는 errors=0/warnings=0이며 한국어 시작 화면과 양궁장을 실제 실행본에서 확인했습니다. MediaPipe CPU 초기화와 카메라 스트림·재시작은 별도 진단 실행본에서 확인했으며 실제 양손 발사는 미검증입니다.

최종 Windows 합성 검사는 종료 0입니다. 2라운드(50·30점), 실제 화살 충돌 8발·빗나감 2발, 중복 점수 방지·재시작, PlayMode 정리 14개, 자연 6초 timeout 5개가 통과했습니다.

D3D12 진단 인터페이스, 일부 URP 후처리 셰이더, Editor 종료 시 JobTempAlloc 경고는 원시 로그에 남습니다. 빌드 보고서 수치와 런타임/종료 경고를 구분합니다. 메모리 누수 없음이나 모든 기기 호환성을 주장하지 않습니다.
