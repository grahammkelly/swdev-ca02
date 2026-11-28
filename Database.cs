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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LMS;
using Logging;

namespace LMS.Data
{
    public class DatabaseFactory {
        private static Database? INSTANCE;

        public static Database GetDatabase() {
            return INSTANCE == null ? (INSTANCE = new FileBackedInMemoryDatabase()) : INSTANCE;
        }

        //NB - Test purposes only. 
        // This will instantiate a new database and will load any saved data from the default files.
        // Any changes here will not affect the main database instance. and this database _should_ not allow saving the 
        // data back to the filesystem.
        public static Database GetTestDatabaseInstance() {
            return new TestDatabase();
        }
    }

    public interface ChangeNotificationReceiver {
        public abstract void CoursesChanged();
        public abstract void SubjectsChanged();
    }
    
    //Initially only FileBackedInMemoryDatabase (and a test that disables sqving), but allow for
    // other Database types in the future.
    public abstract class Database { 
        public abstract List<CourseDAO> GetCourses();
        public abstract List<Subject> GetSubjects();

        private List<ChangeNotificationReceiver> _changeReceivers = new List<ChangeNotificationReceiver>();

        public abstract CourseDAO? GetCourseByCode(string courseCode);
        public void AddUpdateCourse(CourseDAO course) {
            AddUpdateCourseInternal(course);
            //Notify Listeners
            _changeReceivers.ForEach(receiver => receiver.CoursesChanged());
        }
        protected abstract void AddUpdateCourseInternal(CourseDAO course);

        public void RemoveCourse(string courseId) {
            RemoveCourseInternal(courseId);
            _changeReceivers.ForEach(receiver => receiver.CoursesChanged());
        }
        protected abstract void RemoveCourseInternal(string courseId);

        public abstract Subject? GetSubjectByCode(string subjectCode);

        public void AddUpdateSubject(Subject subject) {
            AddUpdateSubjectInternal(subject);
            //Notify Listeners
            _changeReceivers.ForEach(receiver => receiver.SubjectsChanged());
        }

        protected abstract void AddUpdateSubjectInternal(Subject subject);

        public void RemoveSubject(string subjectId) {
            RemoveSubjectInternal(subjectId);
            _changeReceivers.ForEach(receiver => receiver.SubjectsChanged());
        }

        protected abstract void RemoveSubjectInternal(string subjectId);

        public abstract void Save();

        public void RemoveAllData() {
            RemoveAllDataInternal();
            _changeReceivers.ForEach(receiver => {
                receiver.CoursesChanged();
                receiver.SubjectsChanged();
            });
        }
    
        protected abstract void RemoveAllDataInternal();

        public void RegisterForNotifications(ChangeNotificationReceiver receiver) {
            _changeReceivers.Add(receiver);
        }
    }

    //I don't like 'internal', but want to restrict access to only this file.
    //
    //Nothing outside this 'Database' file should be able to instantiate or access the FileBasedInMemoryDatabase directly.
    // the only thing directly accessing this class should be the Database Factory or tests.
    //
    // The file storage here will get time-consuming to store/load data as time goes on. Would better
    // to use a _real_ (ish) database such as SQLite, LiteDB (https://github.com/litedb-org/litedb) or
    // CSharpDB (https://csharpdb.com/), but those could be an alternative implementation of the `Database`
    // interface.
    //
     internal class FileBackedInMemoryDatabase : Database {
        //According to Docs I can find, '/' should work for relative paths on BOTH Windows and Unix!
        protected readonly Dictionary<string, CourseDAO> _courses = new Dictionary<string, CourseDAO>();
        protected readonly Dictionary<string, Subject> _subjects = new Dictionary<string, Subject>();
        
        internal protected FileBackedInMemoryDatabase() {   
            DataHandlerHelpers.DoNotProvideBackups();         
            LoadDataFromFile();
        }

        private void LoadDataFromFile() {
            Logger.Debug("Loading data from filesystem");
            
            SubjectDataHandler.LoadSubjectsFromFile().ForEach(subject => _subjects[subject.Code] = subject);
            CourseDataHandler.LoadCoursesFromFile().ForEach(course => _courses[course.Code] = course);
        }


        public override void Save() {
            Logger.Debug("Saving to filesystem");
            int coursesSaved = CourseDataHandler.SaveCoursesToFile(_courses.Values.ToList());
            int subjectsSaved = SubjectDataHandler.SaveSubjectsToFile(_subjects.Values.ToList());
        }

