using System.Text;

namespace BirthdayBuddyLeftChat
{
    public static class Transliterator
    {
        private static readonly Dictionary<string, string> LatinToCyrillicMap = new Dictionary<string, string>
        {
            // Двухбуквенные сочетания
            { "ch", "ч" },
            { "sh", "ш" },
            { "zh", "ж" },
            { "yu", "ю" },
            { "ya", "я" },
            { "yo", "ё" },
            { "ts", "ц" },
            { "shch", "щ" },

            // Однобуквенные
            { "a", "а" },
            { "b", "б" },
            { "v", "в" },
            { "g", "г" },
            { "d", "д" },
            { "e", "е" },
            { "z", "з" },
            { "i", "и" },
            { "j", "й" },
            { "k", "к" },
            { "l", "л" },
            { "m", "м" },
            { "n", "н" },
            { "o", "о" },
            { "p", "п" },
            { "r", "р" },
            { "s", "с" },
            { "t", "т" },
            { "u", "у" },
            { "f", "ф" },
            { "x", "х" },
            { "y", "ы" },
            { "'", "ь" },
            { "\"", "ъ" },
            { "h", "х" }, // или "г" — зависит от контекста, тут упрощённо
        };

        public static string ToCyrillic(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            string lowerInput = input.ToLowerInvariant();
            StringBuilder result = new StringBuilder();
            int i = 0;

            while (i < lowerInput.Length)
            {
                bool matched = false;

                // Проверяем самые длинные возможные сочетания сначала
                foreach (var entry in LatinToCyrillicMap.OrderByDescending(kv => kv.Key.Length))
                {
                    if (lowerInput.Substring(i).StartsWith(entry.Key))
                    {
                        result.Append(entry.Value);
                        i += entry.Key.Length;
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    // Если не нашли соответствие — оставляем символ как есть
                    result.Append(lowerInput[i]);
                    i++;
                }
            }

            return result.ToString();
        }
    }
}
