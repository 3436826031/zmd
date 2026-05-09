using System.Drawing;
using System.Globalization;
using System.Text;

namespace Zmd;

internal sealed class TerminalBuffer
{
    private static readonly Color DefaultForeground = Color.FromArgb(242, 242, 242);
    private static readonly Color DefaultBackground = Color.FromArgb(12, 12, 12);

    private readonly AnsiParser parser = new();
    private readonly List<TerminalLine> scrollback = new();
    private readonly List<TerminalLine> screen = new();
    private readonly List<TerminalLine> alternateScreen = new();
    private Color currentForeground = DefaultForeground;
    private Color currentBackground = DefaultBackground;
    private bool currentBold;
    private bool currentInverse;
    private int cursorRow;
    private int cursorColumn;
    private int savedCursorRow;
    private int savedCursorColumn;
    private int columns = 120;
    private int rows = 36;
    private bool useAlternateScreen;
    private bool cursorVisible = true;

    public int MaxLines { get; set; } = 3000;

    public int CursorRow => cursorRow;

    public int CursorColumn => cursorColumn;

    public bool CursorVisible => cursorVisible;

    public bool UsesAlternateScreen => useAlternateScreen;

    public int ScrollbackCount => scrollback.Count;

    public IReadOnlyList<TerminalLineSnapshot> Snapshot()
    {
        return ActiveScrollback().Concat(ActiveScreen()).Select(line => line.Snapshot()).ToArray();
    }

    public void Resize(int newColumns, int newRows)
    {
        columns = Math.Max(20, newColumns);
        rows = Math.Max(5, newRows);
        EnsureScreenRows();
        cursorRow = Math.Clamp(cursorRow, 0, rows - 1);
        cursorColumn = Math.Clamp(cursorColumn, 0, columns - 1);
    }

    public void Append(string text)
    {
        foreach (var token in parser.Parse(text))
        {
            if (token.Kind == TerminalTokenKind.Text)
            {
                AppendText(token.Text);
                continue;
            }

            ApplyControl(token);
        }
    }

    public void Clear()
    {
        Reset();
    }

    private void AppendText(string text)
    {
        foreach (var ch in text)
        {
            switch (ch)
            {
                case '\r':
                    cursorColumn = 0;
                    break;
                case '\n':
                    NewLine();
                    break;
                case '\b':
                    Backspace();
                    break;
                case '\t':
                    AppendSpaces(4 - cursorColumn % 4);
                    break;
                default:
                    if (!char.IsControl(ch))
                    {
                        AppendChar(ch);
                    }

                    break;
            }
        }
    }

    private void AppendChar(char ch)
    {
        EnsureScreenRows();
        var width = DisplayWidth(ch);
        if (cursorColumn + width > columns)
        {
            NewLine();
        }

        ActiveScreen()[cursorRow].Set(cursorColumn, ch, currentForeground, currentBackground, currentBold, currentInverse, width);
        cursorColumn += width;
    }

    private void AppendSpaces(int count)
    {
        for (var i = 0; i < count; i++)
        {
            AppendChar(' ');
        }
    }

    private void NewLine()
    {
        cursorRow++;
        cursorColumn = 0;

        if (cursorRow >= rows)
        {
            ScrollUp();
            cursorRow = rows - 1;
        }
    }

    private void Backspace()
    {
        if (cursorColumn > 0)
        {
            cursorColumn--;
        }
    }

