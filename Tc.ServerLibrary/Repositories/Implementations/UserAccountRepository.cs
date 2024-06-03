using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private async Task<ApplicationUser> FindUserByEmail(string? email)
        {
            return await appDbContext.ApplicationUsers.FirstOrDefaultAsync(x => x.Email!.ToLower()!.Equals(email!.ToLower()!)); 
        }

        public Task<LoginResponse> SignInAsync(Login user)
        {
            throw new NotImplementedException();
        }
    }
}
