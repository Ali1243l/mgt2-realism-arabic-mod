using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Ali.Mgt2.Shared
{
    /// <summary>
    /// Runtime Arabic Reshaper and BiDi reverser for Unity UI Text rendering.
    /// Safely reshapes raw Arabic letters into presentation forms (U+FE70..U+FEFF),
    /// handles Lam-Alif ligatures, mirrors brackets, and reverses visual runs while
    /// keeping Latin words, numbers, currencies ($), and Unity rich-text tags intact.
    /// </summary>
    public static class ArabicFixer
    {
        private static readonly Regex TokenRegex = new Regex(@"</?(?:b|i|color|size)(?:=[^<>]*)?>|<[^<>]{1,30}>", RegexOptions.IgnoreCase);
        private static readonly Regex NumExprRegex = new Regex(@"[+\-]?\$?(?:\d[\d.,]*\d|\d)%?", RegexOptions.Compiled);
        private static readonly Regex LatinWordRegex = new Regex(@"[A-Za-z0-9_@#]+(?:\s+[A-Za-z0-9_@#]+)*", RegexOptions.Compiled);

        // Forms: Isolated, Final, Initial, Medial
        private struct GlyphForms
        {
            public char Isolated;
            public char Final;
            public char Initial;
            public char Medial;
            public bool CanConnectLeft;

            public GlyphForms(int iso, int fin, int ini, int med, bool left)
            {
                Isolated = (char)iso;
                Final = (char)fin;
                Initial = (char)ini;
                Medial = (char)med;
                CanConnectLeft = left;
            }
        }

        private static readonly Dictionary<char, GlyphForms> Forms = new Dictionary<char, GlyphForms>
        {
            { 'ء', new GlyphForms(0xFE80, 0xFE80, 0xFE80, 0xFE80, false) },
            { 'آ', new GlyphForms(0xFE81, 0xFE82, 0xFE81, 0xFE82, false) },
            { 'أ', new GlyphForms(0xFE83, 0xFE84, 0xFE83, 0xFE84, false) },
            { 'ؤ', new GlyphForms(0xFE85, 0xFE86, 0xFE85, 0xFE86, false) },
            { 'إ', new GlyphForms(0xFE87, 0xFE88, 0xFE87, 0xFE88, false) },
            { 'ئ', new GlyphForms(0xFE89, 0xFE8A, 0xFE8B, 0xFE8C, true) },
            { 'ا', new GlyphForms(0xFE8D, 0xFE8E, 0xFE8D, 0xFE8E, false) },
            { 'ب', new GlyphForms(0xFE8F, 0xFE90, 0xFE91, 0xFE92, true) },
            { 'ة', new GlyphForms(0xFE93, 0xFE94, 0xFE93, 0xFE94, false) },
            { 'ت', new GlyphForms(0xFE95, 0xFE96, 0xFE97, 0xFE98, true) },
            { 'ث', new GlyphForms(0xFE99, 0xFE9A, 0xFE9B, 0xFE9C, true) },
            { 'ج', new GlyphForms(0xFE9D, 0xFE9E, 0xFE9F, 0xFEA0, true) },
            { 'ح', new GlyphForms(0xFEA1, 0xFEA2, 0xFEA3, 0xFEA4, true) },
            { 'خ', new GlyphForms(0xFEA5, 0xFEA6, 0xFEA7, 0xFEA8, true) },
            { 'د', new GlyphForms(0xFEA9, 0xFEAA, 0xFEA9, 0xFEAA, false) },
            { 'ذ', new GlyphForms(0xFEAB, 0xFEAC, 0xFEAB, 0xFEAC, false) },
            { 'ر', new GlyphForms(0xFEAD, 0xFEAE, 0xFEAD, 0xFEAE, false) },
            { 'ز', new GlyphForms(0xFEAF, 0xFEB0, 0xFEAF, 0xFEB0, false) },
            { 'س', new GlyphForms(0xFEB1, 0xFEB2, 0xFEB3, 0xFEB4, true) },
            { 'ش', new GlyphForms(0xFEB5, 0xFEB6, 0xFEB7, 0xFEB8, true) },
            { 'ص', new GlyphForms(0xFEB9, 0xFEBA, 0xFEBB, 0xFEBC, true) },
            { 'ض', new GlyphForms(0xFEBD, 0xFEBE, 0xFEBF, 0xFEC0, true) },
            { 'ط', new GlyphForms(0xFEC1, 0xFEC2, 0xFEC3, 0xFEC4, true) },
            { 'ظ', new GlyphForms(0xFEC5, 0xFEC6, 0xFEC7, 0xFEC8, true) },
            { 'ع', new GlyphForms(0xFEC9, 0xFECA, 0xFECB, 0xFECC, true) },
            { 'غ', new GlyphForms(0xFECD, 0xFECE, 0xFECF, 0xFED0, true) },
            { 'ف', new GlyphForms(0xFED1, 0xFED2, 0xFED3, 0xFED4, true) },
            { 'ق', new GlyphForms(0xFED5, 0xFED6, 0xFED7, 0xFED8, true) },
            { 'ك', new GlyphForms(0xFED9, 0xFEDA, 0xFEDB, 0xFEDC, true) },
            { 'ل', new GlyphForms(0xFEDD, 0xFEDE, 0xFEDF, 0xFEE0, true) },
            { 'م', new GlyphForms(0xFEE1, 0xFEE2, 0xFEE3, 0xFEE4, true) },
            { 'ن', new GlyphForms(0xFEE5, 0xFEE6, 0xFEE7, 0xFEE8, true) },
            { 'ه', new GlyphForms(0xFEE9, 0xFEEA, 0xFEEB, 0xFEEC, true) },
            { 'و', new GlyphForms(0xFEED, 0xFEEE, 0xFEED, 0xFEEE, false) },
            { 'ى', new GlyphForms(0xFEEF, 0xFEF0, 0x0649, 0x0649, false) },
            { 'ي', new GlyphForms(0xFEF1, 0xFEF2, 0xFEF3, 0xFEF4, true) }
        };

        public static bool IsAlreadyVisualArabic(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c >= 0xFE70 && c <= 0xFEFF) return true;
            }
            return false;
        }

        public static bool HasLogicalArabic(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c >= 0x0621 && c <= 0x064A) return true;
            }
            return false;
        }

        public static string Fix(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            if (IsAlreadyVisualArabic(input)) return input;
            if (!HasLogicalArabic(input)) return input;

            // Split into lines to preserve line breaks
            string[] lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                if (HasLogicalArabic(lines[i]))
                {
                    lines[i] = FixLine(lines[i]);
                }
            }
            return string.Join("\n", lines);
        }

        private static string FixLine(string line)
        {
            if (string.IsNullOrEmpty(line) || !HasLogicalArabic(line)) return line;

            // Step 1: Tokenize Unity formatting tags and LTR tokens (numbers/currencies/Latin words)
            var tokens = new List<string>();
            var placeholderMap = new Dictionary<string, string>();
            int tokenIndex = 0;

            // Replace RichText tags
            string processed = TokenRegex.Replace(line, m =>
            {
                string ph = $"\uE000{tokenIndex++}\uE001";
                placeholderMap[ph] = m.Value;
                return ph;
            });

            // Replace numeric/currency expressions (e.g. $25,000,000 or +25% or 92%)
            processed = NumExprRegex.Replace(processed, m =>
            {
                string val = m.Value;
                if (string.IsNullOrEmpty(val)) return val;
                string ph = $"\uE000{tokenIndex++}\uE001";
                placeholderMap[ph] = val;
                return ph;
            });

            // Step 2: Reshape Arabic characters in the string
            string reshaped = ReshapeArabic(processed);

            // Step 3: BiDi reverse the characters
            string reversed = ReverseVisual(reshaped);

            // Step 4: Restore tokens
            foreach (var kvp in placeholderMap)
            {
                reversed = reversed.Replace(kvp.Key, kvp.Value);
            }

            return reversed;
        }

        private static string ReshapeArabic(string text)
        {
            var sb = new StringBuilder(text.Length);
            int len = text.Length;

            for (int i = 0; i < len; i++)
            {
                char c = text[i];

                // Check for Lam-Alif ligatures
                if (c == 'ل' && i + 1 < len)
                {
                    char next = text[i + 1];
                    char ligature = '\0';
                    bool prevConnected = i > 0 && CanConnectNext(text[i - 1]);

                    if (next == 'آ') ligature = prevConnected ? (char)0xFEF6 : (char)0xFEF5;
                    else if (next == 'أ') ligature = prevConnected ? (char)0xFEF8 : (char)0xFEF7;
                    else if (next == 'إ') ligature = prevConnected ? (char)0xFEFA : (char)0xFEF9;
                    else if (next == 'ا') ligature = prevConnected ? (char)0xFEFC : (char)0xFEFB;

                    if (ligature != '\0')
                    {
                        sb.Append(ligature);
                        i++; // skip next
                        continue;
                    }
                }

                if (!Forms.TryGetValue(c, out GlyphForms gf))
                {
                    sb.Append(c);
                    continue;
                }

                bool prevConnects = i > 0 && CanConnectNext(text[i - 1]);
                bool nextConnects = i + 1 < len && CanConnectPrev(text[i + 1]);

                if (prevConnects && nextConnects && gf.CanConnectLeft)
                {
                    sb.Append(gf.Medial);
                }
                else if (prevConnects)
                {
                    sb.Append(gf.Final);
                }
                else if (nextConnects && gf.CanConnectLeft)
                {
                    sb.Append(gf.Initial);
                }
                else
                {
                    sb.Append(gf.Isolated);
                }
            }

            return sb.ToString();
        }

        private static bool CanConnectNext(char c)
        {
            if (Forms.TryGetValue(c, out GlyphForms gf))
            {
                return gf.CanConnectLeft;
            }
            return false;
        }

        private static bool CanConnectPrev(char c)
        {
            return Forms.ContainsKey(c);
        }

        private static string ReverseVisual(string text)
        {
            var chars = text.ToCharArray();
            Array.Reverse(chars);

            // Mirror brackets
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (c == '(') chars[i] = ')';
                else if (c == ')') chars[i] = '(';
                else if (c == '[') chars[i] = ']';
                else if (c == ']') chars[i] = '[';
                else if (c == '{') chars[i] = '}';
                else if (c == '}') chars[i] = '{';
                else if (c == '«') chars[i] = '»';
                else if (c == '»') chars[i] = '«';
            }

            // Restore any placeholder tokens that got reversed (e.g. \uE0010\uE000 -> \uE0000\uE001)
            string reversed = new string(chars);
            var phFixRegex = new Regex(@"\uE001(\d+)\uE000");
            reversed = phFixRegex.Replace(reversed, m =>
            {
                char[] digits = m.Groups[1].Value.ToCharArray();
                Array.Reverse(digits);
                return $"\uE000{new string(digits)}\uE001";
            });

            // Also ensure Latin word runs within Arabic sentences stay readable LTR
            // (e.g. "Galaxy Odyssey" should not be reversed to "yesdydO yxalaG")
            var latinMatches = Regex.Matches(reversed, @"[A-Za-z0-9_@#]+(?:\s+[A-Za-z0-9_@#]+)*");
            var sb = new StringBuilder(reversed);
            // Replace backwards in index order
            for (int i = latinMatches.Count - 1; i >= 0; i--)
            {
                var match = latinMatches[i];
                // Reverse the matched Latin run back to normal LTR order
                char[] wordChars = match.Value.ToCharArray();
                Array.Reverse(wordChars);
                sb.Remove(match.Index, match.Length);
                sb.Insert(match.Index, new string(wordChars));
            }

            return sb.ToString();
        }
    }
}
