using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LMS
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
    public interface Database { //Initially only InMemoryDatabase, but allow for other Database types in the future
        List<Course> GetCourses();
        List<Subject> GetSubjects();

        Course? GetCourseByCode(string courseCode);
        void AddCourse(Course course);
        void RemoveCourse(string courseId);

        Subject? GetSubjectByCode(string subjectCode);
        void AddSubject(Subject subject);
        void RemoveSubject(string subjectId);

        void Save();
    }

    //Nothing outside this 'Database' file should be able to instantiate or access the FileBasedInMemoryDatabase directly.
    // the only thing directly accessing this class should be the Database Factory or tests.
     internal class FileBackedInMemoryDatabase : Database {
        public const string COURSE_FILE = "./courses.dat";  //Public to allow testing
        public const string SUBJECT_FILE = "./subjects.dat";  //Public to allow testing

        private readonly Dictionary<string, Course> _courses = new Dictionary<string, Course>();
        private readonly Dictionary<string, Subject> _subjects = new Dictionary<string, Subject>();
        
        internal protected FileBackedInMemoryDatabase() {  //Not private as I want to be able to instantiate multiple Database instances in _tests_            
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

        public List<Course> GetCourses() => _courses.Values.ToList();   //This makes most sense to me but IntelliSense suggests `[.. _courses.Values]`

        public Course? GetCourseByCode(string courseCode) => 
                _courses.ContainsKey(courseCode) ? _courses[courseCode] : null;

        public void AddCourse(Course newCourse) {
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
                    return ExtractTestedSubjectFromLine(line);
                }
                else if (line.StartsWith("ContinuousAssessmentSubject:")) {
                    return ExtractContinuousAssessmentSubjectFromLine(line);
                }
                throw new InvalidDataException($"Unknown subject type in line: {line}");
            }

            private static TestedSubject ExtractTestedSubjectFromLine(string line) {
                string json = line.Substring("TestedSubject:".Length).Trim();
                return System.Text.Json.JsonSerializer.Deserialize<TestedSubject>(json);
            }

            private static ContinuousAssessmentSubject ExtractContinuousAssessmentSubjectFromLine(string line) {
                string json = line.Substring("ContinuousAssessmentSubject:".Length).Trim();
                return System.Text.Json.JsonSerializer.Deserialize<ContinuousAssessmentSubject>(json);
            }
        }

        private static class CourseDataHandler {
            //Format of Courses on file will be in JSON format
            // So For example, a 'Course' will be '{"Title":"Computer Science","SubjectsByYear":{"1":{"Title":"Programming","Credits":5,"Description":"...","Lecturer":"...","Code":"PROG101","Assignments":[{"Title":"Assignment 1","Weight":50,"Description":"..."},{"Title":"Assignment 2","Weight":50,"Description":"..."}]}},"Description":"...","Code":"CS101"}'

            public static void SaveCoursesToFile(string dataFile, List<Course> courses) {
                //Prepare a list of the lines to write out
                List<string> lines = courses.Select(course => System.Text.Json.JsonSerializer.Serialize(course)).ToList();

                //First back up the old data files (if they exist)
                if (File.Exists(dataFile)) {
                    File.Move(dataFile, $"{dataFile}.bak_{DateTime.UtcNow:yyyyMMddTHHmmss.ffff}");
                }

                //Now save the data
                File.WriteAllLines(dataFile, lines);
            }

            public static List<Course> LoadCoursesFromFile(string dataFile) {
                List<Course> courses = new List<Course>();
                if (File.Exists(dataFile)) {
                    List<string> lines = File.ReadAllLines(dataFile).ToList();
                    lines.ForEach(line => {
                        Course? c = System.Text.Json.JsonSerializer.Deserialize<Course>(line);
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
}