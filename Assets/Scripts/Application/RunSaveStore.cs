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

        private static bool TryRead(string file, out RunGameSession run, out CharacterDefinition character)
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
                    if (reader.ReadInt32() != Magic || reader.ReadInt32() != RunSaveData.FormatVersion || reader.ReadInt32() != RunSaveData.RulesVersion) return false;
                    uint seed = reader.ReadUInt32(); string character = reader.ReadString(); long ticks = reader.ReadInt64();
                    int count = reader.ReadInt32();
                    if (seed == 0 || character.Length > 64 || ticks < 0 || ticks > DateTime.MaxValue.Ticks || count < 0 || count > RunSaveData.MaximumCommands || stream.Length - stream.Position != count * 5L) return false;
                    var commands = new RunCommand[count];
                    for (int i = 0; i < count; i++)
                    {
                        byte kind = reader.ReadByte(); int argument = reader.ReadInt32();
                        if (kind > (byte)RunCommandKind.SkipRefinement) return false;
                        commands[i] = new RunCommand((RunCommandKind)kind, argument);
                    }
                    data = new RunSaveData { Seed = seed, CharacterId = character, SavedUtcTicks = ticks, Commands = commands };
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
