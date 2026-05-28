using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace IC_Diode_Record
{
    /// <summary>
    /// 语音识别结果解析：跳过指令、OL、数值（阿拉伯/中文混读、小数）、电阻单位 K/M。
    /// 架构：轻量标准化 → 先拆单位 → 少量 ASR 预处理 → 混读整数状态机 / 中文整数 / 阿拉伯小数。
    /// </summary>
    internal static class VoiceRecognitionParser
    {
        /// <summary>中文数字字符到阿拉伯数字的映射。</summary>
        private static readonly Dictionary<char, int> CnDigit = new()
        {
            ['零'] = 0, ['一'] = 1, ['二'] = 2, ['两'] = 2, ['三'] = 3, ['四'] = 4, ['五'] = 5,
            ['六'] = 6, ['七'] = 7, ['八'] = 8, ['九'] = 9,
        };

        /// <summary>语音识别短语提示：跳过指令。</summary>
        public static readonly string[] SkipPhrases =
        {
            "跳过", "跳过1个", "跳过2个", "跳过3个", "跳过4个", "跳过5个",
            "跳过一个", "跳过两个", "跳过三个", "跳过四个", "跳过五个",
        };

        /// <summary>语音识别短语提示：测量值、单位和常见口语样本。</summary>
        public static readonly string[] MeasurementPhraseHints = CreateMeasurementPhraseHints();

        /// <summary>构建 Azure 识别短语提示列表，提升常见阻值口语的命中率。</summary>
        private static string[] CreateMeasurementPhraseHints()
        {
            var list = new List<string>
            {
                "OL", "ol", "开路", "过载", "超量程", "无穷",
                "K", "k", "M", "千", "兆", "千欧", "兆欧", "欧姆",
                "点", "点儿", "零点",
                "一百", "两百", "三百", "一千", "一百二十三", "两百五十", "两百五十四", "四百五十三", "五十四", "一千零一",
                "一二三", "四五六", "一二三四",
                "十一点五", "十二点三四", "一百点五", "一百点一", "两百点一", "一百八十点一", "一百九十点一", "一百八十千", "一百九十兆",
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

        #region 公开入口

        /// <summary>
        /// 解析「跳过」指令，支持「跳过」「跳过3个」「跳过二十三」等。
        /// </summary>
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

        /// <summary>
        /// 解析测量口语到写入值：数值（double 或带 K/M 的字符串）或 OL。
        /// </summary>
        public static bool TryParseMeasurement(string recognized, out object? value, out bool olOrange)
        {
            value = null;
            olOrange = false;
            if (string.IsNullOrWhiteSpace(recognized)) return false;

            // 输入保护：过滤过长和命令句。
            var trimmed = recognized.Trim().TrimEnd('。', '！', '!', '.', '…');
            if (trimmed.Length > 64) return false;
            if (trimmed.Contains("跳过", StringComparison.Ordinal)) return false;

            // 轻量标准化（不做语义推断）。
            string s = NormalizeBasic(trimmed);
            string compact = CompactForOlCheck(s);

            if (IsOlUtterance(compact, trimmed))
            {
                value = "OL";
                olOrange = true;
                return true;
            }

            if (!TrySplitTrailingResistanceUnit(s, out string coreRaw, out string? unitNorm))
                return false;

            // 核心串修补：仅处理高频 ASR 形态误差。
            string core = PreparseMeasurementCore(coreRaw);
            core = Regex.Replace(core, @"\s+", "");

            // 兼容「12点34」混写。
            string mix = Regex.Replace(core, @"(\d)点(\d+)", "$1.$2");

            return TryParseCoreWithUnit(mix, unitNorm, out value, out olOrange);
        }

        #endregion

        #region 轻量标准化

        /// <summary>
        /// 轻量标准化：全角数字、减号、点儿；不在此阶段做语义合并。
        /// </summary>
        private static string NormalizeBasic(string s)
        {
            s = s.Replace('\u2212', '-');
            s = s.Replace("点儿", "点", StringComparison.Ordinal);
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                if (c >= '０' && c <= '９') sb.Append((char)('0' + (c - '０')));
                else sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>用于 OL 判断的紧凑串（去空白）。</summary>
        private static string CompactForOlCheck(string s) =>
            Regex.Replace(s, @"\s+", "");

        /// <summary>
        /// 在去掉单位后的「数值核」上做有限 ASR 修补（避免长 Regex 链）。顺序：逐位阿拉伯 → 250+4 → 逗号变点 → Azure 小数碎片。
        /// </summary>
        private static string PreparseMeasurementCore(string s)
        {
            // 1) 逐位阿拉伯（7，8 9 -> 789）
            s = GlueSingleAsciiDigitsSeparatedByPause(s);
            // 2) 整十整百 + 个位拆分（250 4 -> 254）
            s = MergeRoundNumberAndSingleDigit(s);
            // 3) 三百0 1K 等高频单位形态
            s = CollapseHundredZeroTailWithUnit(s);

            // 4) 全角逗号/点转标准小数点
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                if (c == '．' || c == '，') sb.Append('.');
                else sb.Append(c);
            }
            s = sb.ToString();

            // 5) 句号夹数字时按小数处理
            s = Regex.Replace(s, @"(\d)\s*。\s*(\d)", "$1.$2");
            // 6) 逐位数字 + 0.x（1.7.8.0.1 -> 178.1）
            s = MergeDigitSequenceWithZeroFraction(s);
            // 6) Azure 常见小数碎片
            s = MergeAzureSplitDecimals(s);
            // 7) 几十几 + 0.x（7十8 0.2 -> 78.2）
            s = MergeTensOnesWithZeroFraction(s);
            // 7) 一百8十0.1 / 一百0.1 等混写
            s = MergeChineseHundredTenAzureDecimal(s);
            s = MergeHundredZeroDotFraction(s);
            // 上一步可能产生新的 180.0.1 形式
            while (true)
            {
                string n = Regex.Replace(s, @"(\d+)\.0\.(\d+)", "$1.$2");
                if (n == s) break;
                s = n;
            }
            s = FixMisplacedUnitInsideDecimal(s);
            return s;
        }

        /// <summary>
        /// 将「逐位数字序列 + 0.x」归一为整数小数：1.7.8.0.1 -> 178.1。
        /// 仅匹配单个数字序列，避免影响正常 12.34 这种标准小数。
        /// </summary>
        private static string MergeDigitSequenceWithZeroFraction(string s)
        {
            return Regex.Replace(s, @"(?<!\d)(\d(?:[.\s、。]+\d){1,8})[.\s、。]+0\.(\d+)(?!\d)", m =>
            {
                string left = Regex.Replace(m.Groups[1].Value, @"\D+", "");
                if (left.Length < 2 || left.Length > 9)
                    return m.Value;
                return left + "." + m.Groups[2].Value;
            });
        }

        /// <summary>
        /// Azure 常把「一百八十点一」说成「一百8十0.1」；若不归一，后续会误匹配子串 0.1 → 只剩 0.1K。
        /// </summary>
        private static string MergeChineseHundredTenAzureDecimal(string s)
        {
            const string Gap = @"[。\s，,、]*";
            const string Tail = Gap + @"(?:\.0\.|0\.)(\d+)";

            // 一百8十0.1 → 180.1；一百9十0.1 → 190.1
            s = Regex.Replace(s, @"一百([1-9])十" + Tail, m =>
            {
                int d = m.Groups[1].Value[0] - '0';
                return (100 + 10 * d).ToString(CultureInfo.InvariantCulture) + "." + m.Groups[2].Value;
            });

            (string word, int hb)[] hundreds =
            {
                ("两百", 200), ("二百", 200), ("三百", 300), ("四百", 400),
                ("五百", 500), ("六百", 600), ("七百", 700), ("八百", 800), ("九百", 900),
            };
            foreach (var (word, hb) in hundreds)
            {
                string esc = Regex.Escape(word);
                s = Regex.Replace(s, esc + @"([1-9])十" + Tail, m =>
                {
                    int d = m.Groups[1].Value[0] - '0';
                    return (hb + 10 * d).ToString(CultureInfo.InvariantCulture) + "." + m.Groups[2].Value;
                });
            }

            // 阿拉伯百位：2百9十0.1
            s = Regex.Replace(s, @"([1-9])百([1-9])十" + Tail, m =>
            {
                int h = m.Groups[1].Value[0] - '0';
                int t = m.Groups[2].Value[0] - '0';
                return (h * 100 + t * 10).ToString(CultureInfo.InvariantCulture) + "." + m.Groups[3].Value;
            });

            return s;
        }

        /// <summary>
        /// 「一百点一」常被识别成「一百0.1」；阿拉伯百位同理。须在 MergeRoundNumber 类规则之后、避免误伤。
        /// </summary>
        private static string MergeHundredZeroDotFraction(string s)
        {
            const string G = @"[。\s，,、]*";
            (string word, int b)[] cn =
            {
                ("一百", 100), ("两百", 200), ("二百", 200), ("三百", 300), ("四百", 400),
                ("五百", 500), ("六百", 600), ("七百", 700), ("八百", 800), ("九百", 900),
            };
            foreach (var (word, b) in cn)
            {
                string esc = Regex.Escape(word);
                s = Regex.Replace(s, esc + G + @"0\.(\d+)", m =>
                    b.ToString(CultureInfo.InvariantCulture) + "." + m.Groups[1].Value);
            }

            s = Regex.Replace(s, @"([1-9])百" + G + @"0\.(\d+)", m =>
            {
                int h = m.Groups[1].Value[0] - '0';
                return (h * 100).ToString(CultureInfo.InvariantCulture) + "." + m.Groups[2].Value;
            });

            return s;
        }

        /// <summary>7，8 9 → 789；遇「1，0.2」式（停顿后接点+数字）不合并。</summary>
        private static string GlueSingleAsciiDigitsSeparatedByPause(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new StringBuilder(s.Length);
            int i = 0;
            while (i < s.Length)
            {
                if (s[i] < '0' || s[i] > '9')
                {
                    sb.Append(s[i]);
                    i++;
                    continue;
                }

                int runStart = i;
                var digits = new StringBuilder();
                digits.Append(s[i]);
                i++;

                bool invalidRun = false;
                while (i < s.Length)
                {
                    int sepStart = i;
                    while (i < s.Length && IsPauseChar(s[i]))
                        i++;
                    if (i == sepStart)
                        break;

                    if (i >= s.Length || s[i] < '0' || s[i] > '9')
                        break;

                    if (i + 1 < s.Length && s[i + 1] >= '0' && s[i + 1] <= '9')
                    {
                        invalidRun = true;
                        break;
                    }

                    digits.Append(s[i]);
                    i++;
                }

                if (invalidRun || digits.Length < 2)
                {
                    i = runStart + 1;
                    sb.Append(s[runStart]);
                    continue;
                }

                int peek = i;
                while (peek < s.Length && IsPauseChar(s[peek]))
                    peek++;
                if (peek < s.Length && s[peek] == '.' && peek + 1 < s.Length && char.IsAsciiDigit(s[peek + 1]))
                {
                    i = runStart + 1;
                    sb.Append(s[runStart]);
                    continue;
                }

                sb.Append(digits.ToString());
            }

            return sb.ToString();
        }

        /// <summary>停顿类分隔符（空白、逗号、句号、顿号）。</summary>
        private static bool IsPauseChar(char c) =>
            c is '，' or ',' or ' ' or '\t' or '\u3000' or '。' or '、';

        /// <summary>三百0 1K、4百0 1K 等（仅保留与单位连写、高命中片段）。</summary>
        private static string CollapseHundredZeroTailWithUnit(string s)
        {
            (string prefix, int hb)[] bai =
            {
                ("一百", 100), ("两百", 200), ("二百", 200), ("三百", 300), ("四百", 400),
                ("五百", 500), ("六百", 600), ("七百", 700), ("八百", 800), ("九百", 900),
            };
            foreach (var (prefix, hb) in bai)
            {
                string esc = Regex.Escape(prefix);
                s = Regex.Replace(s, esc + @"0(?:[。.]+|\s+)(\d{1,2})\s*([kKmM千千兆])", m =>
                {
                    if (!int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int tail) || tail < 0 || tail > 99)
                        return m.Value;
                    return (hb + tail).ToString(CultureInfo.InvariantCulture) + UnitLetterFromChar(m.Groups[2].Value[0]);
                });
            }

            s = Regex.Replace(s, @"([1-9])百0(?:[。.]+|\s+)(\d{1,2})\s*([kKmM千千兆])", m =>
            {
                int hd = m.Groups[1].Value[0] - '0';
                if (!int.TryParse(m.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int tail) || tail < 0 || tail > 99)
                    return m.Value;
                return (hd * 100 + tail).ToString(CultureInfo.InvariantCulture) + UnitLetterFromChar(m.Groups[3].Value[0]);
            });

            return s;
        }

        /// <summary>
        /// 250 4 → 254。不得合并「100 0.1」→100（会丢掉小数）；个位后不能紧跟「.数字」。
        /// </summary>
        private static string MergeRoundNumberAndSingleDigit(string s)
        {
            return Regex.Replace(s, @"(?<![\d.])(\d+)(?:[\s，,、。]+)+(\d)(?!\d)(?!\.[0-9])", m =>
            {
                if (!long.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long n) || n <= 0)
                    return m.Value;
                if (n % 10 != 0)
                    return m.Value;
                int d = m.Groups[2].Value[0] - '0';
                long sum = n + d;
                if (sum > 9_999_999)
                    return m.Value;
                return sum.ToString(CultureInfo.InvariantCulture);
            });
        }

        private static string MergeAzureSplitDecimals(string s)
        {
            s = CollapseChineseTensDotZeroDot(s);
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
        /// 处理「七八点二」被识别成「7十8 0.2」的变体：7十8 0.2 -> 78.2。
        /// </summary>
        private static string MergeTensOnesWithZeroFraction(string s)
        {
            // 7十8 0.2 -> 78.2
            s = Regex.Replace(s, @"([1-9])十\s*([0-9])(?:[。\s，,、]+)0\.(\d+)", m =>
            {
                int tens = (m.Groups[1].Value[0] - '0') * 10;
                int ones = m.Groups[2].Value[0] - '0';
                return (tens + ones).ToString(CultureInfo.InvariantCulture) + "." + m.Groups[3].Value;
            });

            // 十8 0.2 -> 18.2
            s = Regex.Replace(s, @"(?<![0-9])十\s*([0-9])(?:[。\s，,、]+)0\.(\d+)", m =>
            {
                int ones = m.Groups[1].Value[0] - '0';
                return (10 + ones).ToString(CultureInfo.InvariantCulture) + "." + m.Groups[2].Value;
            });

            return s;
        }

        private static string CollapseChineseTensDotZeroDot(string s)
        {
            string[] cnTens = { "九十", "八十", "七十", "六十", "五十", "四十", "三十", "二十" };
            int[] vals = { 90, 80, 70, 60, 50, 40, 30, 20 };
            for (int i = 0; i < cnTens.Length; i++)
            {
                s = Regex.Replace(s, cnTens[i] + @"\.0\.(\d+)", vals[i].ToString(CultureInfo.InvariantCulture) + ".$1");
            }

            // 十，1，0.1 -> 十.1.0.1 -> 11.1（「十一点一」的 Azure 变体）
            s = Regex.Replace(s, @"十\.([1-9])\.0\.(\d+)", m =>
            {
                int ones = m.Groups[1].Value[0] - '0';
                return (10 + ones).ToString(CultureInfo.InvariantCulture) + "." + m.Groups[2].Value;
            });

            // 2十，3，0.4 -> 23.4（「二十三点四」的混写变体）
            s = Regex.Replace(s, @"([1-9])十\.([1-9])\.0\.(\d+)", m =>
            {
                int tens = (m.Groups[1].Value[0] - '0') * 10;
                int ones = m.Groups[2].Value[0] - '0';
                return (tens + ones).ToString(CultureInfo.InvariantCulture) + "." + m.Groups[3].Value;
            });

            s = Regex.Replace(s, @"([1-9])十\.0\.(\d+)", m =>
            {
                int tens = (m.Groups[1].Value[0] - '0') * 10;
                return tens.ToString(CultureInfo.InvariantCulture) + "." + m.Groups[2].Value;
            });

            s = Regex.Replace(s, @"十\.0\.(\d+)", "10.$1");
            return s;
        }

        /// <summary>
        /// Azure 常把小数中间误插单位字母：1.2K3 -> 1.23K。
        /// </summary>
        private static string FixMisplacedUnitInsideDecimal(string s)
        {
            return Regex.Replace(s, @"(\d+\.\d+)([mMkK])(\d+)([kKmM千千兆]?)$", m =>
            {
                string merged = m.Groups[1].Value + m.Groups[3].Value;
                string explicitTail = m.Groups[4].Value;
                if (!string.IsNullOrEmpty(explicitTail))
                    return merged + explicitTail;
                char mid = m.Groups[2].Value[0];
                return merged + (mid is 'M' or 'm' ? "M" : "K");
            });
        }

        /// <summary>单位字符归一：千/k/K -> K，其它（兆/m/M）-> M。</summary>
        private static string UnitLetterFromChar(char c) =>
            c is '千' or 'k' or 'K' ? "K" : "M";

        #endregion

        #region 单位与 OL

        /// <summary>
        /// 从原始测量串尾部拆单位（K/M/千/兆/千欧/兆欧），返回数值核心串。
        /// </summary>
        private static bool TrySplitTrailingResistanceUnit(string s, out string core, out string? unitNorm)
        {
            core = s.Trim();
            unitNorm = null;
            if (string.IsNullOrEmpty(core))
                return false;

            core = Regex.Replace(core, @"(欧姆|歐姆|欧|歐)+\s*$", "").TrimEnd();

            if (core.EndsWith("千欧", StringComparison.Ordinal))
            {
                core = core[..^2].TrimEnd();
                unitNorm = "K";
                return true;
            }
            if (core.EndsWith("兆欧", StringComparison.Ordinal))
            {
                core = core[..^2].TrimEnd();
                unitNorm = "M";
                return true;
            }

            char last = core[^1];
            if (last is 'k' or 'K')
            {
                core = core[..^1].TrimEnd();
                unitNorm = "K";
                return true;
            }
            if (last is 'm' or 'M')
            {
                core = core[..^1].TrimEnd();
                unitNorm = "M";
                return true;
            }
            if (last == '千')
            {
                core = core[..^1].TrimEnd();
                unitNorm = "K";
                return true;
            }
            if (last == '兆')
            {
                core = core[..^1].TrimEnd();
                unitNorm = "M";
                return true;
            }

            return true;
        }

        /// <summary>判断是否为 OL/开路/过载 等异常量程语义。</summary>
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

        #endregion

        #region 核心解析（小数 / 混读状态机 / 回退）

        /// <summary>
        /// 解析核心数值（不含单位）：阿拉伯 -> 点小数 -> 中文小数 -> 中文整数 -> 混读整数。
        /// </summary>
        private static bool TryParseCoreWithUnit(string core, string? unitNorm, out object? value, out bool olOrange)
        {
            value = null;
            olOrange = false;
            if (string.IsNullOrEmpty(core))
                return false;

            if (TryParseArabicNumberWithUnit(core, unitNorm, out value))
                return true;

            // 混读左侧 + 阿拉伯小数点（如 1百0.1、两百0.1）
            if (TryParseDotDecimalWithUnit(core, unitNorm, out value))
                return true;

            if (TryParseChineseDecimalWith点(core, unitNorm, out value))
                return true;

            if (!core.Contains('.') && TryParseChineseIntegerWithUnit(core, unitNorm, out value))
                return true;

            if (!core.Contains('.') && ContainsAsciiDigit(core) && TryParseMixedIntegerWithUnit(core, unitNorm, out value))
                return true;

            return false;
        }

        /// <summary>
        /// 解析「左侧可混读整数 + '.' + 右侧小数位」形式，如 1百0.1、两百0.1。
        /// </summary>
        private static bool TryParseDotDecimalWithUnit(string core, string? unitNorm, out object? value)
        {
            value = null;
            int dot = core.IndexOf('.');
            if (dot <= 0 || dot >= core.Length - 1)
                return false;
            if (core.IndexOf('.', dot + 1) >= 0)
                return false;

            string left = core[..dot].Trim();
            string right = core[(dot + 1)..].Trim();
            if (right.Length == 0)
                return false;

            // intPart：小数点左侧的整数部分（支持阿拉伯/中文/混读）。
            int intPart;
            if (left.Length == 0 || left == "零")
            {
                intPart = 0;
            }
            else if (Regex.IsMatch(left, @"^-?\d+$"))
            {
                if (!int.TryParse(left, NumberStyles.Integer, CultureInfo.InvariantCulture, out intPart))
                    return false;
            }
            else if (TryParseMixedIntegerSpan(left.AsSpan(), out int mixed))
            {
                intPart = mixed;
            }
            else if (TryParseChineseIntegerSpeech(left, out int cn))
            {
                intPart = cn;
            }
            else
            {
                return false;
            }

            // fracStr：右侧小数位（支持中文逐位）。
            if (!TryParseFractionDigits(right, out string fracStr))
                return false;

            string numCore = intPart.ToString(CultureInfo.InvariantCulture) + "." + fracStr;
            if (!double.TryParse(numCore, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                return false;

            value = string.IsNullOrEmpty(unitNorm) ? d : numCore + unitNorm;
            return true;
        }

        /// <summary>快速判断字符串是否包含 ASCII 数字。</summary>
        private static bool ContainsAsciiDigit(string s)
        {
            foreach (char c in s)
            {
                if (c >= '0' && c <= '9')
                    return true;
            }
            return false;
        }

        /// <summary>解析混读整数并附加单位。</summary>
        private static bool TryParseMixedIntegerWithUnit(string core, string? unitNorm, out object? value)
        {
            value = null;
            if (!TryParseMixedIntegerSpan(core.AsSpan(), out int iv) || iv < 0 || iv > 9_999_999)
                return false;

            string num = iv.ToString(CultureInfo.InvariantCulture);
            value = string.IsNullOrEmpty(unitNorm) ? (double)iv : num + unitNorm;
            return true;
        }

        /// <summary>
        /// 万以内混读：阿拉伯段 + 百十 + 中文数字，句读作分隔。不含小数点。
        /// </summary>
        private static bool TryParseMixedIntegerSpan(ReadOnlySpan<char> s, out int value)
        {
            value = 0;
            s = TrimSpan(s);
            if (s.IsEmpty)
                return false;

            int wanIdx = s.IndexOf('万');
            if (wanIdx >= 0)
            {
                ReadOnlySpan<char> left = s[..wanIdx];
                ReadOnlySpan<char> right = s[(wanIdx + 1)..];
                if (!TryParseSectionUpTo9999(left, out int w))
                    return false;
                if (w <= 0)
                    return false;
                if (!TryParseSectionUpTo9999(TrimSpan(right), out int rest))
                    return false;
                try
                {
                    value = checked(w * 10000 + rest);
                    return value >= 0 && value <= 9_999_999;
                }
                catch
                {
                    return false;
                }
            }

            return TryParseSectionUpTo9999(s, out value) && value >= 0 && value <= 9_999_999;
        }

        /// <summary>
        /// 解析不含「万」的 0~9999 片段（状态机）：pending + 百十千 + 逐位拼接。
        /// </summary>
        private static bool TryParseSectionUpTo9999(ReadOnlySpan<char> s, out int value)
        {
            value = 0;
            s = TrimSpan(s);
            if (s.IsEmpty)
            {
                value = 0;
                return true;
            }

            // pending：等待写入 section 的当前数字块；section：累计值。
            int i = 0;
            int section = 0;
            int pending = -1;

            void FlushPending()
            {
                if (pending >= 0)
                {
                    section += pending;
                    pending = -1;
                }
            }

            while (i < s.Length)
            {
                char c = s[i];
                if (IsPauseInSpan(c))
                {
                    i++;
                    continue;
                }

                if (c >= '0' && c <= '9')
                {
                    int start = i;
                    while (i < s.Length && s[i] >= '0' && s[i] <= '9')
                        i++;
                    int len = i - start;
                    if (!int.TryParse(s[start..i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int run))
                        return false;

                    // 五 + 4 → 54（ASR 漏「十」）
                    if (pending >= 1 && pending <= 9 && len == 1 && run >= 0 && run <= 9)
                    {
                        pending = pending * 10 + run;
                        continue;
                    }

                    FlushPending();
                    pending = run;
                    continue;
                }

                if (TryCnDigitChar(c, out int d))
                {
                    if (pending >= 0)
                        return false;
                    pending = d;
                    i++;
                    continue;
                }

                if (c == '百')
                {
                    int coef = pending < 0 ? 1 : pending;
                    section += coef * 100;
                    pending = -1;
                    i++;
                    continue;
                }

                if (c == '十')
                {
                    int coef = pending < 0 ? 1 : pending;
                    section += coef * 10;
                    pending = -1;
                    i++;
                    continue;
                }

                if (c == '千')
                {
                    int coef = pending < 0 ? 1 : pending;
                    section += coef * 1000;
                    pending = -1;
                    i++;
                    continue;
                }

                return false;
            }

            FlushPending();
            value = section;
            return true;
        }

        /// <summary>去除片段两端停顿符（空白、逗号、句号、顿号）。</summary>
        private static ReadOnlySpan<char> TrimSpan(ReadOnlySpan<char> s)
        {
            while (s.Length > 0 && IsPauseInSpan(s[0]))
                s = s[1..];
            while (s.Length > 0 && IsPauseInSpan(s[^1]))
                s = s[..^1];
            return s;
        }

        /// <summary>状态机中的停顿符判定。</summary>
        private static bool IsPauseInSpan(char c) =>
            char.IsWhiteSpace(c) || c is '，' or ',' or '。' or '、' or '．';

        /// <summary>尝试将中文数字字符转换为 0~9。</summary>
        private static bool TryCnDigitChar(char c, out int d) => CnDigit.TryGetValue(c, out d);

        /// <summary>解析纯阿拉伯数值并附加单位；必要时有限回退提取小数子串。</summary>
        private static bool TryParseArabicNumberWithUnit(string core, string? unitNorm, out object? value)
        {
            value = null;

            if (Regex.IsMatch(core, @"^-?\d+\.\d+$") &&
                double.TryParse(core, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
            {
                value = string.IsNullOrEmpty(unitNorm) ? d : core + unitNorm;
                return true;
            }

            if (Regex.IsMatch(core, @"^-?\d+$") &&
                double.TryParse(core, NumberStyles.Integer, CultureInfo.InvariantCulture, out double di))
            {
                value = string.IsNullOrEmpty(unitNorm) ? di : core + unitNorm;
                return true;
            }

            // 含中文/百十千等时禁止从串中抠小数段，否则「一百8十0.1」会误匹配为 0.1
            if (!Regex.IsMatch(core, @"^-?[\d.]+$") && !ContainsChineseNumericHint(core))
            {
                Match? best = null;
                foreach (Match m in Regex.Matches(core, @"-?\d+\.\d+"))
                {
                    if (best == null || m.Length > best.Length)
                        best = m;
                }
                if (best != null &&
                    double.TryParse(best.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double ds))
                {
                    value = string.IsNullOrEmpty(unitNorm) ? ds : best.Value + unitNorm;
                    return true;
                }
            }

            return false;
        }

        /// <summary>判断串中是否包含中文数值语义字符（用于限制危险回退）。</summary>
        private static bool ContainsChineseNumericHint(string s) =>
            Regex.IsMatch(s, @"[百十千万兆零一二两三四五六七八九点]");

        /// <summary>解析纯中文整数并附加单位。</summary>
        private static bool TryParseChineseIntegerWithUnit(string core, string? unitNorm, out object? value)
        {
            value = null;
            if (core.Contains('点', StringComparison.Ordinal))
                return false;

            if (!TryParseChineseIntegerSpeech(core, out int iv) || iv < 0 || iv > 9_999_999)
                return false;

            string num = iv.ToString(CultureInfo.InvariantCulture);
            value = string.IsNullOrEmpty(unitNorm) ? (double)iv : num + unitNorm;
            return true;
        }

        /// <summary>解析中文「点」小数并附加单位（如 十二点三四、十点二三）。</summary>
        private static bool TryParseChineseDecimalWith点(string core, string? unitNorm, out object? value)
        {
            value = null;
            if (!core.Contains('点', StringComparison.Ordinal))
                return false;

            var parts = core.Split(new[] { '点' }, 2);
            if (parts.Length != 2) return false;
            string L = parts[0].Trim();
            string R = parts[1].Trim();
            if (R.Length == 0) return false;

            string rWork = R;
            string? fracUnitLetter = null;
            var um = Regex.Match(rWork, @"^(.+?)([kKmM千千兆]+)$");
            if (um.Success)
            {
                rWork = um.Groups[1].Value;
                fracUnitLetter = UnitLetterFromChar(um.Groups[2].Value[0]);
            }

            string effectiveUnit = fracUnitLetter ?? unitNorm ?? "";

            if (L == "十" && rWork.Length == 2 &&
                TryParseFractionDigits(rWork, out string fracTen) &&
                double.TryParse("1." + fracTen, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                string nc = "1." + fracTen;
                value = string.IsNullOrEmpty(effectiveUnit) ? double.Parse(nc, CultureInfo.InvariantCulture) : nc + effectiveUnit;
                return true;
            }

            int intVal;
            if (string.IsNullOrEmpty(L) || L == "零")
                intVal = 0;
            else if (ContainsAsciiDigit(L))
            {
                if (!TryParseMixedIntegerSpan(L.AsSpan(), out int mv))
                    return false;
                intVal = mv;
            }
            else if (!TryParseChineseIntegerSpeech(L, out intVal))
                return false;

            if (!TryParseFractionDigits(rWork, out string fracStr))
                return false;

            string numCore = intVal.ToString(CultureInfo.InvariantCulture) + "." + fracStr;
            if (!double.TryParse(numCore, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                return false;

            value = string.IsNullOrEmpty(effectiveUnit)
                ? double.Parse(numCore, CultureInfo.InvariantCulture)
                : numCore + effectiveUnit;
            return true;
        }

        /// <summary>解析小数位：支持阿拉伯与中文逐位。</summary>
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

        #endregion

        #region 中文整数（纯中文路径）

        /// <summary>解析纯中文整数（支持连读、十百千、万）或纯阿拉伯整数串。</summary>
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

        /// <summary>解析含「万」的主结构。</summary>
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

        /// <summary>剔除前导零字。</summary>
        private static ReadOnlySpan<char> SkipZero(ReadOnlySpan<char> s)
        {
            while (!s.IsEmpty && s[0] == '零')
                s = s[1..];
            return s;
        }

        /// <summary>中文数字字符判定。</summary>
        private static bool TryCnDigit(char c, out int d) => CnDigit.TryGetValue(c, out d);

        /// <summary>解析十百千位的系数（空 -> 1）。</summary>
        private static int ParseMultiplier(ReadOnlySpan<char> s)
        {
            s = SkipZero(s);
            if (s.IsEmpty) return 1;
            if (s.Length == 1 && TryCnDigit(s[0], out int d) && d > 0) return d;
            throw new FormatException();
        }

        /// <summary>解析单中文数字位。</summary>
        private static int ParseDigitOnly(ReadOnlySpan<char> s)
        {
            if (s.Length != 1 || !TryCnDigit(s[0], out int d))
                throw new FormatException();
            return d;
        }

        /// <summary>连读逐位拼接（二三四 -> 234）。</summary>
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

        /// <summary>解析 0~9999（千位入口）。</summary>
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

        /// <summary>解析 0~999（百位入口）。</summary>
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

        /// <summary>解析 0~99（十位入口，兼容连读）。</summary>
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

        #endregion
    }
}
