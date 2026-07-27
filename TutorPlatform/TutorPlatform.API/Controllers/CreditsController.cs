using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TutorPlatform.API.Common;
using TutorPlatform.Application.Contracts.Credits;
using TutorPlatform.Application.Features.Credits.Commands.DepositCredits;
using TutorPlatform.Application.Features.Credits.Queries.GetBalance;
using TutorPlatform.Application.Features.Credits.Queries.GetCreditTransactions;
using TutorPlatform.Domain.Common;

namespace TutorPlatform.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CreditsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CreditsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("deposit")]
        [Authorize(Roles = "Student")]
        [ProducesResponseType(typeof(ApiResponse<decimal>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Deposit([FromBody] DepositCreditsCommand command)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            command.UserId = userId;
            var response = await _mediator.Send(command);
            return Ok(ApiResponse<decimal>.Ok(response));
        }

        [HttpGet("balance")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<WalletBalanceDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBalance()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var query = new GetBalanceQuery(userId);
            var response = await _mediator.Send(query);
            return Ok(ApiResponse<WalletBalanceDto>.Ok(response));
        }

        [HttpGet("transactions")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<CreditTransactionDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTransactions([FromQuery] Guid? targetUserId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var query = new GetCreditTransactionsQuery
            {
                UserId = userId,
                TargetUserId = targetUserId,
                RequestorRole = roleClaim ?? string.Empty,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var response = await _mediator.Send(query);
            return Ok(ApiResponse<PagedResult<CreditTransactionDto>>.Ok(response));
        }
    }
}
