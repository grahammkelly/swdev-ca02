// Copyright (C) 2026 Graham Kelly

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://gnu.org>.

using LMS.DisplayDataModels;
using Logging;

namespace LMS.UI;

// Abstracts the UI , so that all of the UIs will act consistently and follow the same interaction pattern.
public abstract class UI {
    private static int _lastErrorLineNum = 0;   //When not specified, use the last error line number to output errors onto!

    //
    //Interesting note - there are 2 main problems in development,
    // * Naming things
    // * Cache invalidation
    // * Off-by-one errors
    //
    // This is an example of a naming challenge. 'UILoop' is a very generic method name
    //
    public void UILoop() {
        bool keepGoing = true;
        while (keepGoing) {
            char choice = DisplayMenuAndGetSelection();
            keepGoing = HandleMenuSelection(choice);
        }
    }

    protected virtual void DisplayUIDetails() {
        Console.Clear();
    }

    protected char DisplayMenuAndGetSelection() {
        DisplayUIDetails();
        DisplayMenu();
        char selection = Char.ToUpper(Console.ReadKey(true).KeyChar);
        while (!(ValidSelectionsExcludingQuit().Contains(selection) || UserSelectedToQuit(selection))) {
            Console.WriteLine($"\nInvalid selection. Please try again, valid selections are [{string.Join(", ", ValidSelectionsExcludingQuit())} or Q]");
            Console.Write("Enter your choice: ");
            selection = Char.ToUpper(Console.ReadKey(true).KeyChar);
        }
        Console.WriteLine();
        return selection;
    }

    private void DisplayMenu() {
        DisplayChildMenu();
        Console.WriteLine("Q - Quit (or press Escape)");
    }

    protected abstract void DisplayChildMenu();

    protected abstract List<char> ValidSelectionsExcludingQuit();

    protected bool HandleMenuSelection(char choice) {
        //Assume the selection, if it gets here, is a valid choice as it doesn't get out of 'DisplayMenuAndGetSelection otherwise
        if (UserSelectedToQuit(choice)) {
            return false;
        }
        return HandleValidSelectionExcludingQuit(choice);
    }

    protected abstract bool HandleValidSelectionExcludingQuit(char choice);

    protected bool UserSelectedToQuit(char choice)  => 
            choice  == 'Q' || choice == (char)ConsoleKey.Escape;

    //Helper methods (static as they do not access any internal state of the class)
    protected void BlankLine() => Console.WriteLine("");

    protected static void ClearToEndOfLineFrom(int lineNum, int columnNum) {
        Console.SetCursorPosition(columnNum, lineNum);
        ClearToEndOfCurrentLine();
    }
    
    protected static void ClearToEndOfCurrentLine() {
        int currentLeft = Console.CursorLeft;   //Current column position
        int currentTop = Console.CursorTop;     //Current row position
        Console.Write(new String(' ', Console.WindowWidth - currentLeft));  //Overwrite line from this point to the full width of the window with spaces
        Console.SetCursorPosition(currentLeft, currentTop); //Restore cursor position to where we cleared from
    }
    
    protected static string TruncateTo(int maxLen, string str) =>
        str.Length <= maxLen ? str : str.Substring(0, maxLen);

    protected static void DisplayAtLine(int lineNum, string content) {
        Console.SetCursorPosition(0, lineNum);
        Console.Write(content);
    }

    protected static void SetErrorLineNumber(int errorLineNum) {
        _lastErrorLineNum = errorLineNum;
    }

    protected static void WriteUIErrorToConsole(string message) {
        int currentLine = Console.CursorTop;
        Console.SetCursorPosition(0, _lastErrorLineNum+1);
        Console.WriteLine($"** {message} **");
        Console.SetCursorPosition(0, currentLine);  //Return the cursor to its original position
    }

    protected static void WriteUIErrorToConsole(int errorLineNum, string message) {
        _lastErrorLineNum = errorLineNum;   //Remember the last erorr line number
        WriteUIErrorToConsole(message);
    }

    protected static void ClearUIErrorFromConsole() {
        int currentLine = Console.CursorTop;
        Console.SetCursorPosition(0, _lastErrorLineNum+1);
        Console.Write(new string(' ', Console.WindowWidth));
        Console.SetCursorPosition(0, currentLine);
    }

    protected ConsoleKey NavigateLines(int selectableLines, List<int> skipLines, ref int currentLine) {
        ConsoleKey key;
        while (true) {
            DisplayAtLine(currentLine, ">");
            key = GetEditKey();
            Logger.Debug($"Edit key pressed: {key}, line before action: {currentLine}, Lines displayed: {selectableLines} , Skip lines: [ {string.Join(",", skipLines)} ]");
            switch (key) {
                case ConsoleKey.UpArrow:
                    ClearUIErrorFromConsole();
                    DisplayAtLine(currentLine, " ");
                    if (currentLine == 1) { //Top line is ALWAYS unselectable
                        WriteUIErrorToConsole("Already at the top selectable line");
                    } else {
                        currentLine --;
                        if (skipLines.Contains(currentLine)) currentLine--;
                    }
                    break;
                case ConsoleKey.DownArrow:
                    ClearUIErrorFromConsole();
                    DisplayAtLine(currentLine, " ");
                    if (currentLine == selectableLines) {
                        WriteUIErrorToConsole("Already on the last selectable line");
                    } else {
                        currentLine++;
                        if (skipLines.Contains(currentLine)) currentLine++;
                    }
                    break;
                default:
                    return key;
            }
        }
        //Unreachable, but required to satisfy the compiler
        return ConsoleKey.Insert;   //Key does nothing, just to satisfy the compiler
    }

    private ConsoleKey GetEditKey() {
        ConsoleKey[] allowedKeys = { ConsoleKey.UpArrow, ConsoleKey.DownArrow, ConsoleKey.Enter, ConsoleKey.Escape, 
                ConsoleKey.Delete, ConsoleKey.Backspace, ConsoleKey.Add };
        ConsoleKeyInfo keyInfo = Console.ReadKey(true);
        ClearUIErrorFromConsole();
        if (! (allowedKeys.Contains(keyInfo.Key))) {
            WriteUIErrorToConsole("Invalid key. Please use Up, Down, Enter, '+', or Delete (Escape to exit).");
            keyInfo = Console.ReadKey(true);
        }
        return keyInfo.Key;
    }
}

public class LMSMainMenuUI : UI {

    private DataStore _db;

    public LMSMainMenuUI(DataStore ds) {
        _db = ds;
    }

    protected override void DisplayChildMenu() {
        Console.WriteLine("Main Menu:");
        Console.WriteLine("C - Course UI");
        Console.WriteLine("S - Subject UI");
    }

    protected override List<char> ValidSelectionsExcludingQuit() {
        return new List<char> { 'C', 'S' };
    }

    protected override bool HandleValidSelectionExcludingQuit(char choice) {
        switch (choice) {
            case 'C':
                new CourseUI(_db).UILoop();
                return true;
            case 'S':
                new SubjectUI(_db).UILoop();
                return true;
            default:
                Console.WriteLine("Invalid selection.");
                return true;
        }
    }
}