    private void ApplyControl(TerminalToken token)
    {
        if (token.SequenceKind == TerminalSequenceKind.Escape)
        {
            ApplyEscape(token.Command);
            return;
        }

        switch (token.Command)
        {
            case '@':
                ActiveScreen()[cursorRow].InsertBlank(cursorColumn, token.GetParameter(0, 1), columns);
                break;
            case 'A':
                cursorRow = Math.Max(0, cursorRow - token.GetParameter(0, 1));
                break;
            case 'B':
                cursorRow = Math.Min(rows - 1, cursorRow + token.GetParameter(0, 1));
                break;
            case 'C':
                cursorColumn = Math.Min(columns - 1, cursorColumn + token.GetParameter(0, 1));
                break;
            case 'D':
                cursorColumn = Math.Max(0, cursorColumn - token.GetParameter(0, 1));
                break;
            case 'E':
                cursorRow = Math.Min(rows - 1, cursorRow + token.GetParameter(0, 1));
                cursorColumn = 0;
                break;
            case 'F':
                cursorRow = Math.Max(0, cursorRow - token.GetParameter(0, 1));
                cursorColumn = 0;
                break;
            case 'G':
                cursorColumn = Math.Clamp(token.GetParameter(0, 1) - 1, 0, columns - 1);
                break;
            case 'H':
            case 'f':
                cursorRow = Math.Clamp(token.GetParameter(0, 1) - 1, 0, rows - 1);
                cursorColumn = Math.Clamp(token.GetParameter(1, 1) - 1, 0, columns - 1);
                break;
            case 'L':
                InsertLines(token.GetParameter(0, 1));
                break;
            case 'M':
                DeleteLines(token.GetParameter(0, 1));
                break;
            case 'P':
                ActiveScreen()[cursorRow].Delete(cursorColumn, token.GetParameter(0, 1));
                break;
            case 'S':
                ScrollUp(token.GetParameter(0, 1));
                break;
            case 'T':
                ScrollDown(token.GetParameter(0, 1));
                break;
            case 'X':
                ActiveScreen()[cursorRow].Erase(cursorColumn, token.GetParameter(0, 1));
                break;
            case 'J':
                ClearDisplay(token.GetParameter(0, 0));
                break;
            case 'K':
                ClearLine(token.GetParameter(0, 0));
                break;
            case 'd':
                cursorRow = Math.Clamp(token.GetParameter(0, 1) - 1, 0, rows - 1);
                break;
            case 'h':
                ApplyMode(token, enabled: true);
                break;
            case 'l':
                ApplyMode(token, enabled: false);
                break;
            case 'm':
                ApplySgr(token.Parameters);
                break;
            case 's':
            case '7':
                SaveCursor();
                break;
            case 'u':
                RestoreCursor();
                break;
        }
    }

    private void ApplyEscape(char command)
    {
        switch (command)
        {
            case '7':
                SaveCursor();
                break;
            case '8':
                RestoreCursor();
                break;
            case 'D':
                NewLine();
                break;
            case 'E':
                NewLine();
                cursorColumn = 0;
                break;
            case 'M':
                ReverseIndex();
                break;
            case 'c':
                Reset();
                break;
        }
    }

    private void ApplySgr(IReadOnlyList<int?> parameters)
    {
        if (parameters.Count == 0)
        {
            ResetStyle();
            return;
        }

        for (var i = 0; i < parameters.Count; i++)
        {
            var code = parameters[i] ?? 0;
            if (code == 0)
            {
                ResetStyle();
                continue;
            }

            if (code == 1)
            {
                currentBold = true;
                continue;
            }

            if (code == 22)
            {
                currentBold = false;
                continue;
            }

            if (code == 7)
            {
                currentInverse = true;
                continue;
            }

            if (code == 27)
            {
                currentInverse = false;
                continue;
            }

            if (code == 39)
            {
                currentForeground = DefaultForeground;
                continue;
            }

            if (code == 49)
            {
                currentBackground = DefaultBackground;
                continue;
            }

            if (code is >= 30 and <= 37)
            {
                currentForeground = AnsiPalette.GetStandardColor(code - 30, currentBold);
                continue;
            }

            if (code is >= 90 and <= 97)
            {
                currentForeground = AnsiPalette.GetStandardColor(code - 90, bright: true);
                continue;
            }

            if (code is >= 40 and <= 47)
            {
                currentBackground = AnsiPalette.GetStandardColor(code - 40, bright: false);
                continue;
            }

            if (code is >= 100 and <= 107)
            {
                currentBackground = AnsiPalette.GetStandardColor(code - 100, bright: true);
                continue;
            }

            if (code == 38 && i + 2 < parameters.Count && parameters[i + 1] == 5)
            {
                currentForeground = AnsiPalette.GetIndexedColor(parameters[i + 2] ?? 7);
                i += 2;
                continue;
            }

            if (code == 38 && i + 4 < parameters.Count && parameters[i + 1] == 2)
            {
                currentForeground = Color.FromArgb(
                    ClampColor(parameters[i + 2] ?? 255),
                    ClampColor(parameters[i + 3] ?? 255),
                    ClampColor(parameters[i + 4] ?? 255));
                i += 4;
                continue;
            }

            if (code == 48 && i + 2 < parameters.Count && parameters[i + 1] == 5)
            {
                currentBackground = AnsiPalette.GetIndexedColor(parameters[i + 2] ?? 0);
                i += 2;
                continue;
            }

            if (code == 48 && i + 4 < parameters.Count && parameters[i + 1] == 2)
            {
                currentBackground = Color.FromArgb(
                    ClampColor(parameters[i + 2] ?? 0),
                    ClampColor(parameters[i + 3] ?? 0),
                    ClampColor(parameters[i + 4] ?? 0));
                i += 4;
            }
        }
    }

