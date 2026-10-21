# MGT2 — تقرير التدقيق قبل التطبيق (Realism & Arabic Localization Audit)

> **الحالة:** مسودة للمراجعة. **لم يُعدَّل أي ملف من ملفات اللعبة.** لن يتم الحقن قبل موافقتك الصريحة في الشات.
> **تاريخ الفحص:** 2026-10-21 · **المسار:** `E:\Steam\steamapps\common\Mad Games Tycoon 2` (build 20855306)
> نسخة ثانية مطابقة موجودة في `D:\Steam\...` (نفس الـ build ونفس الملفات 2166 ملف في `Extern`).

---

## 0. حقائق تقنية اكتُشفت (مهمة قبل أي تعديل)

| # | الحقيقة | الأثر على التعديل |
|---|---------|-------------------|
| T1 | كل البيانات القابلة للتعديل في `Mad Games Tycoon 2_Data\Extern\` — نصوص `.txt` و صور `.png`. لا حاجة لتعديل الـ `.dll` أو `.assets`. | التعديل آمن وقابل للتراجع بنسخ احتياطي. |
| T2 | العربية في `Text\AR\*.txt` مخزّنة **مُشكَّلة مسبقاً** (Presentation Forms U+FExx) و**بترتيب بصري معكوس**، لأن محرك Unity في اللعبة يرسم من اليسار لليمين ولا يدعم RTL. | **"النص المقلوب" ليس خطأ بحد ذاته** — هو الطريقة الوحيدة التي تعمل. الخطأ الحقيقي هو الكلمات اللاتينية/الأرقام التي انعكست معه (مثل `reyalpitluM enilnO`). |
| T3 | أي تصحيح عربي يجب أن يُكتب بالعربية العادية ثم يُحوَّل آلياً: تشكيل الحروف ← عكس الترتيب ← إبقاء الوسوم `<b>`, `<color>`, `<NAME>` والكلمات اللاتينية والأرقام **بالاتجاه الصحيح**. | سأكتب أداة تحويل صغيرة في مرحلة الحقن بدل التحرير اليدوي (التحرير اليدوي للحروف المشكّلة يسبب أخطاء). |
| T4 | ملفات البيانات (`Platforms.txt`, `Publisher.txt`, ...) **لا تحتوي مفتاح `[NAME AR]`** — اللاعب العربي يرى الاسم الإنجليزي `[NAME EN]`. | أسماء المنصات والشركات تبقى بالإنجليزية (وهذا هو الصحيح للعلامات التجارية). يجب تعديل `[NAME EN]` وباقي اللغات اللاتينية معاً. |
| T5 | في `Platforms.txt`: إذا كانت للمنصة `[PUB]`، **يجب أن يطابق `[MANUFACTURER]` اسم الناشر حرفياً** في `Publisher.txt` (حسب تعليق الملف نفسه). | إعادة تسمية `Pony` إلى `Sony` يجب أن تتم في الملفين معاً. |
| T6 | شعارات الشركات: `Extern\CompanyLogos\<PIC>.png` بمقاس **64×64** (203 ملف). أيقونات المنصات: `Extern\Icons_Platforms\*.png` بمقاس **128×128** (135 ملف + `missing.png` 32×32). | الشعارات البديلة يجب أن تكون PNG شفافة بنفس المقاس بالضبط. |
| T7 | `Themes_EN.txt` بترميز UTF-16 ويبدأ بـ 14 سطر تعليق؛ `Themes_AR.txt` بترميز UTF-8 بدون الترويسة — **الترتيب متطابق** (303 موضوع). وسوم الأنواع `<0><1>...` تُقرأ من EN فقط. | لا يوجد خلل في المواضيع، فقط كلمتان غير مترجمتين. |
| T8 | تحديث Steam أو "Verify integrity" سيستبدل الملفات المعدّلة. | سأحفظ نسخة احتياطية + سكربت إعادة تطبيق. |

---

## 1. جدول استبدال المنصات (Platforms.txt — 133 منصة)

✅ = مطابقة مؤكدة (الاسم + التاريخ + الأيقونة)، ⚠️ = منصة خيالية/مستقبلية بلا مقابل حقيقي مباشر — **تحتاج قرارك**.

| ID | الاسم الحالي ➔ الاسم الحقيقي | الشركة الحالية ➔ الحقيقية | |
|---:|---|---|:-:|
| 1 | Amigo 500 ➔ **Amiga 500** | Commander ➔ **Commodore** | ✅ |
| 2 | Amigo 1000 ➔ **Amiga 1000** | Commander ➔ Commodore | ✅ |
| 3 | Katari 2600 ➔ **Atari 2600** | Katari ➔ **Atari** | ✅ |
| 4 | Katari 5200 ➔ **Atari 5200** | Atari | ✅ |
| 5 | Katari 7800 ➔ **Atari 7800** | Atari | ✅ |
| 6 | Commander C63 ➔ **Commodore 64** | Commodore | ✅ |
| 7 | Commander C127 ➔ **Commodore 128** | Commodore | ✅ |
| 8 | Siga Reamcast ➔ **Sega Dreamcast** | Siga ➔ **Sega** | ✅ |
| 9 | Mintendu Game Portable ➔ **Game Boy** | Mintendu ➔ **Nintendo** | ✅ |
| 10 | Mintendu Playcube ➔ **Nintendo GameCube** | Nintendo | ✅ |
| 11 | Siga Play Gear ➔ **Sega Game Gear** | Sega | ✅ |
| 12 | Siga Master Deck ➔ **Sega Master System** | Sega | ✅ |
| 13 | Siga Mega Deck ➔ **Sega Mega Drive** | Sega | ✅ |
| 14 | Mintendu 63 ➔ **Nintendo 64** | Nintendo | ✅ |
| 15 | Mintendu System ➔ **Nintendo Entertainment System** | Nintendo | ✅ |
| 16 | Mintendu D-S ➔ **Nintendo DS** | Nintendo | ✅ |
| 17 | Personal Computer ➔ **IBM PC** | ICM ➔ **IBM** | ✅ |
| 18 | Pony Gamestation ➔ **PlayStation** | Pony ➔ **Sony** | ✅ |
| 19 | Pony Gamestation 2 ➔ **PlayStation 2** | Sony | ✅ |
| 20 | Pony Gamestation 3 ➔ **PlayStation 3** | Sony | ✅ |
| 21 | Pony Gamestation 4 ➔ **PlayStation 4** | Sony | ✅ |
| 22 | Pony Gamestation 5 ➔ **PlayStation 5** | Sony | ✅ |
| 23 | Pony GSP ➔ **PlayStation Portable** | Sony | ✅ |
| 24 | Siga Uranus ➔ **Sega Saturn** | Sega | ✅ |
| 25 | Super Mintendu System ➔ **Super Nintendo** | Nintendo | ✅ |
| 26 | Mintendu Fii ➔ **Wii** | Nintendo | ✅ |
| 27 | Mintendu FiiU ➔ **Wii U** | Nintendo | ✅ |
| 28 | Mintendu Zwitch Pro (DEC 2024) ➔ **Nintendo Switch 2** | Nintendo | ⚠️ الاسم حقيقي، التاريخ الحقيقي JUN 2025 |
| 29 | Minisoft YBox ➔ **Xbox** | Minisoft ➔ **Microsoft** | ✅ |
| 30 | Minisoft YBox 370 ➔ **Xbox 360** | Microsoft | ✅ |
| 31 | Minisoft YBox SX ➔ **Xbox Series X** | Microsoft | ✅ |
| 32 | Minisoft YBox 720 ➔ **Xbox One** | Microsoft | ✅ |
| 33 | Katari Puma ➔ **Atari Jaguar** | Atari | ✅ |
| 34 | Katari Puma 2 (JUL 2026) ➔ اقتراح: **Atari Jaguar II** (مشروع أُلغي فعلاً) | Atari | ⚠️ |
| 35 | 4D-OH ➔ **3DO Interactive Multiplayer** | The 4D-OH Company ➔ **The 3DO Company** | ✅ |
| 36 | Vector-x ➔ **Vectrex** | General Selectric ➔ **General Consumer Electronics** | ✅ |
| 37 | Coleckovision ➔ **ColecoVision** | Colecko ➔ **Coleco** | ✅ |
| 38 | Intelvisual ➔ **Intellivision** | Matal ➔ **Mattel** | ✅ |
| 39 | SIG-2000 ➔ **Sega SG-1000** | Sega | ✅ |
| 40 | Commander VEC-20 ➔ **Commodore VIC-20** | Commodore | ✅ |
| 41 | ZY Spectre ➔ **ZX Spectrum** | Sintech Research ➔ **Sinclair Research** | ✅ |
| 42 | Pippal ➔ **Pippin** | Tapple Computer, Inc. ➔ **Apple Computer, Inc.** | ✅ |
| 43 | Core Engine ➔ **PC Engine** | MEC ➔ **NEC** | ✅ |
| 44 | Katari Lunx ➔ **Atari Lynx** | Atari | ✅ |
| 45 | Oya ➔ **Ouya** | Oya Corporation ➔ **Ouya Inc.** | ✅ |
| 46 | Pony GSViva ➔ **PlayStation Vita** | Sony | ✅ |
| 47 | Geo Reo ➔ **Neo Geo** | SMK Corporation ➔ **SNK** | ✅ |
| 48 | McIntosh ➔ **Macintosh** | Apple Computer, Inc. | ✅ |
| 49 | Tapple I ➔ **Apple I** | Apple Computer, Inc. | ✅ |
| 50 | Gizmos ➔ **Gizmondo** | Puma Telematics ➔ **Tiger Telematics** | ✅ |
| 51 | Amigo CDX-32 ➔ **Amiga CD32** | Commodore | ✅ |
| 52 | Katari STT ➔ **Atari ST** | Atari | ✅ |
| 53 | Amstar CCP ➔ **Amstrad CPC** | Amstar ➔ **Amstrad** | ✅ |
| 54 | Acoon Archimed ➔ **Acorn Archimedes** | Acoon ➔ **Acorn Computers** | ✅ |
| 55 | Game Portable Advance ➔ **Game Boy Advance** | Nintendo | ✅ |
| 56 | Mintendu 3SD ➔ **Nintendo 3DS** | Nintendo | ✅ |
| 57 | Nvideo Pro ➔ **NVIDIA Shield Portable** | Nvideo ➔ **NVIDIA** | ✅ |
| 58 | Core Engine GT ➔ **PC Engine GT** | NEC | ✅ |
| 59 | Core Engine Duo ➔ **PC Engine Duo** | NEC | ✅ |
| 60 | Core-FX ➔ **PC-FX** | NEC | ✅ |
| 61 | Geo Reo Pocket ➔ **Neo Geo Pocket** | SNK | ✅ |
| 62 | Asia Box (FEB 2024) ➔ ؟ | Snake ➔ ؟ | ⚠️ لا مقابل معروف |
| 63 | Wonderbird ➔ **WonderSwan** | Pandai ➔ **Bandai** | ✅ |
| 64 | M-Cage ➔ **N-Gage** | Mokia ➔ **Nokia** | ✅ |
| 65 | Beam Machine ➔ **Steam Machine** | Beam ➔ **Valve** | ✅ |
| 66 | Mintendu Game Portable Color ➔ **Game Boy Color** | Nintendo | ✅ |
| 67 | Siga MegaCast (APR 2025) ➔ ؟ | Sega | ⚠️ خيالية |
| 68 | Commander Game System ➔ **Commodore 64 Games System** | Commodore | ✅ |
| 69 | Commander Game System II (DEC 2023) ➔ ؟ | Commodore | ⚠️ خيالية |
| 70 | Mintendu MX (JAN 2030) ➔ ؟ | Nintendo | ⚠️ مستقبلية |
| 71 | Play-X ➔ **Playdia** | Panda ➔ **Bandai** | ✅ |
| 72 | Fusion F1000 (JUN 2016) ➔ ؟ | Fusion | ⚠️ لا مقابل معروف |
| 73 | Minivision ➔ **Microvision** | MBL ➔ **Milton Bradley** | ✅ |
| 74 | Wasari Megavision ➔ **Watara Supervision** | Wasari ➔ **Watara** | ✅ |
| 75 | Arcade-2640 ➔ **Arcadia 2001** | Emmason ➔ **Emerson** | ✅ |
| 76 | Astro Arcade ➔ **Bally Astrocade** | Wally ➔ **Bally** | ✅ |
| 77 | Fairtech-CF ➔ **Fairchild Channel F** | Fairtech ➔ **Fairchild** | ✅ |
| 78 | PVX-1000 ➔ **Casio PV-1000** | Kaiso ➔ **Casio** | ✅ |
| 79 | Game Mate ➔ **Gamate** | Bit Tech ➔ **Bit Corporation** | ✅ |
| 80 | Magma Olympus II ➔ **Magnavox Odyssey²** | Syslips ➔ **Philips** | ✅ |
| 81 | Tapple II ➔ **Apple II** | Apple Computer, Inc. | ✅ |
| 82 | Mintendu Zwitch ➔ **Nintendo Switch** | Nintendo | ✅ |
| 83 | Amstar GX3000 ➔ **Amstrad GX4000** | Amstrad | ✅ |
| 84 | CassetteView ➔ **Cassette Vision** | Epic Co. ➔ **Epoch Co.** | ✅ |
| 85 | Super CassetteView ➔ **Super Cassette Vision** | Epoch Co. | ✅ |
| 86 | Intex AD-Vision ➔ **Adventure Vision** | Intex ➔ **Entex** | ✅ |
| 87 | Neo X ➔ **Neo Geo X** | SNK | ✅ |
| 88 | Rodovin APVS ➔ **1292 Advanced Programmable Video System** | Rodovin ➔ **Radofin** | ✅ |
| 89 | Mega-A-Can ➔ **Super A'Can** | Funny-Tech ➔ **Funtech** | ✅ |
| 90 | W-Tech Vision ➔ **CreatiVision** | W-Tech ➔ **VTech** | ✅ |
| 91 | W-Tech Laser 2000 ➔ **Laser 2001** | VTech | ✅ |
| 92 | Cyboo ➔ **Zeebo** | Cyboo Inc. ➔ **Zeebo Inc.** | ✅ |
| 93 | Mintendu Virtual Portable ➔ **Virtual Boy** | Nintendo | ✅ |
| 94 | Siga Noman ➔ **Sega Nomad** | Sega | ✅ |
| 95 | Pony Gamestation 6 (DEC 2028) ➔ **PlayStation 6** | Sony | ⚠️ لم تصدر بعد |
| 96 | Minisoft YBox SX-II (NOV 2028) ➔ اقتراح: **Xbox Next** | Microsoft | ⚠️ لم تصدر بعد |
| 97 | Commander PETE ➔ **Commodore PET** | Commodore | ✅ |
| 98 | ZY80 ➔ **ZX80** | Sinclair Research | ✅ |
| 99 | Katari 850 ➔ **Atari 800** | Atari | ✅ |
| 100 | RRS-80 ➔ **TRS-80** | Randy Corporation ➔ **Tandy Corporation** | ✅ |
| 101 | Philops CD-X ➔ **Philips CD-i** | Philops ➔ **Philips** | ✅ |
| 102 | iTapple ➔ **iPhone** | Apple | ✅ |
| 103 | iTapple 2 (2011) ➔ **iPhone 4S** | Apple | ✅ |
| 104 | iTapple 3 (2015) ➔ **iPhone 6s** | Apple | ✅ |
| 105 | iTapple 4 (2019) ➔ **iPhone 11** | Apple | ✅ |
| 106 | iTapple 5 (2023) ➔ **iPhone 15** | Apple | ✅ |
| 107 | Droidan I (2009) ➔ **Android 1.5 Cupcake** | Geeglo Inc. ➔ **Google** | ✅ |
| 108 | Droidan II (2013) ➔ **Android 4.3 Jelly Bean** | Google | ✅ |
| 109 | Droidan III (2017) ➔ **Android 8 Oreo** | Google | ✅ |
| 110 | Droidan IV (2021) ➔ **Android 12** | Google | ✅ |
| 111 | Droidan V (2025) ➔ **Android 16** | Google | ✅ |
| 112 | Minisoft Phone I (2012) ➔ **Windows Phone 8** | Microsoft | ✅ |
| 113 | Minisoft Phone II (2016) ➔ **Windows 10 Mobile** | Microsoft | ✅ |
| 114 | Redberry Z5 (2008) ➔ **BlackBerry Bold** | Redberry Ltd. ➔ **Research In Motion** | ✅ |
| 115 | Redberry Z10 (2012) ➔ **BlackBerry Z10** | Research In Motion | ✅ |
| 116 | Redberry Z20 (2016) ➔ **BlackBerry Priv** | BlackBerry Ltd. | ✅ |
| 117 | Sybios 500 (2009) ➔ **Symbian^1** | Nokida ➔ **Nokia** | ✅ |
| 118 | Sybios 800 (2013) ➔ **Symbian^3** | Nokia | ✅ |
| 119–125, 133 | Arcade System 500…3000 | Arcade Electrics | ⚠️ أنظمة عامة؛ اقتراحي: إبقاؤها كما هي |
| 126 | Amigo CTV ➔ **Commodore CDTV** | Commodore | ✅ |
| 127 | PhaserActive ➔ **LaserActive** | Lioneer ➔ **Pioneer** | ✅ |
| 128 | Katari VCC ➔ **Atari VCS** | Atari | ✅ |
| 129 | Beam Deck ➔ **Steam Deck** | Walve ➔ **Valve** | ✅ |
| 130 | Intelvisual Amikos ➔ **Intellivision Amico** | Intellivision Entertainment | ⚠️ أُلغيت فعلياً |
| 131 | Mintendu 3DEX (FEB 2025) ➔ ؟ | Nintendo | ⚠️ خيالية |
| 132 | Siga Play Gear II (NOV 2025) ➔ ؟ | Sega | ⚠️ خيالية |

**أيقونات المنصات:** أسماء ملفات `Icons_Platforms` حقيقية أصلاً (`3DO.png`, `symbian1.png`, ...). المحتوى رسومات اللعبة؛ الاستبدال بصور حقيقية 128×128 يحتاج مصدر صور (انظر القسم 5).

---

## 2. جدول استبدال الشركات (Publisher.txt — 100 شركة)

الدولة من `[COUNTRY]` (0=USA, 1=Japan, 2=UK, 3=China, 5=Germany, 6=France, 7=Canada, 11=Poland, 12=Sweden, 13=Australia, 15=Finland) استُخدمت للتحقق.

| ID | الحالي ➔ الحقيقي | | ID | الحالي ➔ الحقيقي | |
|---:|---|:-:|---:|---|:-:|
| 0 | Akklaim ➔ **Acclaim** | ✅ | 50 | Epix Ware ➔ **Epyx** | ✅ |
| 1 | Akkoload ➔ **Accolade** | ✅ | 51 | Inter Games ➔ **Interplay** | ✅ |
| 2 | Actvision ➔ **Activision** | ✅ | 52 | User Gold ➔ **U.S. Gold** | ✅ |
| 3 | Katari ➔ **Atari** | ✅ | 53 | Virtual ➔ **Virgin Interactive** | ✅ |
| 4 | Panda ➔ **Bandai** | ✅ | 54 | Code Kings ➔ **Codemasters** | ✅ |
| 5 | Cupcoms ➔ **Capcom** | ✅ | 55 | Zensible Studio ➔ **Sensible Software** | ✅ |
| 6 | Electric Arts ➔ **Electronic Arts** | ✅ | 56 | Ellie ➔ **Elite Systems** | ✅ |
| 7 | E-Mix ➔ **Enix** | ✅ | 57 | Middle ➔ **Midway** | ✅ |
| 8 | Kramlin ➔ **Gremlin Graphics** | ✅ | 58 | Sunwork ➔ **Sunsoft** | ✅ |
| 9 | Hutbros ➔ ؟ (WB Games؟) | ⚠️ | 59 | Mono Soft ➔ **Monolith Productions** | ✅ |
| 10 | Infocorp ➔ **Infocom** | ✅ | 60 | Systech-4 ➔ **System 3** | ✅ |
| 11 | iGames ➔ **Infogrames** | ✅ | 61 | RainCode ➔ **Rainbow Arts** | ✅ |
| 12 | Kencom ➔ **Kemco** | ✅ | 62 | Group 18 ➔ **Team17** | ✅ |
| 13 | Coey ➔ **Koei** | ✅ | 63 | Disc Project ➔ **CD Projekt** | ✅ |
| 14 | Comani ➔ **Konami** | ✅ | 64 | ZZI Games ➔ **SSI** | ✅ |
| 15 | Microarts ➔ **MicroProse** | ✅ | 65 | TLO ➔ **THQ** | ✅ |
| 16 | Minisoft ➔ **Microsoft** | ✅ | 66 | Hutsin ➔ **Hudson Soft** | ✅ |
| 17 | Nanko ➔ **Namco** | ✅ | 67 | Apache (JUL 2008) ➔ ؟ (Apple/App Store؟) | ⚠️ |
| 18 | Mintendu ➔ **Nintendo** | ✅ | 68 | Brobound ➔ **Brøderbund** | ✅ |
| 19 | Blue Ocean ➔ **Ocean Software** | ✅ | 69 | Green Byte ➔ **Blue Byte** | ✅ |
| 20 | Origo ➔ **Origin Systems** | ✅ | 70 | Galaxis ➔ ؟ | ⚠️ |
| 21 | Pigsys ➔ **Psygnosis** | ✅ | 71 | Tenzent ➔ **Tencent** | ✅ |
| 22 | Rore ➔ **Rare** | ✅ | 72 | Kung Entertainment ➔ **King** | ⚠️ محتمل |
| 23 | Siga ➔ **Sega** | ✅ | 73 | HungGo Online ➔ **GungHo Online Entertainment** | ✅ |
| 24 | Ziera ➔ **Sierra On-Line** | ✅ | 74 | Superbell ➔ **Supercell** | ✅ |
| 25 | Pony ➔ **Sony** | ✅ | 75 | Gigantic ➔ ؟ | ⚠️ |
| 26 | Teitu ➔ **Taito** | ✅ | 76 | Gameforce ➔ **Gameforge** | ✅ |
| 27 | Unisoft ➔ **Ubisoft** | ✅ | 77 | Fullbrick Studios ➔ **Halfbrick Studios** | ✅ |
| 28 | 3D Reality ➔ **3D Realms** | ✅ | 78 | Moyan Studios ➔ **Mojang** | ⚠️ محتمل (السويد) |
| 29 | Quantum Dream ➔ **Quantic Dream** | ✅ | 79 | Zigna ➔ **Zynga** | ✅ |
| 30 | Romady ➔ **Remedy Entertainment** | ✅ | 80 | Maxus ➔ **Maxis** | ✅ |
| 31 | Blockstar ➔ **Rockstar Games** | ✅ | 81 | Code Design ➔ **DMA Design** | ✅ |
| 32 | Sir-Code ➔ **Sir-Tech** | ✅ | 82 | BioSoft ➔ **BioWare** | ✅ |
| 33 | Tech Soft ➔ **Tecnosoft** | ⚠️ محتمل | 83 | Beta Softworks ➔ **Bethesda Softworks** | ✅ |
| 34 | Droid Games ➔ ؟ (Troika Games؟) | ⚠️ | 84 | TEKMO ➔ **Tecmo** | ✅ |
| 35 | Eastwood Studios ➔ **Westwood Studios** | ✅ | 85 | Walve ➔ **Valve** | ✅ |
| 36 | BMP Bros ➔ **The Bitmap Brothers** | ✅ | 86 | Lukas Games ➔ **LucasArts** | ✅ |
| 37 | Green Isle ➔ **Black Isle Studios** | ✅ | 87 | Take 3 Software ➔ **Take-Two Interactive** | ✅ |
| 38 | Blizzer Studios ➔ **Blizzard Entertainment** | ✅ | 88 | Squall ➔ **Square** | ✅ |
| 39 | Frog Studios ➔ **Bullfrog Productions** | ✅ | 89 | Parodax Interactive ➔ **Paradox Interactive** | ✅ |
| 40 | Comgie ➔ **Bungie** | ✅ | 90 | SMK Corporation ➔ **SNK** | ✅ |
| 41 | Tripple House ➔ ؟ | ⚠️ | 91 | Gearsoft ➔ **Gearbox Software** | ✅ |
| 42 | Erik Games ➔ **Epic Games** | ✅ | 92 | Micronis ➔ **Microïds** | ✅ |
| 43 | it Studios ➔ **id Software** | ✅ | 93 | Crystal Dynamo ➔ **Crystal Dynamics** | ✅ |
| 44 | Lion Works ➔ **Lionhead Studios** | ✅ | 94 | Avalance Games ➔ **Avalanche Software** | ✅ |
| 45 | Sunglasses Studios ➔ **Looking Glass Studios** | ✅ | 95 | Cinemasoft ➔ **Cinemaware** | ✅ |
| 46 | Naughty Bear ➔ **Naughty Dog** | ✅ | 96 | Adventure Tools ➔ **Adventure Soft** | ⚠️ محتمل |
| 47 | Lava Logic ➔ ؟ | ⚠️ | 97 | Neon Storm ➔ **Ion Storm** | ✅ |
| 48 | Form Studios ➔ **FromSoftware** | ✅ | 98 | Thalan Software ➔ **Thalion Software** | ✅ |
| 49 | Data West ➔ **Data East** | ✅ | 99 | Mindscale ➔ **Mindscape** | ✅ |

**ملاحظة:** الشعار لكل شركة في `CompanyLogos\<PIC>.png` (مثلاً Sony = `26.png`، Valve = `182.png`).

### 2.1 أسماء أخرى محرّفة (اقتراح — ضمن النطاق لكن أولوية أقل)
| الملف | الحالي ➔ الحقيقي |
|---|---|
| `Achivements_AR/EN` | Hurrican ➔ **Hurricane**، Dr. Lario ➔ **Dr. Mario**، Super Lario World ➔ **Super Mario World**، The Island of Monkeys ➔ **Monkey Island**، Dungeons and Masters ➔ **Dungeon Master**، The Civilizations ➔ **Civilization**، Spaceship: Elite ➔ **Elite** (وباقي الإنجازات بنفس النمط — 72 سطراً) |
| `NpcGames.txt` (أكثر من 1000 عنوان) | عناوين ألعاب منافسة خيالية — **خارج النطاق المقترح حالياً** (حجم كبير، كثير منها عناوين عامة غير محرّفة). |
| `NpcEngines`, `AntiCheat`, `CopyProtect` | أسماء عامة غير محرّفة عن منتج حقيقي محدد (مثل "Kernel Anti Cheat") — لا تغيير مقترح. |

---

## 3. أخطاء النصوص العربية (Text\AR)

> الأرقام = رقم السطر في الملف. التصحيح مكتوب بالعربية العادية؛ سيُحوَّل آلياً للصيغة التي تقرؤها اللعبة (T3).

### 3.1 كلمات لاتينية معكوسة (RTL Bugs)
| الملف:السطر | المعيوب | الصحيح (نص عربي مقترح) |
|---|---|---|
| Text:327 | `reyalpitluM enilnO` و `OMM` و `P2F` | الرجاء تحديد نوع اللعبة. بالنسبة لألعاب MMO و F2P، فإن ميزة "اللعب الجماعي عبر الإنترنت" مطلوبة. |
| Text:681 | `OMM` | قائمة اشتراكات ألعاب MMO |
| Text:883 | `OMM` (مرتين) | **تم فتح ألعاب MMO وغرفة الخوادم** — يمكنك الآن تطوير ألعاب MMO وبناء غرفة الخوادم اللازمة. |
| Text:956 | `nOddA` | لأي لعبة تريد تطوير إضافة (AddOn) مدفوعة؟ |
| Text:1080 | `KDS` | تلقّى المبرمجون أحدث حزمة SDK وقاموا بتحديثها فوراً، وبذلك تم إصلاح بعض المشاكل التقنية. |
| Text:1122 | `Edition Collectors` (ترتيب كلمات معكوس) | يجب أن يحتوي إصدار المقتنين على محتوى أكثر من الإصدار الأصغر. |
| Text:1206 | `Access. Early` | وضع اللعب الجماعي ما زال في مرحلة مبكرة. بعض الوظائف المهمة مفقودة وقد ينقطع الاتصال. |
| Text:1921 | `"Do not show "Mad Games Convention` (علامة تنصيص في غير مكانها + غير مترجم) | عدم إظهار "Mad Games Convention" |
| Text:1922 | `"Do not show "Mad Games Awards` | عدم إظهار "Mad Games Awards" |

### 3.2 أرقام معكوسة
| الملف:السطر | المعيوب ➔ الصحيح |
|---|---|
| Objects:132 (آلة القهوة) | `71` ساعة ➔ **17** ساعة |
| Quotes:18 | `16` ➔ **61** |
| Review:190 | `59` لعبة ➔ **95** لعبة |

### 3.3 متغيرات في غير مكانها (الاسم يظهر قبل/بعد الجملة الخطأ) — 14 سطراً
مثال: `اكتملت <NAME1> حملة تسويقية لـ` بدلاً من `اكتملت حملة تسويقية لـ <NAME1>`.

| السطر | التصحيح |
|---|---|
| 530 | اكتملت حملة تسويقية لـ `<NAME1>` |
| 669 | رسائل المعجبين لـ `<b><NAME></b>` |
| 843 | تم إلغاء تحديث لـ `<NAME>`<br>لأن اللعبة سُحبت من السوق. |
| 910 | وصلتنا لعبة حصرية جديدة لـ `<NAME>`. لنرَ إن كانت تستحق الشراء. |
| 916 | اكتمل تحسين أسلوب اللعب لـ `<NAME1>` |
| 919 | اكتمل تحسين الرسومات لـ `<NAME1>` |
| 922 | اكتمل تحسين الصوت لـ `<NAME1>` |
| 924 | اكتمل تسجيل التقاط الحركة لـ `<NAME1>` |
| 1138 | اكتمل الإنتاج لـ `<NAME1>` |
| 1193 | لا يمكن تخفيض التمويل لـ `<color=orange>ألعاب العقود</color>`. |
| 1315 | **لعبة حصرية**<br>بفضل هذه اللعبة، تم بيع ما يقدَّر بـ `<color=blue><NUM> <NAME></color>`. |
| 1376 | لا يمكنك تفعيل الشراء داخل التطبيق لـ `<color=orange>ألعاب العقود</color>`. |
| 1426 | اكتملت حملة تسويقية خاصة لـ `<NAME1>` |
| 1492 | اكتمل تحديث F2P لـ `<NAME>`. |
| 503 | قام المطوّر `<color=blue><NAME1></color>` بترخيص محرّكك لـ `<color=blue><NAME2></color>`. تم إرسال الفاتورة إليك للتو. |
| 831 | **متغير مفقود:** `<NUM> نقاط مهارة متبقية` (النص العربي حذف `<NUM>`) |

### 3.4 أخطاء إملائية/صياغة متكررة
| المعيوب | الصحيح | أمثلة |
|---|---|---|
| إكتمل / إكتملت | **اكتمل / اكتملت** (همزة وصل) | 916, 919, 922, 924, 1138, 530, 1426 |
| ابحاث | **أبحاث** | Text:21 (اسم الغرفة) |
| تاكيد الجودة | **تأكيد الجودة** | Text:22 |
| ستوديو جرافيك / استوديو | توحيد: **استوديو الرسومات** / **استوديو الصوت** | Text:23-24 |
| `Mad Games Tycoon ء2` و `ء.الأخطاء` (همزة شاردة) + "شكرا لشرا" | شكراً لشراء `Mad Games Tycoon 2` ... | Text:839 |
| بالإضافة إلى ، (جملة مبتورة) | بالإضافة إلى ذلك، تزداد نقاط أسلوب اللعب والرسومات والتقنية والصوت. | Text:9 |
| `<br>` يقسم الجملة فيظهر النصف الثاني قبل الأول | إعادة ترتيب الأسطر | Text:66-69 (`left shift key`) |

### 3.5 نصوص غير مترجمة (إنجليزية بالكامل داخل الملف العربي)
| الملف | عدد الأسطر | التفاصيل |
|---|---:|---|
| **Text_AR** | **~931** | كتلة كاملة من السطر **1535 إلى 2450** (ميزات التحديثات الأحدث: Spin-off، Porting، تطوير الكونسول، البحث عن موظفين، الاستحواذ على الشركات، الميرتش، اللعب الجماعي، القوائم المالية...) + أسطر متفرقة: 42, 57, 114 (ألماني!: "Lagerplatz Beschreibung"), 617, 934, 1062-1063, 1089-1090 |
| **Achivements_AR** | 72 (كامل الملف) | الملف نسخة إنجليزية |
| **Objects_AR** | 40 | 150-189 (طاولات، كراسي، لوحات، مكيف، مرحاض فاخر...) |
| **Tutorial_AR** | 19 من 19 | الدرس التعليمي كاملاً بالإنجليزية |
| **Country_AR** | 23 | 42-64 (South Africa ➔ جنوب أفريقيا، Iran ➔ إيران، ... Croatia ➔ كرواتيا) |
| **ContractWork_AR** | 6 | 75-80 (مهام إصلاح/تصميم الكونسول) |
| **Themes_AR** | 2 | Cyberpunk ➔ **سايبربانك**، Steampunk ➔ **ستيمبانك** |
| **Quotes_AR** | 1 | 12: M-M-M-Monsterkill! |
| **Review_AR** | ~100 | أسطر إنجليزية + 30 سطراً فارغاً |
| بقايا داخل جمل عربية | 69 | `left shift key` ➔ مفتاح Shift الأيسر، `DEL key` ➔ مفتاح Delete، `Press ... to select all employees` ➔ اضغط ... لتحديد كل الموظفين، `Max.` ➔ الحد الأقصى |

**مصطلحات تبقى بالإنجليزية عمداً** (معتمدة عند اللاعب العربي): MMO, F2P, GOTY, SDK, NPC, VSync, SSAO, AA/AAA, Mad Games Convention/Awards, MGSR, Steam.
**مصطلحات تُعرَّب:** GFX ➔ رسومات، SFX ➔ مؤثرات صوتية، Early Access ➔ الوصول المبكر، Deluxe Edition ➔ الإصدار الفاخر، Collectors Edition ➔ إصدار المقتنين، AddOn ➔ إضافة، Spin-off ➔ لعبة جانبية، Remaster ➔ نسخة مُحسَّنة (Remaster).

### 3.6 مشاكل محاذاة الملفات
| الملف | المشكلة | الأثر |
|---|---|---|
| Quotes_AR | 82 سطراً مقابل 80 في EN — **سطران إضافيان من السطر 75** فيُزاح كل اقتباس بعده | اقتباسات خاطئة (الاقتباس العربي لا يطابق الإنجليزي) |
| Review_AR | الكتلة 19 (السطر 334) ناقصة سطرين، الكتلة 21 ناقصة سطراً، الكتلتان 26 و40 زائدتان سطراً | تعليقات مراجعات في غير فئتها (مثلاً تعليق "رسومات سيئة" ضمن فئة أخرى) |

---

## 4. أخطاء أخرى مكتشفة في اللعبة
| # | الخطأ | المعالجة |
|---|---|---|
| B1 | `Text_EN:1122` نفسه يقول "Collectors Edition **edition**" (خطأ أصلي) | تصحيح النص العربي فقط (لا نعدّل EN) |
| B2 | `Objects_AR:180-181` النص الإنجليزي فيه بقايا ألمانية `...offices.eiten.` / `.beiten.` | تُترجم ضمن 3.5 |
| B3 | `Text_AR:2156` "Ttechnologies" خطأ مطبعي | تُترجم ضمن 3.5 |
| B4 | الحروف المشكّلة (Shaping) — فحص آلي لكل الملفات: **سليمة**، لا توجد حروف بشكل خاطئ (الاستثناء الوحيد: التطويل `ـ` وهو مقصود) | لا شيء |

---

## 5. خطة الحقن المقترحة (بعد موافقتك فقط)
1. نسخة احتياطية كاملة لـ `Extern\Text` و`Extern\CompanyLogos` و`Extern\Icons_Platforms` إلى `E:\mad games tycoon 2 mods\backup\`.
2. سكربت Python صغير (في مجلد المشروع) يطبّق: جداول الأسماء (1، 2) على كل مفاتيح `[NAME xx]`/`[MANUFACTURER xx]`، وتصحيحات القسم 3 بتحويل العربي تلقائياً للصيغة المعكوسة/المشكّلة، مع فحص ذاتي (عدد الأسطر، تطابق المتغيرات، عدم وجود لاتيني معكوس).
3. ترجمة الكتلة الكبيرة (~931 سطراً + Achievements + Tutorial + Objects) كمرحلة منفصلة — أعرضها عليك في ملف مراجعة قبل الحقن.
4. الشعارات: أحتاج منك **مصدر الصور** (مجلد صور تضعه أنت، أو موافقة صريحة على تنزيلها). الشعارات الحقيقية علامات تجارية — مناسبة للاستخدام الشخصي، لكن نشرها على Steam Workshop قد يُحذف.
5. اختبار: تشغيل اللعبة بالعربية والتأكد من القوائم.

## 6. قرارات مطلوبة منك
1. **المنصات الخيالية/المستقبلية** (⚠️: 28, 34, 62, 67, 69, 70, 72, 95, 96, 119-125, 130-133): إبقاء الاسم الخيالي، أم اسم حقيقي مقترح، أم اسم من اختيارك؟
2. **الشركات غير المؤكدة** (⚠️: 9, 33, 34, 41, 47, 67, 70, 72, 75, 78, 96): اعتماد المقترح أم تحديد بديل؟
3. **التواريخ:** هل نصحح تواريخ الإصدار لتطابق الواقع (مثلاً Switch 2 = JUN 2025)؟ هذا يغيّر توازن اللعب قليلاً — حالياً لم أدرجه لأنه خارج "الأسماء".
4. **الشعارات:** من أين نأخذ الصور؟
5. **الموافقة على الحقن** للبنود 1-3.

---
*ملف البيانات الخام (كل الملاحظات سطراً بسطر):* `audit_data/findings.tsv`

---

## 7. ما تم تطبيقه بعد الموافقة (2026-10-21)

**القرارات المعتمدة من المستخدم:** المنصات المستقبلية بأسماء متوقعة، المنصات الخيالية القديمة ← أجهزة حقيقية نادرة، الشركات غير المؤكدة ← شركات حقيقية مشهورة، تصحيح تواريخ الإصدار، وتنزيل الشعارات الرسمية.

### 7.1 المنصات الخيالية/المستقبلية — الأسماء النهائية
| ID | قبل | بعد | التاريخ |
|---:|---|---|---|
| 28 | Mintendu Zwitch Pro | **Nintendo Switch 2** | JUN 2025 |
| 131 | Mintendu 3DEX (محمول) | **Nintendo Switch Lite** — بدل Switch 2 لأن الاسم مستخدم في 28، ولا تسمح اللعبة باسمين متطابقين | SEP 2019 |
| 70 | Mintendu MX | **Nintendo Switch 3** (اسم متوقع) | JAN 2030 (كما هو) |
| 95 | Pony Gamestation 6 | **PlayStation 6** | DEC 2028 (كما هو) |
| 96 | Minisoft YBox SX-II | **Xbox Next** | NOV 2028 (كما هو) |
| 34 | Katari Puma 2 | **Atari 2600+** | NOV 2023 |
| 62 | Asia Box | **Evercade VS** (Blaze Entertainment) | DEC 2021 |
| 67 | Siga MegaCast | **Sega Genesis Mini** | SEP 2019 |
| 69 | Commander Game System II | **THEC64 Mini** (Retro Games Ltd.) | MAR 2018 |
| 72 | Fusion F1000 | **NES Classic Edition** | NOV 2016 |
| 132 | Siga Play Gear II | **Game Gear Micro** | OCT 2020 |
| 130 | Intelvisual Amikos | **Intellivision Amico** | كما هو |
| 119–125، 133 | Arcade System 500…3000 | **بدون تغيير** — أسماء عامة وليست تحريفاً لمنتج حقيقي | — |

### 7.2 الشركات غير المؤكدة — الأسماء النهائية
9 ➔ Warner Bros. Games · 34 ➔ Troika Games · 41 ➔ Obsidian Entertainment · 47 ➔ Ensemble Studios · 67 ➔ PlatinumGames (الدولة: اليابان) · 70 ➔ Treyarch · 75 ➔ Rovio Entertainment (الدولة: فنلندا) · 72 ➔ King · 78 ➔ Mojang · 33 ➔ Tecnosoft · 96 ➔ Adventure Soft.
تواريخ تأسيس الشركات **لم تُغيَّر**، لأن تغييرها يؤثر على توازن اللعب. التصحيح شمل تواريخ إصدار المنصات فقط.

### 7.3 تواريخ الإصدار المصححة (منصات)
Atari 5200 ➔ NOV 1982 · Wii U ➔ NOV 2012 · Apple Pippin ➔ MAR 1996 · Atari Lynx ➔ SEP 1989 · Amstrad CPC ➔ APR 1984 · ZX80 ➔ FEB 1980 · Philips CD-i ➔ DEC 1991 · LaserActive ➔ AUG 1993 · iPhone (2007/2011/2015/2019/2023) · Android (Cupcake APR 2009، Jelly Bean JUL 2013، Oreo AUG 2017، 12 OCT 2021، 16 JUN 2025) · Windows Phone 8 OCT 2012 · Windows 10 Mobile NOV 2015 · BlackBerry (AUG 2008، JAN 2013، NOV 2015) · Symbian (NOV 2008، OCT 2010).
أغلب التواريخ الأصلية كانت صحيحة أصلاً (مثل PS1 DEC 1994، PS2 MAR 2000، PS5 NOV 2020، Switch MAR 2017). تواريخ منصات البداية (Apple I، Fairchild) بقيت JAN 1976 لأن اللعبة تتطلب ذلك.

### 7.4 ملاحظة حول ملف Themes
فحصت ادعاء الإزاحة في `Themes`: ملف EN يبدأ بـ 14 سطر تعليق ولا توجد هذه الأسطر في AR، لذلك الترتيب **متطابق 1:1** (303 موضوع). المشكلة الفعلية كانت 90 موضوعاً غير مترجم، وتمت ترجمتها كلها. الإزاحة الحقيقية كانت في `Quotes` (سطران زائدان، وتم حذفهما) و`Review` (4 كتل، أعيدت ترجمتها سطراً بسطر). ملف `Review` يُقرأ حسب الوسوم (`[GRAPHIC_50]`...) وليس حسب رقم السطر، لذلك لم تكن هناك جمل في غير فئتها.

### 7.5 أخطاء إضافية اكتُشفت أثناء التنفيذ
- 4 أسطر في Text_AR (1322-1325) كانت مخزّنة **بدون تشكيل وبدون عكس**، فتظهر مقلوبة ومفككة. تم إصلاحها.
- مصطلحات خاطئة في الواجهة تم توحيدها: Close = "قريب" ➔ **إغلاق**، Console = "وحدة التحكم" ➔ **الكونسول**، Fans = "المشجعين" ➔ **المعجبين**، Game Pass = "ممر اللعبة" ➔ **Game Pass**، Mad Games = "حوت الألعاب" ➔ **Mad Games**، Controls = "ضوابط" ➔ **التحكم**.
- 25 سطراً عربياً طويلاً أصلياً (أكثر من 120 حرفاً) قد تُلفّ في اللعبة بترتيب أسطر خاطئ. **لم تُعدَّل**، لأنها تحتاج إعادة صياغة يدوية. الأسطر الجديدة تُلفّ تلقائياً عند 65 حرفاً.