        protected override void RemoveAllDataInternal() {
            throw new NotImplementedException("'RemoveAllData' is not implemented for the real database.");
        }

        public override List<CourseDAO> GetCourses() => _courses.Values.ToList();   //This makes most sense to me but IntelliSense suggests `[.. _courses.Values]`

        public override CourseDAO? GetCourseByCode(string courseCode) => 
                _courses.ContainsKey(courseCode) ? _courses[courseCode] : null;

        protected override void AddUpdateCourseInternal(CourseDAO newCourse) {
            _courses[newCourse.Code] = newCourse;
        }

        protected override void RemoveCourseInternal(string courseId) {
            if (!_courses.ContainsKey(courseId)) {
                throw new NotFoundException($"Course with ID {courseId} not found.");
            }
            _courses.Remove(courseId);
        }

        public override List<Subject> GetSubjects() => _subjects.Values.ToList();


        public override Subject? GetSubjectByCode(string subjectCode) => 
                _subjects.ContainsKey(subjectCode) ? _subjects[subjectCode] : null;

        protected override void AddUpdateSubjectInternal(Subject subject) {
            _subjects[subject.Code] = subject;
        }

        protected override void RemoveSubjectInternal(string subjectId) {
            if (!_subjects.ContainsKey(subjectId)) {
                throw new NotFoundException($"Subject with ID {subjectId} not found.");
            }
            _subjects.Remove(subjectId);
        }


        protected static class DataHandlerHelpers {
            private static bool _provideBackups = true;

            public static void DoNotProvideBackups() {
                _provideBackups = false;
            }

            public static string GetTargetFile(string dataFilePathAndPrefix) =>
                dataFilePathAndPrefix + ".dat";
            
            public static int SaveToFile(string dataFilePathAndPrefix, string dataType, List<string> lines) {
                string targetFileName = GetTargetFile(dataFilePathAndPrefix);
                if (_provideBackups) {
                    BackupIfExists(dataFilePathAndPrefix);
                }
                File.WriteAllLines(targetFileName, lines);
                Logger.Info($"Stored {lines.Count} {dataType} to file: {targetFileName}");
                return lines.Count;
            }

            private static void BackupIfExists(string dataFilePathAndPrefix) {
                string targetFileName = GetTargetFile(dataFilePathAndPrefix);
                string backupFileName = $"{dataFilePathAndPrefix}_{DateTime.UtcNow:yyyyMMddTHHmmss.ffff}.bkp";
                if (File.Exists(targetFileName)) {
                    File.Move(targetFileName, backupFileName);
                    Logger.Info($"Backed up existing `{targetFileName}` to `{backupFileName}`");
                }
            }
        }

        public static class SubjectDataHandler {
            public const string SUBJECT_FILE = "./subjects";  //Public to allow testing
            public static string GetSubjectFile() => DataHandlerHelpers.GetTargetFile(SUBJECT_FILE);
            
            // Line format on the file will be 'Type: {fields in JSON format}'
            // So For example, a 'TestedSubject' will be 'TestedSubject: {"Title":"Law","Credits":5,"Description":"...","Lecturer":"...","Code":"LAW101","ExamDuration":120}'
            // ...and a 'ContinuousAssessmentSubject' will be 'ContinuousAssessmentSubject: {"Title":"Programming","Credits":5,"Description":"...","Lecturer":"...","Code":"PROG101","Assignments":[{"Title":"Assignment 1","Weight":50,"Description":"..."},{"Title":"Assignment 2","Weight":50,"Description":"..."}]}'


            public static int SaveSubjectsToFile(List<Subject> subjects) {
                //Prepare a list of lines to write out
                List<string> lines = ConvertSubjectsToWritabelLines(subjects);

                Logger.Debug($"Saving Subjects:");
                lines.ForEach(l => Logger.Debug($"\t{l}"));

                return DataHandlerHelpers.SaveToFile(SUBJECT_FILE, "subjects", lines);
            }

            private static List<string> ConvertSubjectsToWritabelLines(List<Subject> subjects) {
                List<string> lines = new List<string>();
                foreach (Subject subject in subjects) {
                    if(subject is TestedSubject) {
                        lines.Add($"{subject.GetClassType()}: {System.Text.Json.JsonSerializer.Serialize((TestedSubject)subject)}");
                    } else if(subject is ContinuousAssessmentSubject) {
                        lines.Add($"{subject.GetClassType()}: {System.Text.Json.JsonSerializer.Serialize((ContinuousAssessmentSubject)subject)}");
                    }
                }
                return lines;
            }

