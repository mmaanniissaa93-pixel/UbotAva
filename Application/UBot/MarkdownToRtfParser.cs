using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace UBot;

public class MarkdownToRtfParser
{
    private StringBuilder _rtf;

    public MarkdownToRtfParser()
    {
        _rtf = new StringBuilder();
    }

    public string Parse(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return string.Empty;

        markdown = PreprocessMarkdown(markdown);

        _rtf.Clear();

        _rtf.AppendLine(@"{\rtf1\ansi\deff0");
        _rtf.AppendLine(@"{\fonttbl{\f0\fnil\fcharset0 Segoe UI;}{\f1\fmodern\fcharset0 Consolas;}}");
        _rtf.AppendLine(@"{\colortbl;\red0\green0\blue0;\red240\green240\blue240;\red0\green0\blue255;}");

        var lines = markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
            {
                _rtf.Append(@"\par");
                _rtf.Append("\r\n");
                continue;
            }

            if (line.TrimStart().StartsWith("```"))
            {
                i = ParseCodeBlock(lines, i);
                continue;
            }

            if (line.StartsWith("#"))
            {
                ParseHeader(line);
                continue;
            }

            if (Regex.IsMatch(line.TrimStart(), @"^[\*\-]\s\[([ xX])\]"))
            {
                ParseTaskList(line);
                continue;
            }

            if (Regex.IsMatch(line.TrimStart(), @"^[\*\-\+]\s"))
            {
                ParseUnorderedList(line);
                continue;
            }

            if (Regex.IsMatch(line.TrimStart(), @"^\d+\.\s"))
            {
                ParseOrderedList(line);
                continue;
            }

            if (line.TrimStart().StartsWith(">"))
            {
                ParseBlockquote(line);
                continue;
            }

            if (Regex.IsMatch(line.Trim(), @"^(\*{3,}|-{3,}|_{3,})$"))
            {
                _rtf.Append(@"\par\brdrb\brdrs\brdrw10\brsp20\par");
                _rtf.Append("\r\n");
                continue;
            }

            ParseParagraph(line);
        }

        _rtf.Append("}");

