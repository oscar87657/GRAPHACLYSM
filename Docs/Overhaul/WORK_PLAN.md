# Overhaul 작업 기록

사용자 제공 00~07 문서의 P0 → P1 순서를 따른다. P1 사람 플레이 피드백 전에는 P2를 확대하지 않는다.

| 작업 | 상태 / 현재 위치 |
|---|---|
| P0-01 기준 | 완료. BASELINE_DIFF, 새 기준 194개 테스트 |
| P0-02 입력 재현 | 조사 및 Core 회귀 진행. OS 입력 전 사례는 검증대기 |
| P0-03 단일 예측 | 부분 구현. CombatSkillPreview와 실행 공유, View 착지/피해 표시. stable entity ID 명령은 남음 |
| P0-04 해결 결과 | 미착수. BattleSession ResolvePlot/PreviewDamage/ResolveEnemyTurn, 출처·라운드·파생 제한 필요 |
| P0-05 저장 | 미착수. RunJournal/RunSaveStore 다중 인수와 stable ID. 선행 안전 수정 규칙만 21 |
| P1-01~04 | 미착수. BattleThreats/EnemyState/기하학 판정에 구조 링크·추진·정비·보스 연결 |
| P1-05 | 미착수. BattleSkillLoadout/RunGrowthState에서 실제 네 빌드와 두 슬롯 실행 |
| P1-06~07 | 미착수. 카드 인스턴스/각인 → RunRooms/보상 → 별도 6방 원정 |
| P1-08~09 | 미착수. 실습 안내/화면 회귀/사람 재미 평가 |

## 첫 구현 단위: 기술 안전성과 예측

- `CombatSkillPreview`: 값 타입의 착지/형태/실패 결과, 대상별 기하 접촉과 주 대상 타격/체력/보호막 예측. 난수/상태 변경 없음. 실행은 현재 상태에서 다시 계산한다.
- `BattleSession.TryUseCombatSkill`: 같은 계산을 사용. invalid/dead target과 착지 실패는 피해/쿨타임/이동/마지막 성공 결과를 변경하지 않는다.
- `GraphaclysmSkillPreviewView`: 조준 중에만 표시. Battle/Run.Revision/hover 대상 기준으로 착지 계산과 문자열 캐시. UI 임의 사거리 보정 없음.
- 원정 규칙 21: 잘못된 대상 및 착지 불가 명령 결과가 바뀌므로 구원정을 새 규칙으로 조용히 재생하지 않는다. 저장 형식과 기존 enum ID는 유지. 설정/영구 기록/원본 저장 파일 삭제 없음.
- 이전 240노드 효과, 새 링크, 추진, 6방 원정은 이번 선행 단위의 완료 범위가 아니다.

검증 결과는 실제 실행 후 아래에 기록한다. 수동 OS 입력·전체 원정 재미·Profiler·실행 파일 빌드는 별도다.

## 실행한 검증

- 변경 전: `Logs/overhaul-baseline.xml` **194/194 통과**.
- 변경 후: `Logs/overhaul-p0.xml` **208/208 통과**. 신규 14사례: 8형태의 100회 무변경 예측/실행 피해·착지 일치, invalid/dead 대상 3종, 명시 빠른 선택 -1, 착지 불가, 주 대상/경로 접촉 구분.
- 개발 중 테스트 초기화 오류 2건(내부 상태 접근, 허용 범위 밖 지형 반경)을 테스트 fixture에서 수정 후 재실행했다. 게임 테스트를 삭제하거나 기존 기대값을 일괄 변경하지 않았다.
- `BasicsV9Smoke`는 실제 View의 HandleKeyEvent에 K/우클릭 이벤트를 전달해 조준/취소/저장 무변경을 검사하고 View의 -1 거절 및 표시 피해/예측 일치를 검사한다. 대상 클릭은 View 메서드 fixture이므로 실제 OS 포인터 검증으로 세지 않는다.
- 로컬 Editor 로그 `OverhaulSkill`: 입력 경로·hover·대상 슬롯/콘텐츠 ID·라운드·거리·요청/보정 착지·대상별 기하 접촉·예상 HP/보호막·실제 전후 값을 남긴다. ID는 현재 고정 배열 슬롯이며 신규 stable entity ID 명령 구현을 대신하지 않는다.
- 1080p/720p 예측 화면은 Editor 전용 hover fixture다. 표시 검사와 입력 검사를 구분한다.
- 최종 화면 회귀: `Logs/overhaul-p0-smoke.log`, 격리 사본 `Logs/BasicsV9Captures/smoke-result.txt`: **49장 / 게임 Runtime errors 0**. K 진입/우클릭 취소/예측 피해 일치 검사 통과. `12a-skill-targeting.png`, `21-skill-preview-720p.png`를 직접 열어 착지/원형 영향권/피해 요약이 보이는 것을 확인했다. Unity SearchDatabase 시작 인덱싱 예외는 게임 진입 전 Editor 내부 오류이며 별도 기록한다.

다음 구현 단위는 P0-03의 stable target 계약과 P0-04 해결 결과/출처 추적이다. 기존 기술의 원거리 주 대상 보장·겹친 적 방향 거절을 새 기하학 기술로 교체할 때는 실제 사거리/동작 설계와 회귀를 함께 바꾼다.
