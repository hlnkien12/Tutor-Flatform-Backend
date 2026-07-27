using System;
using System.Collections.Generic;
using TutorPlatform.Domain.Common;

namespace TutorPlatform.Domain.Entities
{
    public class TutorProfile : BaseEntity
    {
        public Guid UserId { get; private set; }
        public string? Bio { get; private set; }
        public string? Qualifications { get; private set; }
        public bool IsApproved { get; private set; }
        public DateTime? ApprovedAt { get; private set; }
        public Guid? ApprovedBy { get; private set; }
        public decimal AverageRating { get; private set; }
        public int TotalReviews { get; private set; }
        public int TotalSessions { get; private set; }
        public string? DefaultMeetingLink { get; private set; }

        private readonly List<TutorSubject> _tutorSubjects = new();
        public IReadOnlyCollection<TutorSubject> TutorSubjects => _tutorSubjects.AsReadOnly();

        private TutorProfile() { } // EF Core

        public TutorProfile(Guid userId, string? bio, string? qualifications)
        {
            UserId = userId;
            Bio = bio;
            Qualifications = qualifications;
            IsApproved = false;
            AverageRating = 0;
            TotalReviews = 0;
            TotalSessions = 0;
        }

        public void UpdateDetails(string? bio, string? qualifications)
        {
            Bio = bio;
            Qualifications = qualifications;
            MarkUpdated();
        }

        public void UpdateDefaultMeetingLink(string? link)
        {
            DefaultMeetingLink = link;
            MarkUpdated();
        }

        public void Approve(Guid adminUserId)
        {
            IsApproved = true;
            ApprovedAt = DateTime.UtcNow;
            ApprovedBy = adminUserId;
            MarkUpdated();
        }

        public void RecalculateRating(int newRating)
        {
            decimal totalScore = (AverageRating * TotalReviews) + newRating;
            TotalReviews++;
            AverageRating = totalScore / TotalReviews;
            MarkUpdated();
        }

        public void IncrementSessions()
        {
            TotalSessions++;
            MarkUpdated();
        }

        public void AddTutorSubject(TutorSubject tutorSubject)
        {
            _tutorSubjects.Add(tutorSubject);
            MarkUpdated();
        }
        
        public void ClearTutorSubjects()
        {
            _tutorSubjects.Clear();
            MarkUpdated();
        }
    }
}
