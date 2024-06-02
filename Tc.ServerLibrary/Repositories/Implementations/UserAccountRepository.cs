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
    internal class UserAccountRepository(IOptions<JwtSection> config, AppDbContext appDbContext) : IUserAccount

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
