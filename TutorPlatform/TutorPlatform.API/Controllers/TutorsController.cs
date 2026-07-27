using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TutorPlatform.API.Common;
using TutorPlatform.Application.Contracts.Tutors;
using TutorPlatform.Application.Features.Tutors.Commands.ApproveTutor;
using TutorPlatform.Application.Features.Tutors.Queries.GetTutorDetail;
using TutorPlatform.Application.Features.Tutors.Queries.SearchTutors;
using TutorPlatform.Domain.Common;

namespace TutorPlatform.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TutorsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TutorsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private Guid GetUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user token.");
            }
            return userId;
        }

        [HttpGet("search")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<TutorSearchResultDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Search([FromQuery] SearchTutorsQuery query)
        {
            // Set defaults if null
            query.PageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            query.PageSize = query.PageSize <= 0 ? 10 : query.PageSize;

            var response = await _mediator.Send(query);
            return Ok(ApiResponse<PagedResult<TutorSearchResultDto>>.Ok(response));
        }

        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<TutorSearchResultDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            var query = new GetTutorDetailQuery(id);
            var response = await _mediator.Send(query);
            return Ok(ApiResponse<TutorSearchResultDto>.Ok(response));
        }

        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Approve(Guid id)
        {
            Guid adminId;
            try
            {
                adminId = GetUserId();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }

            var command = new ApproveTutorCommand
            {
                TutorUserId = id,
                AdminUserId = adminId
            };

            var response = await _mediator.Send(command);
            return Ok(ApiResponse<bool>.Ok(response));
        }
    }
}
