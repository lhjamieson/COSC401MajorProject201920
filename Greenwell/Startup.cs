
using Greenwell.Data;
using Greenwell.Data.Models;
using Greenwell.Models;
using Greenwell.Services;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Greenwell
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));

            services.AddDbContext<greenwelldatabaseContext>(
             options =>
                 options.UseMySql(
                     Configuration.GetConnectionString("Greenwell"),
                     mySqlOptions =>
                     {
                         mySqlOptions.MigrationsAssembly("Greenwell/Data");
                         mySqlOptions.ServerVersion(new Version(8, 0, 17), ServerType.MySql); // replace with your Server Version and Type
                     }
                 )
             );

            services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddRoles<IdentityRole>()
                .AddRoleManager<RoleManager<IdentityRole>>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddIdentityServer()
                .AddApiAuthorization<ApplicationUser, ApplicationDbContext>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
                options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
                .AddIdentityServerJwt();

            //Here we add the profile service so our react profile includes a role.
            services.AddTransient<IProfileService, ProfileService>();
            
            //Configuration for email sending.
            services.AddTransient<IEmailSender, EmailSender>();
            services.Configure<AuthMessageSenderOptions>(Configuration);


            
            services.AddControllersWithViews();
            services.AddRazorPages();

            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseRouting();

            
            app.UseAuthentication();
            app.UseIdentityServer();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });
            CreateRolesAndAdminUser(serviceProvider);
        }

        //This methods creates all the roles and assigns a admin user so there is always a hardcoded admin user.
        private static void CreateRolesAndAdminUser(IServiceProvider serviceProvider)
        {
            const string adminRoleName = "Administrator";
            string[] roleNames = { adminRoleName, "Member" };

            foreach (string roleName in roleNames)
            {
                //We create the role for every for every one in the Array
                CreateRole(serviceProvider, roleName);
            }

            // TODO: Get these value from "appsettings.json" file. 
            string adminUserEmail = "admin@test.com";
            string adminPwd = "Password_123";
            AddUserToRole(serviceProvider, adminUserEmail, adminPwd, adminRoleName);
        }


        //This method checks to see if a role exists on startup, if it doesn't then it adds that role. 
        private static void CreateRole(IServiceProvider serviceProvider, string roleName)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            //We check for the existence of the intended role.
            Task<bool> roleExists = roleManager.RoleExistsAsync(roleName);
            roleExists.Wait();

            //If the role doesnt exist we add it
            if (!roleExists.Result)
            {
                Task<IdentityResult> roleResult = roleManager.CreateAsync(new IdentityRole(roleName));
                roleResult.Wait();
            }
        }

        
        //This function adds a user to a role, we will need to call it on creation of each account or to modify a role.
        //We call this on user creation as there is existing code that creates the user all ready and every user is automatically added to the "Member" role.
        //This is however helpful for adding a default admin user above and could be used in the future.
        private static void AddUserToRole(IServiceProvider serviceProvider, string userEmail,
            string userPwd, string roleName)
        {
            //TEMP
            Console.WriteLine(userEmail + userPwd + roleName);
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            
            //We search to see if the intended user already exists
            Task<ApplicationUser> checkAppUser = userManager.FindByEmailAsync(userEmail);
            checkAppUser.Wait();
            ApplicationUser appUser = checkAppUser.Result;

            //If they don't exist we go about creating a new user
            if (appUser == null)
            {
                //We create a user with their properties
                ApplicationUser newAppUser = new ApplicationUser
                {
                    Email = userEmail,
                    UserName = "Default Admin",
                    //Because we are only calling this on a demo user, we need just pretened the email has been confirmed, in the future,
                    EmailConfirmed = true
                };

                //We create the new user. 
                Task<IdentityResult> taskCreateAppUser = userManager.CreateAsync(newAppUser, userPwd);
                taskCreateAppUser.Wait();

                if (taskCreateAppUser.Result.Succeeded)
                {
                    appUser = newAppUser;
                }
            }

            //This crappy code fixes a problem I introduced earlier, it's only needed if people's admin accounts were created before the fix.
            //It will not be in the final code.
            if (appUser.Email == "admin@test.com") {
                appUser.EmailConfirmed = true;
                userManager.UpdateAsync(appUser).Wait();
                
            }

            //Finally we add the user to the role, regardless if they existed before or not.
            Task<IdentityResult> newUserRole = userManager.AddToRoleAsync(appUser, roleName);
            newUserRole.Wait();
        }
    }
}
