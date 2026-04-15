# Postman Starter Pack

This directory contains importable Postman files for the current API.

## Files

- `KnowledgeBase.Api.postman_collection.json`
- `KnowledgeBase.Local.postman_environment.json`

## Import Steps

1. Open Postman.
2. Import the collection JSON file.
3. Import the environment JSON file.
4. Select the `KnowledgeBase Local` environment.
5. Update `baseUrl`, user credentials, and bootstrap master-admin credentials as needed.

## Notes

- The collection uses the current API routes from the controllers.
- `Login User` captures `accessToken`.
- `Login Master Admin` captures `masterAdminAccessToken`.
- `Login Admin` captures `adminAccessToken`.
- `create note` captures `noteId`.
- `Create Admin` captures `adminUserId`.
- `refresh` and `logout` can use a request body token, but the API also supports reading the refresh token from the cookie if Postman stores it in the cookie jar.
- The governance endpoints require a `MasterAdmin` token.

## Bootstrap Master Admin

The app now supports a development bootstrap master admin through `BootstrapAdmin` settings in the API configuration.

Before using the governance requests:

1. Enable `BootstrapAdmin` in the API config you are running.
2. Set `masterAdminUsername` and `masterAdminPassword` in the environment to match.
3. Run `POST Login Master Admin`.

## Suggested First Run

1. `GET /health`
2. `POST Signup User`
3. `POST Login User`
4. `POST Create Note`
5. `GET Get All Notes`
6. `GET Get Note By Id`
7. `GET Search Notes`
8. `PUT Update Note`
9. `PATCH Patch Note`
10. `GET Sessions`
11. `GET Audit Trail`
12. `DELETE Delete Note`
13. `POST Logout`

## Suggested Admin Flow

1. Enable bootstrap master admin in API config.
2. `POST Login Master Admin`
3. `POST Create Admin`
4. `POST Login Admin`
5. `GET List Users`
6. `PATCH Set User Active Status`
7. `DELETE User Note`
8. `POST Demote Admin`

## Suggested Promotion Flow

1. `POST Signup User`
2. `POST Login Master Admin`
3. `POST Promote User To Admin`
4. `POST Login Admin` using the promoted account credentials

## Repeatability

`POST Signup User` and `POST Create Admin` will fail on repeated runs unless the usernames and emails are changed to unique values.
