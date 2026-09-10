# 기본 기능 v9 · 2026-09-07

## 플레이

- 메인: 이어하기, 새 기록 시작, 카드 사전, 작도 안내, 설정, 게임 종료.
- Esc 또는 오른쪽 위 메뉴: 일시정지, 계속하기, 설정, 보유 덱·유물, 안내, 저장 후 처음으로, 저장 후 종료.
- D: 보유 덱·유물. 10개씩 페이지를 넘기고 이름을 선택해 정확한 효과를 읽는다. 덱의 중복 카드는 각각 표시한다.
- F1 또는 ?: 5단계 조작 안내. 첫 전투에서 자동으로 열리고 닫은 기록은 설정에 저장한다. 실제 카드를 조작하는 별도 연습 전투는 아직 없다.
- 설정: 전체 음량·효과음 음량, 창/전체 화면, 720p/900p/1080p, 연출 줄이기, 창 전환 시 자동 일시정지. 기본값 복원은 안내를 읽은 기록을 유지한다.
- 기본 효과음 6종은 직접 합성한 짧은 소리다. 메뉴 선택·카드·방출·적중·피해·보상에 사용한다. 배경음악은 아직 없다.

일시정지·안내·설정·덱 목록·확인 창을 열면 작도와 적 행동이 멈춘다. 별도의 화면 시계가 멈추므로 게임 속도 전역값을 바꾸지 않는다. 연출 줄이기는 이동하는 카드의 진입과 작도 공개·파편 효과를 줄이며 판정과 행동 순서는 같다.

## 저장과 이어하기

현재 Windows 저장 위치는 `%USERPROFILE%\AppData\LocalLow\DefaultCompany\GRAPHACLYSM`이다. 실제 경로는 [Unity Application.persistentDataPath](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-persistentDataPath.html)를 따른다. 회사명/제품명 설정을 바꾸면 경로 이전을 함께 구현해야 한다.

- `run.save`: 원정 하나. 정상 실행의 상태 변경 명령 후 자동 저장한다. 메뉴 복귀·종료·창 전환 때도 저장한다.
- `run.save.bak`: 이전 정상 저장. 기본 파일의 무결성·버전·복원 검증이 실패하면 백업을 시도하고 화면에 복구 사실을 알린다. 백업은 마지막 행동보다 앞선 상태일 수 있다.
- `settings.save`, `.bak`: 음량·화면·연출·창 전환·안내 확인 기록. 적용/닫기, 창 전환/종료 때 기록한다.
- `.tmp`: 같은 폴더의 임시 파일. 디스크에 끝까지 쓴 후 기존 파일을 교체한다. 중간에 중단된 임시 파일은 이어하기에 사용하지 않는다.

경로·층·체력·공명·덱·유물·보상 선택지·방 선택/덱 제거 대기 상태와 전투의 손패/드로우/버림/예약·수식·기준점·이동 취소·응축·궁극기·적 상태를 복구한다. 방출 도중 저장했다면 아직 판정하지 않은 작도를 다시 보여 주거나 이미 판정한 뒤의 적 행동부터 이어간다. 피해와 드로우를 두 번 적용하지 않는다. 연출의 정확한 중간 프레임은 저장하지 않는다.

새 원정을 시작할 때 기존 슬롯 교체를 게임 안에서 확인한다. 여행자 선택을 취소하면 기존 파일을 유지한다. 패배/완주는 종료 상태를 기본 파일과 백업 모두에 기록해 백업으로 이전 전투가 다시 살아나지 않게 한다. 저장 실패는 화면에 표시하며 저장 후 복귀/종료는 실패한 채 진행하지 않는다. OS 강제 종료의 마지막 저장은 보장할 수 없다.

## 개발 계약

`RunSaveData`는 형식 버전 1, 규칙 버전 9다. 초기 시드·캐릭터 ID와 성공한 Application 명령을 순서대로 저장하고, 새 런에 그 명령을 재생해 상태를 복원한다. Unity 오브젝트나 임의 타입 역직렬화는 사용하지 않는다. SHA-256은 파일 손상 검출용이며 부정행위 방지 서명은 아니다.

카드 수치·던전 생성·셔플·보상·전투 규칙을 바꾸면 `RulesVersion`을 올리거나 마이그레이션을 구현해야 한다. 과거 규칙을 현재 규칙으로 조용히 재생하지 않는다. 불러오기는 별도의 후보 런에서 전체 검증이 끝난 뒤 Flow에 연결한다. 저장된 수식 문자열을 실행하지 않는다.

