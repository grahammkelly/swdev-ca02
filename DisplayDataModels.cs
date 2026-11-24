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
using Logging;

namespace LMS.DisplayDataModels;

public class DataFactory {
  private static DataStore? INSTANCE;

  public static DataStore GetDatastore() {
    if (INSTANCE == null) {
      INSTANCE = new DataStore(DatabaseFactory.GetDatabase());
    }

    return INSTANCE;
  } 
}

public class DataStore : ChangeNotificationReceiver {
  private readonly Database _db;

  private Dictionary<string, Course> _courses = new Dictionary<string, Course>();
  public Dictionary<string, Course> Courses { get => _courses; } //Unable to make the returned Dictionary Immutable!!!!

  private Dictionary<string, Subject> _subjects = new Dictionary<string, Subject>();
  public Dictionary<string, Subject> Subjects { get => _subjects; } //Same!!!

  private CourseConverter _courseConverter;

  internal DataStore(Database db) {
    _db = db;
    _courseConverter = new CourseConverter(_subjects);

    LoadSubjectData();
    LoadCourseData();

    _db.RegisterForNotifications(this);
  }

  public void Save() {
    Logger.Debug($"Datastore Saving - \n Courses: ${_courses}\nSubjects: {_subjects}");
    _db.Save();
  }

  public void CoursesChanged() {
    Logger.Debug("Courses changed notification received.");
    //Just reload all courses. Crude but effective
    LoadCourseData();
  }

  public void SubjectsChanged() {
    Logger.Debug("Subjects changed notification received.");
    //Just reload all Subjects. Again, crude but effective
    LoadSubjectData();
  }

  private void LoadSubjectData() {
    _subjects.Clear();
    foreach (Subject s in _db.GetSubjects()) {
      _subjects[s.Code] = s;
    }
  }

  private void LoadCourseData() {
    _courses.Clear();
    foreach (CourseDAO c in _db.GetCourses()) {
      _courses[c.Code] = _courseConverter.CreateCourseFromDAO(c);
    }
  }

  public Course? GetCourse(string courseCode) {
    return _courses.ContainsKey(courseCode) ? _courses[courseCode] : null;
  }

  public Course AddUpdateCourse(Course course) {
    Logger.Info($"Adding/updating course {course.Code}");
    _db.AddUpdateCourse(_courseConverter.CreateCourseDAOFromCourse(course));
    _courses[course.Code] = course;
    return course;
  }

  public void RemoveCourse(string courseCode) {
    Logger.Debug($"Remove request for Course {courseCode}");
    if (_courses.ContainsKey(courseCode)) {
      _db.RemoveCourse(courseCode);
      Logger.Info($"Course {courseCode} removed from DB and local cache.");
    }
  }

  public Subject? GetSubject(string subjectCode) {
    return _subjects.ContainsKey(subjectCode) ? _subjects[subjectCode] : null;
  }

  public Subject AddUpdateSubject(Subject subject) {
    Logger.Debug($"Adding/updating subject {subject.Code}");
    _subjects[subject.Code] = subject;
    _db.AddUpdateSubject(subject);
    return subject;
  }

  public void RemoveSubject(string subjectCode) {
    Logger.Debug($"Remove request for Subject {subjectCode}");
    if (_subjects.ContainsKey(subjectCode)) {
      _db.RemoveSubject(subjectCode);
      Logger.Info($"Subject {subjectCode} removed from DB and local cache.");
    }
  }
}



public class CourseConverter {

  private readonly Dictionary<string, Subject> _subjects;

  public CourseConverter(Dictionary<string, Subject> subjects) {
    _subjects = subjects;
  }

  public Course CreateCourseFromDAO(CourseDAO c) {
    Dictionary<long, List<Subject>> subjectsByYear = new Dictionary<long, List<Subject>>();
    foreach (long yr in c.SubjectCodesByYear.Keys) {
      List<Subject> subjects = PopulateSubjectsForYear(c.SubjectCodesByYear[yr]);
      subjectsByYear.Add(yr, subjects);
    }
    return new Course(c.Title, c.Code, c.Description, subjectsByYear);
  }

  public CourseDAO CreateCourseDAOFromCourse(Course course) {
    CourseDAO dao = new CourseDAO(course.Title, course.Description, course.Code);
    foreach (long yr in course.SubjectsByYear.Keys) {
      course.SubjectsByYear[yr].ForEach(s => dao.AddSubjectCodeByYear(yr, s.Code));
    }
    return dao;
  }

