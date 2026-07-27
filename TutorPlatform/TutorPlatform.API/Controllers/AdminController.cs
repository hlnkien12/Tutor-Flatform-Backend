using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TutorPlatform.API.Common;
using TutorPlatform.Application.Contracts.Admin;
using TutorPlatform.Application.Features.Admin.Commands.RejectTutor;
using TutorPlatform.Application.Features.Admin.Queries.GetAdminDashboard;
using TutorPlatform.Application.Features.Admin.Queries.GetPendingTutors;
using TutorPlatform.Application.Features.Tutors.Commands.ApproveTutor;
using TutorPlatform.Domain.Common;

namespace TutorPlatform.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AdminController(IMediator mediator)
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

        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(ApiResponse<AdminDashboardDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboard()
        {
            var query = new GetAdminDashboardQuery();
            var response = await _mediator.Send(query);
            return Ok(ApiResponse<AdminDashboardDto>.Ok(response));
        }

        [HttpGet("pending-tutors")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PendingTutorDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingTutors([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var query = new GetPendingTutorsQuery
            {
                PageNumber = pageNumber <= 0 ? 1 : pageNumber,
                PageSize = pageSize <= 0 ? 10 : pageSize
            };
            var response = await _mediator.Send(query);
            return Ok(ApiResponse<PagedResult<PendingTutorDto>>.Ok(response));
        }

        [HttpPut("tutors/{id}/approve")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ApproveTutor(Guid id)
        {
            Guid adminId;
            try
            {
                adminId = GetUserId();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(ApiResponse<object>.Error(401, "Unauthorized admin access."));
            }

            var command = new ApproveTutorCommand
            {
                TutorUserId = id,
                AdminUserId = adminId
            };

            var response = await _mediator.Send(command);
            return Ok(ApiResponse<bool>.Ok(response));
        }

        [HttpPut("tutors/{id}/reject")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RejectTutor(Guid id)
        {
            var command = new RejectTutorCommand(id);
            var response = await _mediator.Send(command);
            return Ok(ApiResponse<bool>.Ok(response));
        }

        // ── User Management ──────────────────────────────────────────────

        [HttpGet("users")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] int? role = null,
            [FromQuery] bool? isActive = null)
        {
            var query = new Application.Features.Admin.Queries.GetAllUsers.GetAllUsersQuery
            {
                PageNumber = pageNumber < 1 ? 1 : pageNumber,
                PageSize = pageSize < 1 ? 20 : pageSize,
                Search = search,
                Role = role,
                IsActive = isActive
            };
            var response = await _mediator.Send(query);
            return Ok(ApiResponse<Domain.Common.PagedResult<Domain.Common.AdminUserResult>>.Ok(response));
        }

        [HttpPut("users/{id}/lock")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> LockUser(Guid id)
        {
            var command = new Application.Features.Admin.Commands.LockUser.LockUserCommand { UserId = id };
            var response = await _mediator.Send(command);
            return Ok(ApiResponse<bool>.Ok(response));
        }

        [HttpPut("users/{id}/unlock")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UnlockUser(Guid id)
        {
            var command = new Application.Features.Admin.Commands.UnlockUser.UnlockUserCommand { UserId = id };
            var response = await _mediator.Send(command);
            return Ok(ApiResponse<bool>.Ok(response));
        }

        // ── Review Management ─────────────────────────────────────────────

        [HttpGet("reviews")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllReviews(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] int? reviewType = null,
            [FromQuery] int? rating = null)
        {
            var query = new Application.Features.Admin.Queries.GetAllReviews.GetAllReviewsQuery
            {
                PageNumber = pageNumber < 1 ? 1 : pageNumber,
                PageSize = pageSize < 1 ? 20 : pageSize,
                Search = search,
                ReviewType = reviewType,
                Rating = rating
            };
            var response = await _mediator.Send(query);
            return Ok(ApiResponse<Domain.Common.PagedResult<Domain.Common.AdminReviewResult>>.Ok(response));
        }
    }
}
