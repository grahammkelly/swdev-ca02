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
  private Dictionary<string, Subject> _subjects = new Dictionary<string, Subject>();

  private CourseConverter _courseConverter;

  internal DataStore(Database db) {
    _db = db;
    _courseConverter = new CourseConverter(_subjects);

    LoadSubjectData();
    LoadCourseData();

    _db.RegisterForNotifications(this);
  }

  public void CoursesChanged() {
    //Just reload all courses. Crude but effective
    LoadCourseData();
  }

  public void SubjectsChanged() {
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
    _courses[course.Code] = course;
    _db.AddUpdateCourse(_courseConverter.CreateCourseDAOFromCourse(course));
    return course;
  }

  public void RemoveCourse(string courseCode) {
    if (_courses.ContainsKey(courseCode)) {
      _db.RemoveCourse(courseCode);
      _courses.Remove(courseCode);
    }
  }

  public Subject? GetSubject(string subjectCode) {
    return _subjects.ContainsKey(subjectCode) ? _subjects[subjectCode] : null;
  }

  public Subject AddUpdateSubject(Subject subject) {
    _subjects[subject.Code] = subject;
    _db.AddUpdateSubject(subject);
    return subject;
  }

  public void RemoveSubject(string subjectCode) {
    if (_subjects.ContainsKey(subjectCode)) {
      _db.RemoveSubject(subjectCode);
      _subjects.Remove(subjectCode);
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
    CourseDAO dao = new CourseDAO(course.Title, course.Code, course.Description);
    Dictionary<long, List<string>> subjectCodesByYear = new Dictionary<long, List<string>>();
    foreach (long yr in course.SubjectsByYear.Keys) {
      subjectCodesByYear[yr] = course.SubjectsByYear[yr].Select(s => s.Code).ToList();
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

  public Course(string title, string code, string description, Dictionary<long, List<Subject>> subjectsByYear) {
    Title = title;
    Code = code;
    Description = description;
    SubjectsByYear = subjectsByYear;
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
  }
}