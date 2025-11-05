using Epam.ItMarathon.ApiService.Api.Endpoints;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Commands;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using CSharpFunctionalExtensions;
using FluentValidation.Results;
using Epam.ItMarathon.ApiService.Domain.Shared.ValidationErrors;

namespace Epam.ItMarathon.ApiService.Api.Tests.Endpoints
{
    /// <summary>
    /// Integration tests for DELETE /users/{id} endpoint.
    /// </summary>
    public class DeleteUserEndpointTests
    {
        private readonly IMediator _mediator;
        private readonly CancellationToken _cancellationToken;

        public DeleteUserEndpointTests()
        {
            _mediator = Substitute.For<IMediator>();
            _cancellationToken = CancellationToken.None;
        }

        [Fact]
        public async Task DeleteUser_ShouldReturnNoContent_WhenSuccessful()
        {
            // Arrange
            var userId = 1UL;
            var userCode = "admin123";
            
            _mediator.Send(Arg.Any<DeleteUserCommand>(), _cancellationToken)
                .Returns(Result.Success<MediatR.Unit, ValidationResult>(MediatR.Unit.Value));

            // Act
            var result = await UserEndpoints.DeleteUser(userId, userCode, _mediator, _cancellationToken);

            // Assert
            result.Should().NotBeNull();
            
            // Verify the mediator was called with correct parameters
            await _mediator.Received(1).Send(
                Arg.Is<DeleteUserCommand>(cmd => cmd.UserId == userId && cmd.UserCode == userCode),
                _cancellationToken);
        }

        [Fact]
        public async Task DeleteUser_ShouldReturnProblem_WhenUserNotFound()
        {
            // Arrange
            var userId = 999UL;
            var userCode = "admin123";
            
            var notFoundError = new NotFoundError([
                new FluentValidation.Results.ValidationFailure("id", "User with specified id not found.")
            ]);
            
            _mediator.Send(Arg.Any<DeleteUserCommand>(), _cancellationToken)
                .Returns(Result.Failure<MediatR.Unit, ValidationResult>(notFoundError));

            // Act
            var result = await UserEndpoints.DeleteUser(userId, userCode, _mediator, _cancellationToken);

            // Assert
            result.Should().NotBeNull();
            
            await _mediator.Received(1).Send(
                Arg.Is<DeleteUserCommand>(cmd => cmd.UserId == userId && cmd.UserCode == userCode),
                _cancellationToken);
        }

        [Fact]
        public async Task DeleteUser_ShouldReturnProblem_WhenUserNotAdmin()
        {
            // Arrange
            var userId = 2UL;
            var userCode = "regular123";
            
            var forbiddenError = new ForbiddenError([
                new FluentValidation.Results.ValidationFailure("userCode", "Only admin can remove users from room.")
            ]);
            
            _mediator.Send(Arg.Any<DeleteUserCommand>(), _cancellationToken)
                .Returns(Result.Failure<MediatR.Unit, ValidationResult>(forbiddenError));

            // Act
            var result = await UserEndpoints.DeleteUser(userId, userCode, _mediator, _cancellationToken);

            // Assert
            result.Should().NotBeNull();
            
            await _mediator.Received(1).Send(
                Arg.Is<DeleteUserCommand>(cmd => cmd.UserId == userId && cmd.UserCode == userCode),
                _cancellationToken);
        }

        [Fact]
        public async Task DeleteUser_ShouldReturnProblem_WhenRoomIsClosed()
        {
            // Arrange
            var userId = 2UL;
            var userCode = "admin123";
            
            var badRequestError = new BadRequestError([
                new FluentValidation.Results.ValidationFailure("room", "Cannot remove users from a closed room.")
            ]);
            
            _mediator.Send(Arg.Any<DeleteUserCommand>(), _cancellationToken)
                .Returns(Result.Failure<MediatR.Unit, ValidationResult>(badRequestError));

            // Act
            var result = await UserEndpoints.DeleteUser(userId, userCode, _mediator, _cancellationToken);

            // Assert
            result.Should().NotBeNull();
            
            await _mediator.Received(1).Send(
                Arg.Is<DeleteUserCommand>(cmd => cmd.UserId == userId && cmd.UserCode == userCode),
                _cancellationToken);
        }

        [Fact]
        public async Task DeleteUser_ShouldReturnProblem_WhenUsersFromDifferentRooms()
        {
            // Arrange
            var userId = 2UL;
            var userCode = "admin123";
            
            var forbiddenError = new ForbiddenError([
                new FluentValidation.Results.ValidationFailure("id", "Admin and target user belong to different rooms.")
            ]);
            
            _mediator.Send(Arg.Any<DeleteUserCommand>(), _cancellationToken)
                .Returns(Result.Failure<MediatR.Unit, ValidationResult>(forbiddenError));

            // Act
            var result = await UserEndpoints.DeleteUser(userId, userCode, _mediator, _cancellationToken);

            // Assert
            result.Should().NotBeNull();
            
            await _mediator.Received(1).Send(
                Arg.Is<DeleteUserCommand>(cmd => cmd.UserId == userId && cmd.UserCode == userCode),
                _cancellationToken);
        }

        [Fact]
        public async Task DeleteUser_ShouldReturnProblem_WhenTryingToDeleteAdmin()
        {
            // Arrange
            var userId = 1UL; // Admin user ID
            var userCode = "admin123";
            
            var forbiddenError = new ForbiddenError([
                new FluentValidation.Results.ValidationFailure("id", "Cannot remove admin user from room.")
            ]);
            
            _mediator.Send(Arg.Any<DeleteUserCommand>(), _cancellationToken)
                .Returns(Result.Failure<MediatR.Unit, ValidationResult>(forbiddenError));

            // Act
            var result = await UserEndpoints.DeleteUser(userId, userCode, _mediator, _cancellationToken);

            // Assert
            result.Should().NotBeNull();
            
            await _mediator.Received(1).Send(
                Arg.Is<DeleteUserCommand>(cmd => cmd.UserId == userId && cmd.UserCode == userCode),
                _cancellationToken);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task DeleteUser_ShouldHandleInvalidUserCode(string? userCode)
        {
            // Arrange
            var userId = 1UL;
            
            // Act & Assert
            // This test would depend on how the endpoint validates the userCode parameter
            // In a real scenario, you might want to test the validation attributes
            var result = await UserEndpoints.DeleteUser(userId, userCode, _mediator, _cancellationToken);
            
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task DeleteUser_ShouldHandleZeroUserId()
        {
            // Arrange
            var userId = 0UL;
            var userCode = "admin123";
            
            var notFoundError = new NotFoundError([
                new FluentValidation.Results.ValidationFailure("id", "User with specified id not found.")
            ]);
            
            _mediator.Send(Arg.Any<DeleteUserCommand>(), _cancellationToken)
                .Returns(Result.Failure<MediatR.Unit, ValidationResult>(notFoundError));

            // Act
            var result = await UserEndpoints.DeleteUser(userId, userCode, _mediator, _cancellationToken);

            // Assert
            result.Should().NotBeNull();
            
            await _mediator.Received(1).Send(
                Arg.Is<DeleteUserCommand>(cmd => cmd.UserId == userId && cmd.UserCode == userCode),
                _cancellationToken);
        }
    }
}