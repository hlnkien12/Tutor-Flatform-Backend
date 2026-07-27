using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TutorPlatform.Domain.Entities;
using TutorPlatform.Domain.Enums;
using TutorPlatform.Domain.Interfaces;
using TutorPlatform.Infrastructure.Models;
using TutorPlatform.Infrastructure.Persistence;

namespace TutorPlatform.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public UserRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            var dataModel = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (dataModel == null) return null;

            return MapToDomain(dataModel);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            var dataModel = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);

            if (dataModel == null) return null;

            return MapToDomain(dataModel);
        }

        public async Task<User> AddAsync(User user)
        {
            var dataModel = new UserDataModel
            {
                Id = user.Id,
                Email = user.Email,
                PasswordHash = user.PasswordHash,
                FullName = user.FullName,
                Role = (int)user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                RefreshToken = user.RefreshToken,
                RefreshTokenExpiryTime = user.RefreshTokenExpiryTime
            };

            await _dbContext.Users.AddAsync(dataModel);
            await _dbContext.SaveChangesAsync();

            return user;
        }

        public async Task UpdateAsync(User user)
        {
            var dataModel = await _dbContext.Users.FindAsync(user.Id);
            if (dataModel != null)
            {
                dataModel.Email = user.Email;
                dataModel.PasswordHash = user.PasswordHash;
                dataModel.FullName = user.FullName;
                dataModel.Phone = user.Phone;
                dataModel.AvatarUrl = user.AvatarUrl;
                dataModel.Role = (int)user.Role;
                dataModel.IsActive = user.IsActive;
                dataModel.CreditBalance = user.CreditBalance;
                dataModel.UpdatedAt = user.UpdatedAt;
                dataModel.RefreshToken = user.RefreshToken;
                dataModel.RefreshTokenExpiryTime = user.RefreshTokenExpiryTime;

                _dbContext.Users.Update(dataModel);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<TutorProfile?> GetTutorProfileAsync(Guid userId)
        {
            var dataModel = await _dbContext.TutorProfiles
                .Include(tp => tp.TutorSubjects)
                .ThenInclude(ts => ts.Subject)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (dataModel == null) return null;

            var profile = new TutorProfile(dataModel.UserId, dataModel.Bio, dataModel.Qualifications);
            var type = typeof(TutorProfile);
            type.GetProperty("Id")?.SetValue(profile, dataModel.Id);
            if (dataModel.IsApproved) profile.Approve(dataModel.ApprovedBy ?? Guid.Empty);

            // Map TutorSubjects from DB to domain entity
            foreach (var tsData in dataModel.TutorSubjects)
            {
                var tutorSubject = new TutorSubject(
                    tsData.TutorProfileId,
                    tsData.SubjectId,
                    (ProficiencyLevel)tsData.ProficiencyLevel,
                    tsData.HourlyCredits);
                var tsType = typeof(TutorSubject);
                tsType.GetProperty("Id")?.SetValue(tutorSubject, tsData.Id);
                profile.AddTutorSubject(tutorSubject);
            }
            
            return profile;
        }

        public async Task<TutorProfile> AddTutorProfileAsync(TutorProfile profile)
        {
            var dataModel = new TutorProfileDataModel
            {
                Id = profile.Id,
                UserId = profile.UserId,
                Bio = profile.Bio,
                Qualifications = profile.Qualifications,
                IsApproved = profile.IsApproved,
                ApprovedAt = profile.ApprovedAt,
                ApprovedBy = profile.ApprovedBy,
                CreatedAt = profile.CreatedAt,
                UpdatedAt = profile.UpdatedAt
            };

            await _dbContext.TutorProfiles.AddAsync(dataModel);
            await _dbContext.SaveChangesAsync();

            return profile;
        }

        public async Task UpdateTutorProfileAsync(TutorProfile profile)
        {
            var dataModel = await _dbContext.TutorProfiles
                .Include(tp => tp.TutorSubjects)
                .FirstOrDefaultAsync(p => p.Id == profile.Id);

            if (dataModel != null)
            {
                dataModel.Bio = profile.Bio;
                dataModel.Qualifications = profile.Qualifications;
                dataModel.IsApproved = profile.IsApproved;
                dataModel.ApprovedAt = profile.ApprovedAt;
                dataModel.ApprovedBy = profile.ApprovedBy;
                dataModel.AverageRating = profile.AverageRating;
                dataModel.TotalReviews = profile.TotalReviews;
                dataModel.TotalSessions = profile.TotalSessions;
                dataModel.UpdatedAt = profile.UpdatedAt;

                // Bulk update logic for TutorSubjects
                var domainSubjectIds = profile.TutorSubjects.Select(ts => ts.SubjectId).ToList();
                
                // 1. Remove subjects that are no longer in the domain entity
                var subjectsToRemove = dataModel.TutorSubjects.Where(ts => !domainSubjectIds.Contains(ts.SubjectId)).ToList();
                foreach (var subjectToRemove in subjectsToRemove)
                {
                    dataModel.TutorSubjects.Remove(subjectToRemove);
                }

                // 2. Add or update subjects from the domain entity
                foreach (var domainSubject in profile.TutorSubjects)
                {
                    var existingSubject = dataModel.TutorSubjects.FirstOrDefault(ts => ts.SubjectId == domainSubject.SubjectId);
                    if (existingSubject == null)
                    {
                        dataModel.TutorSubjects.Add(new TutorSubjectDataModel
                        {
                            TutorProfileId = domainSubject.TutorProfileId,
                            SubjectId = domainSubject.SubjectId,
                            ProficiencyLevel = (int)domainSubject.ProficiencyLevel,
                            HourlyCredits = domainSubject.HourlyCredits,
                            CreatedAt = domainSubject.CreatedAt
                        });
                    }
                    else
                    {
                        existingSubject.ProficiencyLevel = (int)domainSubject.ProficiencyLevel;
                        existingSubject.HourlyCredits = domainSubject.HourlyCredits;
                    }
                }

                _dbContext.TutorProfiles.Update(dataModel);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<StudentProfile?> GetStudentProfileAsync(Guid userId)
        {
            var dataModel = await _dbContext.StudentProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (dataModel == null) return null;

            var profile = new StudentProfile(dataModel.UserId, dataModel.GradeLevel, dataModel.LearningPreferences);
            var type = typeof(StudentProfile);
            type.GetProperty("Id")?.SetValue(profile, dataModel.Id);
            return profile;
        }

        public async Task<StudentProfile> AddStudentProfileAsync(StudentProfile profile)
        {
            var dataModel = new StudentProfileDataModel
            {
                Id = profile.Id,
                UserId = profile.UserId,
                GradeLevel = profile.GradeLevel,
                LearningPreferences = profile.LearningPreferences,
                CreatedAt = profile.CreatedAt,
                UpdatedAt = profile.UpdatedAt
            };

            await _dbContext.StudentProfiles.AddAsync(dataModel);
            await _dbContext.SaveChangesAsync();

            return profile;
        }

        public async Task UpdateStudentProfileAsync(StudentProfile profile)
        {
            var dataModel = await _dbContext.StudentProfiles.FindAsync(profile.Id);
            if (dataModel != null)
            {
                dataModel.GradeLevel = profile.GradeLevel;
                dataModel.LearningPreferences = profile.LearningPreferences;
                dataModel.UpdatedAt = profile.UpdatedAt;

                _dbContext.StudentProfiles.Update(dataModel);
                await _dbContext.SaveChangesAsync();
            }
        }

        private User MapToDomain(UserDataModel dataModel)
        {
            var user = new User(dataModel.Email, dataModel.PasswordHash, dataModel.FullName, dataModel.Phone, dataModel.AvatarUrl, (UserRole)dataModel.Role);
            
            var type = typeof(User);
            type.GetProperty("Id")?.SetValue(user, dataModel.Id);
            type.GetProperty("CreditBalance")?.SetValue(user, dataModel.CreditBalance);
            
            if (dataModel.RefreshToken != null && dataModel.RefreshTokenExpiryTime.HasValue)
            {
                user.SetRefreshToken(dataModel.RefreshToken, dataModel.RefreshTokenExpiryTime.Value);
            }
            
            if (!dataModel.IsActive)
            {
                user.Deactivate();
            }

            return user;
        }
    }
}