    private void ResetStyle()
    {
        currentForeground = DefaultForeground;
        currentBackground = DefaultBackground;
        currentBold = false;
        currentInverse = false;
    }

    private void ClearDisplay(int mode)
    {
        EnsureScreenRows();
        var activeScreen = ActiveScreen();

        if (mode == 2)
        {
            activeScreen.Clear();
            EnsureScreenRows();
            cursorRow = 0;
            cursorColumn = 0;
            return;
        }

        if (mode == 0)
        {
            ClearLine(0);
            for (var i = cursorRow + 1; i < activeScreen.Count; i++)
            {
                activeScreen[i].Clear();
            }
            return;
        }

        if (mode == 1)
        {
            ClearLine(1);
            for (var i = 0; i < cursorRow; i++)
            {
                activeScreen[i].Clear();
            }
        }
    }

    private void ClearLine(int mode)
    {
        EnsureScreenRows();
        var line = ActiveScreen()[cursorRow];

        if (mode == 2)
        {
            line.Clear();
            return;
        }

        if (mode == 0)
        {
            line.RemoveFrom(cursorColumn);
            return;
        }

        if (mode == 1)
        {
            line.ClearUntil(cursorColumn);
        }
    }

    private void ScrollUp()
    {
        ScrollUp(1);
    }

    private void ScrollUp(int count)
    {
        EnsureScreenRows();
        var activeScreen = ActiveScreen();
        for (var i = 0; i < count; i++)
        {
            if (!useAlternateScreen)
            {
                scrollback.Add(activeScreen[0]);
            }

            activeScreen.RemoveAt(0);
            activeScreen.Add(new TerminalLine());
        }

        Trim();
    }

    private void ScrollDown(int count)
    {
        EnsureScreenRows();
        var activeScreen = ActiveScreen();
        for (var i = 0; i < count; i++)
        {
            activeScreen.Insert(0, new TerminalLine());
            if (activeScreen.Count > rows)
            {
                activeScreen.RemoveAt(activeScreen.Count - 1);
            }
        }
    }

    private void InsertLines(int count)
    {
        EnsureScreenRows();
        var activeScreen = ActiveScreen();
        for (var i = 0; i < count && cursorRow < activeScreen.Count; i++)
        {
            activeScreen.Insert(cursorRow, new TerminalLine());
            if (activeScreen.Count > rows)
            {
                activeScreen.RemoveAt(rows);
            }
        }
    }

    private void DeleteLines(int count)
    {
        EnsureScreenRows();
        var activeScreen = ActiveScreen();
        for (var i = 0; i < count && cursorRow < activeScreen.Count; i++)
        {
            activeScreen.RemoveAt(cursorRow);
            activeScreen.Add(new TerminalLine());
        }
    }

    private void SaveCursor()
    {
        savedCursorRow = cursorRow;
        savedCursorColumn = cursorColumn;
    }

    private void RestoreCursor()
    {
        cursorRow = Math.Clamp(savedCursorRow, 0, rows - 1);
        cursorColumn = Math.Clamp(savedCursorColumn, 0, columns - 1);
    }

    private void ReverseIndex()
    {
        if (cursorRow > 0)
        {
            cursorRow--;
            return;
        }

        ScrollDown(1);
    }

    private void ApplyMode(TerminalToken token, bool enabled)
    {
        foreach (var parameter in token.Parameters)
        {
            var mode = parameter ?? 0;
            if (token.IsPrivate && mode is 25)
            {
                cursorVisible = enabled;
                continue;
            }

            if (token.IsPrivate && mode is 47 or 1047 or 1049)
            {
                if (enabled && mode == 1049)
                {
                    SaveCursor();
                }

                useAlternateScreen = enabled;
                EnsureScreenRows();

                if (enabled)
                {
                    ActiveScreen().Clear();
                    EnsureScreenRows();
                }

                if (!enabled && mode == 1049)
                {
                    RestoreCursor();
                }
            }
        }
    }

    private void Reset()
    {
        scrollback.Clear();
        screen.Clear();
        alternateScreen.Clear();
        useAlternateScreen = false;
        cursorVisible = true;
        cursorRow = 0;
        cursorColumn = 0;
        savedCursorRow = 0;
        savedCursorColumn = 0;
        ResetStyle();
        EnsureScreenRows();
    }

