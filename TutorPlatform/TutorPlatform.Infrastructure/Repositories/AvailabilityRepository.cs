using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TutorPlatform.Domain.Entities;
using TutorPlatform.Domain.Interfaces;
using TutorPlatform.Infrastructure.Models;
using TutorPlatform.Infrastructure.Persistence;

namespace TutorPlatform.Infrastructure.Repositories
{
    public class AvailabilityRepository : IAvailabilityRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public AvailabilityRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<Availability>> GetByUserIdAsync(Guid userId)
        {
            var dataModels = await _dbContext.Availabilities
                .AsNoTracking()
                .Where(a => a.UserId == userId)
                .ToListAsync();

            var list = new List<Availability>();
            foreach (var dm in dataModels)
            {
                var availability = dm.IsRecurring 
                    ? new Availability(dm.UserId, (DayOfWeek)dm.DayOfWeek!, dm.StartTime, dm.EndTime)
                    : new Availability(dm.UserId, dm.SpecificDate!.Value, dm.StartTime, dm.EndTime);

                var type = typeof(Availability);
                type.GetProperty("Id")?.SetValue(availability, dm.Id);
                list.Add(availability);
            }

            return list;
        }

        public async Task BulkReplaceAsync(Guid userId, List<Availability> newAvailabilities)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // Delete old ones
                var oldAvailabilities = await _dbContext.Availabilities.Where(a => a.UserId == userId).ToListAsync();
                _dbContext.Availabilities.RemoveRange(oldAvailabilities);

                // Add new ones
                var newDataModels = newAvailabilities.Select(a => new AvailabilityDataModel
                {
                    Id = a.Id == Guid.Empty ? Guid.NewGuid() : a.Id,
                    UserId = a.UserId,
                    DayOfWeek = a.DayOfWeek.HasValue ? (int)a.DayOfWeek.Value : null,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    IsRecurring = a.IsRecurring,
                    SpecificDate = a.SpecificDate.HasValue ? a.SpecificDate.Value : null,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                await _dbContext.Availabilities.AddRangeAsync(newDataModels);
                await _dbContext.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> HasConflictingBookingsAsync(Guid userId, List<Availability> newAvailabilities)
        {
            // TBD: We need to query Bookings where status is Pending or Confirmed, and tutor is this userId.
            // Currently Bookings logic is in Phase 3. Let's just return false for now to allow proceeding,
            // or query the raw Bookings DbSet if available.
            
            // To do this properly, we need to see if any existing Pending/Confirmed booking falls OUTSIDE the new availabilities.
            // Since this logic can be complex (matching specific date or recurring day of week), 
            // a simple approach is: If the tutor has ANY Pending/Confirmed bookings, check if we are deleting any availability that covers them.
            // For now, we return false to allow simple bulk replace. In real scenario, it's safer to just return true if any Pending/Confirmed bookings exist and warn the tutor.
            
            var hasActiveBookings = await _dbContext.Bookings
                .AnyAsync(b => b.TutorId == userId && (b.Status == 0 || b.Status == 1)); // 0=Pending, 1=Confirmed

            if (hasActiveBookings)
            {
                // If they have active bookings, replacing availability is risky.
                // We'll return true to block it, matching our 409 Conflict plan.
                return true; 
            }

            return false;
        }
    }
}
