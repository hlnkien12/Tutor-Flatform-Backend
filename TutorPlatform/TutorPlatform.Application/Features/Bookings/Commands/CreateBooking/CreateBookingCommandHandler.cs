using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using TutorPlatform.Application.Common.Exceptions;
using TutorPlatform.Application.Common.Interfaces;
using TutorPlatform.Domain.Entities;
using TutorPlatform.Domain.Interfaces;

namespace TutorPlatform.Application.Features.Bookings.Commands.CreateBooking
{
    public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, Guid>
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAvailabilityRepository _availabilityRepository;
        private readonly ICreditService _creditService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublisher _publisher;

        public CreateBookingCommandHandler(
            IBookingRepository bookingRepository,
            IUserRepository userRepository,
            IAvailabilityRepository availabilityRepository,
            ICreditService creditService,
            IUnitOfWork unitOfWork,
            IPublisher publisher)
        {
            _bookingRepository = bookingRepository;
            _userRepository = userRepository;
            _availabilityRepository = availabilityRepository;
            _creditService = creditService;
            _unitOfWork = unitOfWork;
            _publisher = publisher;
        }

        public async Task<Guid> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
        {
            var student = await _userRepository.GetByIdAsync(request.StudentId);
            if (student == null) throw new NotFoundException(nameof(User), request.StudentId);

            var tutorProfile = await _userRepository.GetTutorProfileAsync(request.TutorId);
            if (tutorProfile == null) throw new NotFoundException(nameof(TutorProfile), request.TutorId);

            var tutorSubject = tutorProfile.TutorSubjects.FirstOrDefault(ts => ts.SubjectId == request.SubjectId);
            if (tutorSubject == null) throw new BadRequestException("Tutor does not teach this subject.");

            // Bug #4: Ensure UTC kind for correct DayOfWeek/TimeOfDay comparison
            var reqStart = DateTime.SpecifyKind(request.ScheduledStartAt, DateTimeKind.Utc);
            var reqEnd = DateTime.SpecifyKind(request.ScheduledEndAt, DateTimeKind.Utc);
            var reqTimeStart = reqStart.TimeOfDay;
            var reqTimeEnd = reqEnd.TimeOfDay;

            // Check availability bounds logic:
            var availabilities = await _availabilityRepository.GetByUserIdAsync(request.TutorId);
            bool isTimeSlotValid = false;

            foreach (var av in availabilities)
            {
                if (av.IsRecurring && av.DayOfWeek.HasValue && (int)av.DayOfWeek.Value == (int)reqStart.DayOfWeek)
                {
                    if (reqTimeStart >= av.StartTime && reqTimeEnd <= av.EndTime) isTimeSlotValid = true;
                }
                else if (!av.IsRecurring && av.SpecificDate.HasValue && av.SpecificDate.Value.Date == reqStart.Date)
                {
                    if (reqTimeStart >= av.StartTime && reqTimeEnd <= av.EndTime) isTimeSlotValid = true;
                }
            }

            if (!isTimeSlotValid)
            {
                throw new ConflictException("Tutor is not available at this time slot.");
            }

            // Check overlapping booking
            var hasOverlap = await _bookingRepository.HasOverlappingBookingAsync(request.TutorId, reqStart, reqEnd);
            if (hasOverlap)
            {
                throw new ConflictException("Tutor already has a booking at this time.");
            }

            // Calculate cost
            var durationHours = (decimal)(reqEnd - reqStart).TotalHours;
            var cost = tutorSubject.HourlyCredits * durationHours;

            var bookingId = Guid.NewGuid();
            var booking = new Booking(bookingId, request.TutorId, request.StudentId, request.SubjectId, reqStart, reqEnd, cost, request.Notes);

            // Bug #5: Wrap debit + save in a single transaction
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _bookingRepository.AddAsync(booking);
                await _creditService.DebitAsync(request.StudentId, cost, $"Booking holding for {reqStart.ToString("g")}", bookingId);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            // Publish Event to trigger Notifications (Safe side-effect)
            await _publisher.Publish(new TutorPlatform.Application.Features.Notifications.Events.BookingCreatedEvent
            {
                Booking = booking,
                StudentName = student.FullName
            }, cancellationToken);

            return bookingId;
        }
    }
}