            public static List<Subject> LoadSubjectsFromFile() {
                List<Subject> subjects = new List<Subject>();
                if (File.Exists(GetSubjectFile())) {
                    // Load the subjects from the file
                    List<string> lines = File.ReadAllLines(GetSubjectFile()).ToList();
                    lines.ForEach(line => {
                        Subject s = ExtractSubjectFromLine(line);   //Interim variable purely so I can log it!
                        subjects.Add(s);
                        Logger.Debug($"Loaded {s.GetClassType()} {s.Code} to memory");
                    });
                    
                }
                Logger.Info($"Loaded {subjects.Count} subjects from `{GetSubjectFile()}` - {string.Join(", ", subjects.Select(s => s.Code)) }");
                return subjects;
            }

            private static Subject ExtractSubjectFromLine(string line) {
                if (line.StartsWith("TestedSubject:")) {
                    Subject? s = ExtractTestedSubjectFromLine(line);
                    if (s == null || s is not TestedSubject) {
                        throw new InvalidDataException($"TestedSubject '{line}' is not a valid TestedSubject object.");
                    }
                    return s;
                }
                else if (line.StartsWith("ContinuousAssessmentSubject:")) {
                    Subject? s = ExtractContinuousAssessmentSubjectFromLine(line);
                    if (s == null || s is not ContinuousAssessmentSubject) {
                        throw new InvalidDataException($"ContinuousAssessmentSubject '{line}' is not a valid ContinuousAssessmentSubject object.");
                    }
                    return s;
                }
                throw new InvalidDataException($"Unknown subject type in line: {line}");
            }

            private static TestedSubject? ExtractTestedSubjectFromLine(string line) {
                string json = line.Substring("TestedSubject:".Length).Trim();
                return System.Text.Json.JsonSerializer.Deserialize<TestedSubject>(json);
            }

            private static ContinuousAssessmentSubject? ExtractContinuousAssessmentSubjectFromLine(string line) {
                string json = line.Substring("ContinuousAssessmentSubject:".Length).Trim();
                return System.Text.Json.JsonSerializer.Deserialize<ContinuousAssessmentSubject>(json);
            }
        }

        public static class CourseDataHandler {
            public const string COURSE_FILE = "./courses";  //Public to allow testing

            public static string GetCourseFile() => DataHandlerHelpers.GetTargetFile(COURSE_FILE);

            //Format of Courses on file will be in JSON format
            // So For example, a 'Course' will be '{"Title":"Computer Science","SubjectsByYear":{"1":{"Title":"Programming","Credits":5,"Description":"...","Lecturer":"...","Code":"PROG101","Assignments":[{"Title":"Assignment 1","Weight":50,"Description":"..."},{"Title":"Assignment 2","Weight":50,"Description":"..."}]}},"Description":"...","Code":"CS101"}'

            public static int SaveCoursesToFile(List<CourseDAO> courses) {
                //Prepare a list of the lines to write out
                List<string> lines = courses.Select(course => System.Text.Json.JsonSerializer.Serialize(course)).ToList();
                Logger.Debug($"Saving Courses:");
                lines.ForEach(l => Logger.Debug($"\t{l}"));

                return DataHandlerHelpers.SaveToFile(COURSE_FILE, "courses", lines);
            }

            public static List<CourseDAO> LoadCoursesFromFile() {
                List<CourseDAO> courses = new List<CourseDAO>();
                if (File.Exists(GetCourseFile())) {
                    List<string> lines = File.ReadAllLines(GetCourseFile()).ToList();
                    courses = CreateCourseDAOForEachLine(lines);
                }
                Logger.Info($"Loaded {courses.Count} courses from `{GetCourseFile()}` - {string.Join(", ", courses.Select(c => c.Code)) }");
                return courses;
            }

            private static List<CourseDAO> CreateCourseDAOForEachLine(List<string> lines) {
                List<CourseDAO> courses = new List<CourseDAO>();
                foreach (string line in lines) {
                    CourseDAO? c = System.Text.Json.JsonSerializer.Deserialize<CourseDAO>(line);
                    if (c != null) {
                        courses.Add(c);
                    }
                }
                return courses;
            }
        }
    }


    internal class TestDatabase : FileBackedInMemoryDatabase {
        internal TestDatabase() : base() {}

        new public void Save() {
            throw new InvalidOperationException("Test database should not save data.");
        }

        protected override void RemoveAllDataInternal() {
            
            _courses.Clear();
            _subjects.Clear();
        }
    }