  private List<Subject> PopulateSubjectsForYear(List<string> subjectCodes) {
    List<Subject> subjectsList = new List<Subject>();
    foreach (string code in subjectCodes) {
      AddSubjectIfExistsOnDB(subjectsList, _subjects[code]);
    }
    return subjectsList;
  }

  private void AddSubjectIfExistsOnDB(List<Subject> subjectsList, Subject subject) {
    if (_subjects.ContainsKey(subject.Code) && !subjectsList.Contains(subject)) {
      subjectsList.Add(subject);
    }
  }
}


public class Course {
  public string Title { get; set; }
  public string Code { get; set; }
  public string Description { get; set; }
  public Dictionary<long, List<Subject>> SubjectsByYear = new Dictionary<long, List<Subject>>();

  public Course(string title, string code, string description, Dictionary<long, List<Subject>> subjectsByYear) : 
        this(title, code, description){
    SubjectsByYear = subjectsByYear;
  }

  public Course(string title, string code, string description) {
    Title = title;
    Code = code;
    Description = description;
  }

  public void AddSubjectByYear(long year, Subject? s) {
    if (s == null) {  //Fast fail if null passed in!
      return;
    }

    if (!SubjectsByYear.ContainsKey(year)) {
      SubjectsByYear[year] = new List<Subject>();
    }
    SubjectsByYear[year].Add(s);
  }

  public void RemoveSubjectFromYear(long year, string subjectCode) {
    Logger.Info($"Attempting to remove subject {subjectCode} from course {Code} for year {year}: Subject years [{String.Join(", ", SubjectsByYear.Keys)}]. Year {year}: {(SubjectsByYear.ContainsKey(year) ? string.Join(", ", SubjectsByYear[year].Select(s=> s.Code)) : "-None-")}");

    //Check the course has the year, AND that the subject code is a subject for the specific year
    if (! SubjectsByYear.ContainsKey(year)) {
      throw new NotFoundException($"Course {Code}: No subjects foryear {year}");
    }
    Subject? s = SubjectsByYear[year].First(sub => sub.Code == subjectCode);
    if (s == null) {
      throw new NotFoundException($"Course {Code}: Subject {subjectCode} is not studed in year {year}");
    }
    //OK, the year exists, and the subject is one for that year. Remove it
    SubjectsByYear[year].Remove(s);

    //Now check if the subject list for that year is empty and remove the year if so.
    if (SubjectsByYear[year].Count == 0) {
      SubjectsByYear.Remove(year);
    }
  }

  public List<Subject> GetSubjectsForYear(long year) {
    return SubjectsByYear.ContainsKey(year) ? SubjectsByYear[year].AsReadOnly().ToList() : new List<Subject>();
  }

  public override string ToString() {
    return $"Course: {Code} - {Title}, Description: {Description}, Subjects By Year: [\n\t{string.Join("\n\t", SubjectsByYear.Select(kv => $"{kv.Key}: [{string.Join(", ", kv.Value)}]"))}\n]";
  }
}


public class DisplayDataModelTester : Tester {
  public static void PrepareDisplayDataModelTests() {
    Tester.AddModuleTester(new DisplayDataModelTester());
  }

  private DisplayDataModelTester() : base("Data models for display") {}


