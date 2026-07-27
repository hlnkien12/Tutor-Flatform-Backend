using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using TutorPlatform.Application.Contracts.Notifications;
using TutorPlatform.Domain.Entities;
using TutorPlatform.Domain.Enums;
using TutorPlatform.Domain.Interfaces;

namespace TutorPlatform.Application.Features.Notifications.Events
{
    public class NotificationEventHandlers :
        INotificationHandler<BookingCreatedEvent>,
        INotificationHandler<BookingCancelledEvent>,
        INotificationHandler<ReviewReceivedEvent>,
        INotificationHandler<CreditChangedEvent>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly INotificationSender _notificationSender;

        public NotificationEventHandlers(
            INotificationRepository notificationRepository,
            INotificationSender notificationSender)
        {
            _notificationRepository = notificationRepository;
            _notificationSender = notificationSender;
        }

        public async Task Handle(BookingCreatedEvent notificationEvent, CancellationToken cancellationToken)
        {
            var title = "Lịch học mới!";
            var message = $"Học viên {notificationEvent.StudentName} vừa đặt lịch học với bạn vào {notificationEvent.Booking.ScheduledStartAt.ToString("dd/MM/yyyy HH:mm")}.";
            
            var notification = new Notification(
                notificationEvent.Booking.TutorId,
                title,
                message,
                NotificationType.BookingCreated,
                notificationEvent.Booking.Id,
                "Booking"
            );

            await _notificationRepository.AddAsync(notification);

            var dto = new NotificationDto
            {
                Id = notification.Id,
                Title = title,
                Message = message,
                Type = notification.Type.ToString(),
                IsRead = false,
                CreatedAt = notification.CreatedAt
            };

            await _notificationSender.SendNotificationAsync(notification.UserId, dto);
        }

        public async Task Handle(BookingCancelledEvent notificationEvent, CancellationToken cancellationToken)
        {
            var isStudentCancelled = notificationEvent.CancelledByUserId == notificationEvent.Booking.StudentId;
            var targetUserId = isStudentCancelled ? notificationEvent.Booking.TutorId : notificationEvent.Booking.StudentId;
            
            var title = "Lịch học bị hủy";
            var message = $"{notificationEvent.CancelledByUserName} đã hủy lịch học vào {notificationEvent.Booking.ScheduledStartAt.ToString("dd/MM/yyyy HH:mm")}.";
            
            var notification = new Notification(
                targetUserId,
                title,
                message,
                NotificationType.BookingCancelled,
                notificationEvent.Booking.Id,
                "Booking"
            );

            await _notificationRepository.AddAsync(notification);

            var dto = new NotificationDto
            {
                Id = notification.Id,
                Title = title,
                Message = message,
                Type = notification.Type.ToString(),
                IsRead = false,
                CreatedAt = notification.CreatedAt
            };

            await _notificationSender.SendNotificationAsync(targetUserId, dto);
        }

        public async Task Handle(ReviewReceivedEvent notificationEvent, CancellationToken cancellationToken)
        {
            var title = "Đánh giá mới!";
            var message = $"Học viên {notificationEvent.ReviewerName} vừa để lại đánh giá {notificationEvent.Review.Rating} sao cho buổi học của bạn.";
            
            var notification = new Notification(
                notificationEvent.Review.RevieweeId,
                title,
                message,
                NotificationType.ReviewReceived,
                notificationEvent.Review.Id,
                "Review"
            );

            await _notificationRepository.AddAsync(notification);

            var dto = new NotificationDto
            {
                Id = notification.Id,
                Title = title,
                Message = message,
                Type = notification.Type.ToString(),
                IsRead = false,
                CreatedAt = notification.CreatedAt
            };

            await _notificationSender.SendNotificationAsync(notification.UserId, dto);
        }

        public async Task Handle(CreditChangedEvent notificationEvent, CancellationToken cancellationToken)
        {
            var title = "Biến động số dư";
            var actionStr = notificationEvent.Amount > 0 ? "nạp" : "trừ";
            var message = $"Tài khoản của bạn vừa được {actionStr} {Math.Abs(notificationEvent.Amount):N0} Credits. Số dư mới: {notificationEvent.NewBalance:N0} Credits.";
            
            var notification = new Notification(
                notificationEvent.UserId,
                title,
                message,
                NotificationType.CreditChanged,
                null,
                "Credit"
            );

            await _notificationRepository.AddAsync(notification);

            var dto = new NotificationDto
            {
                Id = notification.Id,
                Title = title,
                Message = message,
                Type = notification.Type.ToString(),
                IsRead = false,
                CreatedAt = notification.CreatedAt
            };

            await _notificationSender.SendNotificationAsync(notification.UserId, dto);
        }
    }
}
