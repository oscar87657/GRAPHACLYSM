using System;
using System.IO;
using Graphaclysm.Application;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Runs;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private RunSaveStore saves;
        private LegacyProgressionStore legacyStore;
        private LegacyProgression legacy;
        private GamePreferencesStore preferenceStore;
        private GamePreferences preferences;
        private GameAudio sound;
        private RunGameSession savedRun, lastSavedRun, observedRun;
        private CharacterDefinition savedCharacter;
        private int savedRevision = -1;
        private bool persistenceEnabled, paused, settingsOpen, inventoryOpen, inventoryRelics, growthOpen, legacyOpen;
        private int inventoryPage, inventorySelection, helpPage, growthFocusedNode = -1;
        private Vector2 growthPan;
        private float growthZoom = .72f;
        private bool growthDragging;
        private float growthDragDistance;
        private GrowthTreeLayout growthLayout;
        private RunGrowthState growthLayoutOwner;
        private bool[] growthAncestors;
        private enum Confirmation { None, NewRun, Quit }
        private Confirmation confirmation;
        private string saveNotice = "", savedSummary = "이어갈 기록이 없습니다.", inventoryPageLabel = "", inventoryDetail = "";
        private float viewTime;
        private float nextSaveRetry;
        private RunPhase observedPhase;
        private bool settingsDirty, legacyDirty;
        private bool saveFailed;
        private float ViewTime => viewTime;
        private bool ModalOpen => nodeComparison != null || paused || helpOpen || settingsOpen || inventoryOpen || growthOpen || legacyOpen || confirmation != Confirmation.None;
        private bool HasContinue => savedRun != null && savedRun.Phase != RunPhase.Completed && savedRun.Phase != RunPhase.Defeated;
        private static readonly string[] ResolutionLabels = { "1280 × 720", "1600 × 900", "1920 × 1080" };
        private static readonly string[] VolumeLabels = BuildVolumeLabels();
        private static readonly string[] HelpTitles = { "1 / 5   파편을 조립하기", "2 / 5   같은 선 위의 적과 자신", "3 / 5   이동과 취소", "4 / 5   방출 · 응축 · 해체", "5 / 5   원정과 자동 저장" };
        private static readonly string[] HelpBodies =
        {
            "아래 손패를 클릭하거나 숫자 1~8을 누르세요. 어느 카드든 첫 파편으로 쓸 수 있습니다.\n\n새 카드는 앞선 궤적 전체를 바꿉니다. 순서가 달라지면 모양도 달라집니다. 필드의 예측선을 보고 조립하세요.\n\n카드에 마우스를 올리면 자세한 효과를 읽을 수 있습니다. 카드 사전에서는 첫 카드의 실제 곡선도 볼 수 있습니다.",
            "그래프가 적에게 닿으면 피해와 카드의 적중 효과를 줍니다. 같은 선이 자신에게 닿으면 보호막과 자기 강화 효과를 받습니다.\n\n한 번 방출할 때 각 대상은 한 번만 판정합니다. 적 2명 이상 적중 시 공명 +1, 자신과 적을 동시에 적중하면 공명 +2를 얻습니다. 빈 방출이나 단일 적중만으로는 차지 않습니다.\n\n공명 6이 모이면 궁극기를 준비할 수 있으며 실제 방출에서 소비합니다.",
            "필드를 클릭해 턴마다 한 번 이동합니다. 원은 사거리, 마름모는 실제 도착점입니다. 적에게 붙거나 겹칠 수 있고 기둥만 이동을 막습니다. 방향키는 빠른 이동입니다.\n\n맥동 균열은 짝수 턴에 자신과 적에게 피해 6. 피하거나 실제 작도로 통과해 이번 맥동을 봉쇄하세요. 응축으로는 봉쇄되지 않습니다. 다른 장치도 마우스를 올리면 설명이 나옵니다.\n\nBackspace는 이동 취소, 우클릭은 마지막 파편 취소입니다. 기술 이동은 취소 불가. 첫 파편의 기준점은 내 위치이며, 이미 만든 식은 이후 이동을 따라오지 않습니다.",
            "Enter / 방출: 조립한 그래프를 발동하고 조립대를 비웁니다. 적 행동 후 기본 2장을 보충합니다.\n\nSpace / 응축: 조립을 유지하며 체력 2, 두 번째는 4를 씁니다. 기본 1장에 추가 최대 1장을 보충하고 적도 행동합니다. 방출/해체 사이 두 번까지이며 확정된 파편은 취소할 수 없습니다.\n\n해체: 조립을 버리고 적 행동 후 손패를 보충합니다. 손패와 조립은 각각 최대 8장입니다.",
            "새 원정은 5층, 층마다 보스 포함 8개 방입니다. 이전 저장은 원래 층수를 유지합니다. 카드·유물·체력은 다음 층으로 이어집니다. 새 원정은 성장점 3점으로 시작하며 유물은 기본 지급하지 않습니다. 비보스 전투 5승마다 +1점(최대 2점), 1~4층 보스마다 +1점으로 최대 9점을 모읍니다. 전리품은 한 줄로 나옵니다. 드래그/휠로 좌우 탐색하며, 받은 카드는 사라집니다. 남은 보상은 두고 갈 수 있습니다. 상자는 열쇠 1개로 확실하게 열거나 성공률 50%로 한 번 시도합니다. G에서 전투방식별 16노드를 살펴보고 기술 각성과 궁극기 각성을 하나씩 선택할 수 있습니다. 기존 저장은 당시 성장 예산과 트리를 유지합니다. 지도와 휴식에서는 전체 재배분할 수 있습니다. 기술 버튼의 안내에 따라 방향이나 위치를 정해 사용하며, 우클릭으로 조준을 취소합니다.\n\n행동 후 자동 저장합니다. Esc 메뉴에서 저장 후 처음으로 돌아가거나 게임을 종료할 수 있습니다. 다음 실행에서 이어하기를 선택하세요.\n\nEsc: 일시정지 / D: 보유 덱·유물 / F1 또는 ?: 이 안내\n\n새 원정은 원정 성장을 초기화합니다. 패배·완주로 얻는 잔광과 메인 화면의 영구 기록은 계속 남습니다."
        };

        private static string[] BuildVolumeLabels()
        { var labels = new string[101]; for (int i = 0; i < labels.Length; i++) labels[i] = i + "%"; return labels; }

        private void InitializeServices()
        {
            viewTime = Time.unscaledTime;
            persistenceEnabled = !UnityEngine.Application.isBatchMode;
            string directory = persistenceEnabled ? UnityEngine.Application.persistentDataPath : Path.GetFullPath("Logs/BatchUserData");
            legacyStore = new LegacyProgressionStore(directory);
            legacy = persistenceEnabled ? legacyStore.Load() : new LegacyProgression();
            if(!legacy.TreeResetApplied)
            {
                int refund=legacy.RefundValue;
                if(legacy.TryResetForTree(legacyStore.TrySave))legacyCartMessage="트리 개편 · 투자 잔광 "+refund+" 전액 반환";
                else legacyCartMessage="초기화 저장에 실패했습니다. 기존 기록은 보존했습니다. 다시 실행해 주세요.";
            }
            flow.LegacyBenefits = legacy.Benefits;
            flow.PreparationUpgrades=legacy.Preparation(selectedSquad);
            saves = new RunSaveStore(directory);
            preferenceStore = new GamePreferencesStore(directory);
            preferences = persistenceEnabled ? preferenceStore.Load() : new GamePreferences { TutorialCompleted = true };
            sound = new GameAudio(gameObject); ApplyPreferences(true);
            if (persistenceEnabled) ReadContinue();
        }

        private void ReadContinue()
        {
            saves.TryLoad(out savedRun, out savedCharacter, out saveNotice);
            UpdateSaveSummary();
        }

        private void UpdateSaveSummary()
        {
            savedSummary = HasContinue ? savedCharacter.DisplayName + " · " + savedRun.CurrentFloor + "층 · " + (savedRun.EncounterNumber == 0 ? "출발 전" : savedRun.EncounterNumber + "번째 방")
                : savedRun != null ? "지난 원정을 마쳤습니다. 새 기록을 시작하세요." : "이어갈 기록이 없습니다.";
        }

        private bool SaveCurrent(bool force = false)
        {
            RunGameSession current = flow?.TrialReturnRun ?? flow?.CurrentRun;
            if (!persistenceEnabled || current == null || current.IsPractice) return true;
            if (!force && ReferenceEquals(current, lastSavedRun) && current.Revision == savedRevision) return true;
            if (!force && Time.unscaledTime < nextSaveRetry) return false;
            if (!saves.TrySave(current, out string error))
            { saveNotice = error; saveFailed = true; nextSaveRetry = Time.unscaledTime + 5; return false; }
            lastSavedRun = current; savedRevision = current.Revision;
            savedRun = current; savedCharacter = flow.TrialReturnCharacter ?? flow.CurrentCharacter;
            saveFailed = false; nextSaveRetry = 0; saveNotice = "자동 저장 완료"; UpdateSaveSummary(); return true;
        }

        private void RefreshSystemState()
        {
            var current = flow.CurrentRun;
            if (!ReferenceEquals(current, observedRun))
            {
                observedRun = current;
                if (current != null) observedPhase = current.Phase;
            }
            else if (current != null && current.Phase != observedPhase)
            {
                if (current.Phase == RunPhase.CardReward || current.Phase == RunPhase.RelicReward || current.Phase == RunPhase.Loot || current.Phase == RunPhase.Completed) sound?.Play(GameCue.Reward);
                observedPhase = current.Phase;
            }
            if (current != null && !current.IsPractice && current.Phase == RunPhase.Battle && !preferences.TutorialCompleted && !helpOpen)
            { helpPage = 0; helpOpen = true; }
            if (current != null && !current.IsPractice && (current.Phase == RunPhase.Completed || current.Phase == RunPhase.Defeated))
            {
                int earned = legacy.AwardRun(current.Seed, current.Phase == RunPhase.Completed, current.CurrentFloor);
                if (earned > 0)
                {
                    legacyDirty = true;
                    saveNotice = "원정의 잔광 +" + earned + " · 메인 화면의 영구 기록에서 사용할 수 있습니다.";
                }
            }
            SaveCurrent();
            SaveLegacy();
        }

        private void ContinueSavedRun()
        {
            // Re-read to avoid adopting an in-memory object whose save failed.
            ReadContinue();
            if (!HasContinue || !flow.TryContinueRun(savedRun, savedCharacter)) return;
            castActive = false; helpOpen = false; paused = false; confirmation = Confirmation.None;
            // Keep recovery information visible until the next successful action.
            lastSavedRun = savedRun; savedRevision = savedRun.Revision;
            Refresh();
            if (run.Phase != RunPhase.Battle) return;
            if (battle.Phase == BattlePhase.Plotting || battle.Phase == BattlePhase.EnemyTurn)
            {
                impactApplied = battle.Phase == BattlePhase.EnemyTurn;
                castActive = true; castStarted = ViewTime - (impactApplied ? ImpactTime : 0);
                lastPlotName = "이어지는 궤적";
                for (int i = 0; i < castHits.Length; i++) { castHits[i] = !impactApplied && damagePreview[i] > 0; hitNumbers[i] = enemyDamage[i]; }
                castSelfHit = selfPreview; castUltimate = battle.Tactics.UltimateArmed;
            }
        }

        private void RequestNewRun()
        {
            if (saves.HasFiles && (HasContinue || savedRun == null)) confirmation = Confirmation.NewRun;
            else { flow.OpenCharacterSelection(); Refresh(); }
        }

        private void SaveAndReturn()
        {
            if(flow.IsNodeTrial) { ReturnNodeTrial(); return; }
            if (!SaveCurrent(true)) return;
            castActive = false; paused = false; helpOpen = false; inventoryOpen = false; growthOpen = false;
            flow.ReturnToMainMenu(); Refresh();
        }

        private void QuitGame()
        {
            if (!SaveCurrent(true) || !SavePreferences() || !SaveLegacy()) return;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused || preferences == null) return;
            if (preferences.PauseOnFocusLoss && flow.CurrentRun != null) paused = true;
            SaveCurrent(true); SavePreferences(); SaveLegacy();
        }
        private void OnApplicationPause(bool suspended)
        {
            if (!suspended || preferences == null) return;
            if (flow.CurrentRun != null) paused = true;
            SaveCurrent(true); SavePreferences(); SaveLegacy();
        }
        private void OnApplicationQuit() { SaveCurrent(true); SavePreferences(); SaveLegacy(); }

        private void OpenSettings() { settingsOpen = true; }
        private bool SavePreferences()
        {
            if (!persistenceEnabled || preferences == null || !settingsDirty) return true;
            if (!preferenceStore.TrySave(preferences)) { saveNotice = "설정을 저장하지 못했습니다. 다시 시도하세요."; return false; }
            settingsDirty = false; return true;
        }

        private void DrawGrowthWorldLine(Vector2 from, Vector2 to, Vector2 viewportSize, Color color, float width)
        {
            Vector2 start = GrowthWorldToCanvas(from, viewportSize);
            Vector2 end = GrowthWorldToCanvas(to, viewportSize);
            if (!ClipGrowthLine(ref start, ref end, viewportSize)) return;
            DrawGrowthCanvasLine(start, end, color, Mathf.Max(run!=null&&run.Growth.IsStyleTree?1.5f:.4f, width * growthZoom));
        }


        private static bool ClipGrowthLine(ref Vector2 start, ref Vector2 end, Vector2 viewportSize)
        {
            const float margin = 2f;
            float minX = margin, minY = margin;
            float maxX = Mathf.Max(margin, viewportSize.x - margin);
            float maxY = Mathf.Max(margin, viewportSize.y - margin);
            float dx = end.x - start.x, dy = end.y - start.y;
            float enter = 0f, exit = 1f;
            if (!ClipGrowthLineEdge(-dx, start.x - minX, ref enter, ref exit)
                || !ClipGrowthLineEdge(dx, maxX - start.x, ref enter, ref exit)
                || !ClipGrowthLineEdge(-dy, start.y - minY, ref enter, ref exit)
                || !ClipGrowthLineEdge(dy, maxY - start.y, ref enter, ref exit)) return false;
            Vector2 original = start;
            start = original + new Vector2(dx, dy) * enter;
            end = original + new Vector2(dx, dy) * exit;
            return true;
        }

        private static bool ClipGrowthLineEdge(float direction, float distance, ref float enter, ref float exit)
        {
            if (Mathf.Abs(direction) < .0001f) return distance >= 0;
            float ratio = distance / direction;
            if (direction < 0)
            {
                if (ratio > exit) return false;
                if (ratio > enter) enter = ratio;
            }
            else
            {
                if (ratio < enter) return false;
                if (ratio < exit) exit = ratio;
            }
            return true;
        }

        private void DrawGrowthCanvasLine(Vector2 start, Vector2 end, Color color, float width)
        {
            if (Event.current.type != EventType.Repaint) return;
            float distance = Vector2.Distance(start, end);
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / (run!=null&&run.Growth.IsStyleTree?1f:3f)));
            float radius = Mathf.Max(.85f, width * .52f);
            for (int step = 0; step <= steps; step++)
                Disc(Vector2.Lerp(start, end, step / (float)steps), radius, color);
        }

        private void DrawGrowthCanvasRing(Vector2 center, float radius, Color color, float width, Vector2 viewportSize)
        {
            int samples = Mathf.Max(14, Mathf.CeilToInt(radius * Mathf.PI * 2f / 6f));
            float dotRadius = Mathf.Max(.55f, width * .52f);
            for (int sample = 0; sample < samples; sample++)
            {
                float angle = sample * Mathf.PI * 2f / samples;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                if (point.x >= 0 && point.y >= 0 && point.x <= viewportSize.x && point.y <= viewportSize.y)
                    Disc(point, dotRadius, color);
            }
        }

        private void DrawGrowthCanvasDiamond(Vector2 center, float radius, Color color, float width, Vector2 viewportSize)
        {
            Vector2 top = center + Vector2.up * radius, right = center + Vector2.right * radius;
            Vector2 bottom = center + Vector2.down * radius, left = center + Vector2.left * radius;
            DrawGrowthClippedCanvasLine(top, right, color, width, viewportSize);
            DrawGrowthClippedCanvasLine(right, bottom, color, width, viewportSize);
            DrawGrowthClippedCanvasLine(bottom, left, color, width, viewportSize);
            DrawGrowthClippedCanvasLine(left, top, color, width, viewportSize);
        }

        private void DrawGrowthClippedCanvasLine(Vector2 start, Vector2 end, Color color, float width, Vector2 viewportSize)
        {
            if (ClipGrowthLine(ref start, ref end, viewportSize)) DrawGrowthCanvasLine(start, end, color, width);
        }

        private Vector2 GrowthWorldToCanvas(Vector2 world, Vector2 viewportSize)
            => (world - growthPan) * growthZoom + viewportSize * .5f;

        private bool SaveLegacy()
        {
            if (!persistenceEnabled || legacy == null || !legacyDirty) return true;
            if (!legacyStore.TrySave(legacy))
            { saveNotice = "영구 기록을 저장하지 못했습니다. 다시 시도하세요."; return false; }
            legacyDirty = false;
            return true;
        }
        private void ApplyPreferences(bool screen)
        {
            preferences.Normalize(); sound?.Apply(preferences.MasterVolume, preferences.EffectsVolume);
            UnityEngine.Application.targetFrameRate = 60;
            if (!screen || UnityEngine.Application.isEditor || UnityEngine.Application.isBatchMode) return;
            int width = preferences.Resolution == 0 ? 1280 : preferences.Resolution == 1 ? 1600 : 1920;
            int height = preferences.Resolution == 0 ? 720 : preferences.Resolution == 1 ? 900 : 1080;
            Screen.SetResolution(width, height, preferences.Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }

        private void PlayClick() { sound?.Play(GameCue.Click); }
        private void OpenHelp() { helpPage = 0; helpOpen = true; }
        private void CloseHelp()
        {
            helpOpen = false; preferences.TutorialCompleted = true; settingsDirty = true; SavePreferences();
        }

        private void DrawSystemOverlay()
        {
            if (nodeComparison != null) DrawNodeComparison();
            else if (confirmation != Confirmation.None) DrawConfirmation();
            else if (settingsOpen) DrawSettings();
            else if (helpOpen) DrawHelp();
            else if (inventoryOpen) DrawInventory();
            else if (growthOpen) DrawGrowthTree();
            else if (legacyOpen) DrawLegacyTree();
            else if (paused) DrawPause();
        }

        private void ModalPanel(string title)
        {
            Fill(new Rect(0, 0, 1920, 1080), new Color(Ink.r, Ink.g, Ink.b, .78f));
            Rect panel = new Rect(420, 150, 1080, 780); Fill(panel, Paper); Border(panel, Gold, 24);
            Label(new Rect(476, 184, 960, 70), title, ui.Heading);
            Line(new Vector2(478, 267), new Vector2(1442, 267), Gold);
        }

        private void DrawPause()
        {
            ModalPanel("잠시 멈춘 기록");
            Label(new Rect(480, 288, 940, 54), "게임이 일시정지되었습니다. 조립과 작도 진행은 그대로 유지됩니다.", ui.Body);
            if (ui.Button(new Rect(480, 376, 455, 64), "계속하기   Esc", true)) paused = false;
            if (ui.Button(new Rect(963, 376, 455, 64), "설정")) OpenSettings();
            if (ui.Button(new Rect(480, 466, 455, 64), "보유 덱 · 유물   D")) OpenInventory();
            if (ui.Button(new Rect(963, 466, 455, 64), "작도 안내   F1")) OpenHelp();
            if (ui.Button(new Rect(480, 556, 938, 58), flow.IsNodeTrial ? "체험 종료 · 원래 성좌로   G" : "이번 원정 성장   G"))
            { if(flow.IsNodeTrial) ReturnNodeTrial(); else { paused = false; growthOpen = true; } }
            if (ui.Button(new Rect(480, 636, 455, 58), flow.IsNodeTrial ? "원래 원정으로 돌아가기" : "저장 후 처음으로")) SaveAndReturn();
            if (ui.Button(new Rect(963, 636, 455, 58), "저장 후 게임 종료")) confirmation = Confirmation.Quit;
            Label(new Rect(480, 767, 938, 105), saveNotice, ui.Small);
        }

        private void DrawSettings()
        {
            ModalPanel("설정");
            Label(new Rect(480, 300, 210, 48), "전체 음량", ui.Body);
            Label(new Rect(1300, 300, 100, 48), VolumeLabels[preferences.MasterVolume], ui.Body, true);
            int master = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(735, 320, 530, 30), preferences.MasterVolume, 0, 100));
            Label(new Rect(480, 378, 210, 48), "효과음", ui.Body);
            Label(new Rect(1300, 378, 100, 48), VolumeLabels[preferences.EffectsVolume], ui.Body, true);
            int effects = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(735, 398, 530, 30), preferences.EffectsVolume, 0, 100));
            if (master != preferences.MasterVolume || effects != preferences.EffectsVolume)
            { preferences.MasterVolume = master; preferences.EffectsVolume = effects; settingsDirty = true; ApplyPreferences(false); }
            if (ui.Button(new Rect(480, 467, 435, 54), preferences.Fullscreen ? "화면 모드: 전체 화면" : "화면 모드: 창"))
            { preferences.Fullscreen = !preferences.Fullscreen; settingsDirty = true; }
            if (ui.Button(new Rect(953, 467, 465, 54), ResolutionLabels[preferences.Resolution]))
            { preferences.Resolution = (preferences.Resolution + 1) % 3; settingsDirty = true; }
            if (ui.Button(new Rect(480, 544, 938, 54), preferences.ReduceMotion ? "연출 줄이기: 켜짐" : "연출 줄이기: 꺼짐"))
            { preferences.ReduceMotion = !preferences.ReduceMotion; settingsDirty = true; }
            if (ui.Button(new Rect(480, 621, 938, 54), preferences.PauseOnFocusLoss ? "다른 창으로 전환하면 일시정지: 켜짐" : "다른 창으로 전환하면 일시정지: 꺼짐"))
            { preferences.PauseOnFocusLoss = !preferences.PauseOnFocusLoss; settingsDirty = true; }
            Label(new Rect(480, 694, 938, 53), UnityEngine.Application.isEditor ? "화면 모드와 해상도는 실행 빌드에서 적용됩니다." : "화면 설정은 적용 버튼을 누르면 바뀝니다.", ui.Small);
            if (ui.Button(new Rect(480, 787, 280, 60), "기본값 복원"))
            { bool learned = preferences.TutorialCompleted; preferences = new GamePreferences { TutorialCompleted = learned }; settingsDirty = true; ApplyPreferences(false); }
            if (ui.Button(new Rect(1038, 787, 380, 60), "적용하고 닫기", true)) CloseSettings();
            Label(new Rect(480, 855, 938, 52), saveNotice, ui.Small);
        }

        private void CloseSettings()
        { ApplyPreferences(true); if (SavePreferences()) settingsOpen = false; }

        private void DrawConfirmation()
        {
            bool start = confirmation == Confirmation.NewRun;
            ModalPanel(start ? "새 기록을 시작할까요?" : "게임을 종료할까요?");
            Label(new Rect(480, 338, 938, 220), start
                ? "이어서 진행할 수 있는 저장 슬롯은 하나입니다. 새 여행자를 골라 원정을 시작하면 기존 기록이 교체됩니다.\n\n여행자 선택에서 돌아오면 기존 기록을 유지합니다."
                : "현재 원정을 저장한 뒤 종료합니다. 다음 실행에서 이어하기로 돌아올 수 있습니다.", ui.Body);
            if (ui.Button(new Rect(480, 685, 420, 70), "돌아가기")) confirmation = Confirmation.None;
            if (ui.Button(new Rect(998, 685, 420, 70), start ? "여행자 선택" : "저장 후 종료", true))
            {
                if (start) { confirmation = Confirmation.None; flow.OpenCharacterSelection(); Refresh(); }
                else QuitGame();
            }
            Label(new Rect(480, 796, 938, 85), saveNotice, ui.Small);
        }

        private void OpenInventory()
        { if (run == null) return; inventoryOpen = true; inventoryRelics = false; inventoryPage = 0; SelectInventory(0); }
        private void SelectInventory(int index)
        {
            int count = inventoryRelics ? run.Relics.Count : run.Deck.Count;
            inventorySelection = Mathf.Clamp(index, 0, Mathf.Max(0, count - 1));
            inventoryPageLabel = (inventoryPage + 1) + " / " + Mathf.Max(1, (count + 9) / 10);
            inventoryDetail = count == 0 ? "아직 얻은 유물이 없습니다." : inventoryRelics ? run.Relics.GetRelic(inventorySelection).Description : Visual(run.Deck[inventorySelection])?.Details ?? "";
        }
        private void DrawInventory()
        {
            ModalPanel("이번 원정의 기록");
            if (ui.Button(new Rect(480, 291, 260, 52), "보유 덱", !inventoryRelics))
            { inventoryRelics = false; inventoryPage = 0; SelectInventory(0); }
            if (ui.Button(new Rect(765, 291, 260, 52), "보유 유물", inventoryRelics))
            { inventoryRelics = true; inventoryPage = 0; SelectInventory(0); }
            int count = inventoryRelics ? run.Relics.Count : run.Deck.Count;
            for (int row = 0; row < 10; row++)
            {
                int index = inventoryPage * 10 + row; if (index >= count) break;
                string name = inventoryRelics ? run.Relics.GetRelic(index).DisplayName : run.Deck[index].DisplayName;
                if (ui.Button(new Rect(480, 365 + row * 41, 397, 36), name, index == inventorySelection)) SelectInventory(index);
            }
            if (inventoryRelics && count > 0)
            {
                DrawRelicArt(new Rect(1015, 348, 300, 250), run.Relics.GetRelic(inventorySelection));
                DrawExplainedText(new Rect(925, 615, 484, 148), inventoryDetail, ui.Body, true);
            }
            else if(count>0) DrawReadableCardDetails(new Rect(925,365,484,398),Visual(run.Deck[inventorySelection]));
            else DrawExplainedText(new Rect(925,365,484,398),inventoryDetail,ui.Body);
            Label(new Rect(587, 799, 180, 52), inventoryPageLabel, ui.Small, true);
            if (ui.Button(new Rect(480, 799, 90, 52), "이전", enabled: inventoryPage > 0)) { inventoryPage--; SelectInventory(inventoryPage * 10); }
            if (ui.Button(new Rect(787, 799, 90, 52), "다음", enabled: (inventoryPage + 1) * 10 < count)) { inventoryPage++; SelectInventory(inventoryPage * 10); }
            if (ui.Button(new Rect(1127, 799, 280, 52), "닫기   D / Esc", true)) inventoryOpen = false;
        }

        private static Color GrowthConnectionColor(RunGrowthState growth, int sourceIndex, int targetIndex)
        {
            bool sourceReady = sourceIndex < 0 || growth.IsUnlocked(sourceIndex);
            if (sourceReady && growth.IsUnlocked(targetIndex)) return new Color(Violet.r, Violet.g, Violet.b, .86f);
            if (sourceReady && growth.CanPurchase(targetIndex)) return new Color(Gold.r, Gold.g, Gold.b, .62f);
            return new Color(Muted.r, Muted.g, Muted.b, .25f);
        }

        private static bool IsGrowthExclusiveLocked(int index, RunGrowthState growth)
        {
            if(growth.IsSpecialized && index>0 && IsGrowthExclusiveLocked(growth.ParentOf(index),growth))return true;
            GrowthNodeDefinition node = growth.GetNode(index);
            if (string.IsNullOrEmpty(node.ExclusiveGroup)) return false;
            for (int i = 0; i < growth.NodeCount; i++)
                if (i != index && growth.IsUnlocked(i) && growth.GetNode(i).ExclusiveGroup == node.ExclusiveGroup) return true;
            return false;
        }


        private static string GrowthNodeStatus(int index, RunGrowthState growth, bool canEdit)
        {
            if (growth.IsEquipped(index)) return "장착됨";
            if (growth.IsUnlocked(index))
                return growth.GetNode(index).EquipKind == GrowthEquipKind.Passive
                    ? growth.GetNode(index).Owner == GrowthOwner.Common ? "활성 · 습득 완료" : "습득 완료"
                    : growth.CanSelect(index) && canEdit ? "클릭하여 장착" : "습득 완료";
            var node = growth.GetNode(index);
            if (growth.SpentPoints < GrowthTreePaths.RequiredInvestment(node.Id))
                return "먼저 총 " + GrowthTreePaths.RequiredInvestment(node.Id) + "점 투자";
            if (!GrowthTreePaths.IsReachable(growth, index)) return "아래 진입 경로를 먼저 습득";
            if (!string.IsNullOrEmpty(node.ExclusiveGroup))
                for (int i = 0; i < growth.NodeCount; i++)
                    if (growth.IsUnlocked(i) && growth.GetNode(i).ExclusiveGroup == node.ExclusiveGroup) return "배타 갈래 선택됨";
            for (int i = 0; i < node.RequiresAll.Length; i++) if (!growth.IsUnlocked(node.RequiresAll[i])) return "선행 노드 필요";
            if (node.RequiresAtLeastCount > 0)
            {
                int count = 0;
                for (int i = 0; i < node.RequiresAtLeast.Length; i++) if (growth.IsUnlocked(node.RequiresAtLeast[i])) count++;
                if (count < node.RequiresAtLeastCount) return "선행 선택 " + count + " / " + node.RequiresAtLeastCount;
            }
            if (growth.Points < node.Cost) return "성장점 부족";
            return canEdit ? "클릭하여 습득" : "지도에서 습득 가능";
        }

        private static string EquippedGrowthName(RunGrowthState growth, GrowthEquipKind kind, int ordinal)
        {
            GrowthNodeDefinition node = growth.GetEquippedNode(kind, ordinal);
            return node == null ? "없음" : node.Name;
        }

        private void DrawLegacyTree() => DrawLegacyArchive();
    }
}
