using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace ItemSpawnerPlus
{
    // toneless hanzi -> pinyin lookup so Chinese players can search item names by
    // pinyin (the game shows the localized Chinese name, but most people type pinyin,
    // not hanzi). Data is a gzipped embedded blob of "<hanzi><ascii-pinyin>\n" lines,
    // one primary reading per character (Unihan kMandarin, BMP CJK only)
    internal static class PinyinIndex
    {
        private static Dictionary<char, string> _map;
        private static bool _tried;

        private static void EnsureLoaded()
        {
            if (_map != null || _tried) return;
            _tried = true;
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using var raw = asm.GetManifestResourceStream("ItemSpawnerPlus.pinyin.bin");
                if (raw == null) { _map = new Dictionary<char, string>(); return; }
                using var gz = new GZipStream(raw, CompressionMode.Decompress);
                using var reader = new StreamReader(gz, Encoding.UTF8);
                var map = new Dictionary<char, string>(21000);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length < 2) continue;
                    map[line[0]] = line.Substring(1);
                }
                _map = map;
            }
            catch (Exception e)
            {
                Plugin.Instance?.Log?.LogWarning($"PinyinIndex: failed to load table ({e.Message}); pinyin search disabled.");
                _map = new Dictionary<char, string>();
            }
        }

        internal static bool HasHan(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char c in s)
                if (c >= 0x4E00 && c <= 0x9FFF) return true;
            return false;
        }

        // appends the full pinyin, a v->u variant, and the syllable initials of every
        // hanzi in <name> to <blob> (each on its own line so a search only matches
        // within one form)
        internal static void Append(string name, StringBuilder blob)
        {
            if (!HasHan(name)) return;
            EnsureLoaded();
            if (_map.Count == 0) return;

            var full = new StringBuilder(name.Length * 4);
            var initials = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                if (_map.TryGetValue(c, out string py))
                {
                    full.Append(py);
                    initials.Append(py[0]);
                }
                else if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    full.Append(c);
                    initials.Append(c);
                }
            }
            if (full.Length == 0) return;

            string f = full.ToString();
            blob.Append('\n').Append(f);
            if (f.IndexOf('v') >= 0) blob.Append('\n').Append(f.Replace('v', 'u'));
            blob.Append('\n').Append(initials);
        }
    }
}
