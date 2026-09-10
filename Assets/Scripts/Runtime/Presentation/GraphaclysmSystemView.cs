using System;
using System.IO;
using Graphaclysm.Application;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private RunSaveStore saves;
        private GamePreferencesStore preferenceStore;
        private GamePreferences preferences;
        private GameAudio sound;
        private RunGameSession savedRun, lastSavedRun, observedRun;
        private CharacterDefinition savedCharacter;
        private int savedRevision = -1;
        private bool persistenceEnabled, paused, settingsOpen, inventoryOpen, inventoryRelics;
        private int inventoryPage, inventorySelection, helpPage;
        private enum Confirmation { None, NewRun, Quit }
        private Confirmation confirmation;
        private string saveNotice = "", savedSummary = "이어갈 기록이 없습니다.", inventoryPageLabel = "", inventoryDetail = "";
        private float viewTime;
        private float nextSaveRetry;
        private RunPhase observedPhase;
        private bool settingsDirty;
        private bool saveFailed;
        private float ViewTime => viewTime;
        private bool ModalOpen => paused || helpOpen || settingsOpen || inventoryOpen || confirmation != Confirmation.None;
        private bool HasContinue => savedRun != null && savedRun.Phase != RunPhase.Completed && savedRun.Phase != RunPhase.Defeated;
        private static readonly string[] ResolutionLabels = { "1280 × 720", "1600 × 900", "1920 × 1080" };
        private static readonly string[] VolumeLabels = BuildVolumeLabels();
        private static readonly string[] HelpTitles = { "1 / 5   파편을 조립하기", "2 / 5   같은 선 위의 적과 자신", "3 / 5   이동과 취소", "4 / 5   방출 · 응축 · 해체", "5 / 5   원정과 자동 저장" };
        private static readonly string[] HelpBodies =
        {
            "아래 손패를 클릭하거나 숫자 1~8을 누르세요. 어느 카드든 첫 파편으로 쓸 수 있습니다.\n\n새 카드는 앞선 궤적 전체를 바꿉니다. 순서가 달라지면 모양도 달라집니다. 필드의 예측선을 보고 조립하세요.\n\n카드에 마우스를 올리면 자세한 효과를 읽을 수 있습니다. 카드 사전에서는 첫 카드의 실제 곡선도 볼 수 있습니다.",
            "그래프가 적에게 닿으면 피해와 카드의 적중 효과를 줍니다. 같은 선이 자신에게 닿으면 보호막과 자기 강화 효과를 받습니다.\n\n한 번 방출할 때 각 대상은 한 번만 판정합니다. 여러 번 교차해도 같은 피해를 반복하지 않습니다.\n\n공명 6이 모이면 궁극기를 준비할 수 있습니다. 공명은 실제 방출에서 소비합니다.",
            "방향키 또는 왼쪽 이동 버튼으로 턴마다 한 번 이동할 수 있습니다. 적이 조준한 붉은 범위를 확인하고 피하세요.\n\nBackspace는 이동 취소, 마우스 오른쪽 버튼은 마지막 파편 취소입니다.\n\n귀환점 카드는 사용한 순간의 내 위치로 그래프 중심을 옮깁니다. 이후 내가 움직여도 이미 정한 중심은 따라오지 않습니다.",
            "Enter / 방출: 조립한 그래프를 발동하고 조립대를 비웁니다. 적 행동 후 기본 2장을 보충합니다.\n\nSpace / 응축: 조립을 유지하며 체력 2, 두 번째는 4를 씁니다. 기본 1장에 추가 최대 1장을 보충하고 적도 행동합니다. 방출/해체 사이 두 번까지이며 확정된 파편은 취소할 수 없습니다.\n\n해체: 조립을 버리고 적 행동 후 손패를 보충합니다. 손패와 조립은 각각 최대 8장입니다.",
            "원정은 3층, 층마다 보스 포함 8개 방입니다. 카드·유물·체력은 다음 층으로 이어집니다.\n\n행동 후 자동 저장합니다. Esc 메뉴에서 저장 후 처음으로 돌아가거나 게임을 종료할 수 있습니다. 다음 실행에서 이어하기를 선택하세요.\n\nEsc: 일시정지 / D: 보유 덱·유물 / F1 또는 ?: 이 안내\n\n새 원정을 시작하면 이어하기 슬롯이 교체됩니다. 패배하거나 완주한 원정은 이어할 수 없습니다."
        };

        private static string[] BuildVolumeLabels()
        { var labels = new string[101]; for (int i = 0; i < labels.Length; i++) labels[i] = i + "%"; return labels; }

        private void InitializeServices()
        {
            viewTime = Time.unscaledTime;
            persistenceEnabled = !UnityEngine.Application.isBatchMode;
            string directory = persistenceEnabled ? UnityEngine.Application.persistentDataPath : Path.GetFullPath("Logs/BatchUserData");
            saves = new RunSaveStore(directory); preferenceStore = new GamePreferencesStore(directory);
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
            RunGameSession current = flow?.CurrentRun;
            if (!persistenceEnabled || current == null) return true;
            if (!force && ReferenceEquals(current, lastSavedRun) && current.Revision == savedRevision) return true;
            if (!force && Time.unscaledTime < nextSaveRetry) return false;
            if (!saves.TrySave(current, out string error))
            { saveNotice = error; saveFailed = true; nextSaveRetry = Time.unscaledTime + 5; return false; }
            lastSavedRun = current; savedRevision = current.Revision;
            savedRun = current; savedCharacter = flow.CurrentCharacter;
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
                if (current.Phase == RunPhase.CardReward || current.Phase == RunPhase.RelicReward || current.Phase == RunPhase.Completed) sound?.Play(GameCue.Reward);
                observedPhase = current.Phase;
            }
            if (current != null && current.Phase == RunPhase.Battle && !preferences.TutorialCompleted && !helpOpen)
            { helpPage = 0; helpOpen = true; }
            SaveCurrent();
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
            if (!SaveCurrent(true)) return;
            castActive = false; paused = false; helpOpen = false; inventoryOpen = false;
            flow.ReturnToMainMenu(); Refresh();
        }

        private void QuitGame()
        {
            if (!SaveCurrent(true) || !SavePreferences()) return;
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
            SaveCurrent(true); SavePreferences();
        }
        private void OnApplicationPause(bool suspended)
        {
            if (!suspended || preferences == null) return;
            if (flow.CurrentRun != null) paused = true;
            SaveCurrent(true); SavePreferences();
        }
        private void OnApplicationQuit() { SaveCurrent(true); SavePreferences(); }

        private void OpenSettings() { settingsOpen = true; }
        private bool SavePreferences()
        {
            if (!persistenceEnabled || preferences == null || !settingsDirty) return true;
            if (!preferenceStore.TrySave(preferences)) { saveNotice = "설정을 저장하지 못했습니다. 다시 시도하세요."; return false; }
            settingsDirty = false; return true;
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
            if (confirmation != Confirmation.None) DrawConfirmation();
            else if (settingsOpen) DrawSettings();
            else if (helpOpen) DrawHelp();
            else if (inventoryOpen) DrawInventory();
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
            if (ui.Button(new Rect(480, 578, 938, 64), "저장 후 처음으로")) SaveAndReturn();
            if (ui.Button(new Rect(480, 668, 938, 64), "저장 후 게임 종료")) confirmation = Confirmation.Quit;
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
            Label(new Rect(925, 365, 484, 398), inventoryDetail, ui.Body);
            Label(new Rect(587, 799, 180, 52), inventoryPageLabel, ui.Small, true);
            if (ui.Button(new Rect(480, 799, 90, 52), "이전", enabled: inventoryPage > 0)) { inventoryPage--; SelectInventory(inventoryPage * 10); }
            if (ui.Button(new Rect(787, 799, 90, 52), "다음", enabled: (inventoryPage + 1) * 10 < count)) { inventoryPage++; SelectInventory(inventoryPage * 10); }
            if (ui.Button(new Rect(1127, 799, 280, 52), "닫기   D / Esc", true)) inventoryOpen = false;
        }
    }
}
