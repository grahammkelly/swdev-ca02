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

using System.Runtime.InteropServices.Marshalling;
using LMS.Data;
using LMS.DisplayDataModels;
using Logging;

namespace LMS.UI;

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

    // private void AllowSelectCourse(List<string> courseCodes) {
    //     int currentLineNum = 1;

    //     Console.WriteLine("\nPress 'Enter' to select the current course, 'up' or 'down' to navigate. (Escape to exit).");
    //     Console.WriteLine("For Addition or removal of courses, press '+' or Delete respectively");
    //     SetErrorLineNumber(courseCodes.Count + 3);  // Set the error line number below the list of courses and the instructions

    //     while (currentLineNum <= courseCodes.Count) {
    //         ConsoleKey key = NavigateLines(courseCodes.Count, [], ref currentLineNum);

    //         switch (key) { //Only need to handle the 'special' cases (i.e. cases specific to course selection)
    //             case ConsoleKey.Enter:
    //                 Course? selectedCourse = GetSelectedCourse(courseCodes[currentLineNum - 1]);
    //                 break;
    //             case ConsoleKey.Escape:
    //                 return;
    //             case ConsoleKey.Add:
    //             case ConsoleKey.OemPlus:
    //                 Logger.Debug("Selected to add a new course");
    //                 // Handle addition of a course
    //                 break;
    //             case ConsoleKey.Delete:
    //                 // Handle removal of a course
    //                 break;
    //         }
    //     }
    // }

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
        Console.WriteLine("A - Add a course");
        Console.WriteLine("S - Select a course (enter course code)");
    }

    protected override List<char> ValidSelectionsExcludingQuit() {
        return new List<char> { 'A', 'S' }; // Add valid selections for the course menu
    }

    protected override bool HandleValidSelectionExcludingQuit(char choice) {
        // Handle course-specific menu selections here
        switch (choice) {
            case 'S':
                //Allow the use to 
                CourseSelectionUI();

                break;
            case 'A':
                Course? c = AddNewCourse();
                if (c != null) {
                    _db.AddUpdateCourse(c);
                    Logger.Info($"Added course - {c}");
                }
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

    private Course? AddNewCourse() {
        Course? newCourse = new CourseDetailsUI(_db, Course.EmptyCourse()).AddCourse();
        return newCourse;
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
            selectedCourse = GetSelectedCourse(code.ToUpper());
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

    public Course? AddCourse() {
        EditCourseDetails(true);
        return _course;
    }

    protected override List<char> ValidSelectionsExcludingQuit() {
        return new List<char> { 'E', 'R' };
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
        int lineCount = 0;   //Will be used to pad a string to allow truncation, as C# seems to error out on truncation if a string is shorter than the max length specified
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
                    Console.WriteLine($"              {subject.Code, -15} - {TruncateTo(Console.WindowWidth - 16, subject.Title)}");  //We need _exact_ spacing at start of the line for edit!
                    lineCount++;
                }
            }
            //3 Blank lines
            BlankLine();
            BlankLine();
            BlankLine();
        } else {
            Console.WriteLine("Course not found.");
        }
        return lineCount;
    }

    protected override ConsoleKey[] GetAllowedKeys() =>
        new ConsoleKey[] { ConsoleKey.UpArrow, ConsoleKey.DownArrow, ConsoleKey.Enter, ConsoleKey.Escape, 
                ConsoleKey.Delete, ConsoleKey.Backspace, ConsoleKey.Add, ConsoleKey.Spacebar };

    protected virtual string GetInvalidKeyMessage() =>
        "Invalid key. Please use Up, Down, Enter, '+', Delete, Spacebar (to save) or Escape (to exit).";


    private int DisplayCourseForEditing(bool newCourse, out List<int> skipLines) {
        int linesDisplayed = DisplayCourseDetails(true, out skipLines);
        if (newCourse) {
            Console.WriteLine($"\nPress 'Enter' to edit the current line, 'up' or 'down' to navigate. (Space to save and exit or Escape to abandon and exit)");
        } else {
            Console.WriteLine($"\nPress 'Enter' to edit the current line, 'up' or 'down' to navigate. (Escape to save and exit).");    
        }
        
        Console.WriteLine("For Addition or removal of subjects, press '+' or Delete (on the subject line) respectively");
        SetErrorLineNumber(linesDisplayed + 6); //Set the errors to display below the course detials and the instructions
        // The '6' above is pretty arbitrary, to give me some empty space between the end of the course details and 
        // the error details.
        return linesDisplayed;
    }

    //This method will depend heavily on the 'DisplayCourseDetails' method as the lines will need to be the _exact_ same
    // We will allow the course to display, then clear the current line, allowing test entry
    private Course? EditCourseDetails(bool newCourse = false) {
        List<int> skipLines;
        int linesDisplayed = DisplayCourseForEditing(newCourse, out skipLines);

        int topLine = (newCourse ? 0 : 1);
        int currentLineToEdit = topLine;  //We will NOT allow the code to be editted
        while (currentLineToEdit < linesDisplayed) {
            var key = NavigateLines(topLine, linesDisplayed, skipLines, ref currentLineToEdit);
            switch (key) {  //Only need to handle the 'special' cases (i.e. cases specific to course details)
                case ConsoleKey.Enter:
                    //Edit the current line
                    Course editedCourse = EditCourseLine(currentLineToEdit, skipLines);
                    if (!newCourse) _db.AddUpdateCourse(editedCourse);
                    Logger.Debug($"Editted course line {currentLineToEdit}. Course details are now {_course}");           
                    break;
                case ConsoleKey.Spacebar:
                    //For now - save the course and exit
                    if (_course.Code == null || _course.Code.Trim() == "") {
                        WriteUIErrorToConsole("Cannot save a course with no course code.");
                        break;
                    }
                    _db.AddUpdateCourse(_course);
                    return _course;
                case ConsoleKey.Escape:
                    return null; //Exit this UI element (the editing)
                case ConsoleKey.Delete:
                case ConsoleKey.Backspace:
                    if (!IsSubjectLineSelected(currentLineToEdit, skipLines)) {
                        WriteUIErrorToConsole("Operation is only alloweed on subject lines");
                        break;
                    }
                    //Remove the current subject                    
                    WriteUIErrorToConsole("Removing a subject not implemented yet.");
                    break;
                case ConsoleKey.Add:
                    //Add a subject
                    bool added = AddSubjectToCourse(linesDisplayed+1);
                    linesDisplayed = DisplayCourseForEditing(newCourse, out skipLines);
                    break;
            }
        }   
        return _course;     
    }

    private bool AddSubjectToCourse(int subjEntryLine) {
        bool added = false;
        Subject? subject = null;
        int yr = GetSubjectYear(subjEntryLine);
        if (yr > 0) {
            subject = GetSubjectToAdd(subjEntryLine+1);
            Logger.Debug($"Attempting to add subject {(subject?.Code ?? "null")} for year {yr}");
        }
        if (subject != null) {  //No need to check year as subject can only be non-empty if year was valid
            //Check if the subject exists for this year on the course (we will allow the same subject to be added on multiple years but NOT multiple times on the same year!)
            if (_course.SubjectsByYear.ContainsKey(yr) && _course.SubjectsByYear[yr].Contains(subject)) {
                WriteUIErrorToConsole($"Subject {subject.Code} is already on the list for year {yr}.");
            } else {
                if (!_course.SubjectsByYear.ContainsKey(yr)) {
                    _course.SubjectsByYear[yr] = new List<Subject>();
                }
                _course.SubjectsByYear[yr].Add(subject);
                Logger.Debug($"Added subject. Course is now - {_course}");
                added = true;
            }
        }
        return added;
    }

    private int GetSubjectYear(int subjEntryLine) {
        int yr = 0;
        while (yr == 0) {
            Console.SetCursorPosition(0, subjEntryLine);
            Console.Write("Adding a subject for the course. What year of the course? ");
            ConsoleKeyInfo key = Console.ReadKey();
            switch (key.Key) {
                case ConsoleKey.Escape:
                    yr = -1; // Indicate escape pressed
                    break;
                default:
                    yr = key.KeyChar - '0';
                    if (yr == 0) {
                        WriteUIErrorToConsole("Year cannot be 0.");
                        yr = 0;
                    } else if (yr > 9) {
                        WriteUIErrorToConsole("Invalid course year. Must enter a year between 1 and 9 (inclusive).");
                        yr = 0;
                    }
                    break;
            }
        }
        return yr;
    }

    private Subject? GetSubjectToAdd(int subjEntryLine) {
        string? code = null;
        while (code == null) {
            Console.SetCursorPosition(0, subjEntryLine);
            Console.Write("Enter the subject code: ");
            code = Console.ReadLine().ToUpper();
            if (code != "" && !_db.Subjects.ContainsKey(code)) {
                WriteUIErrorToConsole($"Subject {code} does not exist in the database.");
                code = null; // Reset code to prompt again
                ClearToEndOfLineFrom(24, subjEntryLine);   //Clear out the incorrect code!
            }
        }
        return string.IsNullOrEmpty(code.Trim()) ? null : _db.Subjects[code];  //We know this will always be non-null due to the 'ContainsKey'check above
    }

    private bool IsSubjectLineSelected(int lineNum, List<int> skipLines) {
        return (skipLines.Count > 0 && lineNum >= skipLines[0]);
    }

    private Course EditCourseLine(int lineNum, List<int> skipLines) {

        if (skipLines.Count > 0 && lineNum >= skipLines[0]) {
            WriteUIErrorToConsole("You can add or remove subjects but not edit them.");
            return _course;
            
        }
            
        //Console.SetCursorPosition(22, lineNum); //Magic number, 22 is the column the field contents starts displaying from
        //ClearToEndOfCurrentLine();
        ClearToEndOfLineFrom(22, lineNum);

        //Research on StackOverflow and in MS documentation does not show any way to pre-seed the 
        // current field contents into the STDIN buffer so the user needs to re-enter the entire 
        // field contents.
        string? newContent = Console.ReadLine();
        if (string.IsNullOrEmpty(newContent)) {
            WriteUIErrorToConsole("Input cannot be empty.");
            return _course;
        }

        //Very blunt way to specify the field to update based on the line number
        switch (lineNum) {  //Remember, the line numbers are 0 based
            case 0: // The Code (not available for 'edit' but will be for addition)
                newContent = newContent.ToUpper();  //Make sure the code is always upper case
                //Check the course code does not already exist (as we're adding the course!)
                if (_db.Courses.ContainsKey(newContent)) {
                    WriteUIErrorToConsole($"A course with code '{newContent}' already exists.");
                    ClearToEndOfLineFrom(22, lineNum);   //Clear out the incorrect code!
                    Logger.Warning($"A course with code {newContent} already exists, not storing this code");
                } else {
                    Logger.Debug($"Setting the course code to {newContent}");
                    _course.Code = newContent;
                }
                break;
            case 1: //Title line
                Logger.Debug($"Setting the course title to {newContent}");
                _course.Title = newContent;
                break;
            case 2: //Description line
                Logger.Debug($"Setting the course description to {newContent}");
                _course.Description = newContent;
                break;
            default: //Anything not covered as a specific line (above) is a subject line
                //Edit is not allowed for subjects, You can Add or remove subjects, but we'll need 
                // to use a different input UI for Subjects
                WriteUIErrorToConsole("Should not have gotten here.");
                break;
        }
        return _course;
    }
}

