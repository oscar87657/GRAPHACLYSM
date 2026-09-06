# GRAPHACLYSM 성능 검증

> 2026-09-06 전투 V2 이후 참고: 아래 수치는 이전 순차 공개·기존 카드 UI의 GL 비교 기준값이다. 현재는 중앙 방사 클리핑과 작도 중 예측선/발광선 두 패스를 사용한다. 새 전투 전체 성능을 다시 측정한 수치로 해석하지 않는다.


## 2026-09-06: 폴리노미오그래프 선 배치 출력

5·8·12근의 선 출력 CPU 비용을 약 97% 줄였다. Core의 선분을 그대로 사용하며, 미리보기와 1.15초 순차 작도 및 적 표시 순서를 유지한다.

`PolynomiographLineRenderer`가 화면 두께를 가진 선분 사각형을 한 번의 `GL.Begin/End`로 제출한다. Repaint에서만 실행하고 Material 하나를 View 생명주기 동안 재사용한다. 기존 IMGUI 경로는 Editor의 A/B 측정용으로 남겨 두었으며 Player에서는 제외된다.

## 측정 조건과 결과

- Unity 6000.3.23f1, Windows, NVIDIA GeForce RTX 4070 Laptop GPU, Direct3D 12
- `Assets/Scenes/SampleScene.unity`의 실제 Editor Play Mode Game 뷰, 1920×1080
- 동일 캐릭터·조우·수식, 각 경우 45프레임 준비 후 120프레임 수집
- `GRAPHACLYSM.Polynomiograph.Draw` ProfilerMarker를 ProfilerRecorder로 수집
- 미리보기 상태 측정. 작도 애니메이션은 별도 화면·상태 검증
- `targetFrameRate=60`. Main Thread 시간에는 프레임 제한 대기가 포함되므로 순수 CPU 작업 시간으로 해석하지 않는다.

대표 측정 원본: [20260906-015720/profile.csv](Logs/PolynomiographProfile/20260906-015720/profile.csv)

| 수식 | 선분 | IMGUI 출력 평균 | GL 출력 평균 | GL 출력 P95 | Main Thread 평균: IMGUI → GL |
|---|---:|---:|---:|---:|---:|
| 5근 | 4,479 | 28.2607 ms | 0.8058 ms | 0.8975 ms | 34.5314 → 16.6571 ms |
| 8근 | 4,838 | 32.1484 ms | 0.8620 ms | 0.9621 ms | 38.5590 → 16.6527 ms |
| 12근 | 4,990 | 34.0632 ms | 0.8818 ms | 0.9630 ms | 40.5879 → 16.6563 ms |

앞선 동일 해상도 측정에서는 IMGUI 30.5~47.9 ms, GL 0.80~1.10 ms였다. 실행 시점에 따른 호스트 성능 변동이 있으므로 절대 수치는 보장값이 아니다. 세 실행 모두 큰 출력 비용 감소와 약 60fps의 Game 뷰 프레임 간격을 확인했다.

배치 출력 구현 전에 남겨진 401×664 측정은 16:9 조건을 만족하지 않아 위 비교에 사용하지 않았다.

## 메모리와 측정 한계

- 준비 구간 이후 출력 호출을 감싼 `GC.GetAllocatedBytesForCurrentThread` 측정은 두 경로 모두 최대 0 B였다. 이는 출력 호출의 정상 상태 측정이며, 최초 수렴 필드 생성과 다른 UI까지 무할당이라는 뜻은 아니다.
- Profiler의 `GC Allocated In Frame`은 대표 측정에서 IMGUI 약 15 KB, GL 약 82~86 KB였다. 전체 프레임 값이 증가했으므로 출력 구간 수치와 함께 기록한다.
- 별도 [Editor 포함 Profiler 캡처](Logs/PolynomiographProfile/20260906-015720/gc-allocation-parents.txt)의 10프레임에서 Editor GameView의 `IMGUIContainer / OnGUI`에 평균 약 65.6 KB, 기존 게임 `GraphaclysmPrototypeView.OnGUI`에 약 6.1 KB가 기록됐다. 그 밖의 Editor UI 처리도 있다. GL 출력 마커 내부의 할당은 없었다. 전체 프레임 값에는 이 Editor 처리가 섞여 있으므로 Player의 할당량은 별도로 측정해야 한다.
- GPU 시간, 최종 Player 성능, 다른 그래픽 API·해상도·DPI, 8,192개 선분을 모두 사용하는 인공 최대 부하는 아직 측정하지 않았다.
- GL 제출은 한 `Begin/End` 구간으로 묶었지만 드라이버 내부 draw call 수는 별도로 계측하지 않았다.

## 시각·상태 검증

대표 실행의 캡처를 직접 확인했다.

- [5근 미리보기](Logs/PolynomiographProfile/20260906-015720/GL-PolynomiographPentacle.png): 실제 선 형태와 적중 표시
- [12근 미리보기](Logs/PolynomiographProfile/20260906-015720/GL-PolynomiographDodecagram.png): 기존 IMGUI 출력과 좌표·선 형태·겹침 순서 비교
- [상승·증폭·반전](Logs/PolynomiographProfile/20260906-015720/GL-Transforms.png), [마지막 변환 제거](Logs/PolynomiographProfile/20260906-015720/GL-RemovedTransform.png): 수식에 맞춰 선 갱신
- [작도 중간](Logs/PolynomiographProfile/20260906-015720/GL-PlotHalf.png): 선분 순서대로 밝고 두꺼운 선을 출력
- [다음 턴](Logs/PolynomiographProfile/20260906-015720/GL-NextTurn.png): 피해·적 이동 해결 후 이전 그래프 제거

Unity EditMode 테스트 44개가 모두 통과했다. 최종 결과는 [테스트 XML](Logs/editmode-line-batch-final.xml)에 있다. Core와 Application 소스는 이번 작업에서 변경하지 않았다.

## 재현

Editor에서 Play Mode를 끄고 `GRAPHACLYSM → Diagnostics → Profile Polynomiograph`를 실행한다. 도구는 Full HD 설정을 적용한 뒤 이전 해상도를 복구한다. 타이밍 측정 후 Editor를 포함한 GC 원인 캡처와 작도 검증을 수행하고 이전 Profiler 설정을 복구한다. 캡처와 CSV는 `Logs/PolynomiographProfile/<실행 시각>/`에 저장한다.

PowerShell에서 Editor가 닫힌 상태로 실행할 수도 있다. 렌더링을 측정하므로 `-nographics`를 붙이지 않으며 도구가 종료하므로 `-quit`도 붙이지 않는다.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe' `
  -batchmode -projectPath $PWD.Path `
  -executeMethod Graphaclysm.Editor.PolynomiographProfiling.RunBatch `
  -logFile "$PWD/Logs/polynomiograph-profile.log"
```

Temp 검증 프로젝트가 없을 때의 회귀 검증:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe' `
  -batchmode -projectPath $PWD.Path -runTests -testPlatform EditMode `
  -testResults "$PWD/Logs/editmode-line-batch.xml" `
  -logFile "$PWD/Logs/editmode-line-batch.log"
```

구현 참고: Unity의 [GL API](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GL.html), [픽셀 좌표 행렬](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GL.LoadPixelMatrix.html), [ProfilerRecorder](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Unity.Profiling.ProfilerRecorder.html).
