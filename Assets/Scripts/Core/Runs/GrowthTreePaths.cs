using System;
using System.Collections.Generic;

namespace Graphaclysm.Core.Runs
{
    /// <summary>Traversal requirements, separate from equipment dependencies. Every visible edge is a real requirement.</summary>
    public static class GrowthTreePaths
    {
        public const string RootId = "common.geometry.01";
        public static readonly string[] Empty = Array.Empty<string>();
        private static readonly Dictionary<string, string[]> routes = BuildRoutes();
        private static readonly Dictionary<string, int> depths = BuildDepths();
        public static int Tier(string id) => ApproachGrowthCatalog.IsId(id)?1:depths[id] <= 2 ? 1 : depths[id] <= 5 ? 2 : depths[id] <= 7 ? 3 : depths[id] == 8 ? 4 : 5;
        public static int RequiredInvestment(string id)
        {
            switch (Tier(id)) { case 2: return 5; case 3: return 10; case 4: return 16; case 5: return 22; default: return 0; }
        }
        private static Dictionary<string,int> BuildDepths()
        {
            var result = new Dictionary<string,int>(StringComparer.Ordinal);
            foreach (var node in GrowthCatalog.All) ComputeDepth(node.Id,result,new HashSet<string>());
            return result;
        }
        private static int ComputeDepth(string id, Dictionary<string,int> result, HashSet<string> active)
        {
            if (result.TryGetValue(id,out int value)) return value;
            if (!active.Add(id)) throw new InvalidOperationException("Growth cycle: " + id);
            GrowthNodeDefinition node = null;
            foreach (var candidate in GrowthCatalog.All) if (candidate.Id == id) { node = candidate; break; }
            if (node == null) throw new InvalidOperationException("Missing growth node: " + id);
            value = MinimumHeight(node);
            foreach (string parent in node.RequiresAll) value = Math.Max(value,ComputeDepth(parent,result,active)+1);
            foreach (string parent in node.RequiresAtLeast) value = Math.Max(value,ComputeDepth(parent,result,active)+1);
            foreach (string parent in Entrances(id)) value = Math.Max(value,ComputeDepth(parent,result,active)+1);
            active.Remove(id); result[id] = value; return value;
        }

        private static Dictionary<string, string[]> BuildRoutes()
        {
            var result = new Dictionary<string, string[]>(StringComparer.Ordinal);
            foreach (var node in GrowthCatalog.All) result.Add(node.Id, CreateEntrances(node.Id));
            return result;
        }

        public static string[] Entrances(string id) => routes.TryGetValue(id, out var result) ? result : Empty;

