using Epam.ItMarathon.ApiService.Api.Dto.CreateDtos;
using Epam.ItMarathon.ApiService.Api.Dto.ReadDtos;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Epam.ItMarathon.ApiService.Api.Tests.Integration
{
    /// <summary>
    /// Integration tests for DELETE /users/{id} endpoint with full application context.
    /// </summary>
    public class DeleteUserIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public DeleteUserIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        [Fact]
        public async Task DeleteUser_ShouldReturnNoContent_WhenValidAdminDeletesRegularUser()
        {
            // Arrange - Create room and users
            var (adminUserCode, regularUserId) = await SetupRoomWithUsers();

            // Act - Delete regular user
            var response = await _client.DeleteAsync($"/api/users/{regularUserId}?userCode={adminUserCode}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify user was actually deleted by trying to get users
            var getUsersResponse = await _client.GetAsync($"/api/users?userCode={adminUserCode}");
            getUsersResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var usersJson = await getUsersResponse.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<UserReadDto>>(usersJson, _jsonOptions);
            
            users.Should().NotBeNull();
            users.Should().NotContain(u => u.Id == regularUserId);
        }

        [Fact]
        public async Task DeleteUser_ShouldReturnNotFound_WhenUserIdDoesNotExist()
        {
            // Arrange
            var (adminUserCode, _) = await SetupRoomWithUsers();
            var nonExistentUserId = 99999UL;

            // Act
            var response = await _client.DeleteAsync($"/api/users/{nonExistentUserId}?userCode={adminUserCode}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteUser_ShouldReturnNotFound_WhenUserCodeDoesNotExist()
        {
            // Arrange
            var (_, regularUserId) = await SetupRoomWithUsers();
            var nonExistentUserCode = "nonexistent123";

            // Act
            var response = await _client.DeleteAsync($"/api/users/{regularUserId}?userCode={nonExistentUserCode}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteUser_ShouldReturnForbidden_WhenRegularUserTriesToDelete()
        {
            // Arrange
            var (adminUserCode, regularUserId) = await SetupRoomWithUsers();
            
            // Get regular user's code by joining another user
            var anotherUserCode = await JoinUserToExistingRoom(adminUserCode);

            // Act - Regular user tries to delete someone
            var response = await _client.DeleteAsync($"/api/users/{regularUserId}?userCode={anotherUserCode}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task DeleteUser_ShouldReturnForbidden_WhenTryingToDeleteAdmin()
        {
            // Arrange
            var (adminUserCode, _) = await SetupRoomWithUsers();
            
            // Get admin user ID
            var getUsersResponse = await _client.GetAsync($"/api/users?userCode={adminUserCode}");
            var usersJson = await getUsersResponse.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<UserReadDto>>(usersJson, _jsonOptions);
            var adminUser = users.First(u => u.IsAdmin);

            // Act - Try to delete admin
            var response = await _client.DeleteAsync($"/api/users/{adminUser.Id}?userCode={adminUserCode}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task DeleteUser_ShouldReturnBadRequest_WhenRoomIsAlreadyClosed()
        {
            // Arrange
            var (adminUserCode, regularUserId) = await SetupRoomWithUsers();
            
            // Draw the room to close it
            var drawResponse = await _client.PostAsync($"/api/rooms/draw?userCode={adminUserCode}", null);
            drawResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Act - Try to delete user from closed room
            var response = await _client.DeleteAsync($"/api/users/{regularUserId}?userCode={adminUserCode}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task DeleteUser_ShouldReturnForbidden_WhenUsersFromDifferentRooms()
        {
            // Arrange
            var (adminUserCode1, _) = await SetupRoomWithUsers();
            var (adminUserCode2, regularUserId2) = await SetupRoomWithUsers();

            // Act - Admin from room 1 tries to delete user from room 2
            var response = await _client.DeleteAsync($"/api/users/{regularUserId2}?userCode={adminUserCode1}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task DeleteUser_ShouldReturnBadRequest_WhenUserCodeIsEmpty()
        {
            // Arrange
            var (_, regularUserId) = await SetupRoomWithUsers();

            // Act
            var response = await _client.DeleteAsync($"/api/users/{regularUserId}?userCode=");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task DeleteUser_ShouldReturnBadRequest_WhenUserIdIsInvalid(long invalidUserId)
        {
            // Arrange
            var (adminUserCode, _) = await SetupRoomWithUsers();

            // Act
            var response = await _client.DeleteAsync($"/api/users/{invalidUserId}?userCode={adminUserCode}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Helper method to create a room with admin and regular user.
        /// </summary>
        /// <returns>Tuple of (adminUserCode, regularUserId)</returns>
        private async Task<(string adminUserCode, ulong regularUserId)> SetupRoomWithUsers()
        {
            // Create room with admin
            var roomRequest = new RoomCreationRequest
            {
                Room = new RoomCreateDto
                {
                    Name = $"Test Room {Guid.NewGuid()}",
                    Description = "Test Description",
                    GiftExchangeDate = DateTime.UtcNow.AddDays(7),
                    GiftMaximumBudget = 1000
                },
                AdminUser = new UserCreateDto
                {
                    FirstName = "Admin",
                    LastName = "User",
                    Phone = "+380000000000",
                    DeliveryInfo = "Admin delivery",
                    WantSurprise = true,
                    Interests = "Admin interests"
                }
            };

            var roomJson = JsonSerializer.Serialize(roomRequest, _jsonOptions);
            var roomContent = new StringContent(roomJson, Encoding.UTF8, "application/json");
            
            var roomResponse = await _client.PostAsync("/api/rooms", roomContent);
            roomResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            
            var roomResponseJson = await roomResponse.Content.ReadAsStringAsync();
            var roomResult = JsonSerializer.Deserialize<RoomCreationResponse>(roomResponseJson, _jsonOptions);
            
            var adminUserCode = roomResult.UserCode;
            var roomCode = roomResult.Room.InvitationCode;

            // Add regular user to room
            var regularUserId = await JoinUserToRoom(roomCode);

            return (adminUserCode, regularUserId);
        }

        /// <summary>
        /// Helper method to join a user to an existing room.
        /// </summary>
        private async Task<ulong> JoinUserToRoom(string roomCode)
        {
            var userRequest = new UserCreateDto
            {
                FirstName = "Regular",
                LastName = "User",
                Phone = "+380111111111",
                DeliveryInfo = "Regular delivery",
                WantSurprise = true,
                Interests = "Regular interests"
            };

            var userJson = JsonSerializer.Serialize(userRequest, _jsonOptions);
            var userContent = new StringContent(userJson, Encoding.UTF8, "application/json");
            
            var userResponse = await _client.PostAsync($"/api/users?roomCode={roomCode}", userContent);
            userResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            
            var userResponseJson = await userResponse.Content.ReadAsStringAsync();
            var userResult = JsonSerializer.Deserialize<UserCreationResponse>(userResponseJson, _jsonOptions);
            
            return userResult.Id;
        }

        /// <summary>
        /// Helper method to join another user to an existing room and return their code.
        /// </summary>
        private async Task<string> JoinUserToExistingRoom(string adminUserCode)
        {
            // First get the room code
            var roomResponse = await _client.GetAsync($"/api/rooms?userCode={adminUserCode}");
            var roomJson = await roomResponse.Content.ReadAsStringAsync();
            var room = JsonSerializer.Deserialize<RoomReadDto>(roomJson, _jsonOptions);
            
            var userRequest = new UserCreateDto
            {
                FirstName = "Another",
                LastName = "User",
                Phone = "+380222222222",
                DeliveryInfo = "Another delivery",
                WantSurprise = true,
                Interests = "Another interests"
            };

            var userJson = JsonSerializer.Serialize(userRequest, _jsonOptions);
            var userContent = new StringContent(userJson, Encoding.UTF8, "application/json");
            
            var userResponse = await _client.PostAsync($"/api/users?roomCode={room.InvitationCode}", userContent);
            var userResponseJson = await userResponse.Content.ReadAsStringAsync();
            var userResult = JsonSerializer.Deserialize<UserCreationResponse>(userResponseJson, _jsonOptions);
            
            return userResult.UserCode;
        }
    }
}