        return _rtf.ToString();
    }

    private string PreprocessMarkdown(string markdown)
    {
        if (markdown.Contains("\n") || markdown.Contains("\r"))
            return markdown;

        markdown = Regex.Replace(markdown, @"(\S)\s+(##\s)", "$1\n$2");
        markdown = Regex.Replace(markdown, @"(\S)\s+(###\s)", "$1\n$2");
        markdown = Regex.Replace(markdown, @"(\S)\s+(\*\s+[^\*])", "$1\n$2");
        markdown = Regex.Replace(markdown, @"(\S)\s+(\-\s+[^\-])", "$1\n$2");
        markdown = Regex.Replace(markdown, @"(\S)\s+(\d+\.\s)", "$1\n$2");

        // **Full Changelog** gibi bold textlerden önce newline
        markdown = Regex.Replace(markdown, @"(\S)\s+(\*\*[^\*])", "$1\n$2");

        return markdown;
    }

    private void ParseHeader(string line)
    {
        int level = 0;
        while (level < line.Length && line[level] == '#')
            level++;

        string text = line.Substring(level).Trim();

        int fontSize = level switch
        {
            1 => 32,
            2 => 28,
            3 => 24,
            4 => 20,
            5 => 18,
            _ => 16
        };

        text = EscapeRtf(text);

        _rtf.Append(@"\pard\sb100\sa100");
        _rtf.Append(@"\b\fs" + fontSize + " ");
        _rtf.Append(text);
        _rtf.Append(@"\b0\fs20");
        _rtf.Append(@"\par");
        _rtf.Append("\r\n");
    }

    private void ParseParagraph(string line)
    {
        string formatted = ProcessInlineFormatting(line);

        _rtf.Append(@"\pard\sb100\sa100 ");
        _rtf.Append(formatted);
        _rtf.Append(@"\par");
        _rtf.Append("\r\n");
    }

    private void ParseBlockquote(string line)
    {
        string text = line.TrimStart().Substring(1).Trim();
        text = ProcessInlineFormatting(text);

        _rtf.Append(@"\pard\li720\ri720\sb100\sa100 ");
        _rtf.Append(text);
        _rtf.Append(@"\par");
        _rtf.Append("\r\n");
    }

    private void ParseUnorderedList(string line)
    {
        string text = Regex.Replace(line.TrimStart(), @"^[\*\-\+]\s", "");
        text = ProcessInlineFormatting(text);

        _rtf.Append(@"\pard\fi-360\li720\sb50\sa50 ");
        _rtf.Append(@"\bullet\tab ");
        _rtf.Append(text);
        _rtf.Append(@"\par");
        _rtf.Append("\r\n");
    }

    private void ParseOrderedList(string line)
    {
        var match = Regex.Match(line.TrimStart(), @"^(\d+)\.\s(.*)");

        if (match.Success)
        {
            string number = match.Groups[1].Value;
            string text = match.Groups[2].Value;
            text = ProcessInlineFormatting(text);

            _rtf.Append(@"\pard\fi-360\li720\sb50\sa50 ");
            _rtf.Append(number);
            _rtf.Append(@".\tab ");
            _rtf.Append(text);
            _rtf.Append(@"\par");
            _rtf.Append("\r\n");
        }
    }

    private void ParseTaskList(string line)
    {
        var match = Regex.Match(line.TrimStart(), @"^[\*\-]\s\[([ xX])\]\s(.*)");

        if (match.Success)
        {
            bool isChecked = match.Groups[1].Value.ToLower() == "x";
            string text = match.Groups[2].Value;
            text = ProcessInlineFormatting(text);

            string checkbox = isChecked ? "[X]" : "[ ]";

            _rtf.Append(@"\pard\fi-360\li720\sb50\sa50 ");
            _rtf.Append(checkbox);
            _rtf.Append(@"\tab ");
            _rtf.Append(text);
            _rtf.Append(@"\par");
            _rtf.Append("\r\n");
        }
    }

    private int ParseCodeBlock(string[] lines, int startIndex)
    {
        string lang = lines[startIndex].TrimStart().Substring(3).Trim();
        var codeLines = new List<string>();

        int i = startIndex + 1;
        while (i < lines.Length && !lines[i].TrimStart().StartsWith("```"))
        {
            codeLines.Add(lines[i]);
            i++;
        }

        _rtf.Append(@"\pard\sb100\sa100\cbpat2\f1\fs18 ");

        if (!string.IsNullOrEmpty(lang))
        {
            _rtf.Append(@"\cf3 // ");
            _rtf.Append(EscapeRtf(lang));
            _rtf.Append(@"\cf1\par");
            _rtf.Append("\r\n");
        }

        foreach (var codeLine in codeLines)
        {
            _rtf.Append(EscapeRtf(codeLine));
            _rtf.Append(@"\par");
            _rtf.Append("\r\n");
        }

        _rtf.Append(@"\cbpat0\f0\fs20\par");
        _rtf.Append("\r\n");

        return i;
    }

    private string ProcessInlineFormatting(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        text = EscapeRtf(text);

        // URLs first - before @mentions
        text = Regex.Replace(text, @"https?://[^\s\)]+", match =>
        {
            string url = match.Value;
            return $@"\cf3\ul {url}\ul0\cf1 ";
        });

        // @mentions - keep as @username but make clickable
        // Format: @username with special marker that we can parse in LinkClicked event
        text = Regex.Replace(text, @"@(\w+)", match =>
        {
            string username = match.Groups[1].Value;
            return $@"\cf3\ul @{username}\ul0\cf1 ";
        });

        // Bold
        text = Regex.Replace(text, @"\*\*([^\*]+?)\*\*", @"\b $1\b0 ");
        text = Regex.Replace(text, @"__([^_]+?)__", @"\b $1\b0 ");

        // Italic
        text = Regex.Replace(text, @"(?<!\*)\*(?!\*)([^\*]+?)(?<!\*)\*(?!\*)", @"\i $1\i0 ");
        text = Regex.Replace(text, @"(?<!_)_(?!_)([^_]+?)(?<!_)_(?!_)", @"\i $1\i0 ");

        // Strikethrough
        text = Regex.Replace(text, @"~~([^~]+?)~~", @"\strike $1\strike0 ");

        // Inline code
        text = Regex.Replace(text, @"`([^`]+?)`", @"\f1\cbpat2 $1\cbpat0\f0 ");

        // Markdown links [text](url)
        text = Regex.Replace(text, @"\[([^\]]+?)\]\(([^\)]+?)\)", match =>
        {
            string linkText = match.Groups[1].Value;
            string url = match.Groups[2].Value;

            if (url.Contains(@"\cf3"))
                return match.Value;

            return $@"{linkText} (\cf3\ul {url}\ul0\cf1 )";
        });

        // Images
        text = Regex.Replace(text, @"!\[([^\]]*?)\]\(([^\)]+?)\)", @"[Image: $1]");

        return text;
    }

    private string EscapeRtf(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var sb = new StringBuilder();

        foreach (char c in text)
        {
            switch (c)
            {
                case '\\':
                    sb.Append(@"\\");
                    break;
                case '{':
                    sb.Append(@"\{");
                    break;
                case '}':
                    sb.Append(@"\}");
                    break;
                case '\t':
                    sb.Append(@"\tab ");
                    break;
                default:
                    if (c <= 127)
                    {
                        sb.Append(c);
                    }
                    else
                    {
                        sb.Append(@"\u");
                        sb.Append((int)c);
                        sb.Append("?");
                    }
                    break;
            }
        }

        return sb.ToString();
    }
}
