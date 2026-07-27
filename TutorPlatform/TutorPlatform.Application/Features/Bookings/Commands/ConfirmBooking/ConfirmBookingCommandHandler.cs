using System.Threading;
using System.Threading.Tasks;
using MediatR;
using TutorPlatform.Application.Common.Exceptions;
using TutorPlatform.Domain.Interfaces;
using TutorPlatform.Domain.Entities;

namespace TutorPlatform.Application.Features.Bookings.Commands.ConfirmBooking
{
    public class ConfirmBookingCommandHandler : IRequestHandler<ConfirmBookingCommand, bool>
    {
        private readonly IBookingRepository _bookingRepository;

        public ConfirmBookingCommandHandler(IBookingRepository bookingRepository)
        {
            _bookingRepository = bookingRepository;
        }

        public async Task<bool> Handle(ConfirmBookingCommand request, CancellationToken cancellationToken)
        {
            var booking = await _bookingRepository.GetByIdAsync(request.BookingId);
            if (booking == null) throw new NotFoundException(nameof(Booking), request.BookingId);

            if (booking.TutorId != request.TutorId) throw new ForbiddenException("You can only confirm your own bookings.");

            booking.Confirm(request.MeetingLink);

            await _bookingRepository.UpdateAsync(booking);

            return true;
        }
    }
}