  protected override void ExecuteModuleTests() {
    // Implement tests for DisplayDataModel here

    Database testDB = DatabaseFactory.GetTestDatabaseInstance();
    
    //Populate the test database with subjects and courses
        // TestedSubject(string title, long credits, string description, string lecturer, string code, long examDuration)
    TestedSubject ts1 = new TestedSubject("Test Subject 1", 10, "Description for Tested Test Subject 1", "Lecturer 1", "TS1", 120);
    testDB.AddUpdateSubject(ts1);
    TestedSubject ts2 = new TestedSubject("Test Subject 2", 10, "Description for Tested Test Subject 2", "Lecturer 2", "TS2", 120);
    testDB.AddUpdateSubject(ts2);
    TestedSubject ts3 = new TestedSubject("Test Subject 3", 10, "Description for Tested Test Subject 3", "Lecturer 3", "TS3", 120);
    testDB.AddUpdateSubject(ts3);
    TestedSubject ts4 = new TestedSubject("Test Subject 4", 10, "Description for Tested Test Subject 4", "Lecturer 4", "TS4", 120);
    testDB.AddUpdateSubject(ts4);

    ContinuousAssessmentSubject cas1 = new ContinuousAssessmentSubject("Continuous Assessment Subject 1", 10, 
        "Description for Continuous Assessment Subject 1", "Lecturer 1", "CAS1");
    cas1.AddAssignment(new Assignment("Assessment 1", 100, "Assessment 1"));
    testDB.AddUpdateSubject(cas1);
    ContinuousAssessmentSubject cas2 = new ContinuousAssessmentSubject("Continuous Assessment Subject 2", 10, 
        "Description for Continuous Assessment Subject 2", "Lecturer 2", "CAS2");
    cas2.AddAssignment(new Assignment("Assessment 2", 100, "Assessment 2"));
    testDB.AddUpdateSubject(cas2);
    ContinuousAssessmentSubject cas3 = new ContinuousAssessmentSubject("Continuous Assessment Subject 3", 10, 
        "Description for Continuous Assessment Subject 3", "Lecturer 3", "CAS3");
    cas3.AddAssignment(new Assignment("Assessment 3", 100, "Assessment 3"));
    testDB.AddUpdateSubject(cas3);
    
    CourseDAO c1 = new CourseDAO("Course 1", "Description for Course 1", "C1");
    c1.AddSubjectCodeByYear(1, cas1.Code);
    c1.AddSubjectCodeByYear(1, ts1.Code);
    c1.AddSubjectCodeByYear(2, ts2.Code);
    testDB.AddUpdateCourse(c1);

    CourseDAO c2 = new CourseDAO("Course 2", "Description for Course 2", "C2");
    c2.AddSubjectCodeByYear(1, cas2.Code);
    testDB.AddUpdateCourse(c2);

    //Tests

    DataStore testDatastore = new DataStore(testDB);

    Course c = testDatastore.GetCourse("C1");

    RunTest(c.Code == "C1", "Retrieved course should have code 'C1'");
    // Console.WriteLine(c);
    List<Subject> year1Subjects = c.GetSubjectsForYear(1);
    RunTest(year1Subjects.Count == 2, "Year 1 should have 2 subjects");
    RunTest(year1Subjects[0] == cas1, "First subject in Year 1 should be the CAS1 object");

    //Test changes to the DB will update the Datastore
    RunTest(testDB.GetSubjectByCode("CAS3") != null, "Test Database should have subject \"CAS3\"");
    RunTest(testDatastore.GetSubject("CAS3") != null, "Test Datastore should have subject \"CAS3\"");
    testDB.RemoveSubject("CAS3");
    RunTest(testDB.GetSubjectByCode("CAS3") == null, "Test Database should not have subject \"CAS3\" after removal");
    RunTest(testDatastore.GetSubject("CAS3") == null, "\"CAS3\" should have been removed from the Datastore when deleted from the test DB");

    //Tests for the CourseConverter
    CourseConverter cut = new CourseConverter(testDatastore.Subjects); //'cut' -> Class under test!.CreateCourseFromDAO(courseForConversion);
    CourseDAO courseForConversion = new CourseDAO("Course for Conversion", "Description for Course for Conversion", "CONV");
    courseForConversion.AddSubjectCodeByYear(1, cas1.Code);
    RunTest(courseForConversion.GetSubjectCodesForYear(1).Contains(cas1.Code), 
        "CourseDAO, for Conversion, should have CAS1 in Year 1");
    Course converted = cut.CreateCourseFromDAO(courseForConversion);
    List<Subject> subjects = converted.GetSubjectsForYear(1);
    RunTest(subjects.Count == 1, "Converted course should have 1 subject in Year 1");
    RunTest(subjects[0].Code == cas1.Code, "First subject in Year 1 of converted course should be CAS1");
    RunTest(subjects[0] == cas1, "First subject in Year 1 of converted course should be the CAS1 object");

    CourseDAO reconverted  = cut.CreateCourseDAOFromCourse(converted);
    List<string> subjectCodes = reconverted.GetSubjectCodesForYear(1);
    RunTest(subjectCodes.Count == 1, "Reconverted CourseDAO should have 1 subject code in Year 1");
    RunTest(subjectCodes[0] == cas1.Code, "First subject code in Year 1 of reconverted CourseDAO should be CAS1");
  }
}