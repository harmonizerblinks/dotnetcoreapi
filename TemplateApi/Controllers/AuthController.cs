﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TemplateApi.Extensions;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using TemplateApi.Models;
using Microsoft.Extensions.Configuration;
using TemplateApi.Services;
using TemplateApi.Repository;

namespace TemplateApi.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        private readonly IAppUserRepository _appuserRepository;

        public AuthController(UserManager<AppUser> usrMgr, SignInManager<AppUser> signinMgr, IConfiguration configuration,
             /*HttpContext context,*/ IEmailSender emailSender, IAppUserRepository appuserRepository)
        {
            _userManager = usrMgr;
            _signInManager = signinMgr;
            _configuration = configuration;
            _emailSender = emailSender;
            _appuserRepository = appuserRepository;
        }

        [HttpPost()]
        [AllowAnonymous]
        public async Task<IActionResult> Logins([FromBody] Login model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(model.Username);//.Include(b => b.User);
                if (user == null) return BadRequest("Invalid Username: user doesn't Exist");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, true);
            if (!result.Succeeded) return BadRequest($"Incorrect Password for {model.Username}");

            await _signInManager.SignInAsync(user, true, "Bearer");

            var userClaims = await _userManager.GetClaimsAsync(user);

            userClaims.Add(new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName));
            userClaims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("wertyuiopasdfghjklzxcvbnm123456"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            
            userClaims.Add(new Claim("Id", user.Id));
            userClaims.Add(new Claim("FullName", user.FullName));
            userClaims.Add(new Claim("Image", user.Image));
            userClaims.Add(new Claim("Mobile", user.PhoneNumber));
            userClaims.Add(new Claim("Email", user.Email));
            userClaims.Add(new Claim("UserType", user.UserType));
            userClaims.Add(new Claim("LoginTime", DateTime.Now.ToString()));

            var token = new JwtSecurityToken(
                issuer: _configuration.GetSection("AppSettings")["Url"],
                audience: "http://localhost:53720",
                claims: userClaims,
                expires: DateTime.Now.AddHours(12),
                signingCredentials: creds);

            user.Login = DateTime.UtcNow;
            user.IsLoggedIn = true;
            await _userManager.UpdateAsync(user);
            var auth = new JwtSecurityTokenHandler().WriteToken(token);
            await _userManager.SetAuthenticationTokenAsync(user, "Server", user.UserName, auth);
            var tok = await _userManager.CreateSecurityTokenAsync(user);
            
            //await _signInManager.CanSignInAsync(user);

            return Ok(new { Access_Token = auth, Expires_In_Hours = 12, DateTime.UtcNow.Date });
        }

        [HttpPost("User")]
        public async Task<IActionResult> AddUser([FromBody] AddUser user)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var appemail = _userManager.Users.Any(u => u.Email.Equals(user.Email) || u.UserName.Equals(user.Username));
            if (appemail) return BadRequest("Email Or Username already taken ");
            
            var appUser = new AppUser
            {
                Email = user.Email, PhoneNumber = user.Mobile, UserName = user.Username,
                MUserId = user.UserId, FullName = user.Fullname, Image = user.Image,
                Login = DateTime.Now, LogOut = DateTime.Now, IsLoggedIn = false, UserType = user.UserType,
                EmailConfirmed = true, MDate = DateTime.UtcNow
            };
            var result = await _userManager.CreateAsync(appUser, user.Password);

            if (!result.Succeeded) return BadRequest($"Message: {result} ");

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(appUser);
            var callbackUrl = Url.EmailConfirmationLink(appUser.Id, code, Request.Scheme);
            await _emailSender.SendEmailConfirmationAsync(user.Email, callbackUrl, appUser);

            return Ok(user);
        }
        
        [HttpGet("Users")]
        public async Task<IActionResult> GetUser()
        {
            var users = await _userManager.Users.Select(
                    u => new AddUser()
                    {
                        Id = u.Id, Email = u.Email, Mobile = u.PhoneNumber, Username = u.UserName,
                        Fullname = u.FullName, Login = u.Login, LogOut = u.LogOut,
                        Image = u.Image, UserType = u.UserType,
                        MUserId = u.MUserId, MDate = u.MDate, IsLoggedIn = u.IsLoggedIn
                    }).OrderByDescending(o => o.Login).ToListAsync();
            //RecurringJob.AddOrUpdate("Adding Services",() => Console.WriteLine("Transparent!"), Cron.Hourly);
            return Ok(users);
        }

        [HttpGet("identity")]
        public async Task<IActionResult> GetIndentity()
        {
            var users = await _userManager.Users.OrderByDescending(o => o.Login).ToListAsync();

            return Ok(users);
        }

        [HttpPut("User/{id}")]
        public async Task<IActionResult> UpdateUser([FromRoute] string id, [FromBody] AddUser user)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var appuser = _userManager.Users.FirstOrDefault(u => u.Id == id);
            if (appuser == null) return NotFound($"User Doesn't Exist with id {id} in Database ");
            
            appuser.Email = user.Email; appuser.PhoneNumber = user.Mobile;
            appuser.UserName = user.Username; appuser.Image = user.Image;
            appuser.UserType = user.UserType; appuser.FullName = user.Fullname;
            appuser.MUserId = user.MUserId; appuser.MDate = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(appuser);
            if (!result.Succeeded) return BadRequest($"Message: {result} ");

            if (user.Password != null)
            {
                await _userManager.RemovePasswordAsync(appuser);
                await _userManager.AddPasswordAsync(appuser, user.Password);
                string passwordhash = _userManager.PasswordHasher.HashPassword(appuser, user.Password);
                appuser.PasswordHash = passwordhash;
            }
            await _userManager.UpdateNormalizedEmailAsync(appuser);
            await _userManager.UpdateNormalizedUserNameAsync(appuser);
            await _userManager.UpdateSecurityStampAsync(appuser);
            await _userManager.UpdateAsync(appuser);
            // await _userManager.SaveChangesAsync(appuser);

            return Ok(user);
        }
        

        [HttpGet("Logout/{username}")]
        public async Task<IActionResult> Logout([FromRoute]string username)
        {
            if (!ModelState.IsValid) return BadRequest();
           
            var user = await _userManager.FindByNameAsync(username);
            user.Login = DateTime.UtcNow;
            user.IsLoggedIn = false;
            await _userManager.UpdateAsync(user);
            //await _userManager.RemoveClaimAsync(user);
            //_context.SaveChanges();

            await _signInManager.SignOutAsync();

            return Ok("Logout successfull");
        }

        [HttpPost("ChangePassword")]
        public async Task<IActionResult> PasswordChange([FromBody] ChangePassword model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var appuser = await _userManager.FindByNameAsync(model.Username);
            if (appuser == null) return NotFound("User does not exist");
            var resul = await _signInManager.CheckPasswordSignInAsync(appuser, model.OldPassword, true);
            if (!resul.Succeeded) return BadRequest("Current Password is incorrect");

            IdentityResult result = await _userManager.ChangePasswordAsync(appuser, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _userManager.UpdateAsync(appuser);
                return Ok("Password Changed Successfully");
            }
            else
            {
                return Content("Could not change Password");
            }
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteById([FromBody] string id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var appmail = _userManager.Users.Any(u => u.Id.Equals(id));
            if (appmail) return NotFound($"There is no valid Account with {id}");

            var appuser = await _userManager.FindByEmailAsync(id);
            appuser.EmailConfirmed = false;

            try
            {
                await _userManager.DeleteAsync(appuser);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsersExists(appuser.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return Ok(new { Status = "Ok", Message = "Account Deleted Successfully" });
        }
        
        [AllowAnonymous]
        [HttpPost("Reset")]
        public async Task<IActionResult> PassWordReset([FromBody] Reset reset)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var appmail = _userManager.Users.Any(u => u.Email.Equals(reset.Email));
            if (appmail) return NotFound($"There is no valid Account with {reset.Email}");

            var appuser = await _userManager.FindByEmailAsync(reset.Email);
            var Pin = DateTime.Now.ToString("ddhhmmss"); appuser.EmailConfirmed = true;

            try
            {
                await _userManager.RemovePasswordAsync(appuser);
                await _userManager.AddPasswordAsync(appuser, Pin);
                string passwordhash = _userManager.PasswordHasher.HashPassword(appuser, Pin);
                appuser.PasswordHash = passwordhash;
                await _userManager.UpdateNormalizedEmailAsync(appuser);
                await _userManager.UpdateNormalizedUserNameAsync(appuser);
                await _userManager.UpdateSecurityStampAsync(appuser);
                await _userManager.UpdateAsync(appuser);
                //await _context.SaveChangesAsync();

                var code = await _userManager.GenerateEmailConfirmationTokenAsync(appuser);
                var callbackUrl = Url.EmailConfirmationLink(appuser.Id, code, Request.Scheme);
                await _emailSender.SendEmailResetConfirmationAsync(appuser.Email, callbackUrl, appuser);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsersExists(appuser.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return Ok(new { Status = "Ok", Message = "Account Reset Successfully", Output = "Check Mail for new Password" });
        }
        
        [AllowAnonymous]
        [HttpGet("Reset/{email}")]
        public async Task<IActionResult> PassWordReset([FromRoute] string email)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var appmail = _userManager.Users.Any(u => u.Email.Equals(email));
            if (!appmail) return NotFound($"There is no valid Account with {email}");
            var appuser = await _userManager.FindByEmailAsync(email);
            var pin = DateTime.Now.ToString("ddhms"); appuser.EmailConfirmed = true;
            
            try
            {
                await _userManager.RemovePasswordAsync(appuser);
                await _userManager.AddPasswordAsync(appuser, pin);
                string passwordhash = _userManager.PasswordHasher.HashPassword(appuser, pin);
                appuser.PasswordHash = passwordhash;
                await _userManager.UpdateNormalizedEmailAsync(appuser);
                await _userManager.UpdateNormalizedUserNameAsync(appuser);
                await _userManager.UpdateSecurityStampAsync(appuser);
                await _userManager.UpdateAsync(appuser);
                //await _context.SaveChangesAsync();

                var code = await _userManager.GenerateEmailConfirmationTokenAsync(appuser);
                var callbackUrl = Url.EmailConfirmationLink(appuser.Id, code, Request.Scheme);
                await _emailSender.SendEmailResetConfirmationAsync(callbackUrl, pin, appuser);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsersExists(appuser.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return Ok(new { Status = "Ok", Message = "Account Reset Successfully", Output = "Check Mail for new Password" });
        }

        [HttpGet("Token")]
        [AllowAnonymous]
        public IActionResult AccessToken()
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("qwertyuiopasdfghjklzxcvbnm123456"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _configuration.GetSection("AppSettings")["Url"],
                audience: "http://localhost:53720",
                expires: DateTime.Now.AddMinutes(50),
                signingCredentials: creds);
            var response = new { Access_Token = new JwtSecurityTokenHandler().WriteToken(token), expires_in_Minutes = 50 };
            return Ok(response);
        }

        private bool UsersExists(string id)
        {
            return _userManager.Users.Any(e => e.Id == id);
        }

        public class Login
        {
            [Required]
            public string Username { get; set; }
            [Required]
            public string Password { get; set; }
        }

        public class Profile
        {
            [Required]
            public string Username { get; set; }
            [Required]
            public string Image { get; set; }
        }

        public class Reset
        {
            [Required]
            public string Email { get; set; }
        }

        public class ChangePassword
        {
            [Required]
            public string Username { get; set; }
            [Required]
            public string OldPassword { get; set; }
            [Required]
            public string NewPassword { get; set; }
        }

    }

}