using DuyProject.API.Configurations;
using DuyProject.API.Services;
using DuyProject.API.ViewModels;
using DuyProject.API.ViewModels.User;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DuyProject.API.Endpoints;

public static class UserEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("api/authenticate/login", async (UserService userService, LoginCommand request) =>
            {
                ServiceResult<LoginViewModel> result = await userService.Login(request);
                return result.Success ? Results.Ok(result) : Results.NoContent();
            })
            .AllowAnonymous()
            .WithName("POST_Login").WithGroupName("User");

        app.MapPost("api/authenticate/googleLogin", async (UserService userService, GoogleLoginCommand googleLoginCommand) =>
            {
                ServiceResult<LoginViewModel> result = await userService.LoginWithGoogle(googleLoginCommand);
                return result.Success ? Results.Ok(result) : Results.NoContent();
            })
            .AllowAnonymous()
            .WithName("POST_GoogleLogin").WithGroupName("User");

        app.MapPost("api/authenticate/facebook-login", async (UserService userService, FacebookLoginCommand facebookLoginCommand) =>
            {
                ServiceResult<LoginViewModel> result = await userService.LoginWithFacebook(facebookLoginCommand);
                return result.Success ? Results.Ok(result) : Results.NoContent();
            })
            .AllowAnonymous()
            .WithName("POST_FacebookLogin").WithGroupName("User");

        app.MapPut("api/authenticate/{id}/update-User", async (UserService userService, [FromRoute] string id,
                [FromBody] UserUpdateCommand userViewModel) =>
            {
                ServiceResult<UserViewModel> result = await userService.Update(id, userViewModel);
                return Results.Ok(result);
            })
            .RequireAuthorization(new AuthorizeAttribute
            { AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme })
            .WithName("PUT_User").WithGroupName("User");

        app.MapPut("api/authenticate/update-UserChat/{sender}/{recipient}", async (UserService userService, [FromRoute] string sender,
                [FromRoute] string recipient) =>
            {
                ServiceResult<UserViewModel> result = await userService.UpdateConnectedChatUser(sender, recipient);
                return Results.Ok(result);
            })
            .RequireAuthorization(new AuthorizeAttribute
            { AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme })
            .WithName("PUT_UserChat").WithGroupName("User");

        app.MapPost("api/authenticate/refresh-token", async (UserService userService, RefreshTokenCommand command) =>
            {
                ServiceResult<LoginViewModel> result = await userService.RefreshToken(command);
                return Results.Ok(result);
            })
            .AllowAnonymous()
            .WithName("POST_RefreshToken").WithGroupName("User");

        app.MapPost("api/authenticate/register", async (UserService userService, UserCreateCommand command) =>
            {
                ServiceResult<UserViewModel> result = await userService.Create(command);
                return Results.Ok(result);
            })
            .AllowAnonymous()
            .WithName("POST_Register").WithGroupName("User");

        app.MapPost("api/admin/create-user", async (UserService userService, UserCreateCommand command) =>
            {
                ServiceResult<UserViewModel> result = await userService.Create(command);
                return Results.Ok(result);
            })
            .RequireAuthorization(new AuthorizeAttribute
            { AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppSettings.AdminRole })
            .WithName("POST_CreateUser").WithGroupName("User");

        app.MapPut("api/user/forgot-password", async (UserService userService, ForgotPassword forgotPassword) =>
            {
                var email = new List<string> { forgotPassword.Email };
                ServiceResult<MailModel> result = await userService.ResetPassword(email);
                return Results.Ok(result);
            })
           .AllowAnonymous()
           .WithName("PUT_ResetPassword").WithGroupName("User");

        app.MapPut("api/admin/{id}/toggle", async (UserService userService, string id) =>
            {
                ServiceResult<UserViewModel> result = await userService.ToggleActive(id);
                return Results.Ok(result);
            })
            .RequireAuthorization(new AuthorizeAttribute
            { AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppSettings.AdminRole })
            .WithName("PUT_DisableUser").WithGroupName("User");

        app.MapPut("api/authenticate/{id}/change-password", async (UserService userService, [FromRoute] string id, [FromBody] ChangePasswordCommand passWordForm) =>
            {
                ServiceResult<UserViewModel> result = await userService.ChangePassword(id, passWordForm);
                return Results.Ok(result);
            })
            .RequireAuthorization(new AuthorizeAttribute
            { AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme })
            .WithName("PUT_ChangePassword").WithGroupName("User");

        app.MapGet("api/users", async (UserService userService, string? filterValue, int? pageNumber, int? pageSize, string? Role, string? tabManage) =>
            {
                ServiceResult<PaginationResponse<UserViewModel>> result = await userService.List(pageNumber ?? 1, pageSize ?? AppSettings.DefaultPageSize,
                    filterValue, Role, tabManage);
                return Results.Ok(result);
            })
            .AllowAnonymous()
            .WithName("GET_Users").WithGroupName("User");

        app.MapGet("api/user/getUserDetail/{id}", async (UserService userService, [FromRoute] string id) =>
            {
                ServiceResult<UserViewModel> result = await userService.GetById(id);
                return Results.Ok(result);
            })
            .AllowAnonymous()
            .WithName("GET_UserDetail").WithGroupName("User");
    }
}