    public class DatabaseTester : Tester {
        
        class TestChangeReceiver : ChangeNotificationReceiver {
            private Func<bool> _subjCallback = () => true;
            private Func<bool> _courseCallback = () => true;
            
            public Func<bool> SubjCallback { get => _subjCallback; set => _subjCallback = value; }
            public Func<bool> CourseCallback { get => _courseCallback; set => _courseCallback = value; }

            public void CoursesChanged() {
                _courseCallback?.Invoke();
            }

            public void SubjectsChanged() {
                _subjCallback?.Invoke();
            }
        }

        private class ChangeNotification {
            public bool SubjectChanged { get; set; } = false;
            public bool CourseChanged { get; set; } = false;

            public void Reset() {
                SubjectChanged = false;
                CourseChanged = false;
            }

            public bool SubjectWasChanged() => SubjectChanged;
            public bool CourseWasChanged() => CourseChanged;

            public Func<bool> SubjectCallback => () => SubjectChanged = true;
            public Func<bool> CourseCallback => () => CourseChanged = true;
        }

        public static void PrepareDatabaseTests() {
            Tester.AddModuleTester(new DatabaseTester());
        }
        private DatabaseTester() : base("Database and DatabaseAccessModels") {}

        private Database GetDatabase() {
            //Start from a clean slate!
            EnsureFileDoesNotExist(FileBackedInMemoryDatabase.CourseDataHandler.GetCourseFile());
            EnsureFileDoesNotExist(FileBackedInMemoryDatabase.SubjectDataHandler.GetSubjectFile());

            //Test the Database classes
            return DatabaseFactory.GetDatabase();
        }

        private void EnsureFileDoesNotExist(string filename) {
            if (File.Exists(filename)) File.Delete(filename);
        }

        private TestedSubject CreateTestedSubject(string code) {
            return new TestedSubject("Law", 5, "Introduction to Law", "Dr. Smith", code, 120);
        }

        private ContinuousAssessmentSubject CreateContinuousAssessmentSubject(string code) {
            ContinuousAssessmentSubject s = new ContinuousAssessmentSubject("Programming", 5,
                "Introduction to Programming", "Dr. Johnson", code);
            s.SetAssignments(new List<Assignment>() {
                new Assignment("Assignment 1", 50, "First Assignment"),
                new Assignment("Assignment 2", 150, "Second Assignment")
            });
            return s;
        }

