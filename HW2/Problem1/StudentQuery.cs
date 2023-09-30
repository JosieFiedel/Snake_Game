/// CS2100 X-in-Practice Homework 2
/// Josie Fiedel -- u1300286
/// January 27, 2023
namespace _2100_XiP1;
using System;
using System.Collections.Generic;

/// <summary>
/// Defines a student with a first name, last name, year (0-4), and major.
/// </summary>
class Student
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public int Year { get; set; }
    public string? Major { get; set; }
}

class StudentQuery
{
    /// <summary>
    /// Given a list of students and specific required information, this method acts as a query to return a list containing all
    /// students who meet the specific required information. If a 0 or an empty string is passed in as a parameter, this means
    /// that the piece of information is not required and is simply skipped over while iterating through the students.
    /// </summary>
    /// <param name="allStudents"> List containing all students to check </param>
    /// <param name="firstName"> Specified first name of student </param>
    /// <param name="lastName"> Specified last name of student </param>
    /// <param name="year"> Specified year of student (1-4) </param>
    /// <param name="major"> Specified major of student </param>
    /// <returns> query list containing all students who meet the specifications </returns>
    public static List<Student> QueryStudents(List<Student> allStudents, string firstName, string lastName, int year, string major)
    {
        // List to contain all students who meet the specified requirements.
        List<Student> query = new();

        // Cycle through each student in the list of all students, checking for the required information.
        foreach (Student student in allStudents)
        {
            // Student first name:
            if (!firstName.Equals(""))
                if (student.FirstName == null || !student.FirstName.Equals(firstName))
                    continue;

            // Student last name:
            if (!lastName.Equals(""))
                if (student.LastName == null || !student.LastName.Equals(lastName))
                    continue;

            // Student year:
            if (year != 0)
                if (student.Year != year)
                    continue;
            
            // Student major: 
            if (!major.Equals(""))
                if (student.Major == null || student.Major.Equals(major))
                    continue;

            // If all of the checks have been passed, then the student is added to the list. 
            query.Add(student);
        }
        return query;
    }

    /// <summary>
    /// **For testing purposes**
    /// </summary>
    /// <param name="args"></param>
    public static void Main(string[] args)
    {
    }
}
