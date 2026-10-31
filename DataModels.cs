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
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LMS {

    [Serializable]
    public class Course {
        public string Title { get; private set; }
        private Dictionary<long, List<string>> _subjectCodesByYear = new Dictionary<long, List<string>>();

        public Dictionary<long, List<string>> SubjectCodesByYear {
            get => _subjectCodesByYear.ToDictionary(entry => entry.Key, entry => entry.Value.AsReadOnly().ToList());
        }
    
        public string Description { get; private set; }
        public string Code { get; private set; }

        public Course(string title, string description, string code) {
            Title = title;
            Description = description;
            Code = code;
        }

        [JsonConstructor]
        public Course(string title, string description, string code, Dictionary<long, List<string>> subjectCodesByYear)
                : this(title, description, code) {
            _subjectCodesByYear = subjectCodesByYear;
        }

    
        public void AddSubjectByYear(long year, Subject? subject) {
            if (subject == null) return;
            AddSubjectCodeByYear(year, subject.Code);
        }

        public void AddSubjectCodeByYear(long year, string subjectCode) {
            if (!_subjectCodesByYear.ContainsKey(year)) {
                _subjectCodesByYear[year] = new List<string>();
            }

            if (_subjectCodesByYear[year].Contains(subjectCode)) return;
            _subjectCodesByYear[year].Add(subjectCode);
        }

        public List<string> GetSubjectCodesForYear(long year) {
            //Make sure the returned list cannot be amended
            return _subjectCodesByYear.ContainsKey(year) ? 
                    _subjectCodesByYear[year].AsReadOnly().ToList() : new List<string>();
        }
    }

    [Serializable]
    public abstract class Subject {
        public string Title { get; protected set; }
        public long Credits { get; protected set; }
        public string Description { get; protected set; }
        public string Lecturer { get; protected set; }
        public string Code { get; protected set; }

        public Subject(string title, long credits, string description, string lecturer, string code) {
            Title = title;
            Credits = credits;
            Description = description;
            Lecturer = lecturer;
            Code = code;
        }

        public abstract string GetClassType();

        public abstract string GetSubjectAssessmentLocation();
    }

    [Serializable]
    public class TestedSubject : Subject {

        public override string GetClassType() => "TestedSubject";
        public long ExamDuration { get; protected set; }

        public TestedSubject(string title, long credits, string description, string lecturer, string code, long examDuration)
            : base(title, credits, description, lecturer, code) {
            ExamDuration = examDuration;
        }

        public override string GetSubjectAssessmentLocation() {
            return "Exam Hall";
        }
    }

    [Serializable]  
    public class ContinuousAssessmentSubject : Subject {
        public override string GetClassType() => "ContinuousAssessmentSubject";
        private List<Assignment> _assignments = new List<Assignment>();
        
        public List<Assignment> Assignments {
            get => _assignments.AsReadOnly().ToList();  //Don't allow the list to be amended after returning
            protected set {
                _assignments = value;
                RebalanceAssignmentWeights();
            }
        }

        public ContinuousAssessmentSubject(string title, long credits, string description, string lecturer, string code) 
                : base(title, credits, description, lecturer, code) {
        }

        [JsonConstructor]
        public ContinuousAssessmentSubject(string title, long credits, string description, string lecturer, string code, List<Assignment> assignments) 
                : this(title, credits, description, lecturer, code) {
            SetAssignments(assignments == null ? new List<Assignment>() : assignments);
        }

        public void AddAssignment(Assignment newAssignment) {
            _assignments.Add(newAssignment);
            RebalanceAssignmentWeights();   //Make sure the grade weights are appropriate
        }

        public void SetAssignments(List<Assignment> newAssignments) {
            _assignments.Clear();
            //Make sure the Assignments are completely disconnected from the original list so changes outside to the `newAssignments` list don't affect the internal list
            newAssignments.ForEach(it => _assignments.Add(new Assignment(it.Title, it.Weight, it.Description)));
            RebalanceAssignmentWeights();   //Make sure the grade weights are appropriate
        }

        public void RemoveAssignment(Assignment assignment) {
            _assignments.Remove(assignment);
            RebalanceAssignmentWeights();
        }

        //Makes sure the total weight of all assignments adds up to 100. This _may_ result in rounding errors for 
        // individual assignments but should be reasonably accurate
        private void RebalanceAssignmentWeights() {
            long totalWeight = _assignments.Aggregate(0L, (total, assignment) => total + assignment.Weight);
            //Since the grade must add up to 100, we'll divide each assignment's weight proportionally
            _assignments.ForEach(it => it.Weight = it.Weight * 100 / totalWeight);
        }

        public override string GetSubjectAssessmentLocation() => "Blackboard online assessment";
    }

    [Serializable]
    public class Assignment {
        public string Title { get; private set; }
        public long Weight { get; set; }    //Not sure what modifier to use on the setter, I want it only accessible in _this file_
        public string Description { get; private set; }

        [JsonConstructor]
        public Assignment(string title, long weight, string description) {
            Title = title;
            Weight = weight;
            Description = description;
        }
    }
}