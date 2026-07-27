using System.Threading;
using System.Threading.Tasks;
using MediatR;
using TutorPlatform.Application.Common.Exceptions;
using TutorPlatform.Domain.Interfaces;

namespace TutorPlatform.Application.Features.Profiles.Commands.UpdateTutorProfile
{
    public class UpdateTutorProfileCommandHandler : IRequestHandler<UpdateTutorProfileCommand, bool>
    {
        private readonly IUserRepository _userRepository;

        public UpdateTutorProfileCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<bool> Handle(UpdateTutorProfileCommand request, CancellationToken cancellationToken)
        {
            var profile = await _userRepository.GetTutorProfileAsync(request.UserId);
            
            if (profile == null)
            {
                throw new NotFoundException(nameof(Domain.Entities.TutorProfile), request.UserId);
            }

            profile.UpdateDetails(request.Bio, request.Qualifications);
            if (request.DefaultMeetingLink != null) 
            {
                profile.UpdateDefaultMeetingLink(request.DefaultMeetingLink);
            }

            await _userRepository.UpdateTutorProfileAsync(profile);

            return true;
        }
    }
}
