using CSharpFunctionalExtensions;
using Epam.ItMarathon.ApiService.Domain.Shared.ValidationErrors;
using FluentValidation.Results;
using MediatR;

namespace Epam.ItMarathon.ApiService.Application.UseCases.User.Commands
{
    /// <summary>
    /// </summary>
    public record DeleteUserCommand(string UserCode, ulong UserId) : IRequest<Result<Unit, ValidationResult>>;
}
