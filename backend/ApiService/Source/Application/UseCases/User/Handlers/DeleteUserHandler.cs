using CSharpFunctionalExtensions;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Commands;
using Epam.ItMarathon.ApiService.Domain.Abstract;
using Epam.ItMarathon.ApiService.Domain.Shared.ValidationErrors;
using FluentValidation.Results;
using MediatR;

namespace Epam.ItMarathon.ApiService.Application.UseCases.User.Handlers
{
    /// <summary>
    /// Handler for deleting user from room by admin's userCode.
    /// </summary>
    public class DeleteUserHandler(IRoomRepository roomRepository, IUserReadOnlyRepository userRepository) :
        IRequestHandler<DeleteUserCommand, Result<Unit, ValidationResult>>
    {
        public async Task<Result<Unit, ValidationResult>> Handle(DeleteUserCommand request,
            CancellationToken cancellationToken)
        {
            var roomResult = await roomRepository.GetByUserCodeAsync(request.UserCode, cancellationToken);
            if (roomResult.IsFailure)
            {
                return roomResult.ConvertFailure<Unit>();
            }

            var room = roomResult.Value;
            
            // Check if room is already closed (drawn)
            if (room.ClosedOn is not null)
            {
                return Result.Failure<Unit, ValidationResult>(new BadRequestError([
                    new FluentValidation.Results.ValidationFailure("room", "Cannot remove users from a closed room.")
                ]));
            }
            
            var authUser = room.Users.FirstOrDefault(u => u.AuthCode.Equals(request.UserCode));
            if (authUser is null)
            {
                return Result.Failure<Unit, ValidationResult>(new NotFoundError([
                    new FluentValidation.Results.ValidationFailure("userCode", "Auth user not found in room.")
                ]));
            }

            if (!authUser.IsAdmin)
            {
                return Result.Failure<Unit, ValidationResult>(new ForbiddenError([
                    new FluentValidation.Results.ValidationFailure("userCode", "Only admin can remove users from room.")
                ]));
            }

            // Check if target user exists globally first
            var targetUserGlobalResult = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (targetUserGlobalResult.IsFailure)
            {
                return Result.Failure<Unit, ValidationResult>(new NotFoundError([
                    new FluentValidation.Results.ValidationFailure("id", "User with specified id not found.")
                ]));
            }

            // Check if target user belongs to the same room as admin
            var targetUser = room.Users.FirstOrDefault(u => u.Id == request.UserId);
            if (targetUser is null)
            {
                return Result.Failure<Unit, ValidationResult>(new ForbiddenError([
                    new FluentValidation.Results.ValidationFailure("id", "Admin and target user belong to different rooms.")
                ]));
            }

            if (targetUser.IsAdmin)
            {
                return Result.Failure<Unit, ValidationResult>(new ForbiddenError([
                    new FluentValidation.Results.ValidationFailure("id", "Cannot remove admin user from room.")
                ]));
            }

            // Remove target user from room
            room.Users.Remove(targetUser);

            var updatingResult = await roomRepository.UpdateAsync(room, cancellationToken);
            if (updatingResult.IsFailure)
            {
                return Result.Failure<Unit, ValidationResult>(new BadRequestError([
                    new FluentValidation.Results.ValidationFailure(string.Empty, updatingResult.Error)
                ]));
            }

            return Result.Success<Unit, ValidationResult>(Unit.Value);
        }
    }
}
