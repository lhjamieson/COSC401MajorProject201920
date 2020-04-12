using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Greenwell.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Greenwell.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class AdminOnlyController : Controller
    {
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly UserManager<ApplicationUser> userManager;

        public AdminOnlyController(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            this.roleManager = roleManager;
            this.userManager = userManager;
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
    }
}