    private void EnsureScreenRows()
    {
        var activeScreen = ActiveScreen();
        while (activeScreen.Count < rows)
        {
            activeScreen.Add(new TerminalLine());
        }

        while (activeScreen.Count > rows)
        {
            activeScreen.RemoveAt(activeScreen.Count - 1);
        }
    }

    private List<TerminalLine> ActiveScreen()
    {
        return useAlternateScreen ? alternateScreen : screen;
    }

    private IReadOnlyList<TerminalLine> ActiveScrollback()
    {
        return useAlternateScreen ? Array.Empty<TerminalLine>() : scrollback;
    }

    private void Trim()
    {
        while (scrollback.Count > MaxLines)
        {
            scrollback.RemoveAt(0);
        }
    }

    private static int ClampColor(int value)
    {
        return Math.Clamp(value, 0, 255);
    }

    private static int DisplayWidth(char ch)
    {
        if (char.IsControl(ch))
        {
            return 0;
        }

        var category = CharUnicodeInfo.GetUnicodeCategory(ch);
        if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark)
        {
            return 0;
        }

        return IsWideCharacter(ch) ? 2 : 1;
    }

    private static bool IsWideCharacter(char ch)
    {
        return ch is >= '\u1100' and <= '\u115f'
            or >= '\u2329' and <= '\u232a'
            or >= '\u2e80' and <= '\ua4cf'
            or >= '\uac00' and <= '\ud7a3'
            or >= '\uf900' and <= '\ufaff'
            or >= '\ufe10' and <= '\ufe19'
            or >= '\ufe30' and <= '\ufe6f'
            or >= '\uff00' and <= '\uff60'
            or >= '\uffe0' and <= '\uffe6';
    }
}

internal sealed class TerminalLine
{
    private readonly List<TerminalCell> cells = new();

    public TerminalLineSnapshot Snapshot()
    {
        return new TerminalLineSnapshot(cells.Select(cell => new TerminalCellSnapshot(
            cell.Character,
            cell.Foreground,
            cell.Background,
            cell.Bold,
            cell.Inverse,
            cell.Width)).ToArray());
    }

    public void Set(int column, char character, Color foreground, Color background, bool bold, bool inverse, int width)
    {
        while (cells.Count < column)
        {
            cells.Add(TerminalCell.Blank());
        }

        if (column > 0 && column - 1 < cells.Count && cells[column - 1].Width == 2)
        {
            cells[column - 1] = TerminalCell.Blank();
        }

        if (column == cells.Count)
        {
            cells.Add(new TerminalCell(character, foreground, background, bold, inverse, width));
        }
        else
        {
            cells[column].Character = character;
            cells[column].Foreground = foreground;
            cells[column].Background = background;
            cells[column].Bold = bold;
            cells[column].Inverse = inverse;
            cells[column].Width = width;
        }

        if (width == 2)
        {
            var nextColumn = column + 1;
            if (nextColumn == cells.Count)
            {
                cells.Add(new TerminalCell(' ', foreground, background, bold, inverse, 0));
            }
            else if (nextColumn < cells.Count)
            {
                cells[nextColumn].Character = ' ';
                cells[nextColumn].Foreground = foreground;
                cells[nextColumn].Background = background;
                cells[nextColumn].Bold = bold;
                cells[nextColumn].Inverse = inverse;
                cells[nextColumn].Width = 0;
            }
        }
        else if (column + 1 < cells.Count && cells[column + 1].Width == 0)
        {
            cells[column + 1] = TerminalCell.Blank();
        }
    }

    public void Clear()
    {
        cells.Clear();
    }

    public void RemoveFrom(int column)
    {
        if (column < cells.Count)
        {
            cells.RemoveRange(column, cells.Count - column);
        }
    }

    public void InsertBlank(int column, int count, int maxColumns)
    {
        column = Math.Clamp(column, 0, cells.Count);
        count = Math.Max(0, count);
        for (var i = 0; i < count; i++)
        {
            cells.Insert(column, TerminalCell.Blank());
        }

        TrimToColumns(maxColumns);
        NormalizeWideCells();
    }

    public void Delete(int column, int count)
    {
        if (column >= cells.Count || count <= 0)
        {
            return;
        }

        cells.RemoveRange(column, Math.Min(count, cells.Count - column));
        NormalizeWideCells();
    }

