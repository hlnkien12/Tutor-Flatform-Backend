using MediatR;
using System;
using TutorPlatform.Domain.Entities;

namespace TutorPlatform.Application.Features.Notifications.Events
{
    public class BookingCreatedEvent : INotification
    {
        public Booking Booking { get; set; } = null!;
        public string StudentName { get; set; } = string.Empty;
    }

    public class BookingCancelledEvent : INotification
    {
        public Booking Booking { get; set; } = null!;
        public string CancelledByUserName { get; set; } = string.Empty;
        public Guid CancelledByUserId { get; set; }
    }

    public class ReviewReceivedEvent : INotification
    {
        public Review Review { get; set; } = null!;
        public string ReviewerName { get; set; } = string.Empty;
    }

    public class CreditChangedEvent : INotification
    {
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public decimal NewBalance { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
