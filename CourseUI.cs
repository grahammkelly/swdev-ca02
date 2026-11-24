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
                    if (forEdit) {
                        Console.WriteLine($"              {subject.Code, 15} - {TruncateTo(Console.WindowWidth - 16, subject.Title)}");  //We need _exact_ spacing at start of the line for edit!
                    } else {
                        Console.WriteLine($"              {subject.Code, 15} - {subject}");
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

        Console.WriteLine("\nPress 'Enter' to edit the current line, 'up' or 'down' to navigate. (Escape to exit).");
        Console.WriteLine("For Addition or removal of subjects, press '+' or Delete respectively");
        SetErrorLineNumber(linesDisplayed + 4); //Set the errors to display below the course detials and the instructions

        int currentLineToEdit = 1;  //We will NOT allow the code to be editted
        while (currentLineToEdit < linesDisplayed) {
            var key = NavigateLines(linesDisplayed, skipLines, ref currentLineToEdit);
            switch (key) {  //Only need to handle the 'special' cases (i.e. cases specific to course details)
                case ConsoleKey.Enter:
                    //Edit the current line
                    _db.AddUpdateCourse(EditCourseLine(currentLineToEdit, skipLines));        
                    Logger.Debug($"Editted course line {currentLineToEdit}. Course details are now {_course}");           
                    break;
                case ConsoleKey.Escape:
                    return; //Exit this UI element (the editing)
                case ConsoleKey.Delete:
                case ConsoleKey.Backspace:
                    if (!IsSubjectLineSelected(currentLineToEdit)) {
                        WriteUIErrorToConsole("Operation is only alloweed on subject lines");
                        break;
                    }
                    //Remove the current subject                    
                    WriteUIErrorToConsole("Removing a subject not implemented yet.");
                    break;
                case ConsoleKey.Add:
                    if (!IsSubjectLineSelected(currentLineToEdit)) {
                        WriteUIErrorToConsole("Operation is only alloweed on subject lines");
                        break;
                    }
                    //Add a subject
                    WriteUIErrorToConsole("Adding a subject not implemented yet.");
                    break;
            }
        }        
    }

    private bool IsSubjectLineSelected(int lineNum) {
        return lineNum > 3;
    }

    private Course EditCourseLine(int lineNum, List<int> skipLines) {

        if (skipLines.Count > 0 && lineNum >= skipLines[0]) {
            WriteUIErrorToConsole("You can add or remove subjects but not edit them.");
            return _course;
            
        }
            
        Console.SetCursorPosition(22, lineNum); //Magic number, 22 is the column the field contents starts displaying from
        ClearToEndOfCurrentLine();

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
                //Check the course code does not already exist (as we're adding the course!)
                Logger.Debug($"Setting the course code to {newContent}");
                _course.Code = newContent;
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

