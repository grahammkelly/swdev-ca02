
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

public class DataStore {
  private readonly Database _db;

  private Dictionary<string, Course> _courses = new Dictionary<string, Course>();
  private Dictionary<string, Subject> _subjects = new Dictionary<string, Subject>();

  internal DataStore(Database db) {
    _db = db;
    
    LoadSubjectData();
    LoadCourseData();
  }

  private void LoadSubjectData() {
    _db.GetSubjects().ForEach(s => {
      _subjects.Add(s.Code, s);
    });
  }

  private void LoadCourseData() {
    _db.GetCourses().ForEach(c => {
      Dictionary<long, List<Subject>> subjectsByYear = new Dictionary<long, List<Subject>>();
      foreach (long yr in c.SubjectCodesByYear.Keys) {
        List<Subject> subjects = new List<Subject>();
        foreach (string code in c.SubjectCodesByYear[yr]) {
          if (_subjects.ContainsKey(code)) {
            subjects.Add(_subjects[code]);
          }
        }

        if (subjects.Count > 0) {
          subjectsByYear.Add(yr, subjects);
        }
      }

      _courses[c.Code] = new Course(c.Title, c.Code, c.Description, subjectsByYear);
    });
  }

  public void AddUpdateCourse(Course course) {
    _courses[course.Code] = course;
  }
  
  public void UpdateCourse(Course course) {}
}

public class Course {
  public string Title { get; set; }
  public string Code { get; set; }
  public string Description { get; set; }
  public Dictionary<long, List<Subject>> SubjectsByYear { get; }

  public Course(string title, string code, string description, Dictionary<long, List<Subject>> subjectsByYear) {
    Title = title;
    Code = code;
    Description = description;
    SubjectsByYear = subjectsByYear;
  }
}