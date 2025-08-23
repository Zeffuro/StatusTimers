using System;
using System.Collections.Generic;
using System.Globalization;

public class TimeFormatTemplate
{
    private static readonly Dictionary<string, TimeFormatTemplate>
        AutoTemplates = new()
        {
            {"hours", new TimeFormatTemplate("{H}:{mm}:{ss}")},
            {"minutes", new TimeFormatTemplate("{m}:{ss}")},
            {"seconds", new TimeFormatTemplate("{S.0}")}
        };
    private readonly List<IToken> _tokens;

    public TimeFormatTemplate(string template)
    {
        _tokens = TryParseTemplate(template, out var tokens)
            ? tokens
            : TryParseTemplate("{S.0}s", out var defaultTokens) ? defaultTokens : new List<IToken>();
    }

    public string Format(double seconds)
    {
        int totalSeconds = (int)Math.Floor(seconds);
        int ms = (int)((seconds - totalSeconds) * 1000);
        int s = totalSeconds % 60;
        int m = (totalSeconds / 60) % 60;
        int h = totalSeconds / 3600;

        var result = new System.Text.StringBuilder();
        foreach (var token in _tokens) {
            result.Append(token.GetValue(h, m, s, ms, seconds));
        }

        return result.ToString();
    }

    private interface IToken
    {
        string GetValue(int h, int m, int s, int ms, double totalSeconds);
    }

    private class TextToken : IToken
    {
        private readonly string _text;
        public TextToken(string text) { _text = text; }
        public string GetValue(int h, int m, int s, int ms, double totalSeconds) => _text;
    }

    private class ValueToken : IToken
    {
        private readonly string _component;
        private readonly string? _format;

        private static readonly HashSet<string> ValidComponents =
            ["h", "hh", "H", "HH", "m", "mm", "M", "MM", "s", "ss", "S", "SS", "ms", "auto"];
        public static bool IsValidComponent(string component) => ValidComponents.Contains(component);
        public ValueToken(string component, string? format)
        {
            _component = component;
            _format = format;
        }
        public string GetValue(int h, int m, int s, int ms, double totalSeconds) {
            return _component switch {
                "h" => h.ToString(),
                "hh" => h.ToString("D2"),
                "H" => Math.Floor(totalSeconds / 3600).ToString(CultureInfo.InvariantCulture),
                "HH" => ((int)Math.Floor(totalSeconds / 3600)).ToString("D2"),
                "m" => ((int)(totalSeconds / 60)).ToString(),
                "mm" => m.ToString("D2"),
                "M" => Math.Floor(totalSeconds / 60).ToString(CultureInfo.InvariantCulture),
                "MM" => ((int)Math.Floor(totalSeconds / 60)).ToString("D2"),
                "s" => s.ToString(),
                "ss" => s.ToString("D2"),
                "S" => totalSeconds.ToString(_format ?? "G", CultureInfo.InvariantCulture),
                "SS" => ((int)totalSeconds).ToString("D2"),
                "ms" => ms.ToString("D3"),
                "auto" => new AutoToken().GetValue(h, m, s, ms, totalSeconds),
                _ => "0"
            };
        }
    }

    private class AutoToken : IToken
    {
        public string GetValue(int h, int m, int s, int ms, double totalSeconds)
        {
            if (h > 0)
            {
                return AutoTemplates["hours"].Format(totalSeconds);
            }
            if (m > 0)
            {
                return AutoTemplates["minutes"].Format(totalSeconds);
            }
            return AutoTemplates["seconds"].Format(totalSeconds);
        }
    }

    private static bool TryParseTemplate(string template, out List<IToken> tokens)
    {
        tokens = new List<IToken>();
        int i = 0;
        while (i < template.Length)
        {
            int start = template.IndexOf('{', i);
            int nextClose = template.IndexOf('}', i);

            // If a '}' appears before a '{', it's invalid
            if (nextClose >= 0 && (start < 0 || nextClose < start))
                return false;

            if (start < 0)
            {
                tokens.Add(new TextToken(template.Substring(i)));
                break;
            }
            if (start > i) {
                tokens.Add(new TextToken(template.Substring(i, start - i)));
            }

            int end = template.IndexOf('}', start);
            if (end < 0) {
                return false; // Invalid: unmatched '{'
            }

            string token = template.Substring(start + 1, end - start - 1);
            if (string.IsNullOrWhiteSpace(token)) {
                return false; // Invalid: empty token
            }

            string component;
            string? format = null;
            int dot = token.IndexOf('.');
            if (dot >= 0)
            {
                component = token.Substring(0, dot);
                format = $"0.{token.Substring(dot + 1)}";
            }
            else
            {
                component = token;
            }
            if (!ValueToken.IsValidComponent(component)) {
                return false; // Invalid component
            }

            tokens.Add(new ValueToken(component, format));
            i = end + 1;
        }
        return true;
    }
}
