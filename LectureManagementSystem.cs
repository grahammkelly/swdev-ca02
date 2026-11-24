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
using LMS.UI;
using Logging;

namespace LMS;

public class LectureManagementSystem {
    public static int Main(string[] args) {
        Logger.InitializeLogger("./output.log", LogLevel.Debug);
        Logger.LogWithBlanks(LogLevel.Info, "Starting Lecture Management System", 10);
        new LectureManagementSystem().RunLMS();
        return 0;   //OK
    }

    public LectureManagementSystem() {
    }

    public void RunLMS() {
        DataStore db = PrepopulateDatabase();

        try {
            new LMSMainMenuUI(db).UILoop();
        } catch (Exception ex) {
            Logger.Error($"An error occurred: {ex.Message}");
        } finally {
            Logger.Info("Saving data to file system");
            db.Save();
        }

    }


    private DataStore PrepopulateDatabase() {
        DataStore ds = DataFactory.GetDatastore();
    
        // Add prepopulation logic here if needed
        //Set up some sample subjects
        prepopulatedSubjects(ds);

        //Now add sample courses
        if (ds.GetCourse("COMPSCI-L8") == null) {
            Course bscCompSci = new Course("BSc (Hons) Computer Science", "COMPSCI-L8", 
                    "Level 8, Bachelor of Science in Computer Science");
            bscCompSci.AddSubjectByYear(1, ds.GetSubject("PROG001"));
            bscCompSci.AddSubjectByYear(1, ds.GetSubject("PROC001"));
            bscCompSci.AddSubjectByYear(2, ds.GetSubject("PROG002"));
            bscCompSci.AddSubjectByYear(2, ds.GetSubject("LAW01"));
            bscCompSci.AddSubjectByYear(2, ds.GetSubject("DATA01"));
            bscCompSci.AddSubjectByYear(3, ds.GetSubject("ALGO01"));
            bscCompSci.AddSubjectByYear(3, ds.GetSubject("MATH01"));
            bscCompSci.AddSubjectByYear(4, ds.GetSubject("FINAL001"));
            ds.AddUpdateCourse(bscCompSci);
            Logger.Info($"Created course - {bscCompSci}");
        }
        
        if (ds.GetCourse("COMPSCI-L7") == null) {
            Course degCompSci = new Course("Degree in Computer Science", "COMPSCI-L7", 
                    "Level 7, Degree in Computer Science");
            degCompSci.AddSubjectByYear(1, ds.GetSubject("PROG001"));
            degCompSci.AddSubjectByYear(1, ds.GetSubject("PROC001"));
            degCompSci.AddSubjectByYear(2, ds.GetSubject("DATA01"));
            degCompSci.AddSubjectByYear(2, ds.GetSubject("LAW01"));
            degCompSci.AddSubjectByYear(3, ds.GetSubject("FINAL001"));
            ds.AddUpdateCourse(degCompSci);
            Logger.Info($"Created course - {degCompSci}");
        }

        if (ds.GetCourse("ARTS-L7") == null) {
            Course arts = new Course("Bachelor of Arts", "ARTS-L7", 
                    "Level 7, Bachelor of Arts and other useless stuff");
            arts.AddSubjectByYear(1, ds.GetSubject("READ01"));
            arts.AddSubjectByYear(1, ds.GetSubject("ETHICS01"));
            arts.AddSubjectByYear(2, ds.GetSubject("PSYC01"));
            arts.AddSubjectByYear(2, ds.GetSubject("FRIES01"));
            ds.AddUpdateCourse(arts);
            Logger.Info($"Created course - {arts}");
        }

        return ds;
    }