    public void Erase(int column, int count)
    {
        if (count <= 0)
        {
            return;
        }

        while (cells.Count < column + count)
        {
            cells.Add(TerminalCell.Blank());
        }

        var end = Math.Min(column + count, cells.Count);
        for (var i = column; i < end; i++)
        {
            cells[i] = TerminalCell.Blank();
        }
    }

    public void ClearUntil(int column)
    {
        var end = Math.Min(column + 1, cells.Count);
        for (var i = 0; i < end; i++)
        {
            cells[i].Character = ' ';
            cells[i].Foreground = TerminalBufferColors.DefaultForeground;
            cells[i].Background = TerminalBufferColors.DefaultBackground;
            cells[i].Bold = false;
            cells[i].Inverse = false;
            cells[i].Width = 1;
        }
    }

    private void TrimToColumns(int maxColumns)
    {
        if (cells.Count > maxColumns)
        {
            cells.RemoveRange(maxColumns, cells.Count - maxColumns);
        }
    }

    private void NormalizeWideCells()
    {
        for (var i = 0; i < cells.Count; i++)
        {
            if (cells[i].Width != 2)
            {
                continue;
            }

            var next = i + 1;
            if (next >= cells.Count)
            {
                cells[i] = TerminalCell.Blank();
                continue;
            }

            cells[next].Character = ' ';
            cells[next].Foreground = cells[i].Foreground;
            cells[next].Background = cells[i].Background;
            cells[next].Bold = cells[i].Bold;
            cells[next].Inverse = cells[i].Inverse;
            cells[next].Width = 0;
        }
    }
}

internal sealed class TerminalCell
{
    public TerminalCell(char character, Color foreground, Color background, bool bold, bool inverse, int width)
    {
        Character = character;
        Foreground = foreground;
        Background = background;
        Bold = bold;
        Inverse = inverse;
        Width = width;
    }

    public char Character { get; set; }

    public Color Foreground { get; set; }

    public Color Background { get; set; }

    public bool Bold { get; set; }

    public bool Inverse { get; set; }

    public int Width { get; set; }

    public static TerminalCell Blank()
    {
        return new TerminalCell(
            ' ',
            TerminalBufferColors.DefaultForeground,
            TerminalBufferColors.DefaultBackground,
            bold: false,
            inverse: false,
            width: 1);
    }
}

internal readonly struct TerminalLineSnapshot
{
    public TerminalLineSnapshot(IReadOnlyList<TerminalCellSnapshot> cells)
    {
        Cells = cells;
    }

    public IReadOnlyList<TerminalCellSnapshot> Cells { get; }
}

internal readonly struct TerminalCellSnapshot
{
    public TerminalCellSnapshot(char character, Color foreground, Color background, bool bold, bool inverse, int width)
    {
        Character = character;
        Foreground = foreground;
        Background = background;
        Bold = bold;
        Inverse = inverse;
        Width = width;
    }

    public char Character { get; }

    public Color Foreground { get; }

    public Color Background { get; }

    public bool Bold { get; }

    public bool Inverse { get; }

    public int Width { get; }
}

internal static class TerminalBufferColors
{
    public static readonly Color DefaultForeground = Color.FromArgb(242, 242, 242);
    public static readonly Color DefaultBackground = Color.FromArgb(12, 12, 12);
}

internal static class AnsiPalette
{
    private static readonly Color[] Normal =
    {
        Color.FromArgb(12, 12, 12),
        Color.FromArgb(197, 15, 31),
        Color.FromArgb(19, 161, 14),
        Color.FromArgb(193, 156, 0),
        Color.FromArgb(0, 55, 218),
        Color.FromArgb(136, 23, 152),
        Color.FromArgb(58, 150, 221),
        Color.FromArgb(204, 204, 204)
    };

    private static readonly Color[] Bright =
    {
        Color.FromArgb(118, 118, 118),
        Color.FromArgb(231, 72, 86),
        Color.FromArgb(22, 198, 12),
        Color.FromArgb(249, 241, 165),
        Color.FromArgb(59, 120, 255),
        Color.FromArgb(180, 0, 158),
        Color.FromArgb(97, 214, 214),
        Color.FromArgb(242, 242, 242)
    };

    public static Color GetStandardColor(int index, bool bright)
    {
        var palette = bright ? Bright : Normal;
        return palette[Math.Clamp(index, 0, palette.Length - 1)];
    }

