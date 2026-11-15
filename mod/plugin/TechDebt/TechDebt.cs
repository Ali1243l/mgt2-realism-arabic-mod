// GDD Point 7: Programming Languages, Engine Technical Debt (Spaghetti Code) & Core Refactoring.
// Built for MGT2Mod namespace, netstandard2.1, 32-bit Mono x86 safe.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MGT2Mod
{
    public enum ProgLanguageParadigm
    {
        ClassicAsmC = 0,          // +20% engine perf, -22% dev speed, +30% bugs, 1.65x porting friction
        ModernManagedCppCSharp = 1, // +10% dev speed, -15% bugs, 0.85x porting friction
        AiNeuralCompiler = 2      // +42% dev speed, 0.65x porting friction, requires high server/tooling budget
    }

    [Serializable]
    public class EngineTechState
    {
        public int engineId;
        public string engineName = "";
        public ProgLanguageParadigm language = ProgLanguageParadigm.ModernManagedCppCSharp;
        public float techDebtPct = 0f; // 0 .. 100% (Spaghetti Code)
        public int gamesBuiltSinceRefactor = 0;
        public int totalRefactors = 0;
    }

    [Serializable]
    public class TechDebtSaveContainer
    {
        public List<EngineTechState> engines = new List<EngineTechState>();
        public List<int> processedGameIds = new List<int>();
    }

    [BepInPlugin("ali.mgt2.techdebt", "Programming Languages & Engine Tech Debt", "1.0.0")]
    public class TechDebtPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            new Harmony("ali.mgt2.techdebt").PatchAll(typeof(TechDebtPlugin).Assembly);
            Log.LogInfo("Programming Languages & Engine Tech Debt loaded (F11)");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F11))
                TechDebtWindow.Toggle();
        }
    }

    public static class TechDebtEngine
    {
        public static readonly Dictionary<int, EngineTechState> States = new Dictionary<int, EngineTechState>();
        public static readonly HashSet<int> ProcessedGames = new HashSet<int>();

        public static EngineTechState GetOrCreate(engineScript es)
        {
            if (es == null) return new EngineTechState();
            if (!States.TryGetValue(es.myID, out var st))
            {
                st = new EngineTechState
                {
                    engineId = es.myID,
                    engineName = es.GetName(),
                    language = ProgLanguageParadigm.ModernManagedCppCSharp,
                    techDebtPct = 0f
                };
                States[es.myID] = st;
            }
            st.engineName = es.GetName();
            return st;
        }

        /// <summary>
        /// Effective development speed multiplier in [0.45, 1.45].
        /// At 100% Spaghetti Code Tech Debt, developer productivity drops by 40%.
        /// </summary>
        public static float CalculateDevSpeedMultiplier(ProgLanguageParadigm lang, float techDebtPct)
        {
            float langSpeed = lang == ProgLanguageParadigm.ClassicAsmC ? 0.78f
                : lang == ProgLanguageParadigm.ModernManagedCppCSharp ? 1.10f : 1.42f;

            float clampedDebt = Mathf.Clamp(techDebtPct, 0f, 100f);
            float debtPenalty = 1f - 0.40f * Mathf.Pow(clampedDebt / 100f, 1.20f);
            return Mathf.Clamp(langSpeed * debtPenalty, 0.45f, 1.45f);
        }

        /// <summary>
        /// Quadratic Bug Generation Multiplier during game development.
        /// </summary>
        public static float CalculateBugMultiplier(ProgLanguageParadigm lang, float techDebtPct)
        {
            float langBug = lang == ProgLanguageParadigm.ClassicAsmC ? 1.30f
                : lang == ProgLanguageParadigm.ModernManagedCppCSharp ? 0.85f : 0.95f;

            float d = Mathf.Clamp(techDebtPct, 0f, 100f);
            float debtAmplifier = 1f + 0.014f * d + 0.00022f * d * d;
            return Mathf.Clamp(langBug * debtAmplifier, 0.70f, 4.20f);
        }

        /// <summary>
        /// Cross-platform Porting Friction Multiplier (1.65x for proprietary/low-level vs 0.65x for AI unified).
        /// </summary>
        public static float GetPortingFrictionMultiplier(ProgLanguageParadigm lang)
        {
            return lang == ProgLanguageParadigm.ClassicAsmC ? 1.65f
                : lang == ProgLanguageParadigm.ModernManagedCppCSharp ? 0.85f : 0.65f;
        }

        /// <summary>
        /// Accumulates Technical Debt when a new game is built on an existing engine without refactoring.
        /// </summary>
        public static float AccumulateEngineFatigue(EngineTechState st)
        {
            if (st == null) return 0f;
            st.gamesBuiltSinceRefactor++;
            float langDebtFactor = st.language == ProgLanguageParadigm.ClassicAsmC ? 1.35f
                : st.language == ProgLanguageParadigm.ModernManagedCppCSharp ? 0.90f : 1.05f;

            float deltaDebt = 12.5f * langDebtFactor * (1f + 0.08f * st.gamesBuiltSinceRefactor);
            st.techDebtPct = Mathf.Clamp(st.techDebtPct + deltaDebt, 0f, 100f);
            return st.techDebtPct;
        }

        public static long GetRefactorCost(EngineTechState st)
        {
            if (st == null) return 150000L;
            return 120000L + (long)(st.techDebtPct * 6500f) + st.totalRefactors * 85000L;
        }

        public static bool RefactorEngineCore(mainScript ms, EngineTechState st)
        {
            if (ms == null || st == null || st.techDebtPct <= 0.5f) return false;
            long cost = GetRefactorCost(st);
            if (ms.money < cost) return false;

            ms.Pay(cost, 1);
            st.techDebtPct = 0f;
            st.gamesBuiltSinceRefactor = 0;
            st.totalRefactors++;
            return true;
        }

        public static void OnGameTick(gameScript g)
        {
            if (g == null || !g.IsMyGame() || g.engineID <= 0) return;

            var allEngines = UnityEngine.Object.FindObjectsOfType<engineScript>();
            var es = allEngines.FirstOrDefault(e => e != null && e.myID == g.engineID);
            if (es == null) return;

            var st = GetOrCreate(es);

            // When a new game starts or completes on this engine, register its tech debt impact once
            if (!ProcessedGames.Contains(g.myID))
            {
                ProcessedGames.Add(g.myID);
                AccumulateEngineFatigue(st);

                // Apply bug multiplier from tech debt & language paradigm
                float bugMult = CalculateBugMultiplier(st.language, st.techDebtPct);
                if (bugMult > 1.05f)
                {
                    g.points_bugs = Mathf.Clamp(g.points_bugs * bugMult, 0f, 999f);
                }

                // Classic Assembly/C grants a raw technical performance boost
                if (st.language == ProgLanguageParadigm.ClassicAsmC)
                {
                    g.points_technik = g.points_technik * 1.15f;
                }
            }
        }
    }

    [HarmonyPatch(typeof(gameScript), nameof(gameScript.SellGame))]
    internal static class TechDebtGameHookPatch
    {
        private static void Postfix(gameScript __instance)
        {
            try { TechDebtEngine.OnGameTick(__instance); }
            catch (Exception e) { TechDebtPlugin.Log.LogError(e); }
        }
    }

    [HarmonyPatch(typeof(savegameScript), nameof(savegameScript.Save))]
    internal static class TechDebtSavePatch
    {
        private static void Postfix(int i)
        {
            try
            {
                var container = new TechDebtSaveContainer
                {
                    engines = new List<EngineTechState>(TechDebtEngine.States.Values),
                    processedGameIds = new List<int>(TechDebtEngine.ProcessedGames)
                };
                string dir = Path.Combine(Application.persistentDataPath, "MGT2TechDebt");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, $"slot_{i}.json"), JsonUtility.ToJson(container, true));
            }
            catch (Exception e) { TechDebtPlugin.Log.LogError(e); }
        }
    }

    [HarmonyPatch(typeof(savegameScript), nameof(savegameScript.Load))]
    internal static class TechDebtLoadPatch
    {
        private static void Postfix(int i)
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, "MGT2TechDebt", $"slot_{i}.json");
                if (!File.Exists(path)) return;
                var container = JsonUtility.FromJson<TechDebtSaveContainer>(File.ReadAllText(path));
                TechDebtEngine.States.Clear();
                TechDebtEngine.ProcessedGames.Clear();
                if (container?.engines != null)
                {
                    foreach (var s in container.engines)
                        TechDebtEngine.States[s.engineId] = s;
                }
                if (container?.processedGameIds != null)
                {
                    foreach (var id in container.processedGameIds)
                        TechDebtEngine.ProcessedGames.Add(id);
                }
            }
            catch (Exception e) { TechDebtPlugin.Log.LogError(e); }
        }
    }
}
