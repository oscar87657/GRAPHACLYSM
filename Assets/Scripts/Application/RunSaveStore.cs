using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Graphaclysm.Core.Characters;

namespace Graphaclysm.Application
{
    /// <summary>One local slot with a checksummed envelope and atomic replacement on the same volume.</summary>
    public sealed class RunSaveStore
    {
        private const int Magic = 0x47524150;
        private const int MaximumBytes = 1024 * 1024;
        private readonly string path;
        private bool recoveredBackup;
        public string FilePath => path;
        public bool HasFiles => File.Exists(path) || File.Exists(path + ".bak");

        public RunSaveStore(string directory) { path = Path.Combine(directory, "run.save"); }

        public bool TrySave(RunGameSession run, out string error)
        {
            error = "";
            if (run == null || !run.CanSave) { error = "현재 원정을 저장할 수 없습니다."; return false; }
            try
            {
                byte[] bytes = Encode(run.CaptureSave());
                if (run.Phase == RunPhase.Completed || run.Phase == RunPhase.Defeated)
                {
                    // A damaged terminal save must not recover the previous live battle.
                    AtomicWrite(path + ".bak", bytes, false);
                    AtomicWrite(path, bytes, false);
                }
                else AtomicWrite(path, bytes, !recoveredBackup);
                recoveredBackup = false;
                return true;
            }
            catch (Exception ex) when (IsFileError(ex)) { error = "저장하지 못했습니다. 저장 공간과 폴더 접근 권한을 확인하고 다시 시도하세요."; return false; }
        }

        public bool TryLoad(out RunGameSession run, out CharacterDefinition character, out string message)
        {
            run = null; character = null; message = "";
            if (!HasFiles) return false;
            if (TryRead(path, out run, out character)) { recoveredBackup = false; return true; }
            if (TryRead(path + ".bak", out run, out character))
            {
                recoveredBackup = true;
                message = "이전 자동 저장에서 복구했습니다. 마지막 행동 일부는 다시 진행해야 할 수 있습니다.";
                return true;
            }
            message = "저장 기록을 읽을 수 없습니다. 손상되었거나 현재 게임 버전과 맞지 않습니다. 기존 파일은 보존했습니다.";
            return false;
        }

        private bool TryRead(string file, out RunGameSession run, out CharacterDefinition character)
        {
            run = null; character = null;
            try
            {
                if (!File.Exists(file) || new FileInfo(file).Length > MaximumBytes) return false;
                return TryDecode(File.ReadAllBytes(file), out RunSaveData data) && RunGameSession.TryRestore(data, out run, out character);
            }
            catch (Exception ex) when (IsFileError(ex)) { return false; }
        }

