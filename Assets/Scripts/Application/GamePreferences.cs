using System;
using System.IO;

namespace Graphaclysm.Application
{
    public sealed class GamePreferences
    {
        public int MasterVolume = 80;
        public int EffectsVolume = 70;
        public bool Fullscreen = true;
        public const int NativeResolution = 3;
        public int Resolution = NativeResolution;
        public bool ReduceMotion;
        public bool PauseOnFocusLoss = true;
        public bool TutorialCompleted;

        public void Normalize()
        {
            MasterVolume = Math.Max(0, Math.Min(100, MasterVolume));
            EffectsVolume = Math.Max(0, Math.Min(100, EffectsVolume));
            Resolution = Math.Max(0, Math.Min(NativeResolution, Resolution));
        }
    }

    public sealed class GamePreferencesStore
    {
        private readonly string path;
        public GamePreferencesStore(string directory) { path = Path.Combine(directory, "settings.save"); }
        public GamePreferences Load()
        {
            if (TryRead(path, out var preferences) || TryRead(path + ".bak", out preferences)) return preferences;
            return new GamePreferences();
        }
        private static bool TryRead(string file, out GamePreferences preferences)
        {
            preferences = null;
            try
            {
                if (!File.Exists(file) || new FileInfo(file).Length != 25) return false;
                using (var reader = new BinaryReader(File.OpenRead(file)))
                {
                    if (reader.ReadInt32() != 0x47525046 || reader.ReadInt32() != 1) return false;
                    preferences = new GamePreferences { MasterVolume = reader.ReadInt32(), EffectsVolume = reader.ReadInt32(), Resolution = reader.ReadInt32(),
                        Fullscreen = reader.ReadBoolean(), ReduceMotion = reader.ReadBoolean(), PauseOnFocusLoss = reader.ReadBoolean(), TutorialCompleted = reader.ReadBoolean() };
                    if (reader.ReadByte() != 0x7E) return false;
                    preferences.Normalize(); return true;
                }
            }
            catch (Exception ex) when (RunSaveStore.IsFileError(ex)) { return false; }
        }
        public bool TrySave(GamePreferences preferences)
        {
            preferences.Normalize();
            try
            {
                using (var stream = new MemoryStream())
                {
                    using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                    {
                        writer.Write(0x47525046); writer.Write(1); writer.Write(preferences.MasterVolume); writer.Write(preferences.EffectsVolume); writer.Write(preferences.Resolution);
                        writer.Write(preferences.Fullscreen); writer.Write(preferences.ReduceMotion); writer.Write(preferences.PauseOnFocusLoss); writer.Write(preferences.TutorialCompleted); writer.Write((byte)0x7E);
                    }
                    RunSaveStore.AtomicWrite(path, stream.ToArray()); return true;
                }
            }
            catch (Exception ex) when (RunSaveStore.IsFileError(ex)) { return false; }
        }
    }
}
