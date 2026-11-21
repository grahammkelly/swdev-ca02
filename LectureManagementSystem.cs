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

using LMS.Data;
using LMS.DisplayDataModels;
using Logging;

namespace LMS;

public class LectureManagementSystem {
    public static int Main(string[] args) {
        Logger.InitializeLogger("./output.log");
        new LectureManagementSystem().RunLMS();
        return 0;   //OK
    }

    public LectureManagementSystem() {
    }

    public void RunLMS() {
        DataStore db = PrepopulateDatabase();

        try {
            new LMSMainMenuUI(db).UILoop();
        } catch (Exception ex) {
            Logger.Error($"An error occurred: {ex.Message}");
        } finally {
            Logger.Info("Saving data to file system");
            db.Save();
        }

    }


    private DataStore PrepopulateDatabase() {
        DataStore ds = DataFactory.GetDatastore();
        // Add prepopulation logic here if needed
        //Set up some sample subjects
        ContinuousAssessmentSubject prog001 = new ContinuousAssessmentSubject("Software Development", 50, 
                "Introductory Software Development", "Joe Blogs", "PROG001");
        prog001.AddAssignment(new Assignment("Console application CA1", 60, "Console Application to do XXXXXX"));
        prog001.AddAssignment(new Assignment("Console application CA2", 40, "Another Console Application to do YYYYYY"));
        ds.AddUpdateSubject(prog001);

        ContinuousAssessmentSubject prog002 = new ContinuousAssessmentSubject("Advanced Software Development", 40, 
                "Advanced topics in Software Development", "John Smith", "PROG002");
        prog002.AddAssignment(new Assignment("GUI Application CA1", 50, "Advanced GUI Application to do ZZZZZZ"));
        ds.AddUpdateSubject(prog002);

        ContinuousAssessmentSubject finalProject = new ContinuousAssessmentSubject("Final Project", 60, 
                "Capstone project for the course", "Alice Johnson", "FINAL001");
        finalProject.AddAssignment(new Assignment("Final Project Proposal", 20, "Submit project proposal"));
        finalProject.AddAssignment(new Assignment("Final Project Implementation", 80, "Implement the final project"));
        ds.AddUpdateSubject(finalProject);

        ContinuousAssessmentSubject proc001 = new ContinuousAssessmentSubject("Software Process", 30, 
                "Introduction to Software Process", "Jane Doe", "PROC001");
        proc001.AddAssignment(new Assignment("Use case modeling, CA01", 100, "Model use cases for a LMS system"));
        ds.AddUpdateSubject(proc001);

        TestedSubject patentLaw = new TestedSubject("Patent Law", 20, 
                "Introduction to Patent Law as it applies to software", "Richard Roe", "LAW01", 120);
        ds.AddUpdateSubject(patentLaw);

        Course bscCompSci = new Course("BSc (Hons) Computer Science", "COMPSCI-L8", "Level 8, Bachelor of Science in Computer Science");
        bscCompSci.AddSubjectByYear(1, prog001);
        bscCompSci.AddSubjectByYear(1, proc001);
        bscCompSci.AddSubjectByYear(2, prog002);
        bscCompSci.AddSubjectByYear(2, patentLaw);
        bscCompSci.AddSubjectByYear(3, finalProject);
        ds.AddUpdateCourse(bscCompSci);
        Logger.Info($"Created course - {bscCompSci}");
        
        Course degCompSci = new Course("Degree in Computer Science", "COMPSCI-L7", "Level 7, Degree in Computer Science");
        degCompSci.AddSubjectByYear(1, prog001);
        degCompSci.AddSubjectByYear(1, proc001);
        degCompSci.AddSubjectByYear(2, finalProject);
        ds.AddUpdateCourse(degCompSci);
        Logger.Info($"Created course - {degCompSci}");

        return ds;
    }
}

// Abstracts the UI , so that all of the UIs will actr the same way.
public abstract class UI {
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

    protected abstract string ValidSelectionsExcludingQuit();

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