    private void prepopulatedSubjects(DataStore ds) {
        if (ds.GetSubject("PROG001") == null) {
            ContinuousAssessmentSubject prog001 = new ContinuousAssessmentSubject("Software Development", 50, 
                    "Introductory Software Development", "Joe Blogs", "PROG001");
            prog001.AddAssignment(new Assignment("Console application CA1", 60, "Console Application to do XXXXXX"));
            prog001.AddAssignment(new Assignment("Console application CA2", 40, "Another Console Application to do YYYYYY"));
            ds.AddUpdateSubject(prog001);
        }

        if (ds.GetSubject("PROG002") == null) {
            ContinuousAssessmentSubject prog002 = new ContinuousAssessmentSubject("Advanced Software Development", 40, 
                    "Advanced topics in Software Development", "John Smith", "PROG002");
            prog002.AddAssignment(new Assignment("GUI Application CA1", 50, "Advanced GUI Application to do ZZZZZZ"));
            ds.AddUpdateSubject(prog002);
        }

        if (ds.GetSubject("FINAL001") == null) {
            ContinuousAssessmentSubject finalProject = new ContinuousAssessmentSubject("Final Project", 60, 
                    "Capstone project for the course", "Alice Johnson", "FINAL001");
            finalProject.AddAssignment(new Assignment("Final Project Proposal", 20, "Submit project proposal"));
            finalProject.AddAssignment(new Assignment("Final Project Implementation", 80, "Implement the final project"));
            ds.AddUpdateSubject(finalProject);
        }

        if (ds.GetSubject("PROC001") == null) {
            ContinuousAssessmentSubject proc001 = new ContinuousAssessmentSubject("Software Process", 30, 
                    "Introduction to Software Process", "Jane Doe", "PROC001");
            proc001.AddAssignment(new Assignment("Use case modeling, CA01", 100, "Model use cases for a LMS system"));
            ds.AddUpdateSubject(proc001);
        }

        if (ds.GetSubject("LAW01") == null) {
            TestedSubject patentLaw = new TestedSubject("Patent Law", 20, 
                    "Introduction to Patent Law as it applies to software", "Richard Roe", "LAW01", 120);
            ds.AddUpdateSubject(patentLaw);
        }

        if (ds.GetSubject("LAW02") == null) {
            TestedSubject softwareLaw = new TestedSubject("Software Law", 25, 
                    "Introduction to Software Law", "Emily Davis", "LAW02", 90);
            ds.AddUpdateSubject(softwareLaw);
        }

        if (ds.GetSubject("PSYC01") == null) {
            TestedSubject psychology = new TestedSubject("Psychology", 30, 
                    "Introduction to Psychology", "Michael Brown", "PSYC01", 100);
            ds.AddUpdateSubject(psychology);
        }

        if (ds.GetSubject("READ01") == null) {
            TestedSubject reading = new TestedSubject("Reading", 20,
                    "Basic reading for those that don't study a practical subject", "Sarah Green", "READ01", 100);
            ds.AddUpdateSubject(reading);
        }
        if (ds.GetSubject("ETHICS01") == null) {
            TestedSubject ethics = new TestedSubject("Ethics", 20,
                    "Introduction to Ethics and other navel gazing", "Laura White", "ETHICS01", 100);
            ds.AddUpdateSubject(ethics);
        }

        if (ds.GetSubject("SDETH01") == null) {
            ContinuousAssessmentSubject ethicsOfSD = new ContinuousAssessmentSubject("Ethics in Software Development", 20,
                    "Ethical considerations in software development", "Laura White", "SDETH01");
            ethicsOfSD.AddAssignment(new Assignment("Ethics Case Study", 100, "Analyze an ethics case study in software development"));
            ds.AddUpdateSubject(ethicsOfSD);
        }

        if (ds.GetSubject("AIETH01") == null) {
            ContinuousAssessmentSubject aiEthics = new ContinuousAssessmentSubject("AI Ethics", 25,
                    "Ethical considerations in AI development", "David Green", "AIETH01");
            aiEthics.AddAssignment(new Assignment("AI Ethics Case Study", 100, "Analyze an AI ethics case study"));
            ds.AddUpdateSubject(aiEthics);
        }

        if (ds.GetSubject("MATH01") == null) {
            TestedSubject mathsOfSoftwareDevelopment = new TestedSubject("Mathematics of Software Development", 30,
                    "Mathematical foundations for software development", "Alice Smith", "MATH01", 120);
            ds.AddUpdateSubject(mathsOfSoftwareDevelopment);
        }

        if (ds.GetSubject("ALG01") == null) {
            ContinuousAssessmentSubject algorithms = new ContinuousAssessmentSubject("Algorithms", 30,
                    "Study of algorithms in computer science", "Bob Johnson", "ALG01");
            algorithms.AddAssignment(new Assignment("Algorithm Analysis", 25, "Analyze the complexity of given algorithms"));
            algorithms.AddAssignment(new Assignment("Big O Notation", 25, "Analyze the Big O complexity of given algorithms"));
            algorithms.AddAssignment(new Assignment("Bubble Sort", 25, "Implement and analyze various sorting algorithms"));
            algorithms.AddAssignment(new Assignment("Sorting Algorithms", 25, "Implement and analyze various sorting algorithms"));
            ds.AddUpdateSubject(algorithms);
        }

        if (ds.GetSubject("DATA01") == null) {
            TestedSubject dataStructures = new TestedSubject("Data Structures", 30,
                    "Study of data structures in computer science", "Carol White", "DATA01", 120);
            ds.AddUpdateSubject(dataStructures);
        }

        if (ds.GetSubject("FRIES01") == null) {
            TestedSubject fries = new TestedSubject("Fries", 10,
                    "Work experience for Arts students", "Chef Gordon", "FRIES01", 60);
            ds.AddUpdateSubject(fries);
        }
    }
}
