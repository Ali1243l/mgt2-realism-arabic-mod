import csv

with open(r'E:\mad games tycoon 2 mods\mod\data\publishers.tsv', encoding='utf-8') as f:
    pubs = list(csv.DictReader(f, delimiter='\t'))

with open(r'E:\mad games tycoon 2 mods\backup\Extern\Text\DATA\Publisher.txt', encoding='utf-8') as f:
    lines = f.readlines()

old_pubs = {}
cur_id = None
for l in lines:
    l = l.strip()
    if l.startswith('[ID]'):
        cur_id = int(l[4:])
    elif l.startswith('[NAME EN]') and cur_id is not None:
        old_pubs[cur_id] = l[9:].strip()

pub_id_map = {int(p['id']): p['name'].strip() for p in pubs}
pub_parody_map = {}
for p in pubs:
    pid = int(p['id'])
    real_name = p['name'].strip()
    old_name = old_pubs.get(pid, '')
    if old_name:
        pub_parody_map[old_name] = real_name

with open(r'E:\mad games tycoon 2 mods\mod\data\platforms.tsv', encoding='utf-8') as f:
    plats = list(csv.DictReader(f, delimiter='\t'))

with open(r'E:\mad games tycoon 2 mods\backup\Extern\Text\DATA\Platforms.txt', encoding='utf-8') as f:
    lines = f.readlines()

old_plats = {}
cur_id = None
for l in lines:
    l = l.strip()
    if l.startswith('[ID]'):
        cur_id = int(l[4:])
    elif l.startswith('[NAME EN]') and cur_id is not None:
        old_plats[cur_id] = l[9:].strip()

plat_id_map = {int(p['id']): p['name'].strip() for p in plats}
plat_parody_map = {}
for p in plats:
    pid = int(p['id'])
    real_name = p['name'].strip()
    old_name = old_plats.get(pid, '')
    if old_name:
        plat_parody_map[old_name] = real_name

cs_code = '''using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Ali.Mgt2.Shared
{
    /// <summary>
    /// Runtime enforcer for official real-world studio, publisher, and platform licenses.
    /// Overrides legacy savegame parody names (e.g. Mintendu -> Nintendo, Katari -> Atari, Siga -> Sega)
    /// across all game systems, subsidiary menus, stock market, and publisher directories.
    /// </summary>
    public static class RealismLicenseEnforcer
    {
        public static readonly Dictionary<int, string> RealPublisherNamesById = new Dictionary<int, string>
        {
'''
for pid in sorted(pub_id_map.keys()):
    name = pub_id_map[pid].replace('"', '\\"')
    cs_code += f'            {{ {pid}, "{name}" }},\n'

cs_code += '''        };

        public static readonly Dictionary<string, string> RealPublisherNamesByParody = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
'''
for old in sorted(pub_parody_map.keys()):
    real = pub_parody_map[old].replace('"', '\\"')
    old_escaped = old.replace('"', '\\"')
    cs_code += f'            {{ "{old_escaped}", "{real}" }},\n'

cs_code += '''        };

        public static readonly Dictionary<int, string> RealPlatformNamesById = new Dictionary<int, string>
        {
'''
for pid in sorted(plat_id_map.keys()):
    name = plat_id_map[pid].replace('"', '\\"')
    cs_code += f'            {{ {pid}, "{name}" }},\n'

cs_code += '''        };

        public static readonly Dictionary<string, string> RealPlatformNamesByParody = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
'''
for old in sorted(plat_parody_map.keys()):
    real = plat_parody_map[old].replace('"', '\\"')
    old_escaped = old.replace('"', '\\"')
    cs_code += f'            {{ "{old_escaped}", "{real}" }},\n'

cs_code += '''        };

        public static string GetRealPublisherName(int id, string currentName = null)
        {
            if (RealPublisherNamesById.TryGetValue(id, out var realById) && !string.IsNullOrEmpty(realById))
                return realById;

            if (!string.IsNullOrEmpty(currentName) && RealPublisherNamesByParody.TryGetValue(currentName, out var realByParody))
                return realByParody;

            return currentName ?? "";
        }

        public static string GetRealPlatformName(int id, string currentName = null)
        {
            if (RealPlatformNamesById.TryGetValue(id, out var realById) && !string.IsNullOrEmpty(realById))
                return realById;

            if (!string.IsNullOrEmpty(currentName) && RealPlatformNamesByParody.TryGetValue(currentName, out var realByParody))
                return realByParody;

            return currentName ?? "";
        }

        public static void Enforce(mainScript ms)
        {
            if (ms == null) return;

            // 1. Enforce Publishers
            if (ms.arrayPublisherScripts != null)
            {
                for (int i = 0; i < ms.arrayPublisherScripts.Length; i++)
                {
                    var pub = ms.arrayPublisherScripts[i];
                    if (pub == null) continue;

                    string real = GetRealPublisherName(pub.myID, pub.name_EN);
                    if (!string.IsNullOrEmpty(real))
                    {
                        pub.name_EN = real;
                        pub.name_GE = real;
                        pub.name_TU = real;
                        pub.name_CH = real;
                        pub.name_FR = real;
                        pub.name_JA = real;
                        pub.name_UA = real;
                        pub.name_TH = real;
                    }
                }
            }

            // 2. Enforce Platforms
            if (ms.arrayPlatformsScripts != null)
            {
                for (int i = 0; i < ms.arrayPlatformsScripts.Length; i++)
                {
                    var plat = ms.arrayPlatformsScripts[i];
                    if (plat == null) continue;

                    string real = GetRealPlatformName(plat.myID, plat.name_EN);
                    if (!string.IsNullOrEmpty(real))
                    {
                        plat.name_EN = real;
                        plat.name_GE = real;
                        plat.name_TU = real;
                        plat.name_CH = real;
                        plat.name_FR = real;
                        plat.name_JA = real;
                        plat.name_UA = real;
                        plat.name_TH = real;
                    }

                    if (!string.IsNullOrEmpty(plat.manufacturer_EN) && RealPublisherNamesByParody.TryGetValue(plat.manufacturer_EN, out var realMan))
                    {
                        plat.manufacturer_EN = realMan;
                        plat.manufacturer_GE = realMan;
                        plat.manufacturer_TU = realMan;
                        plat.manufacturer_CH = realMan;
                        plat.manufacturer_FR = realMan;
                        plat.manufacturer_JA = realMan;
                        plat.manufacturer_UA = realMan;
                        plat.manufacturer_TH = realMan;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(publisherScript), "GetName")]
    internal static class Patch_PublisherScript_GetName
    {
        private static void Postfix(publisherScript __instance, ref string __result)
        {
            if (__instance == null) return;
            string real = RealismLicenseEnforcer.GetRealPublisherName(__instance.myID, __result);
            if (!string.IsNullOrEmpty(real))
            {
                __result = real;
            }
        }
    }

    [HarmonyPatch(typeof(platformsScript), "GetName")]
    internal static class Patch_PlatformsScript_GetName
    {
        private static void Postfix(platformsScript __instance, ref string __result)
        {
            if (__instance == null) return;
            string real = RealismLicenseEnforcer.GetRealPlatformName(__instance.myID, __result);
            if (!string.IsNullOrEmpty(real))
            {
                __result = real;
            }
        }
    }
}
'''

with open(r'E:\mad games tycoon 2 mods\mod\plugin\Shared\RealismLicenseEnforcer.cs', 'w', encoding='utf-8') as f:
    f.write(cs_code)

print('Generated RealismLicenseEnforcer.cs successfully!')
