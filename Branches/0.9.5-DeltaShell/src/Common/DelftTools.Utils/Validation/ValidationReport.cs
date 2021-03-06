﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace DelftTools.Utils.Validation
{
    public class ValidationReport
    {
        /// <summary>
        /// Gets the severity of the report, eg the maximum severity of any of its issues.
        /// </summary>
        public ValidationSeverity Severity { get; private set; }
        public string Category { get; private set; }
        public IEnumerable<ValidationIssue> Issues { get; private set; }
        public IEnumerable<ValidationReport> SubReports { get; private set; }

        public ValidationReport(string category, IEnumerable<ValidationIssue> issues, IEnumerable<ValidationReport> subReports = null)
        {
            Category = category;
            Issues = AsList(issues ?? new ValidationIssue[0]);
            SubReports = AsList(subReports ?? new ValidationReport[0]);
            Severity = DetermineSeverity();
        }

        public ValidationReport(string category, IEnumerable<ValidationReport> subReports)
            : this(category, null, subReports)
        {
        }

        /// <summary>
        /// Creates an empty validation report. Validation reports are read-only so they cannot be added to.
        /// </summary>
        public static ValidationReport Empty(string name)
        {
            return new ValidationReport(name, null);
        }

        /// <summary>
        /// IsEmpty is true when the report contains no issues and no subreports
        /// </summary>
        public bool IsEmpty
        {
            get { return !Issues.Any() && !SubReports.Any(); }
        }

        public IList<ValidationIssue> GetAllIssuesRecursive()
        {
            var allIssues = new List<ValidationIssue>();

            allIssues.AddRange(Issues);
            foreach(var report in SubReports)
            {
                allIssues.AddRange(report.GetAllIssuesRecursive());
            }

            return allIssues;
        }

        public IEnumerable<ValidationIssue> AllErrors
        {
            get { return GetAllIssuesRecursive().Where(i => i.Severity == ValidationSeverity.Error); }
        }

        private ValidationSeverity DetermineSeverity()
        {
            var issueMax = Issues.Any() ? Issues.Max(i => i.Severity) : ValidationSeverity.None;
            var reportMax = SubReports.Any() ? SubReports.Max(i => i.Severity) : ValidationSeverity.None;
            return (ValidationSeverity) Math.Max((int) issueMax, (int) reportMax);
        }

        /// <summary>
        /// The total number of issues (recursive) with severity level 'Error'
        /// </summary>
        public int ErrorCount
        {
            get { return GetIssueCount(ValidationSeverity.Error); }
        }

        /// <summary>
        /// The total number of issues (recursive) with severity level 'Warning'
        /// </summary>
        public int WarningCount
        {
            get { return GetIssueCount(ValidationSeverity.Warning); }
        }

        /// <summary>
        /// The total number of issues (recursive) with severity level 'Info'
        /// </summary>
        public int InfoCount
        {
            get { return GetIssueCount(ValidationSeverity.Info); }
        }

        private int GetIssueCount(ValidationSeverity severity)
        {
            return SubReports.Sum(r => r.GetIssueCount(severity)) + Issues.Count(i => i.Severity == severity);
        }

        public override bool Equals(object obj)
        {
            if (obj is ValidationReport)
            {
                return Equals(obj as ValidationReport);
            }
            return false;
        }

        public bool Equals(ValidationReport other)
        {
            if (other == null)
                return false;
            if (ReferenceEquals(this, other))
                return true;

            var issues = Issues.ToList();
            var otherIssues = other.Issues.ToList();

            if (issues.Count != otherIssues.Count)
                return false;

            for (int i = 0; i < issues.Count; i++)
            {
                if (!issues[i].Equals(otherIssues[i]))
                {
                    return false;
                }
            }

            var subreports = SubReports.ToList();
            var otherSubreports = other.SubReports.ToList();

            if (subreports.Count != otherSubreports.Count)
                return false;

            for (int i = 0; i < subreports.Count; i++)
            {
                if (!subreports[i].Equals(otherSubreports[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public override string ToString()
        {
            return string.Format("{0}, severity: {1} ({2} error(s), {3} warning(s), {4} info)",
                                 Category, Severity,
                                 ErrorCount, WarningCount, InfoCount);
        }
        
        private static IList<T> AsList<T>(IEnumerable<T> enumerable)
        {
            return enumerable as IList<T> ?? enumerable.ToList();
        }
    }
}