    protected static void WriteUIErrorToConsole(int errorLineNum, string message) {
        int currentLine = Console.CursorTop;
        Console.SetCursorPosition(0, errorLineNum+1);
        Console.WriteLine($"** {message} **");
        Console.SetCursorPosition(0, currentLine);  //Return the cursor to its original position
    }

    protected static void ClearUIErrorFromConsole(int errorLineNum) {
        int currentLine = Console.CursorTop;
        Console.SetCursorPosition(0, errorLineNum+1);
        Console.Write(new string(' ', Console.WindowWidth));
        Console.SetCursorPosition(0, currentLine);
    }

    protected ConsoleKey NavigateLines(int errorLineNum, int selectableLines, List<int> skipLines, ref int currentLine) {
        ConsoleKey key;
        while (true) {
            DisplayAtLine(currentLine, ">");
            key = GetEditKey(errorLineNum);
            Logger.Debug($"Edit key pressed: {key}, line before action: {currentLine}, Lines displayed: {selectableLines} , Skip lines: [ {string.Join(",", skipLines)} ]");
            switch (key) {
                case ConsoleKey.UpArrow:
                    ClearUIErrorFromConsole(errorLineNum);
                    DisplayAtLine(currentLine, " ");
                    if (currentLine == 1) { //Top line is ALWAYS unselectable
                        WriteUIErrorToConsole(errorLineNum, "Already at the top selectable line");
                    } else {
                        currentLine --;
                        if (skipLines.Contains(currentLine)) currentLine--;
                    }
                    break;
                case ConsoleKey.DownArrow:
                    ClearUIErrorFromConsole(errorLineNum);
                    DisplayAtLine(currentLine, " ");
                    if (currentLine == selectableLines) {
                        WriteUIErrorToConsole(errorLineNum, "Already on the last selectable line");
                    } else {
                        currentLine++;
                        if (skipLines.Contains(currentLine)) currentLine++;
                    }
                    break;
                default:
                    return key;
            }
        }
        return key;
    }

