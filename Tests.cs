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

using LMS;

void RunTest(bool passCondition, string testDesc) {
    Console.WriteLine($"Test: {testDesc} - {(passCondition ? "PASS" : "FAIL")}");  
}

Console.WriteLine("Running tests");

//Start from a clean slate!
if (File.Exists(FileBackedInMemoryDatabase.COURSE_FILE)) File.Delete(FileBackedInMemoryDatabase.COURSE_FILE);
if (File.Exists(FileBackedInMemoryDatabase.SUBJECT_FILE)) File.Delete(FileBackedInMemoryDatabase.SUBJECT_FILE);

//Test the Database classes
Database db = DatabaseFactory.GetDatabase();

//Populate subjects and courses in the database
TestedSubject law101 = new TestedSubject("Law", 5, "Introduction to Law", "Dr. Smith", "LAW101", 120);
ContinuousAssessmentSubject prog101 =  new ContinuousAssessmentSubject("Programming", 5, "Introduction to Programming", "Dr. Johnson", "PROG101");
prog101.SetAssignments(new List<Assignment>() {
    new Assignment("Assignment 1", 50, "First Assignment"),
    new Assignment("Assignment 2", 150, "Second Assignment")
});

//Check the balance was applied correctly
RunTest(string.Join(", ", prog101.Assignments.Select(a => $"{a.Weight}")) == "25, 75", 
    "Assignments should have weight of 25/75");

db.AddSubject(law101);
RunTest(db.GetSubjects().Count == 1, "Only one subject should be stored in the DB");
RunTest(db.GetSubjectByCode(law101.Code) != null, "Subject LAW101 now exists");
RunTest(db.GetSubjectByCode(law101.Code) is TestedSubject, 
    "Subject LAW101, stored to the DB, has the correct type");

db.AddSubject(prog101);
RunTest(db.GetSubjects().Count == 2, "Should now be 2 subjects on the DB");
RunTest(db.GetSubjectByCode(prog101.Code) != null, "PROG101 should now exist");
RunTest(db.GetSubjectByCode(prog101.Code) is ContinuousAssessmentSubject,
    "Subject PROG101, stored to the DB, has the correct type");

Course newCourse = new Course("Computer Science", "Introduction to Computer Science", "CS101");
newCourse.AddSubjectByYear(1, prog101);
RunTest(newCourse.GetSubjectCodesForYear(1).Contains(prog101.Code), "Subject was added to the course correctly");

string subjCodeFromCourse = newCourse.GetSubjectCodesForYear(1).First();
Subject? subjFromCourse = db.GetSubjectByCode(subjCodeFromCourse);
RunTest(subjCodeFromCourse != null, "Subject from the DB is not null");
RunTest(subjFromCourse == prog101, "Subject from the code is the one expected");
RunTest(subjFromCourse is ContinuousAssessmentSubject, 
    "Subject added to the course has the correct type");
RunTest(((ContinuousAssessmentSubject)subjFromCourse).Assignments.Count == prog101.Assignments.Count, 
    "Subject added to the course has the correct number of assignments?");

//Added all these as I had a lot of trouble with serialization of the derived types
RunTest(subjFromCourse == db.GetSubjectByCode("PROG101"), 
    "Subject added to the course should be the same as that on the database");

Subject? subjFromDB = db.GetSubjectByCode("PROG101");
RunTest(subjFromDB != null, "Subject retrieved from DB should not be null");
RunTest(subjFromDB.Equals(subjFromCourse), "Subject added to the course and DB are the same?");

db.AddCourse(newCourse);

db.Save();
RunTest(File.Exists(FileBackedInMemoryDatabase.COURSE_FILE), 
    $"Database was saved, {FileBackedInMemoryDatabase.COURSE_FILE} should exist");
RunTest(File.Exists(FileBackedInMemoryDatabase.SUBJECT_FILE), 
    $"Database was saved, {FileBackedInMemoryDatabase.SUBJECT_FILE} should exist");

Database testDB = DatabaseFactory.GetTestDatabaseInstance();
List<Subject> loadedSubjects = testDB.GetSubjects();
List<Subject> storedSubjects = db.GetSubjects();
RunTest(loadedSubjects.Count == storedSubjects.Count, 
    "Same amount of subjects should be loaded as saved");
List<string> loadedSubjectCodes = loadedSubjects.Select(x => x.Code).ToList();
foreach (Subject subj in loadedSubjects) {
    RunTest(loadedSubjectCodes.Contains(subj.Code), 
        $"Subject: {subj.Code} should be in the loaded subject list");
}
