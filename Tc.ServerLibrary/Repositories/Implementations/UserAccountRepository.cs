﻿using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Tc.BaseLibrary.DTOs;
using Tc.BaseLibrary.Entities;
using Tc.BaseLibrary.Responses;
using Tc.ServerLibrary.Data;
using Tc.ServerLibrary.Helpers;
using Tc.ServerLibrary.Repositories.Contracts;

namespace Tc.ServerLibrary.Repositories.Implementations
{
    public class UserAccountRepository(IOptions<JwtSection> config, AppDbContext appDbContext) : IUserAccount

    {
        public async Task<GeneralResponse> CreateAsync(Register user)
        {
            if (user == null)
            {
                return new GeneralResponse(false, "Model is empty");
            }

            var checkUser = await FindUserByEmail(user.Email);
            if (checkUser != null)
            {
                return new GeneralResponse(false, "User already exist");
            }

            var applicationUser = await AddToDatabase(new ApplicationUser
            {
                Email = user.Email,
                Fullname = user.Fullname,
                Password = BCrypt.Net.BCrypt.HashPassword(user.Password)
            });

            var checkAdminRole = await appDbContext.SystemRoles.FirstOrDefaultAsync(_ => _.Name!.Equals(Constants.Admin));
            if (checkAdminRole is null)
            {
                var createAdminRole = await AddToDatabase(new SystemRole { Name = Constants.Admin });
                await AddToDatabase(new UserRole { RoleId = createAdminRole.Id , UserId = applicationUser.Id});
                return new GeneralResponse(true, "Account created!");
            }

            var checkUserRole = await appDbContext.SystemRoles.FirstOrDefaultAsync(_ => _.Name!.Equals(Constants.User));
            SystemRole response = new();
            if (checkUserRole is null)
            {
                response = await AddToDatabase(new SystemRole { Name = Constants.User });
                await AddToDatabase(new UserRole { RoleId = response.Id, UserId = applicationUser.Id });
            }
            else
            {
                await AddToDatabase(new UserRole { RoleId = checkUserRole.Id, UserId = applicationUser.Id });
            }
            return new GeneralResponse(true, "Account created!");
        }

        private async Task<T> AddToDatabase<T>(T model)
        {
            var result = appDbContext.Add(model!);
            await appDbContext.SaveChangesAsync();
            return (T)result.Entity;
        }


        public async Task<LoginResponse> SignInAsync(Login user)
        {
            if (user is null)
            {
                return new LoginResponse(false, "Model is empty");
            }

            var applicationUser = await FindUserByEmail(user.Email);
            if (applicationUser is null)
            {
                return new LoginResponse(false, "Email or Password not correct");
            }

            if (!BCrypt.Net.BCrypt.Verify(user.Password, applicationUser.Password))
            {
                return new LoginResponse(false, "Email or Password not correct");
            }

            var getUserRole = await FindUserRole(applicationUser.Id);
            if (getUserRole is null)
            {
                return new LoginResponse(false, "User role not found");
            }

            var getRoleName = await FindRoleName(getUserRole.RoleId);
            if(getRoleName is null)
            {
                return new LoginResponse(false, "User role not found");
            }

            string jwtToken = GenerateToken(applicationUser, getRoleName!.Name!);
            string refreshToken = GenerateRefreshToken();
            return new LoginResponse(true, "Login successfully", jwtToken, refreshToken);
        }

        private string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }

        private string GenerateToken(ApplicationUser user, string role)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.Value.Key!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var userClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Fullname!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Role, role!)
            };

            var token = new JwtSecurityToken(
                issuer: config.Value.Issuer,
                audience: config.Value.Audience,
                claims: userClaims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: credentials
                );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<UserRole> FindUserRole(int userId)
        {
            return await appDbContext.UserRoles.FirstOrDefaultAsync(_ => _.UserId == userId);
        }

        private async Task<SystemRole> FindRoleName(int roleId)
        {
            return await appDbContext.SystemRoles.FirstOrDefaultAsync(_ => _.Id == roleId);
        }

        private async Task<ApplicationUser> FindUserByEmail(string? email)
        {
            return await appDbContext.ApplicationUsers.FirstOrDefaultAsync(x => x.Email!.ToLower()!.Equals(email!.ToLower()!));
        }

        public async Task<LoginResponse> RefreshTokenAsync(RefreshToken token)
        {
            if (token is null)
            {
                return new LoginResponse(false, "Model is empty");
            }

            var findToken = await appDbContext.RefreshTokenInfos.FirstOrDefaultAsync(_ => _.Token!.Equals(token.Token));
            if (findToken is null)
            {
                return new LoginResponse(false, "Refresh token is requiredd");
            }

            var user = await appDbContext.ApplicationUsers.FirstOrDefaultAsync(_ => _.Id == findToken.UserId);
            if (user is null)
            {
                return new LoginResponse(false, "Refresh token could not be generated because user not found");
            }

            var userRole = await FindUserRole(user.Id);
            var roleName = await FindRoleName(userRole.RoleId);
            string jwtToken = GenerateToken(user, roleName.Name!);
            string refreshToken = GenerateRefreshToken();

            var updateRefreshToken = await appDbContext.RefreshTokenInfos.FirstOrDefaultAsync(_ => _.UserId == user.Id);
            if (updateRefreshToken is null)
            {
                return new LoginResponse(false, "Refresh token could not be generated because user has not signed in");
            }

            updateRefreshToken.Token = refreshToken;
            await appDbContext.SaveChangesAsync();
            return new LoginResponse(true, "Token refreshed successfully", jwtToken, refreshToken);
        }
    }
}