명령은 최대 65,536개, 파일은 최대 1 MiB까지 읽는다. 일반적인 원정 길이보다 넉넉하지만 무한히 취소를 반복하는 런까지 무제한 지원하지 않는다. 제한 초과 시 저장 실패를 알리고 마지막 정상 파일을 보존한다. 재생 시간은 명령 수에 비례하므로 장기적으로 큰 콘텐츠 확장 시 방 단위 체크포인트를 검토한다.

Runtime의 게임 변경은 `RunGameSession` 명령을 통한다. 새 명령을 추가하면 명령 ID·재생·회귀를 함께 추가한다. Core를 직접 조작하는 Editor fixture는 사용자 저장과 섞지 않는다. 일반 batch 진단은 저장을 끄고, v9 fixture만 `Logs/BasicsV9Captures/UserData-*`에 독립 파일을 쓴다.

## 검증 결과

- Unity EditMode **137/137 통과**: `Logs/editmode-basics-v9.xml`. 실제 생성 원정 두 시드에서 보상·방의 선택/취소와 2층 첫 전투까지 복원 비교를 수행했다. 전투 중 이동/카드 취소, 응축과 다음 드로우, 판정 전/후, 파일 손상/쓰기 실패, 종료 원정의 백업, 설정도 확인했다.
- Full HD/720p 화면 fixture **22장, Runtime errors 0**: `Logs/CombatV2VerificationProject/Logs/BasicsV9Captures`, `Logs/basics-v9-smoke-final.log`. View를 폐기하고 다시 만들어 파일에서 복원하고, 작도 중 일시정지/안내, 저장 실패로 메뉴 복귀 거부 후 재시도, 설정 유지, 덱/유물과 기존 슬롯 교체 확인을 검사했다.
- 대표 화면: `Docs/Screenshots/title-v9.png`, `pause-v9.png`, `settings-v9.png`.
- Windows x64 빌드 성공, 오류 0/경고 0: `Logs/Builds/GraphaclysmV9/GRAPHACLYSM.exe`, `Logs/basics-v9-build.log`. 빌드는 `BasicsV9Smoke.BuildWindowsBatch`를 격리 프로젝트에서 실행해 만든 로컬 산출물이다. 배포 빌드에서 직접 화면 설정·음향을 수동 플레이한 결과는 아니다.
- 빌드 후 원본/검증 사본 비교는 `Logs/basics-v9-source-verification.txt`에 있다. 스크립트와 asmdef는 전부 일치한다. Unity가 검증 사본에 기록한 URP 빌드 필터/런타임 목록·볼륨 직렬화, Standalone 배칭, UnityConnect 설정 5개 차이는 원본으로 되가져오지 않았다.

## 검증 실행

원본 Unity가 열려 있으면 `Logs/CombatV2VerificationProject`의 Assets/Packages/ProjectSettings를 최신 원본과 일치시킨 뒤 실행한다. 사용자 Editor를 종료하지 않는다. 두 검증은 Unity 프로세스 종료를 확인하고 순차 실행한다.

```powershell
$sourceRoot = $PWD.Path
$verifyRoot = (Resolve-Path 'Logs/CombatV2VerificationProject').Path
$unityExe = 'C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe'
$testArgs = '-batchmode -nographics -projectPath "' + $verifyRoot + '" -runTests -testPlatform EditMode -testResults "' + $sourceRoot + '\Logs\editmode-basics-v9.xml" -logFile "' + $sourceRoot + '\Logs\editmode-basics-v9.log"'
Start-Process -FilePath $unityExe -ArgumentList $testArgs -WindowStyle Hidden -Wait
$smokeArgs = '-batchmode -projectPath "' + $verifyRoot + '" -executeMethod Graphaclysm.Editor.BasicsV9Smoke.RunBatch -logFile "' + $sourceRoot + '\Logs\basics-v9-smoke.log"'
Start-Process -FilePath $unityExe -ArgumentList $smokeArgs -WindowStyle Hidden -Wait
```

화면 캡처는 실행한 검증 프로젝트의 `Logs/BasicsV9Captures`에 생성된다. 원본 프로젝트에서 실행한 이전 캡처와 구별한다. 화면 fixture는 저장과 메뉴 동작 확인이며 전체 3층의 최종 밸런스나 새 Profiler 검증을 뜻하지 않는다.
