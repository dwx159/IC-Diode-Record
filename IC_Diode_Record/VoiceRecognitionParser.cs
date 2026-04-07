using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace IC_Diode_Record
{
    /// <summary>
    /// 语音识别结果解析：跳过命令、OL、阿拉伯/中文整数与小数、K/M 单位、口语习惯（一二三、一百二十三等）。
    /// </summary>
    internal static class VoiceRecognitionParser
    {
        private static readonly Dictionary<char, int> CnDigit = new()
        {
            ['零'] = 0, ['一'] = 1, ['二'] = 2, ['两'] = 2, ['三'] = 3, ['四'] = 4, ['五'] = 5,
            ['六'] = 6, ['七'] = 7, ['八'] = 8, ['九'] = 9,
        };

        public static readonly string[] SkipPhrases =
        {
            "跳过", "跳过1个", "跳过2个", "跳过3个", "跳过4个", "跳过5个",
            "跳过一个", "跳过两个", "跳过三个", "跳过四个", "跳过五个",
        };

        public static readonly string[] MeasurementPhraseHints = CreateMeasurementPhraseHints();

        private static string[] CreateMeasurementPhraseHints()
        {
            var list = new List<string>
            {
                "OL", "ol", "开路", "过载", "超量程", "无穷",
                "K", "k", "M", "千", "兆", "千欧", "兆欧", "欧姆",
                "点", "点儿", "零点",
                "一百", "两百", "三百", "一千", "一百二十三", "两百五十", "一千零一",
                "一二三", "四五六", "一二三四",
                "十一点五", "十二点三四", "一百点五",
            };
            for (int i = 0; i <= 9; i++)
                list.Add(i.ToString(CultureInfo.InvariantCulture));
            for (int t = 1; t <= 9; t++)
                list.Add("0." + t.ToString(CultureInfo.InvariantCulture));
            for (int ab = 10; ab <= 99; ab++)
                list.Add("0." + ab.ToString(CultureInfo.InvariantCulture));
            for (int tenths = 2; tenths <= 9; tenths++)
            {
                list.Add($"0.{tenths}15");
                list.Add($"0.{tenths}50");
            }
            return list.ToArray();
        }

        /// <summary>解析「跳过」：支持阿拉伯、中文整数（含一百二十三、两、二十三）。</summary>
        public static bool TryParseSkipCommand(string recognized, out int count)
        {
            count = 0;
            if (string.IsNullOrWhiteSpace(recognized)) return false;
            var t = recognized.Trim().TrimEnd('。', '！', '!', '.');
            if (!t.Contains("跳过", StringComparison.Ordinal)) return false;

            if (Regex.IsMatch(t, @"^\s*跳过\s*[。！!.…]*\s*$"))
            {
                count = 1;
                return true;
            }

            var mNum = Regex.Match(t, @"跳过\s*(\d{1,4})\s*[个格]?");
            if (mNum.Success && int.TryParse(mNum.Groups[1].Value, out int d) && d >= 1)
            {
                count = d;
                return true;
            }

            var mTail = Regex.Match(t, @"跳过\s*(.+)$");
            if (mTail.Success)
            {
                string inner = mTail.Groups[1].Value.Trim();
                inner = Regex.Replace(inner, @"\s*[个格]\s*$", "").Trim();
                inner = inner.TrimEnd('。', '.', '!', '！');
                if (inner.Length > 0 && TryParseChineseIntegerSpeech(inner, out int cn) && cn >= 1 && cn <= 99_999)
                {
                    count = cn;
                    return true;
                }
            }

            return false;
        }

        /// <summary>识别文本 → 写入值；olOrange 表示 OL 格橙色。</summary>
        public static bool TryParseMeasurement(string recognized, out object? value, out bool olOrange)
        {
            value = null;
            olOrange = false;
            if (string.IsNullOrWhiteSpace(recognized)) return false;

            var trimmed = recognized.Trim().TrimEnd('。', '！', '!', '.', '…');
            if (trimmed.Length > 64) return false;
            if (trimmed.Contains("跳过", StringComparison.Ordinal)) return false;

            string normalized = NormalizeDigitsAndSeparators(trimmed);
            normalized = normalized.Replace("点儿", "点", StringComparison.Ordinal);
            normalized = Regex.Replace(normalized, @"(\d)\s*。\s*(\d)", "$1.$2");
            // 「一百八十 K」常被写成「一百8 10K」「一百8十10K」；「一百八十点一 K」→「一百8十0.1 K」（十与 0.1 之间无点）
            normalized = FixAzureHundredArabicTenKForm(normalized);
            // Azure zh-CN 常把「一点二」写成「1，0.2」（，已规范成 . → 1.0.2）、「1 0.56」去空格会变成 10.56
            normalized = CollapseAzureSplitDecimalParts(normalized);
            string compact = Regex.Replace(normalized, @"\s+", "");
            compact = FixMisplacedUnitBetweenDecimalDigits(compact);
            compact = Regex.Replace(compact, @"(欧姆|歐姆|欧|歐)+$", "");

            if (IsOlUtterance(compact, trimmed))
            {
                value = "OL";
                olOrange = true;
                return true;
            }

            // ① 纯阿拉伯（含小数点），与「12点34」混合
            string mix = Regex.Replace(compact, @"(\d)点(\d+)", "$1.$2");
            if (TryParseArabicMeasurement(mix, out value, out olOrange))
                return true;

            // ② 中文「点」小数：整数部分用语义解析（一百二十三、十二、零）
            if (TryParseChineseDecimalWith点(compact, out value, out olOrange))
                return true;

            // ③ 中文整数 + 可选 K/M/千/兆（无「点」、无阿拉伯小数点）
            if (!compact.Contains('.') && TryParseChineseIntegerWithUnit(compact, out value))
            {
                olOrange = false;
                return true;
            }

            return false;
        }

        private static bool TryParseChineseIntegerWithUnit(string compact, out object? value)
        {
            value = null;
            if (compact.Contains('点', StringComparison.Ordinal)) return false;

            string body = compact;
            string unitRaw = "";
            var um = Regex.Match(compact, @"^(.+)([kKmM千千兆])$");
            if (um.Success)
            {
                body = um.Groups[1].Value;
                unitRaw = um.Groups[2].Value;
            }

            if (!TryParseChineseIntegerSpeech(body, out int iv) || iv < 0 || iv > 9_999_999)
                return false;

            if (string.IsNullOrEmpty(unitRaw))
            {
                value = (double)iv;
                return true;
            }

            string suf = unitRaw is "千" or "k" or "K" ? "K" : "M";
            value = iv.ToString(CultureInfo.InvariantCulture) + suf;
            return true;
        }

        private static bool TryParseArabicMeasurement(string compact, out object? value, out bool olOrange)
        {
            value = null;
            olOrange = false;

            var mDec = Regex.Match(compact, @"^(?<num>-?\d+\.\d+)(?<u>[kKmM千千兆])?$");
            if (mDec.Success &&
                double.TryParse(mDec.Groups["num"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
            {
                if (!mDec.Groups["u"].Success || string.IsNullOrEmpty(mDec.Groups["u"].Value))
                    value = d;
                else
                {
                    string u = mDec.Groups["u"].Value;
                    value = mDec.Groups["num"].Value + (u is "千" or "k" or "K" ? "K" : "M");
                }
                return true;
            }

            var mInt = Regex.Match(compact, @"^(?<num>-?\d+)(?<u>[kKmM千千兆])?$");
            if (mInt.Success &&
                double.TryParse(mInt.Groups["num"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out double di))
            {
                if (!mInt.Groups["u"].Success || string.IsNullOrEmpty(mInt.Groups["u"].Value))
                    value = di;
                else
                {
                    string u = mInt.Groups["u"].Value;
                    value = mInt.Groups["num"].Value + (u is "千" or "k" or "K" ? "K" : "M");
                }
                return true;
            }

            Match? best = null;
            foreach (Match m in Regex.Matches(compact, @"-?\d+\.\d+"))
            {
                if (best == null || m.Length > best.Length)
                    best = m;
            }
            if (best != null &&
                double.TryParse(best.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double ds))
            {
                value = ds;
                return true;
            }

            return false;
        }

        private static bool TryParseChineseDecimalWith点(string compact, out object? value, out bool olOrange)
        {
            value = null;
            olOrange = false;
            if (!compact.Contains('点', StringComparison.Ordinal)) return false;

            var parts = compact.Split(new[] { '点' }, 2);
            if (parts.Length != 2) return false;
            string L = parts[0].Trim();
            string R = parts[1].Trim();
            if (R.Length == 0) return false;

            string rWork = R;
            string unitRaw = "";
            var um = Regex.Match(rWork, @"^(.+?)([kKmM千千兆]+)$");
            if (um.Success)
            {
                rWork = um.Groups[1].Value;
                unitRaw = um.Groups[2].Value;
            }

            // zh-CN 常把英文 1.23 写成「十点二三」
            if (L == "十" && rWork.Length == 2 &&
                TryParseFractionDigits(rWork, out string fracTen) &&
                double.TryParse("1." + fracTen, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                string nc = "1." + fracTen;
                if (string.IsNullOrEmpty(unitRaw))
                    value = double.Parse(nc, CultureInfo.InvariantCulture);
                else
                    value = nc + (unitRaw is "千" or "k" or "K" ? "K" : "M");
                return true;
            }

            int intVal;
            if (string.IsNullOrEmpty(L) || L == "零")
                intVal = 0;
            else if (!TryParseChineseIntegerSpeech(L, out intVal))
                return false;

            if (!TryParseFractionDigits(rWork, out string fracStr))
                return false;

            string intStr = intVal.ToString(CultureInfo.InvariantCulture);
            string numCore = $"{intStr}.{fracStr}";
            if (!double.TryParse(numCore, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                return false;

            if (string.IsNullOrEmpty(unitRaw))
            {
                value = double.Parse(numCore, CultureInfo.InvariantCulture);
                return true;
            }

            value = numCore + (unitRaw is "千" or "k" or "K" ? "K" : "M");
            return true;
        }

        /// <summary>中文或阿拉伯小数位（逐字：二一五→215；或直接 215）。</summary>
        private static bool TryParseFractionDigits(string r, out string fracStr)
        {
            fracStr = "";
            if (string.IsNullOrEmpty(r)) return false;
            if (Regex.IsMatch(r, @"^\d+$"))
            {
                fracStr = r;
                return true;
            }

            var sb = new StringBuilder(r.Length);
            foreach (char c in r)
            {
                if (CnDigit.TryGetValue(c, out int d))
                    sb.Append((char)('0' + d));
                else if (c >= '0' && c <= '9')
                    sb.Append(c);
                else
                    return false;
            }
            fracStr = sb.ToString();
            return fracStr.Length > 0;
        }

        /// <summary>中文整数口语：一二三、一百二十三、两千、九千九百九十九；纯阿拉伯数字串。</summary>
        public static bool TryParseChineseIntegerSpeech(string body, out int value)
        {
            value = 0;
            body = body.Trim();
            if (string.IsNullOrEmpty(body)) return false;

            if (Regex.IsMatch(body, @"^-?\d+$"))
                return int.TryParse(body, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

            try
            {
                value = ParseChineseIntMain(body.AsSpan());
                return value >= 0 && value <= 9_999_999;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>万以下 + 可选「万」段（如 一万、十二万三千）。</summary>
        private static int ParseChineseIntMain(ReadOnlySpan<char> s)
        {
            s = SkipZero(s);
            if (s.IsEmpty) return 0;
            int idx = s.IndexOf('万');
            if (idx >= 0)
            {
                ReadOnlySpan<char> leftWan = s[..idx];
                int wan = leftWan.IsEmpty ? 1 : ParseUpTo9999(leftWan);
                if (wan <= 0) throw new FormatException();
                return checked(wan * 10000 + ParseUpTo9999(s[(idx + 1)..]));
            }
            return ParseUpTo9999(s);
        }

        private static ReadOnlySpan<char> SkipZero(ReadOnlySpan<char> s)
        {
            while (!s.IsEmpty && s[0] == '零')
                s = s[1..];
            return s;
        }

        private static bool TryCnDigit(char c, out int d) => CnDigit.TryGetValue(c, out d);

        private static int ParseMultiplier(ReadOnlySpan<char> s)
        {
            s = SkipZero(s);
            if (s.IsEmpty) return 1;
            if (s.Length == 1 && TryCnDigit(s[0], out int d) && d > 0) return d;
            throw new FormatException();
        }

        private static int ParseDigitOnly(ReadOnlySpan<char> s)
        {
            if (s.Length != 1 || !TryCnDigit(s[0], out int d))
                throw new FormatException();
            return d;
        }

        /// <summary>逐字拼接整数（二三→23），用于无「十百千万」的连读。</summary>
        private static int ParseDigitConcatRun(ReadOnlySpan<char> s)
        {
            int acc = 0;
            foreach (char c in s)
            {
                if (!TryCnDigit(c, out int d))
                    throw new FormatException();
                acc = acc * 10 + d;
            }
            return acc;
        }

        private static int ParseUpTo9999(ReadOnlySpan<char> s)
        {
            s = SkipZero(s);
            if (s.IsEmpty) return 0;

            int idx = s.IndexOf('千');
            if (idx >= 0)
            {
                int k = ParseMultiplier(s[..idx]);
                return k * 1000 + ParseUpTo999(s[(idx + 1)..]);
            }
            return ParseUpTo999(s);
        }

        private static int ParseUpTo999(ReadOnlySpan<char> s)
        {
            s = SkipZero(s);
            if (s.IsEmpty) return 0;

            int idx = s.IndexOf('百');
            if (idx >= 0)
            {
                int h = ParseMultiplier(s[..idx]);
                return h * 100 + ParseUpTo99(s[(idx + 1)..]);
            }
            return ParseUpTo99(s);
        }

        private static int ParseUpTo99(ReadOnlySpan<char> s)
        {
            s = SkipZero(s);
            if (s.IsEmpty) return 0;

            if (s.Length == 1 && TryCnDigit(s[0], out int single))
                return single;

            int idx = s.IndexOf('十');
            if (idx < 0)
                return ParseDigitConcatRun(s);

            if (idx == 0)
            {
                ReadOnlySpan<char> rest = SkipZero(s[1..]);
                if (rest.IsEmpty) return 10;
                if (rest.Length == 1 && TryCnDigit(rest[0], out int u))
                    return 10 + u;
                throw new FormatException();
            }

            if (idx == s.Length - 1)
            {
                int tens = ParseDigitOnly(s[..idx]);
                return tens * 10;
            }

            int hi = ParseDigitOnly(s[..idx]);
            ReadOnlySpan<char> lo = SkipZero(s[(idx + 1)..]);
            if (lo.IsEmpty) return hi * 10;
            if (lo.Length == 1 && TryCnDigit(lo[0], out int u2))
                return hi * 10 + u2;
            throw new FormatException();
        }

        private static string NormalizeDigitsAndSeparators(string s)
        {
            s = s.Replace('\u2212', '-');
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                if (c >= '０' && c <= '９') sb.Append((char)('0' + (c - '０')));
                else if (c == '．' || c == '，') sb.Append('.');
                else sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Azure 把「一百八十」说成「一百8」+「十」+ 多种尾巴：空格+10K、十0.1、十。1、直接兆/千、十1K 等；
        /// 「二百零一」→「两百0，1」或「三百0 1」；「四百零一」→「4百0 1」（逗号可能变点，0 与个位间也常是空格）。
        /// 须在 CollapseAzureSplitDecimalParts 之前调用。
        /// </summary>
        private static string FixAzureHundredArabicTenKForm(string s)
        {
            static string UnitSuffix(string u) => u is "千" or "k" or "K" ? "K" : "M";

            // 三百0 1 / 三百0，1 K → 301K（0 与个位之间可为句读或空格；逗号已可能变成点）
            (string prefix, int hb)[] baiZeroTail =
            {
                ("一百", 100), ("两百", 200), ("二百", 200), ("三百", 300), ("四百", 400),
                ("五百", 500), ("六百", 600), ("七百", 700), ("八百", 800), ("九百", 900),
            };
            foreach (var (prefix, hb) in baiZeroTail)
            {
                string esc = Regex.Escape(prefix);
                s = Regex.Replace(s, esc + @"0(?:[。.]+|\s+)(\d{1,2})\s*([kKmM千千兆])", m =>
                {
                    if (!int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int tail) || tail < 0 || tail > 99)
                        return m.Value;
                    int n = hb + tail;
                    return n.ToString(CultureInfo.InvariantCulture) + UnitSuffix(m.Groups[2].Value);
                });
            }

            // 4百0 1 K、9百0 1 K → 401K、901K（阿拉伯数字 + 百，同上）
            s = Regex.Replace(s, @"([1-9])百0(?:[。.]+|\s+)(\d{1,2})\s*([kKmM千千兆])", m =>
            {
                int hd = m.Groups[1].Value[0] - '0';
                if (!int.TryParse(m.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int tail) || tail < 0 || tail > 99)
                    return m.Value;
                int n = hd * 100 + tail;
                return n.ToString(CultureInfo.InvariantCulture) + UnitSuffix(m.Groups[3].Value);
            });

            // 一百8十0.1 / 一百8十.0.1（与「十。1」区分：此处为小数点+小数位）
            s = Regex.Replace(s, @"一百([1-9])十(?:\.0\.|0\.)(\d+)\s*([kKmM千千兆])?", m =>
            {
                int d = m.Groups[1].Value[0] - '0';
                int intPart = 100 + 10 * d;
                string frac = m.Groups[2].Value;
                string num = intPart.ToString(CultureInfo.InvariantCulture) + "." + frac;
                if (!m.Groups[3].Success || string.IsNullOrEmpty(m.Groups[3].Value))
                    return num;
                return num + UnitSuffix(m.Groups[3].Value);
            });

            // 一百8十。1 K → 181K（句、句号把个位隔开）
            s = Regex.Replace(s, @"一百([1-9])十[。.]+\s*(\d{1,2})\s*([kKmM千千兆])", m =>
            {
                int d = m.Groups[1].Value[0] - '0';
                if (!int.TryParse(m.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int ones) || ones < 0 || ones > 99)
                    return m.Value;
                int n = 100 + 10 * d + ones;
                return n.ToString(CultureInfo.InvariantCulture) + UnitSuffix(m.Groups[3].Value);
            });

            // 一百8十。一 K → 181K（中文个位）
            s = Regex.Replace(s, @"一百([1-9])十[。.]+\s*([一二三四五六七八九])\s*([kKmM千千兆])", m =>
            {
                int d = m.Groups[1].Value[0] - '0';
                if (!CnDigit.TryGetValue(m.Groups[2].Value[0], out int ones) || ones < 1)
                    return m.Value;
                int n = 100 + 10 * d + ones;
                return n.ToString(CultureInfo.InvariantCulture) + UnitSuffix(m.Groups[3].Value);
            });

            // 一百8十1K / 一百8十12K（十后紧跟个位；排除「十10K」那是「八十」拆法，由下一条/最后一条处理）
            s = Regex.Replace(s, @"一百([1-9])十(?![。.]*0\.)(?!10\s*[kKmM千千兆])(\d{1,2})\s*([kKmM千千兆])", m =>
            {
                int d = m.Groups[1].Value[0] - '0';
                if (!int.TryParse(m.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int ones) || ones < 0 || ones > 99)
                    return m.Value;
                int n = 100 + 10 * d + ones;
                return n.ToString(CultureInfo.InvariantCulture) + UnitSuffix(m.Groups[3].Value);
            });

            // 一百8十兆 / 一百8十 K → 180M / 180K（整十后仅单位）
            s = Regex.Replace(s, @"一百([1-9])十\s*([kKmM千千兆])", m =>
            {
                int d = m.Groups[1].Value[0] - '0';
                int n = 100 + 10 * d;
                return n.ToString(CultureInfo.InvariantCulture) + UnitSuffix(m.Groups[2].Value);
            });

            // 一百8 10K / 一百8十10K（「八十」拆成 8 + 10）
            s = Regex.Replace(s, @"一百([1-9])(?:\s+10(?![0-9])|十\s*10(?![0-9]))\s*([kKmM千千兆])", m =>
            {
                int d = m.Groups[1].Value[0] - '0';
                int n = 100 + 10 * d;
                return n.ToString(CultureInfo.InvariantCulture) + UnitSuffix(m.Groups[2].Value);
            });

            return s;
        }

        /// <summary>
        /// 合并 Azure 中文语音识别拆分的小数：「1 0.56」→ 1.56；「1，0.2」→ 1.0.2 → 1.2；
        /// 「十，0.1兆」「2十，0.1兆」→ 10.1 / 20.1 再带单位（左侧为中文/混写时须先展开）。
        /// 须在去除空白之前调用。
        /// </summary>
        private static string CollapseAzureSplitDecimalParts(string s)
        {
            s = CollapseAzureChineseTensBeforeZeroDecimal(s);

            while (true)
            {
                string n = Regex.Replace(s, @"(\d+)\s+0\.(\d+)", "$1.$2");
                if (n == s) break;
                s = n;
            }
            while (true)
            {
                string n = Regex.Replace(s, @"(\d+)\.0\.(\d+)", "$1.$2");
                if (n == s) break;
                s = n;
            }
            return s;
        }

        /// <summary>
        /// 「十点一兆」常被识别成「十.0.1兆」或「2十.0.1兆」；须先于纯数字的 .0. 合并处理，否则会误匹配子串 0.1。
        /// 顺序：整十中文（二十…九十）→ 阿拉伯+十 → 单独的十，避免「二十」被当成「二」+「十.0.」。
        /// </summary>
        private static string CollapseAzureChineseTensBeforeZeroDecimal(string s)
        {
            string[] cnTens = { "九十", "八十", "七十", "六十", "五十", "四十", "三十", "二十" };
            int[] vals = { 90, 80, 70, 60, 50, 40, 30, 20 };
            for (int i = 0; i < cnTens.Length; i++)
            {
                string cn = cnTens[i];
                int v = vals[i];
                s = Regex.Replace(s, cn + @"\.0\.(\d+)", v.ToString(CultureInfo.InvariantCulture) + ".$1");
            }

            s = Regex.Replace(s, @"([1-9])十\.0\.(\d+)", m =>
            {
                int tens = (m.Groups[1].Value[0] - '0') * 10;
                return tens.ToString(CultureInfo.InvariantCulture) + "." + m.Groups[2].Value;
            });

            s = Regex.Replace(s, @"十\.0\.(\d+)", "10.$1");
            return s;
        }

        private static string FixMisplacedUnitBetweenDecimalDigits(string compact)
        {
            return Regex.Replace(compact, @"(\d+\.\d+)([mMkK])(\d+)([kKmM千千兆]?)$", m =>
            {
                string merged = m.Groups[1].Value + m.Groups[3].Value;
                string explicitTail = m.Groups[4].Value;
                if (!string.IsNullOrEmpty(explicitTail))
                    return merged + explicitTail;
                char mid = m.Groups[2].Value[0];
                return merged + (mid is 'M' or 'm' ? "M" : "K");
            });
        }

        private static bool IsOlUtterance(string compactNoSpace, string trimmedOriginal)
        {
            if (string.IsNullOrEmpty(compactNoSpace)) return false;
            if (Regex.IsMatch(trimmedOriginal, @"(?i)\bol\b")) return true;
            if (compactNoSpace.Contains("开路", StringComparison.Ordinal)) return true;
            if (compactNoSpace.Contains("过载", StringComparison.Ordinal)) return true;
            if (compactNoSpace.Contains("超量程", StringComparison.Ordinal)) return true;
            if (compactNoSpace.Contains("无穷", StringComparison.Ordinal)) return true;
            return compactNoSpace.Equals("OL", StringComparison.OrdinalIgnoreCase);
        }
    }
}
