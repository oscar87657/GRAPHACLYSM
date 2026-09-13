using System;
using System.IO;
using Graphaclysm.Application;
using UnityEditor;

namespace Graphaclysm.Editor
{
    public static class LegacyTreeReset
    {
        public static void RunExplicitPlayerReset()
        {
            if(!UnityEngine.Application.isBatchMode || Array.IndexOf(Environment.GetCommandLineArgs(),"--reset-player-legacy-tree")<0)
                throw new InvalidOperationException("Explicit player reset flag required.");
            string directory=UnityEngine.Application.persistentDataPath;
            string expected=Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"../LocalLow/DefaultCompany/GRAPHACLYSM"));
            if(!string.Equals(Path.GetFullPath(directory),expected,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Unexpected player data path.");
            var store=new LegacyProgressionStore(directory);var p=store.Load();
            if(p.IsReadOnly)throw new InvalidOperationException("Unreadable account; original preserved.");
            int refund=p.RefundValue;string file=Path.Combine(directory,"legacy.save");
            if(!p.TreeResetApplied)
            {
                string backup=file+".before-tree40";if(!File.Exists(backup))File.Copy(file,backup,false);
                if(!p.TryResetForTree(store.TrySave))throw new IOException("Reset save failed.");
            }
            else refund=0;
            File.WriteAllText("Logs/player-legacy-tree-reset.txt","Refund: "+refund+"\nCurrency: "+p.Currency+"\nApplied: "+p.TreeResetApplied+"\nBackup: "+file+".before-tree40");
            EditorApplication.Exit(0);
        }
    }
}