        protected override void ExecuteModuleTests() {
            Database db = GetDatabase();    

            //The following setup will allow me to check if notifications are being received when data on the DB changes
            TestChangeReceiver testReceiver = new TestChangeReceiver();
            ChangeNotification changeNotification = new ChangeNotification();
            testReceiver.SubjCallback = changeNotification.SubjectCallback;
            testReceiver.CourseCallback = changeNotification.CourseCallback;
            db.RegisterForNotifications(testReceiver);

            //Create subjects to populate the DB with
            TestedSubject law101 = CreateTestedSubject("LAW101");
            ContinuousAssessmentSubject prog101 = CreateContinuousAssessmentSubject("PROG101");
            TestedSubject toBeDeleted = CreateTestedSubject("DELETE");

            //Check the weight was applied correctly
            RunTest(string.Join(", ", prog101.Assignments.Select(a => $"{a.Weight}")) == "25, 75", 
                "Assignments should have weight of 25/75");

            db.AddUpdateSubject(law101);
            RunTest(changeNotification.SubjectWasChanged(), 
            "Notification should have been received for adding a subject LAW101");
            RunTest(!changeNotification.CourseWasChanged(), 
                    "Course notification should not have been received for adding a subject LAW101");
            RunTest(db.GetSubjects().Count == 1, "Only one subject should be stored in the DB");
            RunTest(db.GetSubjectByCode(law101.Code) != null, "Subject LAW101 now exists");
            RunTest(db.GetSubjectByCode(law101.Code) is TestedSubject, 
                "Subject LAW101, stored to the DB, has the correct type");

            db.AddUpdateSubject(prog101);
            RunTest(db.GetSubjects().Count == 2, "Should now be 2 subjects on the DB");
            RunTest(db.GetSubjectByCode(prog101.Code) != null, "PROG101 should now exist");
            RunTest(db.GetSubjectByCode(prog101.Code) is ContinuousAssessmentSubject,
                "Subject PROG101, stored to the DB, has the correct type");

            //Reset these so we can check for notifications again
            changeNotification.Reset();

            db.AddUpdateSubject(toBeDeleted);
            RunTest(db.GetSubjects().Count == 3, "Should now be 3 subjects on the DB");
            RunTest(db.GetSubjectByCode(toBeDeleted.Code) != null, "DELETE should now exist");
            db.RemoveSubject(toBeDeleted.Code);
            RunTest(db.GetSubjects().Count == 2, "Should now be 2 subjects on the DB after deletion");
            RunTest(db.GetSubjectByCode(toBeDeleted.Code) == null, 
                    "Subject DELETE should no longer exist in the DB after deletion");
            RunTest(changeNotification.SubjectWasChanged(), "Subject notification should have been received for the subject deletion");
            RunTest(!changeNotification.CourseWasChanged(), 
                    "Course notification should not have been received for the subject deletion");

            CourseDAO newCourse = new CourseDAO("Computer Science", "Introduction to Computer Science", "CS101");
            newCourse.AddSubjectByYear(1, prog101);
            RunTest(newCourse.GetSubjectCodesForYear(1).Contains(prog101.Code), "Subject was added to the course correctly");

            string subjCodeFromCourse = newCourse.GetSubjectCodesForYear(1).First();
            Subject? subjFromCourse = db.GetSubjectByCode(subjCodeFromCourse);
            RunTest(subjCodeFromCourse != null, "Subject from the DB is not null");
            RunTest(subjFromCourse == prog101, "Subject from the code is the one expected");
            RunTest(subjFromCourse is ContinuousAssessmentSubject, 
                "Subject added to the course has the correct type");

            //Next line shows as a potential 'Dereference of a possibly null reference' and 
            // 'Converting null literal or possible null value to non-nullable type', however we have already checked 
            // that subjFromCourse is not null in a previous test
            RunTest(((ContinuousAssessmentSubject)subjFromCourse).Assignments.Count == prog101.Assignments.Count, 
                "Subject added to the course has the correct number of assignments?");

            //Added all these as I had a lot of trouble with serialization of the derived types
            RunTest(subjFromCourse == db.GetSubjectByCode("PROG101"), 
                "Subject added to the course should be the same as that on the database");

            Subject? subjFromDB = db.GetSubjectByCode("PROG101");
            RunTest(subjFromDB != null, "Subject retrieved from DB should not be null");

            //Again, next line us showing as a potential 'Dereference of a possibly null reference', however (again) we 
            // have already checked that subjFromDB is not null in the previous test
            RunTest(subjFromDB.Equals(subjFromCourse), "Subject added to the course and DB are the same?");

            //Again, reset these so we can check for notifications again. This time we'll check the course 
            // notifications get received and subject ones don't.
            changeNotification.Reset();

            db.AddUpdateCourse(newCourse);

            RunTest(changeNotification.CourseChanged, 
                    "Course notification should have been received for the course update");
            RunTest(!changeNotification.SubjectChanged, 
                    "Subject notification should not have been received for the course update");

            db.Save();

            string courseDataFile = FileBackedInMemoryDatabase.CourseDataHandler.GetCourseFile();
            RunTest(File.Exists(courseDataFile), $"Database was saved, {courseDataFile} should exist");
            string subjectDataFile = FileBackedInMemoryDatabase.SubjectDataHandler.GetSubjectFile();
            RunTest(File.Exists(subjectDataFile), $"Database was saved, {subjectDataFile} should exist");

            Database testDB = DatabaseFactory.GetTestDatabaseInstance();
            List<CourseDAO> storedCourses = db.GetCourses();
            List<CourseDAO> loadedCourses = testDB.GetCourses();
            RunTest(storedCourses.Count == loadedCourses.Count, "Loads the samenumber of courses as were stored");
            List<string> loadedCourseCodes = new List<string>();
            loadedCourses.ForEach(course => loadedCourseCodes.Add(course.Code));
            foreach (CourseDAO course in storedCourses) {
                RunTest(loadedCourseCodes.Contains(course.Code), $"Course '{course.Code}' was loaded from DB");
            }

            List<Subject> loadedSubjects = testDB.GetSubjects();
            List<Subject> storedSubjects = db.GetSubjects();
            RunTest(loadedSubjects.Count == storedSubjects.Count, 
                "Same amount of subjects should be loaded as saved");
            List<string> loadedSubjectCodes = loadedSubjects.Select(x => x.Code).ToList();
            foreach (Subject subj in loadedSubjects) {
                RunTest(loadedSubjectCodes.Contains(subj.Code), 
                    $"Subject: {subj.Code} should be in the loaded subject list");
            }
        }
    }
}