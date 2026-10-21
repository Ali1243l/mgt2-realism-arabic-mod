// Developer tool (not part of the released mod): F10 writes the hierarchy of every active
// in-game menu to BepInEx/uidump.txt, so new UI can be cloned to match the game's layout.
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using UnityEngine;
using UnityEngine.UI;

namespace MGT2Mod
{
    [BepInPlugin("ali.mgt2.uidump", "UI Dump", "1.0.0")]
    public class UiDumpPlugin : BaseUnityPlugin
    {
        // menus to study; dumped even while closed (inactive objects keep their layout)
        private static readonly string[] Targets =
            { "Menu_DevGame", "Menu_Dev_F2PUpdate", "Menu_Dev_F2PUpdateSelectGame", "Menu_Game_Umsatz", "Menu_Dev_MMOAddon" };
        private bool autoDone;

        private void Update()
        {
            var ms = FindObjectOfType<mainScript>();
            if (ms != null && Input.GetKeyDown(KeyCode.F9)) FastForward(ms);
            if (ms != null && Input.GetKeyDown(KeyCode.F8)) // the development wizard for a new game
            {
                var gui = ms.guiMain_;
                gui.ActivateMenu(gui.uiObjects[56]);
                gui.uiObjects[56].GetComponent<Menu_DevGame>().InitNewGame(ms.arrayRoomScripts.First(r => r != null && r.typ == 1), 0);
                gui.OpenMenu(hideChars: false);
            }
            if (ms != null && (Input.GetKeyDown(KeyCode.F11) || Input.GetKeyDown(KeyCode.F12))) OpenLiveMenu(ms, Input.GetKeyDown(KeyCode.F11));
            bool auto = !autoDone && ms != null && ms.officeLoaded;
            if (!auto && !Input.GetKeyDown(KeyCode.F10)) return;
            autoDone = true;
            var sb = new StringBuilder();
            foreach (var name in Targets)
            {
                var menu = Resources.FindObjectsOfTypeAll<MonoBehaviour>()
                    .FirstOrDefault(m => m.GetType().Name == name && m.gameObject.scene.IsValid());
                sb.AppendLine("=== " + name + (menu == null ? " (not found)" : ""));
                if (menu != null) Dump(menu.transform, 0, sb);
            }
            // scene-serialized tuning values the code does not contain
            var games = ms != null ? ms.games_ : null;
            if (games != null)
                sb.AppendLine("=== inApp price=" + string.Join(",", games.inAppPurchasePrice) + " hate=" + string.Join(",", games.inAppPurchaseHate));
            var f2p = Resources.FindObjectsOfTypeAll<Menu_Dev_F2PUpdate>().FirstOrDefault(m => m.gameObject.scene.IsValid());
            if (f2p != null)
            {
                sb.AppendLine("=== f2pUpdate cost%=" + string.Join(",", f2p.devCostsPercent) + " boost=" + string.Join(",", f2p.interestBoost));
                for (int i = 0; i < f2p.goButtons.Length; i++)
                    sb.AppendLine($"  goButton[{i}] {(f2p.goButtons[i] ? string.Join(" | ", f2p.goButtons[i].GetComponentsInChildren<Text>(true).Select(x => x.text)) : "-")}");
            }
            string path = Path.Combine(Paths.BepInExRootPath, "uidump.txt");
            File.WriteAllText(path, sb.ToString());
            Logger.LogInfo("UI dumped to " + path);
        }

        // test shortcut: F11 = F2P update menu, F12 = sales window, for the player's newest F2P game on the market
        private void OpenLiveMenu(mainScript ms, bool update)
        {
            var g = ms.games_.arrayGamesScripts.Where(x => x != null && x.IsMyGame() && x.gameTyp == 2 && x.isOnMarket)
                .OrderByDescending(x => x.myID).FirstOrDefault();
            Logger.LogInfo("live games: " + string.Join(", ", ms.games_.arrayGamesScripts.Where(x => x != null && x.IsMyGame() && x.gameTyp > 0)
                .Select(x => $"{x.myID}:{x.GetNameWithTag()} typ{x.gameTyp} market={x.isOnMarket}")));
            if (g == null) return;
            var gui = ms.guiMain_;
            if (update)
            {
                var room = ms.arrayRoomScripts.FirstOrDefault(r => r != null && r.typ == 1 && r.taskID == -1) ?? ms.arrayRoomScripts.First(r => r != null && r.typ == 1);
                gui.ActivateMenu(gui.uiObjects[300]);
                gui.uiObjects[300].GetComponent<Menu_Dev_F2PUpdate>().Init(room, g);
            }
            else
            {
                gui.ActivateMenu(gui.uiObjects[91]);
                gui.uiObjects[91].GetComponent<Menu_Game_Umsatz>().Init(g);
            }
            gui.OpenMenu(hideChars: false);
        }

        // test shortcut: finish every running F2P update, then run 4 game weeks
        private void FastForward(mainScript ms)
        {
            foreach (var t in FindObjectsOfType<taskF2PUpdate>()) t.Work(t.pointsLeft + 1f);
            var live = ms.games_.arrayGamesScripts.Where(x => x != null && x.IsMyGame() && x.gameTyp > 0 && x.isOnMarket).ToArray();
            for (int w = 0; w < 4; w++) ms.WochenUpdates(); // the game's whole week: servers reset, then sales
            Logger.LogInfo("fast-forward: " + string.Join(", ", live.Select(g => $"{g.GetNameWithTag()} f2p={g.f2pInteresse:0.0} players={g.abonnements}")));
        }

        private static void Dump(Transform t, int depth, StringBuilder sb)
        {
            if (depth > 9) return;
            var rt = t as RectTransform;
            var parts = new StringBuilder();
            parts.Append(new string(' ', depth * 2)).Append(t.gameObject.activeSelf ? "" : "(off) ").Append(t.name);
            if (rt != null)
                parts.Append($"  pos={rt.anchoredPosition} size={rt.rect.size} anch={rt.anchorMin}-{rt.anchorMax} piv={rt.pivot}");
            foreach (var c in t.GetComponents<Component>())
            {
                switch (c)
                {
                    case Text x: parts.Append($" [Text '{Short(x.text)}' f={x.fontSize} {x.alignment} col={x.color}]"); break;
                    case Image x: parts.Append($" [Image {(x.sprite ? x.sprite.name : "-")} col={x.color} {x.type}]"); break;
                    case Button x:
                        parts.Append(" [Button");
                        for (int i = 0; i < x.onClick.GetPersistentEventCount(); i++)
                            parts.Append(" ").Append(x.onClick.GetPersistentTarget(i)?.GetType().Name).Append(".").Append(x.onClick.GetPersistentMethodName(i));
                        parts.Append("]");
                        break;
                    case ScrollRect x: parts.Append($" [ScrollRect v={x.vertical} h={x.horizontal} content={(x.content ? x.content.name : "-")}]"); break;
                    case LayoutGroup x: parts.Append($" [{x.GetType().Name}]"); break;
                    case ContentSizeFitter _: parts.Append(" [ContentSizeFitter]"); break;
                    case LayoutElement _: parts.Append(" [LayoutElement]"); break;
                    case Mask _: parts.Append(" [Mask]"); break;
                    case MonoBehaviour m when m.GetType().Namespace == null: parts.Append(" [" + m.GetType().Name + "]"); break;
                }
            }
            sb.AppendLine(parts.ToString());
            foreach (Transform child in t) Dump(child, depth + 1, sb);
        }

        private static string Short(string s) => s == null ? "" : (s.Length > 40 ? s.Substring(0, 40) + "…" : s).Replace("\n", "\\n");
    }
}
