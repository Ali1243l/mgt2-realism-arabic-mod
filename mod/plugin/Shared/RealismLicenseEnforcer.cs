using System;
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
            { 0, "Acclaim" },
            { 1, "Accolade" },
            { 2, "Activision" },
            { 3, "Atari" },
            { 4, "Bandai" },
            { 5, "Capcom" },
            { 6, "Electronic Arts" },
            { 7, "Enix" },
            { 8, "Gremlin Graphics" },
            { 9, "Warner Bros. Games" },
            { 10, "Infocom" },
            { 11, "Infogrames" },
            { 12, "Kemco" },
            { 13, "Koei" },
            { 14, "Konami" },
            { 15, "MicroProse" },
            { 16, "Microsoft" },
            { 17, "Namco" },
            { 18, "Nintendo" },
            { 19, "Ocean Software" },
            { 20, "Origin Systems" },
            { 21, "Psygnosis" },
            { 22, "Rare" },
            { 23, "Sega" },
            { 24, "Sierra On-Line" },
            { 25, "Sony" },
            { 26, "Taito" },
            { 27, "Ubisoft" },
            { 28, "3D Realms" },
            { 29, "Quantic Dream" },
            { 30, "Remedy Entertainment" },
            { 31, "Rockstar Games" },
            { 32, "Sir-Tech" },
            { 33, "Tecnosoft" },
            { 34, "Troika Games" },
            { 35, "Westwood Studios" },
            { 36, "The Bitmap Brothers" },
            { 37, "Black Isle Studios" },
            { 38, "Blizzard Entertainment" },
            { 39, "Bullfrog Productions" },
            { 40, "Bungie" },
            { 41, "Obsidian Entertainment" },
            { 42, "Epic Games" },
            { 43, "id Software" },
            { 44, "Lionhead Studios" },
            { 45, "Looking Glass Studios" },
            { 46, "Naughty Dog" },
            { 47, "Ensemble Studios" },
            { 48, "FromSoftware" },
            { 49, "Data East" },
            { 50, "Epyx" },
            { 51, "Interplay" },
            { 52, "U.S. Gold" },
            { 53, "Virgin Interactive" },
            { 54, "Codemasters" },
            { 55, "Sensible Software" },
            { 56, "Elite Systems" },
            { 57, "Midway" },
            { 58, "Sunsoft" },
            { 59, "Monolith Productions" },
            { 60, "System 3" },
            { 61, "Rainbow Arts" },
            { 62, "Team17" },
            { 63, "CD Projekt" },
            { 64, "SSI" },
            { 65, "THQ" },
            { 66, "Hudson Soft" },
            { 67, "PlatinumGames" },
            { 68, "Broderbund" },
            { 69, "Blue Byte" },
            { 70, "Treyarch" },
            { 71, "Tencent" },
            { 72, "King" },
            { 73, "GungHo Online Entertainment" },
            { 74, "Supercell" },
            { 75, "Rovio Entertainment" },
            { 76, "Gameforge" },
            { 77, "Halfbrick Studios" },
            { 78, "Mojang" },
            { 79, "Zynga" },
            { 80, "Maxis" },
            { 81, "DMA Design" },
            { 82, "BioWare" },
            { 83, "Bethesda Softworks" },
            { 84, "Tecmo" },
            { 85, "Valve" },
            { 86, "LucasArts" },
            { 87, "Take-Two Interactive" },
            { 88, "Square" },
            { 89, "Paradox Interactive" },
            { 90, "SNK" },
            { 91, "Gearbox Software" },
            { 92, "Microïds" },
            { 93, "Crystal Dynamics" },
            { 94, "Avalanche Software" },
            { 95, "Cinemaware" },
            { 96, "Adventure Soft" },
            { 97, "Ion Storm" },
            { 98, "Thalion Software" },
            { 99, "Mindscape" },
        };

        public static readonly Dictionary<string, string> RealPublisherNamesByParody = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "3D Reality", "3D Realms" },
            { "Actvision", "Activision" },
            { "Adventure Tools", "Adventure Soft" },
            { "Akklaim", "Acclaim" },
            { "Akkoload", "Accolade" },
            { "Apache", "PlatinumGames" },
            { "Avalance Games", "Avalanche Software" },
            { "BMP Bros", "The Bitmap Brothers" },
            { "Beta Softworks", "Bethesda Softworks" },
            { "BioSoft", "BioWare" },
            { "Blizzer Studios", "Blizzard Entertainment" },
            { "Blockstar", "Rockstar Games" },
            { "Blue Ocean", "Ocean Software" },
            { "Brobound", "Broderbund" },
            { "Cinemasoft", "Cinemaware" },
            { "Code Design", "DMA Design" },
            { "Code Kings", "Codemasters" },
            { "Coey", "Koei" },
            { "Comani", "Konami" },
            { "Comgie", "Bungie" },
            { "Crystal Dynamo", "Crystal Dynamics" },
            { "Cupcoms", "Capcom" },
            { "Data West", "Data East" },
            { "Disc Project", "CD Projekt" },
            { "Droid Games", "Troika Games" },
            { "E-Mix", "Enix" },
            { "Eastwood Studios", "Westwood Studios" },
            { "Electric Arts", "Electronic Arts" },
            { "Ellie", "Elite Systems" },
            { "Epix Ware", "Epyx" },
            { "Erik Games", "Epic Games" },
            { "Form Studios", "FromSoftware" },
            { "Frog Studios", "Bullfrog Productions" },
            { "Fullbrick Studios", "Halfbrick Studios" },
            { "Galaxis", "Treyarch" },
            { "Gameforce", "Gameforge" },
            { "Gearsoft", "Gearbox Software" },
            { "Gigantic", "Rovio Entertainment" },
            { "Green Byte", "Blue Byte" },
            { "Green Isle", "Black Isle Studios" },
            { "Group 18", "Team17" },
            { "HungGo Online", "GungHo Online Entertainment" },
            { "Hutbros", "Warner Bros. Games" },
            { "Hutsin", "Hudson Soft" },
            { "Infocorp", "Infocom" },
            { "Inter Games", "Interplay" },
            { "Katari", "Atari" },
            { "Kencom", "Kemco" },
            { "Kramlin", "Gremlin Graphics" },
            { "Kung Entertainment", "King" },
            { "Lava Logic", "Ensemble Studios" },
            { "Lion Works", "Lionhead Studios" },
            { "Lukas Games", "LucasArts" },
            { "Maxus", "Maxis" },
            { "Microarts", "MicroProse" },
            { "Micronis", "Microïds" },
            { "Middle", "Midway" },
            { "Mindscale", "Mindscape" },
            { "Minisoft", "Microsoft" },
            { "Mintendu", "Nintendo" },
            { "Mono Soft", "Monolith Productions" },
            { "Moyan Studios", "Mojang" },
            { "Nanko", "Namco" },
            { "Naughty Bear", "Naughty Dog" },
            { "Neon Storm", "Ion Storm" },
            { "Origo", "Origin Systems" },
            { "Panda", "Bandai" },
            { "Parodax Interactive", "Paradox Interactive" },
            { "Pigsys", "Psygnosis" },
            { "Pony", "Sony" },
            { "Quantum Dream", "Quantic Dream" },
            { "RainCode", "Rainbow Arts" },
            { "Romady", "Remedy Entertainment" },
            { "Rore", "Rare" },
            { "SMK Corporation", "SNK" },
            { "Siga", "Sega" },
            { "Sir-Code", "Sir-Tech" },
            { "Squall", "Square" },
            { "Sunglasses Studios", "Looking Glass Studios" },
            { "Sunwork", "Sunsoft" },
            { "Superbell", "Supercell" },
            { "Systech-4", "System 3" },
            { "TEKMO", "Tecmo" },
            { "TLO", "THQ" },
            { "Take 3 Software", "Take-Two Interactive" },
            { "Tech Soft", "Tecnosoft" },
            { "Teitu", "Taito" },
            { "Tenzent", "Tencent" },
            { "Thalan Software", "Thalion Software" },
            { "Tripple House", "Obsidian Entertainment" },
            { "Unisoft", "Ubisoft" },
            { "User Gold", "U.S. Gold" },
            { "Virtual", "Virgin Interactive" },
            { "Walve", "Valve" },
            { "ZZI Games", "SSI" },
            { "Zensible Studio", "Sensible Software" },
            { "Ziera", "Sierra On-Line" },
            { "Zigna", "Zynga" },
            { "iGames", "Infogrames" },
            { "it Studios", "id Software" },
        };

        public static readonly Dictionary<int, string> RealPlatformNamesById = new Dictionary<int, string>
        {
            { 1, "Amiga 500" },
            { 2, "Amiga 1000" },
            { 3, "Atari 2600" },
            { 4, "Atari 5200" },
            { 5, "Atari 7800" },
            { 6, "Commodore 64" },
            { 7, "Commodore 128" },
            { 8, "Sega Dreamcast" },
            { 9, "Game Boy" },
            { 10, "Nintendo GameCube" },
            { 11, "Sega Game Gear" },
            { 12, "Sega Master System" },
            { 13, "Sega Mega Drive" },
            { 14, "Nintendo 64" },
            { 15, "Nintendo Entertainment System" },
            { 16, "Nintendo DS" },
            { 17, "IBM PC" },
            { 18, "PlayStation" },
            { 19, "PlayStation 2" },
            { 20, "PlayStation 3" },
            { 21, "PlayStation 4" },
            { 22, "PlayStation 5" },
            { 23, "PlayStation Portable" },
            { 24, "Sega Saturn" },
            { 25, "Super Nintendo" },
            { 26, "Wii" },
            { 27, "Wii U" },
            { 28, "Nintendo Switch 2" },
            { 29, "Xbox" },
            { 30, "Xbox 360" },
            { 31, "Xbox Series X" },
            { 32, "Xbox One" },
            { 33, "Atari Jaguar" },
            { 34, "Atari 2600+" },
            { 35, "3DO Interactive Multiplayer" },
            { 36, "Vectrex" },
            { 37, "ColecoVision" },
            { 38, "Intellivision" },
            { 39, "Sega SG-1000" },
            { 40, "Commodore VIC-20" },
            { 41, "ZX Spectrum" },
            { 42, "Apple Pippin" },
            { 43, "PC Engine" },
            { 44, "Atari Lynx" },
            { 45, "Ouya" },
            { 46, "PlayStation Vita" },
            { 47, "Neo Geo" },
            { 48, "Macintosh" },
            { 49, "Apple I" },
            { 50, "Gizmondo" },
            { 51, "Amiga CD32" },
            { 52, "Atari ST" },
            { 53, "Amstrad CPC" },
            { 54, "Acorn Archimedes" },
            { 55, "Game Boy Advance" },
            { 56, "Nintendo 3DS" },
            { 57, "NVIDIA Shield Portable" },
            { 58, "PC Engine GT" },
            { 59, "PC Engine Duo" },
            { 60, "PC-FX" },
            { 61, "Neo Geo Pocket" },
            { 62, "Evercade VS" },
            { 63, "WonderSwan" },
            { 64, "N-Gage" },
            { 65, "Steam Machine" },
            { 66, "Game Boy Color" },
            { 67, "Sega Genesis Mini" },
            { 68, "Commodore 64 Games System" },
            { 69, "THEC64 Mini" },
            { 70, "Nintendo Switch 3" },
            { 71, "Playdia" },
            { 72, "NES Classic Edition" },
            { 73, "Microvision" },
            { 74, "Watara Supervision" },
            { 75, "Arcadia 2001" },
            { 76, "Bally Astrocade" },
            { 77, "Fairchild Channel F" },
            { 78, "Casio PV-1000" },
            { 79, "Gamate" },
            { 80, "Magnavox Odyssey 2" },
            { 81, "Apple II" },
            { 82, "Nintendo Switch" },
            { 83, "Amstrad GX4000" },
            { 84, "Cassette Vision" },
            { 85, "Super Cassette Vision" },
            { 86, "Entex Adventure Vision" },
            { 87, "Neo Geo X" },
            { 88, "Radofin 1292 APVS" },
            { 89, "Super A'Can" },
            { 90, "VTech CreatiVision" },
            { 91, "VTech Laser 2001" },
            { 92, "Zeebo" },
            { 93, "Virtual Boy" },
            { 94, "Sega Nomad" },
            { 95, "PlayStation 6" },
            { 96, "Xbox Next" },
            { 97, "Commodore PET" },
            { 98, "ZX80" },
            { 99, "Atari 800" },
            { 100, "TRS-80" },
            { 101, "Philips CD-i" },
            { 102, "iPhone" },
            { 103, "iPhone 4S" },
            { 104, "iPhone 6s" },
            { 105, "iPhone 11" },
            { 106, "iPhone 15" },
            { 107, "Android 1.5 Cupcake" },
            { 108, "Android 4.3 Jelly Bean" },
            { 109, "Android 8 Oreo" },
            { 110, "Android 12" },
            { 111, "Android 16" },
            { 112, "Windows Phone 8" },
            { 113, "Windows 10 Mobile" },
            { 114, "BlackBerry Bold" },
            { 115, "BlackBerry Z10" },
            { 116, "BlackBerry Priv" },
            { 117, "Nokia Symbian^1" },
            { 118, "Nokia Symbian^3" },
            { 126, "Commodore CDTV" },
            { 127, "Pioneer LaserActive" },
            { 128, "Atari VCS" },
            { 129, "Steam Deck" },
            { 130, "Intellivision Amico" },
            { 131, "Nintendo Switch Lite" },
            { 132, "Game Gear Micro" },
        };

        public static readonly Dictionary<string, string> RealPlatformNamesByParody = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "4D-OH", "3DO Interactive Multiplayer" },
            { "Acoon Archimed", "Acorn Archimedes" },
            { "Amigo 1000", "Amiga 1000" },
            { "Amigo 500", "Amiga 500" },
            { "Amigo CDX-32", "Amiga CD32" },
            { "Amigo CTV", "Commodore CDTV" },
            { "Amstar CCP", "Amstrad CPC" },
            { "Amstar GX3000", "Amstrad GX4000" },
            { "Arcade-2640", "Arcadia 2001" },
            { "Asia Box", "Evercade VS" },
            { "Astro Arcade", "Bally Astrocade" },
            { "Beam Deck", "Steam Deck" },
            { "Beam Machine", "Steam Machine" },
            { "CassetteView", "Cassette Vision" },
            { "Coleckovision", "ColecoVision" },
            { "Commander C127", "Commodore 128" },
            { "Commander C63", "Commodore 64" },
            { "Commander Game System", "Commodore 64 Games System" },
            { "Commander Game System II", "THEC64 Mini" },
            { "Commander PETE", "Commodore PET" },
            { "Commander VEC-20", "Commodore VIC-20" },
            { "Core Engine", "PC Engine" },
            { "Core Engine Duo", "PC Engine Duo" },
            { "Core Engine GT", "PC Engine GT" },
            { "Core-FX", "PC-FX" },
            { "Cyboo", "Zeebo" },
            { "Droidan I", "Android 1.5 Cupcake" },
            { "Droidan II", "Android 4.3 Jelly Bean" },
            { "Droidan III", "Android 8 Oreo" },
            { "Droidan IV", "Android 12" },
            { "Droidan V", "Android 16" },
            { "Fairtech-CF", "Fairchild Channel F" },
            { "Fusion F1000", "NES Classic Edition" },
            { "Game Mate", "Gamate" },
            { "Game Portable Advance", "Game Boy Advance" },
            { "Geo Reo", "Neo Geo" },
            { "Geo Reo Pocket", "Neo Geo Pocket" },
            { "Gizmos", "Gizmondo" },
            { "Intelvisual", "Intellivision" },
            { "Intelvisual Amikos", "Intellivision Amico" },
            { "Intex AD-Vision", "Entex Adventure Vision" },
            { "Katari 2600", "Atari 2600" },
            { "Katari 5200", "Atari 5200" },
            { "Katari 7800", "Atari 7800" },
            { "Katari 850", "Atari 800" },
            { "Katari Lunx", "Atari Lynx" },
            { "Katari Puma", "Atari Jaguar" },
            { "Katari Puma 2", "Atari 2600+" },
            { "Katari STT", "Atari ST" },
            { "Katari VCC", "Atari VCS" },
            { "M-Cage", "N-Gage" },
            { "Magma Olympus II", "Magnavox Odyssey 2" },
            { "McIntosh", "Macintosh" },
            { "Mega-A-Can", "Super A'Can" },
            { "Minisoft Phone I", "Windows Phone 8" },
            { "Minisoft Phone II", "Windows 10 Mobile" },
            { "Minisoft YBox", "Xbox" },
            { "Minisoft YBox 370", "Xbox 360" },
            { "Minisoft YBox 720", "Xbox One" },
            { "Minisoft YBox SX", "Xbox Series X" },
            { "Minisoft YBox SX-II", "Xbox Next" },
            { "Minivision", "Microvision" },
            { "Mintendu 3DEX", "Nintendo Switch Lite" },
            { "Mintendu 3SD", "Nintendo 3DS" },
            { "Mintendu 63", "Nintendo 64" },
            { "Mintendu D-S", "Nintendo DS" },
            { "Mintendu Fii", "Wii" },
            { "Mintendu FiiU", "Wii U" },
            { "Mintendu Game Portable", "Game Boy" },
            { "Mintendu Game Portable Color", "Game Boy Color" },
            { "Mintendu MX", "Nintendo Switch 3" },
            { "Mintendu Playcube", "Nintendo GameCube" },
            { "Mintendu System", "Nintendo Entertainment System" },
            { "Mintendu Virtual Portable", "Virtual Boy" },
            { "Mintendu Zwitch", "Nintendo Switch" },
            { "Mintendu Zwitch Pro", "Nintendo Switch 2" },
            { "Neo X", "Neo Geo X" },
            { "Nvideo Pro", "NVIDIA Shield Portable" },
            { "Oya", "Ouya" },
            { "PVX-1000", "Casio PV-1000" },
            { "Personal Computer", "IBM PC" },
            { "PhaserActive", "Pioneer LaserActive" },
            { "Philops CD-X", "Philips CD-i" },
            { "Pippal", "Apple Pippin" },
            { "Play-X", "Playdia" },
            { "Pony GSP", "PlayStation Portable" },
            { "Pony GSViva", "PlayStation Vita" },
            { "Pony Gamestation", "PlayStation" },
            { "Pony Gamestation 2", "PlayStation 2" },
            { "Pony Gamestation 3", "PlayStation 3" },
            { "Pony Gamestation 4", "PlayStation 4" },
            { "Pony Gamestation 5", "PlayStation 5" },
            { "Pony Gamestation 6", "PlayStation 6" },
            { "RRS-80", "TRS-80" },
            { "Redberry Z10", "BlackBerry Z10" },
            { "Redberry Z20", "BlackBerry Priv" },
            { "Redberry Z5", "BlackBerry Bold" },
            { "Rodovin APVS", "Radofin 1292 APVS" },
            { "SIG-2000", "Sega SG-1000" },
            { "Siga Master Deck", "Sega Master System" },
            { "Siga Mega Deck", "Sega Mega Drive" },
            { "Siga MegaCast", "Sega Genesis Mini" },
            { "Siga Noman", "Sega Nomad" },
            { "Siga Play Gear", "Sega Game Gear" },
            { "Siga Play Gear II", "Game Gear Micro" },
            { "Siga Reamcast", "Sega Dreamcast" },
            { "Siga Uranus", "Sega Saturn" },
            { "Super CassetteView", "Super Cassette Vision" },
            { "Super Mintendu System", "Super Nintendo" },
            { "Sybios 500", "Nokia Symbian^1" },
            { "Sybios 800", "Nokia Symbian^3" },
            { "Tapple I", "Apple I" },
            { "Tapple II", "Apple II" },
            { "Vector-x", "Vectrex" },
            { "W-Tech Laser 2000", "VTech Laser 2001" },
            { "W-Tech Vision", "VTech CreatiVision" },
            { "Wasari Megavision", "Watara Supervision" },
            { "Wonderbird", "WonderSwan" },
            { "ZY Spectre", "ZX Spectrum" },
            { "ZY80", "ZX80" },
            { "iTapple", "iPhone" },
            { "iTapple 2", "iPhone 4S" },
            { "iTapple 3", "iPhone 6s" },
            { "iTapple 4", "iPhone 11" },
            { "iTapple 5", "iPhone 15" },
        };

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

    [HarmonyPatch(typeof(platformScript), "GetName")]
    internal static class Patch_PlatformsScript_GetName
    {
        private static void Postfix(platformScript __instance, ref string __result)
        {
            if (__instance == null) return;
            string real = RealismLicenseEnforcer.GetRealPlatformName(__instance.myID, __result);
            if (!string.IsNullOrEmpty(real))
            {
                __result = real;
            }
        }
    }

    [HarmonyPatch(typeof(savegameScript), "LoadPublisher")]
    internal static class Patch_SavegameScript_LoadPublisher
    {
        private static void Postfix(savegameScript __instance)
        {
            var ms = Traverse.Create(__instance).Field("mS_").GetValue<mainScript>() ?? UnityEngine.Object.FindObjectOfType<mainScript>();
            if (ms != null)
            {
                RealismLicenseEnforcer.Enforce(ms);
            }
        }
    }

    [HarmonyPatch(typeof(savegameScript), "LoadPlatforms")]
    internal static class Patch_SavegameScript_LoadPlatforms
    {
        private static void Postfix(savegameScript __instance)
        {
            var ms = Traverse.Create(__instance).Field("mS_").GetValue<mainScript>() ?? UnityEngine.Object.FindObjectOfType<mainScript>();
            if (ms != null)
            {
                RealismLicenseEnforcer.Enforce(ms);
            }
        }
    }
}
