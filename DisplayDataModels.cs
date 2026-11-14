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