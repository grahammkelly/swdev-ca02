using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LMS;

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
    
    //Initially only FileBackedInMemoryDatabase (and a test that disables sqving), but allow for
    // other Database types in the future.
    public interface Database { 
        List<CourseDAO> GetCourses();
        List<Subject> GetSubjects();

        CourseDAO? GetCourseByCode(string courseCode);
        void AddCourse(CourseDAO course);
        void RemoveCourse(string courseId);

        Subject? GetSubjectByCode(string subjectCode);
        void AddSubject(Subject subject);
        void RemoveSubject(string subjectId);

        void Save();
    }

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
        public const string COURSE_FILE = "./courses.dat";  //Public to allow testing
        public const string SUBJECT_FILE = "./subjects.dat";  //Public to allow testing

        private readonly Dictionary<string, CourseDAO> _courses = new Dictionary<string, CourseDAO>();
        private readonly Dictionary<string, Subject> _subjects = new Dictionary<string, Subject>();
        
        internal protected FileBackedInMemoryDatabase() {            
            LoadDataFromFile(COURSE_FILE, SUBJECT_FILE);
        }

        private void LoadDataFromFile(string courseDataFile, string subjectsDataFile) {
            SubjectDataHandler.LoadSubjectsFromFile(subjectsDataFile)
                    .ForEach(subject => _subjects[subject.Code] = subject);
            CourseDataHandler.LoadCoursesFromFile(courseDataFile)
                    .ForEach(course => _courses[course.Code] = course);
        }


        public void Save() {
            CourseDataHandler.SaveCoursesToFile(COURSE_FILE, _courses.Values.ToList());
            SubjectDataHandler.SaveSubjectsToFile(SUBJECT_FILE, _subjects.Values.ToList());
        }

        public List<CourseDAO> GetCourses() => _courses.Values.ToList();   //This makes most sense to me but IntelliSense suggests `[.. _courses.Values]`

        public CourseDAO? GetCourseByCode(string courseCode) => 
                _courses.ContainsKey(courseCode) ? _courses[courseCode] : null;

        public void AddCourse(CourseDAO newCourse) {
            if (_courses.ContainsKey(newCourse.Code)) {
                throw new AlreadyExistsException($"Course with code {newCourse.Code} already exists.");
            }
            _courses[newCourse.Code] = newCourse;
        }

        public void RemoveCourse(string courseId) {
            if (!_courses.ContainsKey(courseId)) {
                throw new NotFoundException($"Course with ID {courseId} not found.");
            }
            _courses.Remove(courseId);
        }

        public List<Subject> GetSubjects() => _subjects.Values.ToList();


        public Subject? GetSubjectByCode(string subjectCode) => 
                _subjects.ContainsKey(subjectCode) ? _subjects[subjectCode] : null;

        public void AddSubject(Subject subject) {
            if (_subjects.ContainsKey(subject.Code)) {
                throw new AlreadyExistsException($"Subject with code {subject.Code} already exists.");
            }
            _subjects[subject.Code] = subject;
        }

        public void RemoveSubject(string subjectId) {
            if (!_subjects.ContainsKey(subjectId)) {
                throw new NotFoundException($"Subject with ID {subjectId} not found.");
            }
            _subjects.Remove(subjectId);
        }


        private static class SubjectDataHandler {
            // Line format on the file will be 'Type: {fields in JSON format}'
            // So For example, a 'TestedSubject' will be 'TestedSubject: {"Title":"Law","Credits":5,"Description":"...","Lecturer":"...","Code":"LAW101","ExamDuration":120}'
            // ...and a 'ContinuousAssessmentSubject' will be 'ContinuousAssessmentSubject: {"Title":"Programming","Credits":5,"Description":"...","Lecturer":"...","Code":"PROG101","Assignments":[{"Title":"Assignment 1","Weight":50,"Description":"..."},{"Title":"Assignment 2","Weight":50,"Description":"..."}]}'


            public static void SaveSubjectsToFile(string dataFile, List<Subject> subjects) {
                //Prepare a list of lines to write out
                List<string> lines = new List<string>();
                subjects.ForEach(subject => {
                    if(subject is TestedSubject) {
                        lines.Add($"{subject.GetClassType()}: {System.Text.Json.JsonSerializer.Serialize((TestedSubject)subject)}");
                    } else if(subject is ContinuousAssessmentSubject) {
                        lines.Add($"{subject.GetClassType()}: {System.Text.Json.JsonSerializer.Serialize((ContinuousAssessmentSubject)subject)}");
                    }
                });

                //Back up previous data
                if (File.Exists(dataFile)) {
                    File.Move(dataFile, $"{dataFile}.bak_{DateTime.UtcNow:yyyyMMddTHHmmss.ffff}");
                }

                //Now save the data
                File.WriteAllLines(dataFile, lines);
            }

            public static List<Subject> LoadSubjectsFromFile(string dataFile) {
                List<Subject> subjects = new List<Subject>();
                if (File.Exists(dataFile)) {
                    // Load the subjects from the file
                    List<string> lines = File.ReadAllLines(dataFile).ToList();
                    lines.ForEach(line => {
                        subjects.Add(ExtractSubjectFromLine(line));
                    });
                    
                }
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

        private static class CourseDataHandler {
            //Format of Courses on file will be in JSON format
            // So For example, a 'Course' will be '{"Title":"Computer Science","SubjectsByYear":{"1":{"Title":"Programming","Credits":5,"Description":"...","Lecturer":"...","Code":"PROG101","Assignments":[{"Title":"Assignment 1","Weight":50,"Description":"..."},{"Title":"Assignment 2","Weight":50,"Description":"..."}]}},"Description":"...","Code":"CS101"}'

            public static void SaveCoursesToFile(string dataFile, List<CourseDAO> courses) {
                //Prepare a list of the lines to write out
                List<string> lines = courses.Select(course => System.Text.Json.JsonSerializer.Serialize(course)).ToList();

                //First back up the old data files (if they exist)
                if (File.Exists(dataFile)) {
                    File.Move(dataFile, $"{dataFile}.bak_{DateTime.UtcNow:yyyyMMddTHHmmss.ffff}");
                }

                //Now save the data
                File.WriteAllLines(dataFile, lines);
            }

            public static List<CourseDAO> LoadCoursesFromFile(string dataFile) {
                List<CourseDAO> courses = new List<CourseDAO>();
                if (File.Exists(dataFile)) {
                    List<string> lines = File.ReadAllLines(dataFile).ToList();
                    lines.ForEach(line => {
                        CourseDAO? c = System.Text.Json.JsonSerializer.Deserialize<CourseDAO>(line);
                        if (c != null) {
                            courses.Add(c);
                        }
                    });
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
    }

    public class DatabaseTester : Tester {

        public static void PrepareDatabaseTests() {
            Tester.AddModuleTester(new DatabaseTester());
        }
        private DatabaseTester() : base("Database and DatabaseAccessModels") {}

        private Database GetDatabase() {
            //Start from a clean slate!
            EnsureFileDoesNotExist(FileBackedInMemoryDatabase.COURSE_FILE);
            EnsureFileDoesNotExist(FileBackedInMemoryDatabase.SUBJECT_FILE);

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

            //Create subjects to populate the DB with
            TestedSubject law101 = CreateTestedSubject("LAW101");
            ContinuousAssessmentSubject prog101 = CreateContinuousAssessmentSubject("PROG101");

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

            db.AddCourse(newCourse);

            db.Save();
            RunTest(File.Exists(FileBackedInMemoryDatabase.COURSE_FILE), 
                $"Database was saved, {FileBackedInMemoryDatabase.COURSE_FILE} should exist");
            RunTest(File.Exists(FileBackedInMemoryDatabase.SUBJECT_FILE), 
                $"Database was saved, {FileBackedInMemoryDatabase.SUBJECT_FILE} should exist");

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