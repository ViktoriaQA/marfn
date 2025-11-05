@api
Feature: User Management API
  As a participant
  I want to manage my participation in gift exchanges
  So that I can join rooms and specify my preferences

Background:
  Given a room exists with invitation code

Rule: User Creation

  @positive
  Scenario: Join a room with wish list
    Given I have user data with wish list:
      | FirstName | John                 |
      | LastName  | Doe                  |
      | Phone     | +380123456789        |
      | Email     | john.doe@example.com |
    And I have wishes:
      | Name           | InfoLink                          |
      | PlayStation 5  | https://store.sony.com/ps5        |
      | LEGO Set      | https://lego.com/starwars         |
      | Book Collection|                                   |
    When I join the room
    Then I should be added successfully
    And I should receive my user code
    And my wishes should be saved

  @positive
  Scenario: Join a room with surprise preference
    Given I have user data with surprise preference:
      | FirstName   | Jane                           |
      | LastName    | Smith                          |
      | Phone       | +380987654321                  |
      | Email       | jane.smith@example.com         |
      | Interests   | I love books, tech, and sports |
    When I join the room
    Then I should be added successfully
    And my wish list should be empty
    But my interests should be saved

  @negative @validation
  Scenario Outline: Join room with invalid data
    Given I have user data:
      | Field       | Value        |
      | FirstName   | <FirstName>  |
      | LastName    | <LastName>   |
      | Phone       | <Phone>      |
      | Email       | <Email>      |
    When I try to join the room
    Then the request should fail with status 400
    And the error should mention "<Error>"

    Examples:
      | FirstName | LastName | Phone         | Email         | Error                          |
      |           | Doe      | +380123456789 | test@test.com | This field is required         |
      | John      |          | +380123456789 | test@test.com | This field is required         |
      | John      | Doe      |               | test@test.com | This field is required         |
      | John      | Doe      | invalid       | test@test.com | Phone number must be a valid   |
      | John      | Doe      | +380123456789 | invalid       | Email must be valid            |

Rule: User Retrieval

  @positive
  Scenario: Get all users in my room as regular user
    Given I am in a room with multiple users
    When I get the list of users using my code
    Then I should receive all users in the room
    And each user should have basic information including name and ID
    And each user should have gift preference indicator
    But users should not have sensitive data like phone or email or address

  @positive
  Scenario: Get user details as regular user
    Given I am a regular user
    And there are other users in the room
    When I try to get another user's details
    Then the request should return status 200
    But I should not see their sensitive information

  @positive
  Scenario: Get all users in my room as admin
    Given I am a room admin
    When I get the list of users using my code
    Then I should receive all users in the room
    And admin should see all user data including sensitive information

  @positive @authorization
  Scenario: Get specific user details as admin
    Given I am a room admin
    When I get user details by ID
    Then I should see complete user information
    And I should see their gift preferences

Rule: User Deletion

  @positive @admin
  Scenario: Admin successfully removes regular user from room
    Given I am a room admin
    And there is a regular user in my room
    When I delete the regular user by their ID
    Then the user should be removed successfully
    And the user should no longer appear in the room participants list

  @positive @admin
  Scenario: Admin removes multiple users from room
    Given I am a room admin
    And there are 3 regular users in my room
    When I delete 2 of the regular users
    Then both users should be removed successfully
    And only 1 regular user should remain in the room

  @negative @authorization
  Scenario: Regular user cannot delete other users
    Given I am a regular user in a room
    And there is another regular user in my room
    When I try to delete the other user by their ID
    Then the request should fail with status 403
    And I should receive a "forbidden" error message

  @negative @authorization
  Scenario: Admin cannot delete themselves
    Given I am a room admin
    When I try to delete myself using my user ID
    Then the request should fail with status 403
    And I should receive a "cannot remove admin" error message

  @negative @validation
  Scenario: Cannot delete user with invalid ID
    Given I am a room admin
    When I try to delete user with ID 99999
    Then the request should fail with status 404
    And I should receive a "user not found" error message

  @negative @authorization
  Scenario: Cannot delete user with invalid admin code
    Given there is a regular user in a room
    When I try to delete the user with invalid admin code "invalid123"
    Then the request should fail with status 404
    And I should receive a "user not found" error message

  @negative @business-rule
  Scenario: Cannot delete users from closed room
    Given I am a room admin
    And there is a regular user in my room
    And the room has been drawn and closed
    When I try to delete the regular user
    Then the request should fail with status 400
    And I should receive a "room is closed" error message

  @negative @authorization
  Scenario: Admin from different room cannot delete users
    Given I am a room admin in room A
    And there is a regular user in room B
    When I try to delete the user from room B using my admin code
    Then the request should fail with status 403
    And I should receive a "different rooms" error message

  @negative @validation
  Scenario Outline: Delete user with invalid parameters
    Given I am a room admin
    When I try to delete user with ID "<UserID>" and userCode "<UserCode>"
    Then the request should fail with status <StatusCode>
    
    Examples:
      | UserID | UserCode | StatusCode |
      | 0      | admin123 | 400        |
      | -1     | admin123 | 400        |
      | 1      |          | 400        |
      | 1      | null     | 400        |

  @integration
  Scenario: Complete user deletion workflow
    Given I create a room as admin
    And I add 3 regular users to the room
    And all users have submitted their wish lists
    When I delete 1 regular user
    Then the user should be completely removed from the system
    And the remaining users should still have their data intact
    And the room should still be functional for the remaining participants
