using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Greenwell.Models;
using Greenwell.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Greenwell.Controllers
{
    [Authorize(Roles = "Administrator")]
    [Route("api/[controller]")]
    public class AdminOnlyController : Controller
    {
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IEmailSender emailSender;

        public AdminOnlyController(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            this.roleManager = roleManager;
            this.userManager = userManager;
            this.emailSender = emailSender;
        }

        [HttpPost("GetUsers")]
        public async Task<IActionResult> GetUsers([FromForm] string currentUser)
        {
            // get all admin users
            var admins = await userManager.GetUsersInRoleAsync("Administrator");
            // exclude current admin user
            var adminUsers = admins.Where(a => a.ToString() != currentUser).ToList();
            // get all non admin users
            var nonAdminUsers = await userManager.GetUsersInRoleAsync("Member");
            return Ok(new { adminUsers, nonAdminUsers });
        }

        [HttpPost("DeleteUser")]
        public async Task<IActionResult> DeleteUser([FromForm] string currentUser, [FromForm] string userToDelete)
        {
            var passedUser = await userManager.FindByNameAsync(userToDelete.ToString());

            // delete the user
            await userManager.DeleteAsync(passedUser);

            // post delete
            // get all admin users
            var admins = await userManager.GetUsersInRoleAsync("Administrator");
            // exclude current admin user
            var adminUsers = admins.Where(a => a.ToString() != currentUser).ToList().AsEnumerable();
            // get all non admin users
            var nonAdminUsers = await userManager.GetUsersInRoleAsync("Member");

            return Ok(new { adminUsers, nonAdminUsers });
        }

        [HttpPost("MakeUserAdmin")]
        public async Task<IActionResult> MakeUserAdmin([FromForm] string currentUser, [FromForm] string userToMakeAdmin)
        {
            var passedUser = await userManager.FindByNameAsync(userToMakeAdmin.ToString());

            // make non-admin user admin
            await userManager.RemoveFromRoleAsync(passedUser, "Member");
            await userManager.AddToRoleAsync(passedUser, "Administrator");

            // post making user admin
            // get all admin users
            var admins = await userManager.GetUsersInRoleAsync("Administrator");
            // exclude current admin user
            var adminUsers = admins.Where(a => a.ToString() != currentUser).ToList().AsEnumerable();
            // get all non admin users
            var nonAdminUsers = await userManager.GetUsersInRoleAsync("Member");

            return Ok(new { adminUsers, nonAdminUsers });
        }

        [HttpPost("MakeUserNonAdmin")]
        public async Task<IActionResult> MakeUserNonAdmin([FromForm] string currentUser, [FromForm] string userToMakeNonAdmin)
        {
            var passedUser = await userManager.FindByNameAsync(userToMakeNonAdmin.ToString());

            // make non-admin user admin
            await userManager.RemoveFromRoleAsync(passedUser, "Administrator");
            await userManager.AddToRoleAsync(passedUser, "Member");

            // post making user admin
            // get all admin users
            var admins = await userManager.GetUsersInRoleAsync("Administrator");
            // exclude current admin user
            var adminUsers = admins.Where(a => a.ToString() != currentUser).ToList().AsEnumerable();
            // get all non admin users
            var nonAdminUsers = await userManager.GetUsersInRoleAsync("Member");

            return Ok(new { adminUsers, nonAdminUsers });
        }

        [HttpPost("AddUser")]
        public async Task<IActionResult> AddUser([FromForm] string currentUser, [FromForm] string userEmail, [FromForm] string userName)
        {
            Debug.WriteLine("Add User Request From " + userEmail);

            //We check if the intended user already exists.
            Task<ApplicationUser> checkAppUser = userManager.FindByEmailAsync(userEmail);
            checkAppUser.Wait();
            ApplicationUser appUser = checkAppUser.Result;


            //If they already exist we do nothing.
            if (appUser == null)
            {
                //We create a user with their properties
                ApplicationUser newAppUser = new ApplicationUser
                {
                    Email = userEmail,
                    UserName = userName,
                    //We confirm their email so that they can reset their password even if the haven't expressly confirmed their email.
                    EmailConfirmed = true
                };

                //We generate a default password.
                string userpass = GeneratePassword();

                //We then generate the user so that we can send them an email to setup their account.
                Task<IdentityResult> taskCreateAppUser = userManager.CreateAsync(newAppUser, userpass);


                //We check if the user was successfully created.
                if (taskCreateAppUser.Result.Succeeded)
                {
                    appUser = newAppUser;
                }
                else { 
                 //Return user cannot be created error.
                }


                //We add the user to the role, regardless if they existed before or not.
                Task<IdentityResult> newUserRole = userManager.AddToRoleAsync(appUser, "Member");
                newUserRole.Wait();

                //Finally we send the user a email to setup their account which is just a modified password reset.
                var code = await userManager.GeneratePasswordResetTokenAsync(appUser);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/SetupAccount",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                await emailSender.SendEmailAsync(
                    userEmail,
                    "Setup Your Greenwell Account",
                    $"You have been invited to create a Greenwell State Park Account. Please finish setting up account by creating a password <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>here</a>. You must finish setting up your account within 14 days.");
                Debug.WriteLine(callbackUrl);     
            }

            //Return a updated list of users.
            // get all admin users
            var admins = await userManager.GetUsersInRoleAsync("Administrator");
            // exclude current admin user
            var adminUsers = admins.Where(a => a.ToString() != currentUser).ToList().AsEnumerable();
            // get all non admin users
            var nonAdminUsers = await userManager.GetUsersInRoleAsync("Member");

            return Ok(new { adminUsers, nonAdminUsers });
        }

        //Function that generates the temporary password for the created user. It derives all requirements from the userManager.
        private string GeneratePassword()
        {
            //Retrieves user manager password requirements
            var options = userManager.Options.Password;
            int length = options.RequiredLength;
            bool nonAlphanumeric = options.RequireNonAlphanumeric;
            bool digit = options.RequireDigit;
            bool lowercase = options.RequireLowercase;
            bool uppercase = options.RequireUppercase;

            StringBuilder password = new StringBuilder();
            Random random = new Random();

            //Loop that first adds a random assortment of characters
            while (password.Length < length)
            {
                char c = (char)random.Next(32, 126);

                password.Append(c);

                if (char.IsDigit(c))
                    digit = false;
                else if (char.IsLower(c))
                    lowercase = false;
                else if (char.IsUpper(c))
                    uppercase = false;
                else if (!char.IsLetterOrDigit(c))
                    nonAlphanumeric = false;
            }

            //If we missed a requirement for a password we tack it on the end
            if (nonAlphanumeric)
                password.Append((char)random.Next(33, 48));
            if (digit)
                password.Append((char)random.Next(48, 58));
            if (lowercase)
                password.Append((char)random.Next(97, 123));
            if (uppercase)
                password.Append((char)random.Next(65, 91));

            return password.ToString();
        }


    }
}