        // An incoming route is an OR choice. Form/trait ownership requirements from the catalog remain AND requirements.
        private static string[] CreateEntrances(string id)
        {
            switch (id)
            {
                case RootId: return Empty;
                case "common.origin.01": case "common.fragments.01":
                    return new[] { RootId };
                case "common.self.01": return new[] { "common.fragments.01" };
                case "common.geometry.03": return new[] { "common.geometry.02" };
                case "common.condense.01": return new[] { "common.self.01", "common.fragments.01" };
                case "common.resonance.01": return new[] { "common.geometry.02", "common.self.01" };
                case "ian.inscription.core": return new[] { "common.geometry.02" };
                case "ian.archive.core": return new[] { "common.fragments.01", "common.origin.01" };
                case "ian.citadel.core": return new[] { "common.self.01" };
                case "luna.orbit.core": return new[] { "common.origin.01" };
                case "luna.phase.core": return new[] { "common.fragments.01", "common.resonance.01" };
                case "luna.binary.core": return new[] { "common.self.01", "common.origin.01" };
                case "ian.triple.form": return new[] { "common.origin.01" };
                case "ian.execute.form": return new[] { "ian.inscription.core" };
                case "ian.brand.form": return new[] { "common.fragments.01" };
                case "ian.exchange.form": return new[] { "common.origin.02", "ian.archive.core" };
                case "ian.link.form": return new[] { "common.geometry.02", "common.self.02" };
                case "ian.ash.form": return new[] { "common.condense.02", "ian.brand.m3" };
                case "ian.spike.form": return new[] { "common.origin.03", "ian.triple.form" };
                case "ian.shelf.form": return new[] { "common.condense.02", "ian.archive.core" };
                case "ian.wall.form": return new[] { "common.self.02", "ian.citadel.core" };
                case "luna.fulljump.form": return new[] { "luna.orbit.core", "common.origin.03" };
                case "luna.return.form": return new[] { "common.origin.02" };
                case "luna.chain.form": return new[] { "common.origin.03", "luna.fulljump.form" };
                case "luna.rotate.form": return new[] { "common.fragments.02", "luna.orbit.core" };
                case "luna.pull.form": return new[] { "common.geometry.02", "luna.fulljump.m1" };
                case "luna.tide.form": return new[] { "luna.pull.form", "common.origin.05" };
                case "luna.pendulum.form": return new[] { "common.origin.02", "luna.binary.core" };
                case "luna.observatory.form": return new[] { "common.self.03", "luna.binary.core" };
                case "luna.weave.form": return new[] { "common.origin.06", "luna.pendulum.form" };
                case "ian.u.collapse.form": return new[] { "ian.brand.form", "common.geometry.03" };
                case "ian.u.rain.form": return new[] { "common.geometry.06", "ian.ash.m3" };
                case "ian.u.critical.form": return new[] { "ian.execute.m2", "common.resonance.02" };
                case "ian.u.immortal.form": return new[] { "ian.shelf.form", "common.condense.05" };
                case "ian.u.library.form": return new[] { "ian.shelf.awaken", "common.condense.08" };
                case "ian.u.glassbody.form": return new[] { "ian.wall.m3", "common.self.07" };
                case "ian.u.invert.form": return new[] { "ian.exchange.m2", "common.fragments.02" };
                case "ian.u.enemytext.form": return new[] { "common.self.04", "ian.archive.m2" };
                case "ian.u.illusion.form": return new[] { "common.origin.07", "ian.exchange.awaken" };
                case "luna.u.embrace.form": return new[] { "common.self.03", "luna.fulljump.m3" };
                case "luna.u.white.form": return new[] { "luna.orbit.m1", "common.self.05" };
                case "luna.u.twin.form": return new[] { "luna.observatory.m2", "common.self.06" };
                case "luna.u.blade.form": return new[] { "common.geometry.04", "luna.return.m3" };
                case "luna.u.devour.form": return new[] { "common.condense.04", "luna.phase.m3" };
                case "luna.u.meteor.form": return new[] { "common.geometry.06", "luna.tide.m3" };
                case "luna.u.stop.form": return new[] { "luna.pull.m3", "common.origin.05" };
                case "luna.u.align.form": return new[] { "luna.pull.awaken", "common.geometry.07" };
                case "luna.u.timeless.form": return new[] { "common.condense.07", "luna.rotate.awaken" };
                default: return Empty;
            }
        }

        public static bool IsReachable(RunGrowthState growth, int index)
        {
            string[] entrances = Entrances(growth.GetNode(index).Id);
            if (entrances.Length == 0) return true;
            foreach (string id in entrances) if (growth.IsUnlocked(id)) return true;
            return false;
        }

        // Effect magnitude, not skill slot: ordinary adjustments, new actions, linked actions, rule replacement.
        public static int MinimumHeight(GrowthNodeDefinition node)
        {
            string id = node.Id;
            if (id == RootId) return 0;
            if (id == "common.origin.01" || id == "common.self.01" || id == "common.fragments.01") return 1;
            if (id.EndsWith(".awaken", StringComparison.Ordinal)) return 7;
            if (id == "common.geometry.07" || id == "common.geometry.08" || id == "common.origin.07"
                || id == "common.condense.07" || id == "common.condense.08" || id == "common.self.07") return 7;
            if (node.Kind == "공통 핵심") return 6;
            if (id.Contains(".u.timeless.") || id.Contains(".u.illusion.") || id.Contains(".u.library.")) return 8;
            if (id.Contains(".u.align.") || id.Contains(".u.glassbody.")) return 7;
            if (node.EquipKind == GrowthEquipKind.Ultimate) return 4;
            if (node.Kind == "교차 연결") return 5;
            return 1;
        }

        public static string ImpactName(int height)
            => height >= 8 ? "전투 규칙 재편" : height >= 6 ? "판정·행동 확장" : height >= 4 ? "능력 연계" : "행동의 시작";
    }
}