    public static Color GetIndexedColor(int index)
    {
        index = Math.Clamp(index, 0, 255);
        if (index < 8)
        {
            return Normal[index];
        }

        if (index < 16)
        {
            return Bright[index - 8];
        }

        if (index >= 232)
        {
            var level = 8 + (index - 232) * 10;
            return Color.FromArgb(level, level, level);
        }

        var color = index - 16;
        var r = color / 36;
        var g = color / 6 % 6;
        var b = color % 6;
        return Color.FromArgb(ToColorCubeValue(r), ToColorCubeValue(g), ToColorCubeValue(b));
    }

    private static int ToColorCubeValue(int value)
    {
        return value == 0 ? 0 : 55 + value * 40;
    }
}

internal readonly struct TerminalToken
{
    public TerminalToken(string text)
    {
        Text = text;
        Command = '\0';
        Parameters = Array.Empty<int?>();
        Kind = TerminalTokenKind.Text;
        SequenceKind = TerminalSequenceKind.Text;
        IsPrivate = false;
    }

    public TerminalToken(char command, IReadOnlyList<int?> parameters, TerminalSequenceKind sequenceKind, bool isPrivate)
    {
        Text = string.Empty;
        Command = command;
        Parameters = parameters;
        Kind = TerminalTokenKind.Control;
        SequenceKind = sequenceKind;
        IsPrivate = isPrivate;
    }

    public TerminalTokenKind Kind { get; }

    public string Text { get; }

    public char Command { get; }

    public IReadOnlyList<int?> Parameters { get; }

    public TerminalSequenceKind SequenceKind { get; }

    public bool IsPrivate { get; }

    public int GetParameter(int index, int defaultValue)
    {
        return index < Parameters.Count && Parameters[index].HasValue ? Parameters[index]!.Value : defaultValue;
    }
}

internal enum TerminalTokenKind
{
    Text,
    Control
}

internal enum TerminalSequenceKind
{
    Text,
    Escape,
    Csi
}

internal sealed class AnsiParser
{
    private readonly StringBuilder plain = new();
    private readonly StringBuilder sequence = new();
    private AnsiParserState state;

    public IEnumerable<TerminalToken> Parse(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            switch (state)
            {
                case AnsiParserState.Ground:
                    if (ch == '\u001b')
                    {
                        if (plain.Length > 0)
                        {
                            yield return new TerminalToken(plain.ToString());
                            plain.Clear();
                        }

                        state = AnsiParserState.Escape;
                    }
                    else
                    {
                        plain.Append(ch);
                    }

                    break;

                case AnsiParserState.Escape:
                    if (ch == '[')
                    {
                        sequence.Clear();
                        state = AnsiParserState.Csi;
                    }
                    else if (ch == ']')
                    {
                        state = AnsiParserState.Osc;
                    }
                    else
                    {
                        yield return new TerminalToken(ch, Array.Empty<int?>(), TerminalSequenceKind.Escape, isPrivate: false);
                        state = AnsiParserState.Ground;
                    }

                    break;

                case AnsiParserState.Csi:
                    if (ch >= '@' && ch <= '~')
                    {
                        var raw = sequence.ToString();
                        yield return new TerminalToken(ch, ParseParameters(raw), TerminalSequenceKind.Csi, IsPrivateSequence(raw));
                        sequence.Clear();
                        state = AnsiParserState.Ground;
                    }
                    else
                    {
                        sequence.Append(ch);
                    }

                    break;

                case AnsiParserState.Osc:
                    if (ch == '\a')
                    {
                        state = AnsiParserState.Ground;
                    }
                    else if (ch == '\u001b')
                    {
                        state = AnsiParserState.OscEscape;
                    }

                    break;

                case AnsiParserState.OscEscape:
                    state = ch == '\\' ? AnsiParserState.Ground : AnsiParserState.Osc;
                    break;
            }
        }

        if (state == AnsiParserState.Ground && plain.Length > 0)
        {
            yield return new TerminalToken(plain.ToString());
            plain.Clear();
        }
    }

    private static IReadOnlyList<int?> ParseParameters(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return Array.Empty<int?>();
        }

        return value
            .TrimStart('?')
            .Split(';')
            .Select(part => int.TryParse(part, out var number) ? number : (int?)null)
            .ToArray();
    }

    private static bool IsPrivateSequence(string value)
    {
        return value.StartsWith("?", StringComparison.Ordinal);
    }
}

internal enum AnsiParserState
{
    Ground,
    Escape,
    Csi,
    Osc,
    OscEscape
}
