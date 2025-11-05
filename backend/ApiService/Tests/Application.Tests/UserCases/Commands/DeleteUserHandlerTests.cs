using CSharpFunctionalExtensions;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Commands;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Handlers;
using Epam.ItMarathon.ApiService.Domain.Abstract;
using Epam.ItMarathon.ApiService.Domain.Aggregate.Room;
using Epam.ItMarathon.ApiService.Domain.Builders;
using Epam.ItMarathon.ApiService.Domain.Entities.User;
using Epam.ItMarathon.ApiService.Domain.Shared.ValidationErrors;
using FluentAssertions;
using FluentValidation.Results;
using MediatR;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Epam.ItMarathon.ApiService.Application.Tests.UserCases.Commands
{
    /// <summary>
    /// Unit tests for DeleteUserHandler.
    /// </summary>
    public class DeleteUserHandlerTests
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IUserReadOnlyRepository _userRepository;
        private readonly DeleteUserHandler _handler;
        private readonly CancellationToken _cancellationToken;

        public DeleteUserHandlerTests()
        {
            _roomRepository = Substitute.For<IRoomRepository>();
            _userRepository = Substitute.For<IUserReadOnlyRepository>();
            _handler = new DeleteUserHandler(_roomRepository, _userRepository);
            _cancellationToken = CancellationToken.None;
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenValidAdminDeletesRegularUser()
        {
            // Arrange
            var adminUserCode = "admin123";
            var targetUserId = 2UL;
            var command = new DeleteUserCommand(adminUserCode, targetUserId);

            var adminUser = CreateUser(1UL, adminUserCode, isAdmin: true);
            var targetUser = CreateUser(targetUserId, "target123", isAdmin: false);
            var room = CreateRoom(new List<User> { adminUser, targetUser });

            _roomRepository.GetByUserCodeAsync(adminUserCode, _cancellationToken)
                .Returns(Result.Success<Room, ValidationResult>(room));
            
            _userRepository.GetByIdAsync(targetUserId, _cancellationToken)
                .Returns(Result.Success<User, ValidationResult>(targetUser));

            _roomRepository.UpdateAsync(room, _cancellationToken)
                .Returns(Result.Success(room));

            // Act
            var result = await _handler.Handle(command, _cancellationToken);

            // Assert
            result.IsSuccess.Should().BeTrue();
            room.Users.Should().NotContain(u => u.Id == targetUserId);
            await _roomRepository.Received(1).UpdateAsync(room, _cancellationToken);
        }

        [Fact]
        public async Task Handle_ShouldReturnNotFoundError_WhenUserCodeNotFound()
        {
            // Arrange
            var adminUserCode = "nonexistent";
            var targetUserId = 2UL;
            var command = new DeleteUserCommand(adminUserCode, targetUserId);

            _roomRepository.GetByUserCodeAsync(adminUserCode, _cancellationToken)
                .Returns(Result.Failure<Room, ValidationResult>(new NotFoundError([])));

            // Act
            var result = await _handler.Handle(command, _cancellationToken);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
        }

        [Fact]
        public async Task Handle_ShouldReturnBadRequestError_WhenRoomIsAlreadyClosed()
        {
            // Arrange
            var adminUserCode = "admin123";
            var targetUserId = 2UL;
            var command = new DeleteUserCommand(adminUserCode, targetUserId);

            var adminUser = CreateUser(1UL, adminUserCode, isAdmin: true);
            var targetUser = CreateUser(targetUserId, "target123", isAdmin: false);
            var room = CreateRoom(new List<User> { adminUser, targetUser }, closedOn: DateTime.UtcNow);

            _roomRepository.GetByUserCodeAsync(adminUserCode, _cancellationToken)
                .Returns(Result.Success<Room, ValidationResult>(room));

            // Act
            var result = await _handler.Handle(command, _cancellationToken);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<BadRequestError>();
            result.Error.Errors.Should().Contain(e => 
                e.PropertyName == "room" && 
                e.ErrorMessage == "Cannot remove users from a closed room.");
        }

        [Fact]
        public async Task Handle_ShouldReturnNotFoundError_WhenAuthUserNotFoundInRoom()
        {
            // Arrange
            var adminUserCode = "admin123";
            var targetUserId = 2UL;
            var command = new DeleteUserCommand(adminUserCode, targetUserId);

            var targetUser = CreateUser(targetUserId, "target123", isAdmin: false);
            var room = CreateRoom(new List<User> { targetUser }); // No admin user

            _roomRepository.GetByUserCodeAsync(adminUserCode, _cancellationToken)
                .Returns(Result.Success<Room, ValidationResult>(room));

            // Act
            var result = await _handler.Handle(command, _cancellationToken);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
            result.Error.Errors.Should().Contain(e => 
                e.PropertyName == "userCode" && 
                e.ErrorMessage == "Auth user not found in room.");
        }

        [Fact]
        public async Task Handle_ShouldReturnForbiddenError_WhenUserIsNotAdmin()
        {
            // Arrange
            var regularUserCode = "regular123";
            var targetUserId = 2UL;
            var command = new DeleteUserCommand(regularUserCode, targetUserId);

            var regularUser = CreateUser(1UL, regularUserCode, isAdmin: false);
            var targetUser = CreateUser(targetUserId, "target123", isAdmin: false);
            var room = CreateRoom(new List<User> { regularUser, targetUser });

            _roomRepository.GetByUserCodeAsync(regularUserCode, _cancellationToken)
                .Returns(Result.Success<Room, ValidationResult>(room));

            // Act
            var result = await _handler.Handle(command, _cancellationToken);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<ForbiddenError>();
            result.Error.Errors.Should().Contain(e => 
                e.PropertyName == "userCode" && 
                e.ErrorMessage == "Only admin can remove users from room.");
        }

        [Fact]
        public async Task Handle_ShouldReturnNotFoundError_WhenTargetUserNotExistsGlobally()
        {
            // Arrange
            var adminUserCode = "admin123";
            var targetUserId = 999UL;
            var command = new DeleteUserCommand(adminUserCode, targetUserId);

            var adminUser = CreateUser(1UL, adminUserCode, isAdmin: true);
            var room = CreateRoom(new List<User> { adminUser });

            _roomRepository.GetByUserCodeAsync(adminUserCode, _cancellationToken)
                .Returns(Result.Success<Room, ValidationResult>(room));
            
            _userRepository.GetByIdAsync(targetUserId, _cancellationToken)
                .Returns(Result.Failure<User, ValidationResult>(new NotFoundError([])));

            // Act
            var result = await _handler.Handle(command, _cancellationToken);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
            result.Error.Errors.Should().Contain(e => 
                e.PropertyName == "id" && 
                e.ErrorMessage == "User with specified id not found.");
        }

        [Fact]
        public async Task Handle_ShouldReturnForbiddenError_WhenUsersFromDifferentRooms()
        {
            // Arrange
            var adminUserCode = "admin123";
            var targetUserId = 2UL;
            var command = new DeleteUserCommand(adminUserCode, targetUserId);

            var adminUser = CreateUser(1UL, adminUserCode, isAdmin: true);
            var targetUser = CreateUser(targetUserId, "target123", isAdmin: false);
            var room = CreateRoom(new List<User> { adminUser }); // Target user not in this room

            _roomRepository.GetByUserCodeAsync(adminUserCode, _cancellationToken)
                .Returns(Result.Success<Room, ValidationResult>(room));
            
            _userRepository.GetByIdAsync(targetUserId, _cancellationToken)
                .Returns(Result.Success<User, ValidationResult>(targetUser));

            // Act
            var result = await _handler.Handle(command, _cancellationToken);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<ForbiddenError>();
            result.Error.Errors.Should().Contain(e => 
                e.PropertyName == "id" && 
                e.ErrorMessage == "Admin and target user belong to different rooms.");
        }

        [Fact]
        public async Task Handle_ShouldReturnForbiddenError_WhenTryingToDeleteAdmin()
        {
            // Arrange
            var adminUserCode = "admin123";
            var anotherAdminId = 2UL;
            var command = new DeleteUserCommand(adminUserCode, anotherAdminId);

            var adminUser = CreateUser(1UL, adminUserCode, isAdmin: true);
            var anotherAdmin = CreateUser(anotherAdminId, "admin456", isAdmin: true);
            var room = CreateRoom(new List<User> { adminUser, anotherAdmin });

            _roomRepository.GetByUserCodeAsync(adminUserCode, _cancellationToken)
                .Returns(Result.Success<Room, ValidationResult>(room));
            
            _userRepository.GetByIdAsync(anotherAdminId, _cancellationToken)
                .Returns(Result.Success<User, ValidationResult>(anotherAdmin));

            // Act
            var result = await _handler.Handle(command, _cancellationToken);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<ForbiddenError>();
            result.Error.Errors.Should().Contain(e => 
                e.PropertyName == "id" && 
                e.ErrorMessage == "Cannot remove admin user from room.");
        }

        [Fact]
        public async Task Handle_ShouldReturnBadRequestError_WhenRoomUpdateFails()
        {
            // Arrange
            var adminUserCode = "admin123";
            var targetUserId = 2UL;
            var command = new DeleteUserCommand(adminUserCode, targetUserId);

            var adminUser = CreateUser(1UL, adminUserCode, isAdmin: true);
            var targetUser = CreateUser(targetUserId, "target123", isAdmin: false);
            var room = CreateRoom(new List<User> { adminUser, targetUser });

            _roomRepository.GetByUserCodeAsync(adminUserCode, _cancellationToken)
                .Returns(Result.Success<Room, ValidationResult>(room));
            
            _userRepository.GetByIdAsync(targetUserId, _cancellationToken)
                .Returns(Result.Success<User, ValidationResult>(targetUser));

            _roomRepository.UpdateAsync(room, _cancellationToken)
                .Returns(Result.Failure<Room>("Database error"));

            // Act
            var result = await _handler.Handle(command, _cancellationToken);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<BadRequestError>();
        }

        private static User CreateUser(ulong id, string authCode, bool isAdmin = false)
        {
            return UserBuilder.Init()
                .WithId(id)
                .WithAuthCode(authCode)
                .WithIsAdmin(isAdmin)
                .WithFirstName("Test")
                .WithLastName("User")
                .WithPhone("+380000000000")
                .WithDeliveryInfo("Test delivery")
                .WithWantSurprise(true)
                .WithInterests("Test interests")
                .WithWishes([])
                .Build().Value;
        }

        private static Room CreateRoom(List<User> users, DateTime? closedOn = null)
        {
            return RoomBuilder.Init()
                .WithName("Test Room")
                .WithDescription("Test Description")
                .WithInvitationCode("testcode")
                .WithGiftExchangeDate(DateTime.UtcNow.AddDays(7))
                .WithGiftMaximumBudget(1000)
                .WithUsers(users)
                .WithShouldBeClosedOn(closedOn)
                .Build().Value;
        }
    }
}