    private ConsoleKey GetEditKey(int errorLineNum) {
        ConsoleKey[] allowedKeys = { ConsoleKey.UpArrow, ConsoleKey.DownArrow, ConsoleKey.Enter, ConsoleKey.Escape, 
                ConsoleKey.Delete, ConsoleKey.Backspace, ConsoleKey.Add };
        ConsoleKeyInfo keyInfo = Console.ReadKey(true);
        ClearUIErrorFromConsole(errorLineNum);
        if (! (allowedKeys.Contains(keyInfo.Key))) {
            Console.SetCursorPosition(0, errorLineNum);
            WriteUIErrorToConsole(errorLineNum, "Invalid key. Please use Up, Down, Enter, '+', or Delete (Escape to exit).");
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

    protected override string ValidSelectionsExcludingQuit() {
        return "CS";
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

public class CourseUI : UI {
    private DataStore _db;

    public CourseUI(DataStore ds) {
        _db = ds;
    }

    protected override void DisplayUIDetails() {
        BlankLine();    
        List<string> courseCodes = DisplayCourses();
        // AllowSelectCourse(courseCodes);
    }

    private void AllowSelectCourse(List<string> courseCodes) {
        int errorLineNum = courseCodes.Count + 3;  // Set the error line number below the list of courses
        int currentLineNum = 1;

        Console.WriteLine("\nPress 'Enter' to select the current course, 'up' or 'down' to navigate. (Escape to exit).");
        Console.WriteLine("For Addition or removal of courses, press '+' or Delete respectively");

        while (currentLineNum <= courseCodes.Count) {
            ConsoleKey key = NavigateLines(errorLineNum, courseCodes.Count, [], ref currentLineNum);

            switch (key) { //Only need to handle the 'special' cases (i.e. cases specific to course selection)
                case ConsoleKey.Enter:
                    Course? selectedCourse = GetSelectedCourse(courseCodes[currentLineNum - 1]);
                    break;
                case ConsoleKey.Escape:
                    return;
                case ConsoleKey.Add:
                case ConsoleKey.OemPlus:
                    Logger.Debug("Selected to add a new course");
                    // Handle addition of a course
                    break;
                case ConsoleKey.Delete:
                    // Handle removal of a course
                    break;
            }
        }
    }

    private List<string> DisplayCourses() {
        Console.Clear();
        Console.WriteLine("Courses:");
        List<string> courseCodes = new List<string>();
        foreach (var courseCode in _db.Courses.Keys) {
            Console.WriteLine($"  {courseCode, -10} - {_db.Courses[courseCode].Title}");
            courseCodes.Add(courseCode);
        }
        Console.WriteLine("");
        return courseCodes;
    }

    protected override void DisplayChildMenu() {
        Console.WriteLine("Course Menu:");
        Console.WriteLine("S - Select a course (enter course code)");
    }

    protected override string ValidSelectionsExcludingQuit() {
        return "S"; // Add valid selections for the course menu
    }

    protected override bool HandleValidSelectionExcludingQuit(char choice) {
        // Handle course-specific menu selections here
        switch (choice) {
            case 'S':
                //Allow the use to 
                CourseSelectionUI();

                break;
            default:
                Console.WriteLine("Invalid selection.");
                break;
        }
        return true;
    }

    private void CourseSelectionUI() {
        Course? course = GetCourseToDisplay();
        if (course != null) {
            new CourseDetailsUI(_db, course).UILoop();
        }
    }

    private Course? GetCourseToDisplay() {
        Course? selectedCourse = null;
        while (selectedCourse == null) {
            Console.Write("Enter course code: ");
            string code = Console.ReadLine() ?? "";
            if (code == "" || code.Contains("\x1b")) { // Check for Escape key (ASCII 27)
                Console.WriteLine("Course code cannot be empty.");
                return null;
            } 
            selectedCourse = GetSelectedCourse(code);
            if (selectedCourse != null) {
                return selectedCourse;
            }
            Console.WriteLine("Invalid course code. Please enter a valid code from the list above.\n");
        }

        return  null;   //Will never get here as the loop above is infinite with explicit returns
    }

    private Course? GetSelectedCourse(string courseCode) =>
        _db.Courses.ContainsKey(courseCode) ? _db.Courses[courseCode] : null;
    
}

public class CourseDetailsUI : UI {
    private DataStore _db;
    private Course _course;

    public CourseDetailsUI(DataStore ds, Course course) {
        _db = ds;

        if (course == null) {
            throw new ArgumentNullException(nameof(course), "Course cannot be null.");
        }
        _course = course;
    }

    protected override void DisplayUIDetails() {
        List<int> skipLines;    //Will ignore this! for non-editing
        DisplayCourseDetails(false, out skipLines);
    }

    protected override void DisplayChildMenu() {
        Console.WriteLine("Course Details Menu:");
        Console.WriteLine("E - Edit course details");
        Console.WriteLine("R - Remove this course");
    }

    protected override string ValidSelectionsExcludingQuit() {
        return "ER";
    }

    protected override bool HandleValidSelectionExcludingQuit(char choice) {
        switch (choice) {
            case 'E':
                EditCourseDetails();
                break;
            case 'R':
                Logger.Info($"Removing course {_course.Code}");
                _db.RemoveCourse(_course.Code);
                return false;
            default:
                Console.WriteLine("Invalid selection.");
                break;
        }
        return true;
    }

    private int DisplayCourseDetails(bool forEdit, out List<int> skipLines) {
        int lineCount = 0;   //Will be used to padd a string to allow truncation, as C# seems to error out on truncation if a string is shorter than the max length specified
        skipLines = new List<int>();
        if (_course != null) {
            Console.Clear();
            Console.WriteLine($"  Course Code:        {_course.Code}");
            Console.WriteLine($"  Course Title:       {_course.Title}");
            Console.WriteLine($"  Course Description: {(forEdit ? TruncateTo(Console.WindowWidth - 22, _course.Description) : _course.Description)}");
            lineCount = 3; // Course Code, Title, Description
            foreach (long year in _course.SubjectsByYear.Keys) {
                Console.WriteLine($"  Year {year} subjects:");
                skipLines.Add(lineCount);
                lineCount++;
                
                foreach (Subject subject in _course.SubjectsByYear[year]) {
                    if (forEdit) {
                        Console.WriteLine($"        {subject.Code, 15} - {TruncateTo(Console.WindowWidth - 16, subject.Title)}");  //We need _exact_ spacing at start of the line for edit!
                    } else {
                        Console.WriteLine($"        {subject.Code, 15} - {subject}");
                    }
                    lineCount++;
                }
            }
            BlankLine();
        } else {
            Console.WriteLine("Course not found.");
        }
        return lineCount;
    }

    //This method will depend heavily on the 'DisplayCourseDetails' method as the lines will need to be the _exact_ same
    // We will allow the course to display, then clear the current line, allowing test entry
    private void EditCourseDetails() {
        List<int> skipLines;
        int linesDisplayed = DisplayCourseDetails(true, out skipLines);
        int errorLineNum = linesDisplayed + 4;

        Console.WriteLine("\nPress 'Enter' to edit the current line, 'up' or 'down' to navigate. (Escape to exit).");
        Console.WriteLine("For Addition or removal of subjects, press '+' or Delete respectively");

        int currentLineToEdit = 1;  //We will NOT allow the code to be editted
        while (currentLineToEdit < linesDisplayed) {
            var key = NavigateLines(errorLineNum, linesDisplayed, skipLines, ref currentLineToEdit);
            switch (key) {  //Only need to handle the 'special' cases (i.e. cases specific to course details)
                case ConsoleKey.Enter:
                    //Edit the current line
                    WriteUIErrorToConsole(errorLineNum, "Editing not implemented yet.");
                    break;
                case ConsoleKey.Escape:
                    return; //Exit this UI element (the editing)
                case ConsoleKey.Delete:
                case ConsoleKey.Backspace:
                    if (!IsSubjectLineSelected(currentLineToEdit)) {
                        WriteUIErrorToConsole(errorLineNum, "Operation is only alloweed on subject lines");
                        break;
                    }
                    //Remove the current subject                    
                    WriteUIErrorToConsole(errorLineNum, "Removing a subject not implemented yet.");
                    break;
                case ConsoleKey.Add:
                    if (!IsSubjectLineSelected(currentLineToEdit)) {
                        WriteUIErrorToConsole(errorLineNum, "Operation is only alloweed on subject lines");
                        break;
                    }
                    //Add a subject
                    WriteUIErrorToConsole(errorLineNum, "Adding a subject not implemented yet.");
                    break;
            }
        }        
    }

    private bool IsSubjectLineSelected(int lineNum) {
        return lineNum > 3;
    }

    private ConsoleKey GetEditKey(int errorLineNum) {
        ConsoleKey[] allowedKeys = { ConsoleKey.UpArrow, ConsoleKey.DownArrow, ConsoleKey.Enter, ConsoleKey.Escape, 
                ConsoleKey.Delete, ConsoleKey.Backspace, ConsoleKey.Add };
        ConsoleKeyInfo keyInfo = Console.ReadKey(true);
        ClearUIErrorFromConsole(errorLineNum);
        if (! (allowedKeys.Contains(keyInfo.Key))) {
            Console.SetCursorPosition(0, errorLineNum);
            WriteUIErrorToConsole(errorLineNum, "Invalid key. Please use Up, Down, Enter, '+', or Delete (Escape to exit).");
            keyInfo = Console.ReadKey(true);
        }
        return keyInfo.Key;
    }
}

public class SubjectUI : UI {
    private DataStore _db;

    public SubjectUI(DataStore ds) {
        _db = ds;
    }

    protected override void DisplayChildMenu() {
        Console.WriteLine("Subject Menu:");
        Console.WriteLine("S - Select a subject (enter subject code)");
    }

    protected override string ValidSelectionsExcludingQuit() {
        return "S";
    }

    protected override bool HandleValidSelectionExcludingQuit(char choice) {
        switch (choice) {
            case 'S':
                Console.WriteLine("Subject option selected.");
                break;
            default:
                Console.WriteLine("Invalid selection.");
                break;
        }
        return true;
    }
}
