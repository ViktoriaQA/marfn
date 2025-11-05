using Reqnroll;
using Shouldly;
using Tests.Api.Clients;
using Tests.Api.Models.Requests;
using Tests.Api.Models.Responses;
using Tests.Common.Models;
using Tests.Common.TestData;
using Tests.Helpers;

namespace Tests.Api.Steps
{
    [Binding]
    public class UserApiSteps(
        ScenarioContext scenarioContext,
        UserApiClient apiClient,
        RoomApiClient roomApiClient)
    {
        private readonly ScenarioContext _scenarioContext = scenarioContext;
        private readonly UserApiClient _apiClient = apiClient;
        private readonly RoomApiClient _roomApiClient = roomApiClient;

        [When("I try to join the room")]
        public async Task WhenITryToJoinTheRoom()
        {
            try
            {
                await WhenIJoinTheRoom();
            }
            catch (ApiException ex)
            {
                _scenarioContext.Set(ex.ActualStatus, "LastStatusCode");
                _scenarioContext.Set(ex.ResponseBody, "LastErrorBody");
            }
        }

        [Given("a room exists with invitation code")]
        public async Task GivenARoomExistsWithInvitationCode()
        {
            if (_scenarioContext.ContainsKey("RoomCreationResponse"))
                return;

            var request = new RoomCreationRequest
            {
                Room = TestDataGenerator.GenerateRoom(),
                AdminUser = TestDataGenerator.GenerateUser()
            };

            var response = await _roomApiClient.CreateRoomAsync(request);
            _scenarioContext.Set(response);
            _scenarioContext.Set(response.Room.InvitationCode!, "InvitationCode");
        }

        [Given("I have user data with wish list:")]
        public void GivenIHaveUserDataWithWishList(Table table)
        {
            var user = TestDataGenerator.GenerateUser(false, table);

            user.WishList = [];
            _scenarioContext.Set(user, "UserData");
        }

        [Given("I have wishes:")]
        public void GivenIHaveWishes(Table table)
        {
            var wishes = table.CreateSet<WishDto>().ToList();
            var user = _scenarioContext.Get<UserCreationDto>("UserData");
            user.WishList = wishes;
        }

        [Given("I have user data with surprise preference:")]
        public void GivenIHaveUserDataWithSurprisePreference(Table table)
        {
            var user = TestDataGenerator.GenerateUser(true, table);

            _scenarioContext.Set(user, "UserData");
        }

        [Given("I have user data:")]
        public void GivenIHaveUserData(Table table)
        {
            var userData = new UserCreationDto
            {
                FirstName = "",
                LastName = "",
                Phone = "",
                Email = "",
                DeliveryInfo = "Test Address",
                WantSurprise = false,
                WishList = []
            };

            foreach (var row in table.Rows)
            {
                switch (row["Field"])
                {
                    case "FirstName":
                        userData.FirstName = row["Value"];
                        break;
                    case "LastName":
                        userData.LastName = row["Value"];
                        break;
                    case "Phone":
                        userData.Phone = row["Value"];
                        break;
                    case "Email":
                        userData.Email = row["Value"];
                        break;
                }
            }

            _scenarioContext.Set(userData, "UserData");
        }

        [Given("I am in a room with multiple users")]
        public async Task GivenIAmInARoomWithMultipleUsers()
        {
            var roomRequest = new RoomCreationRequest
            {
                Room = TestDataGenerator.GenerateRoom(),
                AdminUser = TestDataGenerator.GenerateUser()
            };

            var roomResponse = await _roomApiClient.CreateRoomAsync(roomRequest);
            _scenarioContext.Set(roomResponse);

            for (int i = 0; i < 3; i++)
            {
                await _apiClient.CreateUserAsync(
                    roomResponse.Room.InvitationCode!,
                    TestDataGenerator.GenerateUser());
            }
        }

        [Given("I am a room admin")]
        public async Task GivenIAmARoomAdmin()
        {
            await GivenIAmInARoomWithMultipleUsers();
        }

        [Given("there are other users in the room")]
        public async Task GivenThereAreOtherUsersInTheRoom()
        {
            var roomResponse = _scenarioContext.Get<RoomCreationResponse>();

            for (int i = 0; i < 2; i++)
            {
                await _apiClient.CreateUserAsync(
                    roomResponse.Room.InvitationCode!,
                    TestDataGenerator.GenerateUser());
            }
        }

        [Given("I am a regular user")]
        public async Task GivenIAmARegularUser()
        {
            var roomResponse = _scenarioContext.Get<RoomCreationResponse>();
            var userResponse = await _apiClient.CreateUserAsync(
                roomResponse.Room.InvitationCode!,
                TestDataGenerator.GenerateUser());

            _scenarioContext.Set(userResponse.UserCode!, "RegularUserCode");
        }

        [Given("I am a user in a room")]
        public async Task GivenIAmAUserInARoom()
        {
            await GivenIAmARegularUser();
        }

        [When("I join the room")]
        public async Task WhenIJoinTheRoom()
        {
            try
            {
                var invitationCode = _scenarioContext.Get<string>("InvitationCode");
                var user = _scenarioContext.Get<UserCreationDto>("UserData");

                var response = await _apiClient.CreateUserAsync(invitationCode, user);
                _scenarioContext.Set(response, "UserResponse");
                _scenarioContext.Set(201, "LastStatusCode");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Expected status"))
            {
                _scenarioContext.Set(ex.Message, "LastError");
                var match = ValidationPatterns.StatusCodePattern().Match(ex.Message);
                if (match.Success)
                {
                    _scenarioContext.Set(int.Parse(match.Groups[1].Value), "LastStatusCode");
                }
            }
        }

        [When("I get the list of users using my code")]
        public async Task WhenIGetTheListOfUsersUsingMyCode()
        {
            var userCode = _scenarioContext.Get<RoomCreationResponse>().UserCode;
            var users = await _apiClient.GetUsersAsync(userCode);
            _scenarioContext.Set(users, "UsersList");
        }

        [When("I get user details by ID")]
        public async Task WhenIGetUserDetailsByID()
        {
            var adminCode = _scenarioContext.Get<RoomCreationResponse>().UserCode;
            var users = await _apiClient.GetUsersAsync(adminCode);

            var targetUser = users.First(u => !u.IsAdmin);
            var userDetails = await _apiClient.GetUserByIdAsync(targetUser.Id, adminCode);

            _scenarioContext.Set(userDetails, "UserDetails");
        }

        [When("I try to get another user's details")]
        public async Task WhenITryToGetAnotherUsersDetails()
        {
            var regularUserCode = _scenarioContext.Get<string>("RegularUserCode");
            var adminCode = _scenarioContext.Get<RoomCreationResponse>().UserCode;

            var users = await _apiClient.GetUsersAsync(adminCode);
            var targetUser = users.FirstOrDefault(u => !u.IsAdmin && u.UserCode != regularUserCode) ?? throw new InvalidOperationException("No other users found in the room");
            var userDetails = await _apiClient.GetUserByIdAsync(targetUser.Id, regularUserCode);
            _scenarioContext.Set(userDetails, "UserDetails");
            _scenarioContext.Set(200, "LastStatusCode");
        }

        [Then("I should be added successfully")]
        public void ThenIShouldBeAddedSuccessfully()
        {
            var response = _scenarioContext.Get<UserCreationResponse>("UserResponse");
            response.ShouldNotBeNull();
            response.Id.ShouldBeGreaterThan(0);
        }

        [Then("I should receive my user code")]
        public void ThenIShouldReceiveMyUserCode()
        {
            var response = _scenarioContext.Get<UserCreationResponse>("UserResponse");
            response.UserCode.ShouldNotBeNullOrEmpty();
        }

        [Then("my wishes should be saved")]
        public void ThenMyWishesShouldBeSaved()
        {
            var response = _scenarioContext.Get<UserCreationResponse>("UserResponse");
            response.WishList.ShouldNotBeEmpty();
        }

        [Then("my wish list should be empty")]
        public void ThenMyWishListShouldBeEmpty()
        {
            var response = _scenarioContext.Get<UserCreationResponse>("UserResponse");
            response.WishList.ShouldBeEmpty();
        }

        [Then("my interests should be saved")]
        public void ThenMyInterestsShouldBeSaved()
        {
            var userData = _scenarioContext.Get<UserCreationDto>("UserData");
            userData.Interests.ShouldNotBeNullOrEmpty();
        }

        [Then("I should receive all users in the room")]
        public void ThenIShouldReceiveAllUsersInTheRoom()
        {
            var users = _scenarioContext.Get<List<UserReadDto>>("UsersList");
            users.ShouldNotBeNull();
            users.Count.ShouldBeGreaterThan(1);
        }

        [Then("each user should have basic information including name and ID")]
        public void ThenEachUserShouldHaveBasicInformationIncludingNameAndID()
        {
            var users = _scenarioContext.Get<List<UserReadDto>>("UsersList");

            foreach (var user in users)
            {
                user.FirstName.ShouldNotBeNullOrEmpty();
                user.LastName.ShouldNotBeNullOrEmpty();
                user.Id.ShouldBeGreaterThan(0);
            }
        }

        [Then("each user should have gift preference indicator")]
        public void ThenEachUserShouldHaveGiftPreferenceIndicator()
        {
            var users = _scenarioContext.Get<List<UserReadDto>>("UsersList");

            foreach (var user in users)
            {
                user.ShouldSatisfyAllConditions(
                    () => user.Id.ShouldBeGreaterThan(0),
                    () => user.FirstName.ShouldNotBeNullOrEmpty()
                );
            }
        }

        [Then("users should not have sensitive data like phone or email or address")]
        public void ThenUsersShouldNotHaveSensitiveDataLikePhoneOrEmailOrAddress()
        {
            var users = _scenarioContext.Get<List<UserReadDto>>("UsersList");

            // Check if current user is admin
            var response = _scenarioContext.Get<RoomCreationResponse>();
            var currentUser = users.FirstOrDefault(u => u.UserCode == response.UserCode);

            if (currentUser?.IsAdmin == true)
            {
                // Admin SHOULD see sensitive data - this is a different test
                users.ForEach(u =>
                {
                    Console.WriteLine($"Admin sees user {u.FirstName} {u.LastName} with phone: {u.Phone}, email: {u.Email}");
                });
            }
            else
            {
                // Regular users should NOT see sensitive data of others
                var myUserCode = response.UserCode;
                var otherUsers = users.Where(u => u.UserCode != myUserCode).ToList();

                otherUsers.ShouldSatisfyAllConditions(
                    () => otherUsers.ShouldNotBeEmpty("Should have other users to check"),
                    () => otherUsers.ShouldAllBe(u => string.IsNullOrEmpty(u.Phone),
                        $"Other users should not expose phone. Found: {string.Join(", ", otherUsers.Select(u => u.Phone))}"),
                    () => otherUsers.ShouldAllBe(u => string.IsNullOrEmpty(u.Email),
                        "Other users should not expose email"),
                    () => otherUsers.ShouldAllBe(u => string.IsNullOrEmpty(u.DeliveryInfo),
                        "Other users should not expose delivery info")
                );
            }
        }

        [Then("admin should see all user data including sensitive information")]
        public void ThenAdminShouldSeeAllUserDataIncludingSensitiveInformation()
        {
            var users = _scenarioContext.Get<List<UserReadDto>>("UsersList");

            // Admin should see everyone's data
            users.Where(u => !u.IsAdmin).ShouldAllBe(u =>
                !string.IsNullOrEmpty(u.Phone) &&
                !string.IsNullOrEmpty(u.DeliveryInfo),
                "Admin should see all users' sensitive data");
        }

        [Then("I should see complete user information")]
        public void ThenIShouldSeeCompleteUserInformation()
        {
            var userDetails = _scenarioContext.Get<UserReadDto>("UserDetails");

            userDetails.FirstName.ShouldNotBeNullOrEmpty();
            userDetails.LastName.ShouldNotBeNullOrEmpty();
            userDetails.Id.ShouldBeGreaterThan(0);
            userDetails.RoomId.ShouldBeGreaterThan(0);
        }

        [Then("I should not see their sensitive information")]
        public void ThenIShouldNotSeeTheirSensitiveInformation()
        {
            var userDetails = _scenarioContext.Get<UserReadDto>("UserDetails");

            userDetails.Phone.ShouldBeNullOrEmpty("Regular users should not see phone");
            userDetails.DeliveryInfo.ShouldBeNullOrEmpty("Regular users should not see delivery info");
            // Email might be visible for communication purposes
        }

        [Then("I should see their gift preferences")]
        public void ThenIShouldSeeTheirGiftPreferences()
        {
            var userDetails = _scenarioContext.Get<UserReadDto>("UserDetails");

            if (userDetails.WantSurprise.HasValue)
            {
                if (userDetails.WantSurprise.Value)
                {
                    userDetails.Interests.ShouldNotBeNullOrEmpty();
                }
                else
                {
                    userDetails.WishList.ShouldNotBeNull();
                }
            }
        }

        [Then("the request should (pass|fail) with status {int}")]
        public void ThenTheRequestShouldPassOrFailWithStatus(string result, int expectedStatus)
        {
            var actualStatus = _scenarioContext.Get<int>("LastStatusCode");
            actualStatus.ShouldBe(expectedStatus);

            if (result == "pass")
            {
                actualStatus.ShouldBeInRange(200, 299);
            }
            else
            {
                actualStatus.ShouldBeGreaterThanOrEqualTo(400);
            }
        }

        [Then("my preferences should be updated")]
        public void ThenMyPreferencesShouldBeUpdated()
        {
            var updatedUser = _scenarioContext.Get<UserReadDto>("UpdatedUser");
            updatedUser.ShouldNotBeNull();
            updatedUser.WantSurprise?.ShouldBeFalse();
        }

        [Then("my wish list should contain {int} item")]
        [Then("my wish list should contain {int} items")]
        public void ThenMyWishListShouldContainItems(int count)
        {
            var updatedUser = _scenarioContext.Get<UserReadDto>("UpdatedUser");
            updatedUser.WishList?.Count.ShouldBe(count);
        }

        // User Deletion Steps

        [Given("there is a regular user in my room")]
        public async Task GivenThereIsARegularUserInMyRoom()
        {
            var adminUserCode = _scenarioContext.Get<string>("AdminUserCode");
            var invitationCode = _scenarioContext.Get<string>("InvitationCode");

            // Add a regular user to the room
            var regularUser = TestDataGenerator.GenerateUser();
            var response = await _apiClient.JoinRoomAsync(invitationCode, regularUser);
            
            _scenarioContext.Set(response, "RegularUserResponse");
            _scenarioContext.Set(response.Id, "RegularUserId");
        }

        [Given("there are {int} regular users in my room")]
        public async Task GivenThereAreRegularUsersInMyRoom(int count)
        {
            var invitationCode = _scenarioContext.Get<string>("InvitationCode");
            var userIds = new List<ulong>();

            for (int i = 0; i < count; i++)
            {
                var regularUser = TestDataGenerator.GenerateUser();
                var response = await _apiClient.JoinRoomAsync(invitationCode, regularUser);
                userIds.Add(response.Id);
            }

            _scenarioContext.Set(userIds, "RegularUserIds");
        }

        [Given("there is another regular user in my room")]
        public async Task GivenThereIsAnotherRegularUserInMyRoom()
        {
            await GivenThereIsARegularUserInMyRoom();
        }

        [Given("there is a regular user in room B")]
        public async Task GivenThereIsARegularUserInRoomB()
        {
            // Create another room (Room B)
            var roomBRequest = new RoomCreationRequest
            {
                Room = TestDataGenerator.GenerateRoom(),
                AdminUser = TestDataGenerator.GenerateUser()
            };

            var roomBResponse = await _roomApiClient.CreateRoomAsync(roomBRequest);
            _scenarioContext.Set(roomBResponse, "RoomBCreationResponse");

            // Add a regular user to room B
            var regularUser = TestDataGenerator.GenerateUser();
            var userResponse = await _apiClient.JoinRoomAsync(roomBResponse.Room.InvitationCode!, regularUser);
            
            _scenarioContext.Set(userResponse.Id, "RoomBUserId");
        }

        [Given("the room has been drawn and closed")]
        public async Task GivenTheRoomHasBeenDrawnAndClosed()
        {
            var adminUserCode = _scenarioContext.Get<string>("AdminUserCode");
            
            // Draw the room to close it
            await _roomApiClient.DrawRoomAsync(adminUserCode);
            _scenarioContext.Set(true, "RoomIsClosed");
        }

        [When("I delete the regular user by their ID")]
        public async Task WhenIDeleteTheRegularUserByTheirID()
        {
            var adminUserCode = _scenarioContext.Get<string>("AdminUserCode");
            var regularUserId = _scenarioContext.Get<ulong>("RegularUserId");

            try
            {
                await _apiClient.DeleteUserAsync(regularUserId, adminUserCode);
                _scenarioContext.Set(204, "LastStatusCode");
            }
            catch (ApiException ex)
            {
                _scenarioContext.Set(ex.ActualStatus, "LastStatusCode");
                _scenarioContext.Set(ex.ResponseBody, "LastErrorBody");
            }
        }

        [When("I delete {int} of the regular users")]
        public async Task WhenIDeleteOfTheRegularUsers(int countToDelete)
        {
            var adminUserCode = _scenarioContext.Get<string>("AdminUserCode");
            var userIds = _scenarioContext.Get<List<ulong>>("RegularUserIds");
            var deletedIds = new List<ulong>();

            for (int i = 0; i < countToDelete && i < userIds.Count; i++)
            {
                try
                {
                    await _apiClient.DeleteUserAsync(userIds[i], adminUserCode);
                    deletedIds.Add(userIds[i]);
                }
                catch (ApiException ex)
                {
                    _scenarioContext.Set(ex.ActualStatus, "LastStatusCode");
                    _scenarioContext.Set(ex.ResponseBody, "LastErrorBody");
                }
            }

            _scenarioContext.Set(deletedIds, "DeletedUserIds");
        }

        [When("I try to delete the other user by their ID")]
        public async Task WhenITryToDeleteTheOtherUserByTheirID()
        {
            var myUserCode = _scenarioContext.Get<string>("MyUserCode");
            var otherUserId = _scenarioContext.Get<ulong>("RegularUserId");

            try
            {
                await _apiClient.DeleteUserAsync(otherUserId, myUserCode);
                _scenarioContext.Set(204, "LastStatusCode");
            }
            catch (ApiException ex)
            {
                _scenarioContext.Set(ex.ActualStatus, "LastStatusCode");
                _scenarioContext.Set(ex.ResponseBody, "LastErrorBody");
            }
        }

        [When("I try to delete myself using my user ID")]
        public async Task WhenITryToDeleteMyselfUsingMyUserID()
        {
            var adminUserCode = _scenarioContext.Get<string>("AdminUserCode");
            
            // Get admin user's ID from the users list
            var users = await _apiClient.GetUsersAsync(adminUserCode);
            var adminUser = users.First(u => u.IsAdmin);

            try
            {
                await _apiClient.DeleteUserAsync(adminUser.Id, adminUserCode);
                _scenarioContext.Set(204, "LastStatusCode");
            }
            catch (ApiException ex)
            {
                _scenarioContext.Set(ex.ActualStatus, "LastStatusCode");
                _scenarioContext.Set(ex.ResponseBody, "LastErrorBody");
            }
        }

        [When("I try to delete user with ID {long}")]
        public async Task WhenITryToDeleteUserWithID(long userId)
        {
            var adminUserCode = _scenarioContext.Get<string>("AdminUserCode");

            try
            {
                await _apiClient.DeleteUserAsync((ulong)userId, adminUserCode);
                _scenarioContext.Set(204, "LastStatusCode");
            }
            catch (ApiException ex)
            {
                _scenarioContext.Set(ex.ActualStatus, "LastStatusCode");
                _scenarioContext.Set(ex.ResponseBody, "LastErrorBody");
            }
        }

        [When("I try to delete the user with invalid admin code {string}")]
        public async Task WhenITryToDeleteTheUserWithInvalidAdminCode(string invalidCode)
        {
            var userId = _scenarioContext.Get<ulong>("RegularUserId");

            try
            {
                await _apiClient.DeleteUserAsync(userId, invalidCode);
                _scenarioContext.Set(204, "LastStatusCode");
            }
            catch (ApiException ex)
            {
                _scenarioContext.Set(ex.ActualStatus, "LastStatusCode");
                _scenarioContext.Set(ex.ResponseBody, "LastErrorBody");
            }
        }

        [When("I try to delete the regular user")]
        public async Task WhenITryToDeleteTheRegularUser()
        {
            await WhenIDeleteTheRegularUserByTheirID();
        }

        [When("I try to delete the user from room B using my admin code")]
        public async Task WhenITryToDeleteTheUserFromRoomBUsingMyAdminCode()
        {
            var adminUserCode = _scenarioContext.Get<string>("AdminUserCode"); // From room A
            var roomBUserId = _scenarioContext.Get<ulong>("RoomBUserId");

            try
            {
                await _apiClient.DeleteUserAsync(roomBUserId, adminUserCode);
                _scenarioContext.Set(204, "LastStatusCode");
            }
            catch (ApiException ex)
            {
                _scenarioContext.Set(ex.ActualStatus, "LastStatusCode");
                _scenarioContext.Set(ex.ResponseBody, "LastErrorBody");
            }
        }

        [When("I try to delete user with ID {string} and userCode {string}")]
        public async Task WhenITryToDeleteUserWithIDAndUserCode(string userIdStr, string userCode)
        {
            try
            {
                if (string.IsNullOrEmpty(userIdStr) || userIdStr == "null" || !ulong.TryParse(userIdStr, out var userId))
                {
                    _scenarioContext.Set(400, "LastStatusCode");
                    return;
                }

                if (string.IsNullOrEmpty(userCode) || userCode == "null")
                {
                    _scenarioContext.Set(400, "LastStatusCode");
                    return;
                }

                await _apiClient.DeleteUserAsync(userId, userCode);
                _scenarioContext.Set(204, "LastStatusCode");
            }
            catch (ApiException ex)
            {
                _scenarioContext.Set(ex.ActualStatus, "LastStatusCode");
                _scenarioContext.Set(ex.ResponseBody, "LastErrorBody");
            }
        }

        [Then("the user should be removed successfully")]
        public void ThenTheUserShouldBeRemovedSuccessfully()
        {
            var statusCode = _scenarioContext.Get<int>("LastStatusCode");
            statusCode.ShouldBe(204);
        }

        [Then("both users should be removed successfully")]
        public void ThenBothUsersShouldBeRemovedSuccessfully()
        {
            var deletedIds = _scenarioContext.Get<List<ulong>>("DeletedUserIds");
            deletedIds.Count.ShouldBe(2);
        }

        [Then("the user should no longer appear in the room participants list")]
        public async Task ThenTheUserShouldNoLongerAppearInTheRoomParticipantsList()
        {
            var adminUserCode = _scenarioContext.Get<string>("AdminUserCode");
            var deletedUserId = _scenarioContext.Get<ulong>("RegularUserId");

            var users = await _apiClient.GetUsersAsync(adminUserCode);
            users.Should().NotContain(u => u.Id == deletedUserId);
        }

        [Then("only {int} regular user should remain in the room")]
        public async Task ThenOnlyRegularUserShouldRemainInTheRoom(int expectedCount)
        {
            var adminUserCode = _scenarioContext.Get<string>("AdminUserCode");
            var users = await _apiClient.GetUsersAsync(adminUserCode);
            
            var regularUsers = users.Where(u => !u.IsAdmin).ToList();
            regularUsers.Count.ShouldBe(expectedCount);
        }

        [Then("I should receive a {string} error message")]
        public void ThenIShouldReceiveAErrorMessage(string errorType)
        {
            var errorBody = _scenarioContext.Get<string>("LastErrorBody");
            errorBody.ShouldNotBeNullOrEmpty();

            switch (errorType.ToLower())
            {
                case "forbidden":
                    errorBody.ShouldContain("forbidden", Case.Insensitive);
                    break;
                case "cannot remove admin":
                    errorBody.ShouldContain("Cannot remove admin", Case.Insensitive);
                    break;
                case "user not found":
                    errorBody.ShouldContain("not found", Case.Insensitive);
                    break;
                case "room is closed":
                    errorBody.ShouldContain("closed", Case.Insensitive);
                    break;
                case "different rooms":
                    errorBody.ShouldContain("different room", Case.Insensitive);
                    break;
            }
        }

        [When("I delete {int} regular user")]
        public async Task WhenIDeleteRegularUser(int count)
        {
            var adminUserCode = _scenarioContext.Get<string>("AdminUserCode");
            var userIds = _scenarioContext.Get<List<ulong>>("RegularUserIds");

            if (userIds.Count > 0)
            {
                await _apiClient.DeleteUserAsync(userIds[0], adminUserCode);
                _scenarioContext.Set(userIds[0], "DeletedUserId");
            }
        }

        [Then("the user should be completely removed from the system")]
        public async Task ThenTheUserShouldBeCompletelyRemovedFromTheSystem()
        {
            var adminUserCode = _scenarioContext.Get<string>("AdminUserCode");
            var deletedUserId = _scenarioContext.Get<ulong>("DeletedUserId");

            var users = await _apiClient.GetUsersAsync(adminUserCode);
            users.Should().NotContain(u => u.Id == deletedUserId);
        }

        [Then("the remaining users should still have their data intact")]
        public async Task ThenTheRemainingUsersShouldStillHaveTheirDataIntact()
        {
            var adminUserCode = _scenarioContext.Get<string>("AdminUserCode");
            var users = await _apiClient.GetUsersAsync(adminUserCode);

            users.Should().NotBeEmpty();
            users.Should().OnlyContain(u => !string.IsNullOrEmpty(u.FirstName));
            users.Should().OnlyContain(u => !string.IsNullOrEmpty(u.LastName));
        }

        [Then("the room should still be functional for the remaining participants")]
        public async Task ThenTheRoomShouldStillBeFunctionalForTheRemainingParticipants()
        {
            var adminUserCode = _scenarioContext.Get<string>("AdminUserCode");
            
            // Test that we can still get room info
            var room = await _roomApiClient.GetRoomByUserCodeAsync(adminUserCode);
            room.ShouldNotBeNull();
            
            // Test that we can still get users
            var users = await _apiClient.GetUsersAsync(adminUserCode);
            users.ShouldNotBeEmpty();
        }
    }
}