        public static byte[] Encode(RunSaveData data)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                {
                    writer.Write(Magic); writer.Write(RunSaveData.FormatVersion); writer.Write(RunSaveData.RulesVersion);
                    writer.Write(data.Seed); writer.Write(data.CharacterId); writer.Write(data.SavedUtcTicks);
                    writer.Write(data.LegacyBenefits.MaxHealth); writer.Write(data.LegacyBenefits.VictoryHealing);
                    writer.Write(data.LegacyBenefits.StartingResonance); writer.Write(data.LegacyBenefits.StartingExperience);
                    writer.Write(data.LegacyBenefits.StartingShield); writer.Write(data.LegacyBenefits.RewardBonus);
                    writer.Write(data.Commands.Length);
                    for (int i = 0; i < data.Commands.Length; i++) { writer.Write((byte)data.Commands[i].Kind); writer.Write(data.Commands[i].Argument); }
                }
                byte[] payload = stream.ToArray();
                using (var hash = SHA256.Create())
                {
                    byte[] checksum = hash.ComputeHash(payload);
                    stream.Write(checksum, 0, checksum.Length);
                    return stream.ToArray();
                }
            }
        }

        public static bool TryDecode(byte[] bytes, out RunSaveData data)
        {
            data = null;
            if (bytes == null || bytes.Length < 64 || bytes.Length > MaximumBytes) return false;
            try
            {
                int length = bytes.Length - 32;
                using (var hash = SHA256.Create())
                {
                    byte[] checksum = hash.ComputeHash(bytes, 0, length);
                    for (int i = 0; i < checksum.Length; i++) if (bytes[length + i] != checksum[i]) return false;
                }
                using (var stream = new MemoryStream(bytes, 0, length, false))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    if (reader.ReadInt32() != Magic || reader.ReadInt32() != RunSaveData.FormatVersion) return false;
                    int rules = reader.ReadInt32();
                    if (rules < 21 || rules > RunSaveData.RulesVersion) return false;
                    uint seed = reader.ReadUInt32(); string character = reader.ReadString(); long ticks = reader.ReadInt64();
                    int maxHealth = reader.ReadInt32(), victoryHealing = reader.ReadInt32();
                    int startingResonance = reader.ReadInt32(), startingExperience = reader.ReadInt32();
                    int startingShield = reader.ReadInt32(), rewardBonus = reader.ReadInt32();
                    if (maxHealth < 0 || maxHealth > 100 || victoryHealing < 0 || victoryHealing > 20
                        || startingResonance < 0 || startingResonance > 6 || startingExperience < 0 || startingExperience > 20
                        || startingShield < 0 || startingShield > 24 || rewardBonus < 0 || rewardBonus > 20) return false;
                    int count = reader.ReadInt32();
                    if (seed == 0 || character.Length > 64 || ticks < 0 || ticks > DateTime.MaxValue.Ticks || count < 0 || count > RunSaveData.MaximumCommands || stream.Length - stream.Position != count * 5L) return false;
                    var commands = new RunCommand[count];
                    for (int i = 0; i < count; i++)
                    {
                        byte kind = reader.ReadByte(); int argument = reader.ReadInt32();
                        if (kind > (byte)(rules == 21 ? RunCommandKind.RefundGrowth : rules == 22 ? RunCommandKind.DiagramAbility : rules == 23 ? RunCommandKind.LunaPull : rules == 24 ? RunCommandKind.SatelliteOrigin : rules == 25 ? RunCommandKind.OpeningRoute : rules == 26 ? RunCommandKind.UseResearch : rules == 27 ? RunCommandKind.ContentExpansion : rules == 28 ? RunCommandKind.GrandArchive : rules == 29 ? RunCommandKind.MarketBalance : rules == 30 ? RunCommandKind.StatusRules : rules == 31 ? RunCommandKind.TowerArchive : rules == 32 ? RunCommandKind.FiveFloors : rules == 33 ? RunCommandKind.BattleRework : rules == 34 ? RunCommandKind.ApproachTree : rules == 35 ? RunCommandKind.StyleTree : rules == 36 ? RunCommandKind.GrowthLimit : rules<=38?RunCommandKind.LeaveLoot:RunCommandKind.ExpeditionPreparation)) return false;
                        if (rules < 24 && kind == (byte)RunCommandKind.ChooseApproach && argument > 3) return false;
                        if (rules < 38 && kind == (byte)RunCommandKind.ExpeditionSupplies && argument != 0) return false;
                        if (rules < 41 && kind == (byte)RunCommandKind.ExpeditionPreparation && (argument & ~511)!=0) return false;
                        commands[i] = new RunCommand((RunCommandKind)kind, argument);
                    }
                    data = new RunSaveData { Seed = seed, CharacterId = character, SavedUtcTicks = ticks,
                        LegacyBenefits = new LegacyBenefits(maxHealth, victoryHealing, startingResonance,
                            startingExperience, startingShield, rewardBonus), Commands = commands };
                    return true;
                }
            }
            catch (Exception ex) when (IsFileError(ex) || ex is ArgumentException || ex is FormatException) { return false; }
        }

        internal static void AtomicWrite(string file, byte[] bytes, bool backup = true)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            string temporary = file + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            if (File.Exists(file)) File.Replace(temporary, file, backup ? file + ".bak" : null);
            else File.Move(temporary, file);
        }

        internal static bool IsFileError(Exception ex) => ex is IOException || ex is UnauthorizedAccessException || ex is System.Security.SecurityException;
    }
}
