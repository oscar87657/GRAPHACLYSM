using Graphaclysm.Runtime.Presentation;
using UnityEngine;

namespace Graphaclysm.Runtime.Bootstrap
{
    /// <summary>
    /// Composition root for the current vertical slice.
    /// This is the only place that decides which concrete presentation is created.
    /// </summary>
    public static class GraphaclysmBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateApplication()
        {
            if (Object.FindFirstObjectByType<GraphaclysmModernView>() != null || Object.FindFirstObjectByType<GraphaclysmPrototypeView>() != null)
            {
                return;
            }

            GameObject application = new GameObject("GRAPHACLYSM Application");
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool("GRAPHACLYSM.CombatV2Smoke", false)
                || UnityEditor.SessionState.GetBool("GRAPHACLYSM.Profile.Pending", false))
            { application.AddComponent<GraphaclysmPrototypeView>(); return; }
#endif
            application.AddComponent<GraphaclysmModernView>();
        }
    }
}
