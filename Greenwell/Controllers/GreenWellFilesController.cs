using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Greenwell.Data;
using Greenwell.Data.Models;
using Greenwell.Models;
using Greenwell.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

//File related functionality
namespace Greenwell.Controllers
{
    [Route("api/[controller]")]
    //Forces all api calls to be authorized
    [Authorize]
    public class GreenWellFilesController : Controller
    {
        private readonly greenwelldatabaseContext _context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IEmailSender emailSender;

        public GreenWellFilesController(greenwelldatabaseContext context, UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _context = context;
            this.userManager = userManager;
            this.emailSender = emailSender;
        }

        //Return all files from the Database and add to a list excluding admin only.
        [HttpPost("GetAllFiles")]
        public ActionResult GetAllFiles()
        {
            // return all files without admin only access
            return Ok(new { files = _context.Files.Where(p => p.Approved == true).Where(p => p.AdminOnly != true).Select(p => p.FullPath).ToList(), tags = _context.Tags.Select(t => t.TagName).ToList() });

        }

        //Return all files from the Database and add to a list including admin only.
        [Authorize(Roles = "Administrator")]
        [HttpPost("AdminGetAllFiles")]
        public ActionResult AGetAllFiles()
        {
            // return files with admin only access
            return Ok(new { files = _context.Files.Where(p => p.Approved == true).Select(p => p.FullPath).ToList(), tags = _context.Tags.Select(t => t.TagName).ToList() });
        }

        //Create directory for file storage on host device
        [HttpGet("[action]")]
        public ActionResult CreateLocalStorage()
        {
            string localStorage = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GreenWellLocalStorage\";
            if (Directory.Exists(localStorage))
            {
                return Ok(new { status = "Local Storage already exists." });
            }
            System.IO.Directory.CreateDirectory(localStorage);
            return Ok(new { files = _context.Files.Select(p => p.FullPath).ToList() });
        }

        //Search for file
        [HttpPost("Search")]
        public ActionResult Search([FromBody] string[] data)
        {
            //Search by filename
            if (data[1].Trim() == "fileName")
            {
                //if empty search bar, display all files
                if (data[0].Trim() == "")
                {
                    // return all files excluding admin only
                    return Ok(new { status = "empty search bar", files = _context.Files.Where(p => p.Approved == true).Where(p => p.AdminOnly != true).Select(p => p.FullPath).ToList() });
                }
                //List of all files that contain the search query
                var fs = _context.Files.Where(p => p.Approved == true).Where(a => a.Filename.ToLower().Contains(data[0].Trim().ToLower()) & a.AdminOnly != true).Select(p => p.Filename).ToList();
                // in case there is no files found
                if (fs.Count() == 0)
                {
                    return Ok(new { status = "no files found", files = _context.Files.Where(p => p.Approved == true).Where(p => p.AdminOnly != true).Select(p => p.FullPath).ToList() });
                }
                //If there are files found we return them
                return Ok(new { status = "200", files = fs });
            }
            //In case of empty search bar
            else if (data[0].Trim() == "")
            {
                // return files excluding admin only
                return Ok(new { status = "empty search bar", files = _context.Files.Where(p => p.Approved == true).Where(p => p.AdminOnly != true).Select(p => p.FullPath).ToList() });
            }

            //Search by tag
            else
            {
                //list of files that have the queried tag
                var res1 = _context.Tags.Include(a => a.Tagmap).Where(a => a.TagName.ToLower() == data[0].Trim().ToLower()).Select(a => a.TagId).ToList();
                var res2 = _context.Tagmap.Include(a => a.File).Where(a => res1.Contains(a.TagId)).ToList();
                // get all files excluding admin only
                var files = res2.Select(a => a.File).Where(p => p.Approved == true).Where(a => a.AdminOnly != true).Select(a => a.Filename).ToList();
                //If none are found we return "no files found"
                if (files.Count() == 0)
                {
                    return Ok(new { status = "no files found", files = _context.Files.Where(p => p.Approved == true).Where(p => p.AdminOnly != true).Select(p => p.FullPath).ToList() });
                }
                //Otherwise we just return all the found files.
                return Ok(new { status = "200", files });
            }
        }


        [HttpPost("AdminSearch")]
        [Authorize(Roles = "Administrator")]
        public ActionResult AdminSearch([FromBody] string[] data)
        {
            //Search by filename
            if (data[1].Trim() == "fileName")
            {
                //if empty search bar, display all files
                if (data[0].Trim() == "")
                {
                    // return files including admin only 
                    return Ok(new { status = "empty search bar", files = _context.Files.Where(p => p.Approved == true).Select(p => p.FullPath).ToList() });
                }
                //list of all files that contain with the search query
                var fs = _context.Files.Where(p => p.Approved == true).Where(a => a.Filename.ToLower().Contains(data[0].Trim().ToLower())).Select(p => p.Filename).ToList();
                // check if user is not admin, and if so return only files without admin only

                //in case no files have been found we return all the files
                if (fs.Count() == 0)
                {
                    return Ok(new { status = "no files matched", files = _context.Files.Where(p => p.Approved == true).Select(p => p.FullPath).ToList() });
                }
                //Return the found files
                return Ok(new { status = "200", files = fs });
            }
            // in case of empty search bar
            if (data[0].Trim() == "")
            {
                // return all files including admin only
                return Ok(new { status = "empty search bar", files = _context.Files.Where(p => p.Approved == true).Select(p => p.FullPath).ToList() });
            }
            //Search by tag
            else
            {
                //list of files that have the queried tag
                var res1 = _context.Tags.Include(a => a.Tagmap).Where(a => a.TagName.ToLower() == data[0].Trim().ToLower()).Select(a => a.TagId).ToList();
                var res2 = _context.Tagmap.Include(a => a.File).Where(a => res1.Contains(a.TagId)).ToList();
                // get all files including admin only
                var files = res2.Select(a => a.File).Where(a => a.Approved == true).Select(a => a.Filename).ToList();
                //if no files are found we return all files
                if (files.Count() == 0)
                {
                    return Ok(new { status = "no files matched", files = _context.Files.Where(p => p.Approved == true).Select(p => p.FullPath).ToList() });
                }
                //otherwise we return all the files
                return Ok(new { status = "200", files });
            }
        }

        //Functionality to add a file from the User for default users.
        [HttpPost("AddFileFromUpload")]
        public async Task<ActionResult> AddFileFromUpload([FromForm] string path, [FromForm] string author,[FromForm] IFormFile f, string[] tags)
        {

            if (_context.Files.Where(g => (g.FullPath.Equals(path))).Any())
            {
                return Ok(new { message = "Unable to upload duplicate file.", status = "201" });
            }

            try
            {
                //If there are tags associated with the file, split at commas to create a list.
                List<int> ids = new List<int>();
                if (!string.IsNullOrEmpty(tags[0]))
                {
                    if (tags[0].Contains(",")) tags = tags[0].Split(",");
                    Greenwell.Data.Models.Tags ts;

                    //Add the tags to the ids list for use later.
                    for (int i = 0; i < tags.Length; i++)
                    {
                        //We check to see if the tag already exists, if its new we add it
                        var check = _context.Tags.FirstOrDefault(a => a.TagName.Equals(tags[i]));
                        if (check == null)
                        {

                            ts = new Greenwell.Data.Models.Tags
                            {
                                TagName = tags[i]
                            };
                            await _context.Tags.AddAsync(ts);
                            await _context.SaveChangesAsync();
                            ids.Add(ts.TagId);
                        }
                        //If the tag already exists, we find add that found TagId.
                        else
                        {
                            ids.Add(check.TagId);
                        }
                    }
                }
                //After collecting the tags we make a new file with the path and filename of their file and mark it as not yet approved.
                Greenwell.Data.Models.Files file = new Greenwell.Data.Models.Files
                {
                    FullPath = path,
                    Filename = System.IO.Path.GetFileName(path),
                    Approved = false,
                    Author = author
                };


                await _context.Files.AddAsync(file);
                await _context.SaveChangesAsync();

                int fileId = file.FileId;

                //Add an association between the file and the tags asynchronously.
                //Allows us to add multiple tags concurrently.
                if (ids.Count() >= 1)
                {
                    Greenwell.Data.Models.Tagmap tmps;
                    for (int i = 0; i < ids.Count; i++)
                    {
                        tmps = new Greenwell.Data.Models.Tagmap
                        {
                            TagId = ids[i],
                            FileId = fileId
                        };
                        await _context.Tagmap.AddAsync(tmps);
                        await _context.SaveChangesAsync();
                    }
                }

                //We get the path of local storage, change the path directories from / to \ if needed (epic windows style)
                //We then use this path to save to, creating a relationship between the file and the local storage.
                string localStorage = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GreenWellLocalStorage";
                path = path.Replace("/", @"\");
                string finalPath = @localStorage + @"\" + @path;

                using (System.IO.FileStream stream = System.IO.File.Create(finalPath))
                {
                    f.CopyTo(stream);
                    stream.Dispose();
                }

            }

            //If we receive error code 500, then something with the server went wrong. It's not specific, but
            //we can catch it and ask them to retry uploading the file.
            catch (Exception e)
            {
                return StatusCode(500, new { error = e.Message, status = "500" });
            }

            //Finally we send an email to all admins about the new file, this method of handling new files is just a proof of concept.
            foreach (var email in userManager.GetUsersInRoleAsync("Administrator").Result) {
               await emailSender.SendEmailAsync(
                    email.Email,
                    "File Requiring Approval",
                    $"A file, {path} has been uploaded by {author} with the tags: {tags.ToString()} and requires approval.");
            }
            
            return Ok(new { message = "File was added successfully.", status = "200" });
        }


        //Functionality to add a file from the User.
        [HttpPost("AdminAddFileFromUpload")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> AdminAddFileFromUpload([FromForm] string path, [FromForm] IFormFile f, [FromForm] string author, string[] tags, bool adminAccessOnly)
        {
            if (_context.Files.Where(g => (g.FullPath.Equals(path))).Any())
            {
                return Ok(new { message = "Unable to upload duplicate file.", status = "201" });
            }


            try
            {
                //If there are tags associated with the file, split at commas to create a list.
                List<int> ids = new List<int>();
                if (!string.IsNullOrEmpty(tags[0]))
                {
                    if (tags[0].Contains(",")) tags = tags[0].Split(",");
                    Greenwell.Data.Models.Tags ts;

                    //Add the tags to the ids list for use later.
                    for (int i = 0; i < tags.Length; i++)
                    {
                        //We check to see if the tag already exists, if its new we add it
                        var check = _context.Tags.FirstOrDefault(a => a.TagName.Equals(tags[i]));
                        if (check == null)
                        {

                            ts = new Greenwell.Data.Models.Tags
                            {
                                TagName = tags[i]
                            };
                            await _context.Tags.AddAsync(ts);
                            await _context.SaveChangesAsync();
                            ids.Add(ts.TagId);
                        }
                        //If the tag already exists, we find add that found TagId.
                        else
                        {
                            ids.Add(check.TagId);
                        }
                    }
                }
                //After collecting the tags we make a new file with the path and filename of their file,  we mark it as the corresponding admin only call and 
                Greenwell.Data.Models.Files file = new Greenwell.Data.Models.Files
                {
                    FullPath = path,
                    Filename = System.IO.Path.GetFileName(path),
                    AdminOnly = adminAccessOnly,
                    Author = author,
                    Approved = true
                };


                await _context.Files.AddAsync(file);
                await _context.SaveChangesAsync();

                int fileId = file.FileId;

                //Add an association between the file and the tags asynchronously.
                //Allows us to add multiple tags concurrently.
                if (ids.Count() >= 1)
                {
                    Greenwell.Data.Models.Tagmap tmps;
                    for (int i = 0; i < ids.Count; i++)
                    {
                        tmps = new Greenwell.Data.Models.Tagmap
                        {
                            TagId = ids[i],
                            FileId = fileId
                        };
                        await _context.Tagmap.AddAsync(tmps);
                        await _context.SaveChangesAsync();
                    }
                }

                //We get the path of local storage, change the path directories from / to \ if needed (epic windows style)
                //We then use this path to save to, creating a relationship between the file and the local storage.
                string localStorage = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GreenWellLocalStorage";
                path = path.Replace("/", @"\");
                string finalPath = @localStorage + @"\" + @path;

                using (System.IO.FileStream stream = System.IO.File.Create(finalPath))
                {
                    f.CopyTo(stream);
                    stream.Dispose();
                }

            }

            //If we receive error code 500, then something with the server went wrong. It's not specific, but
            //we can catch it and ask them to retry uploading the file.
            catch (Exception e)
            {
                return StatusCode(500, new { error = e.Message, status = "500" });
            }
            return Ok(new { message = "File was added successfully.", status = "200" });
        }

        //[HttpPost("AddAFile")]
        //public async Task<ActionResult> AddAFile([FromForm] string[] p, [FromForm] List<IFormFile> f)
        //{
        //    if (p.Length > 1)
        //    {
        //        //We get the path of local storage, change the path directories from / to \ if needed (epic windows style)
        //        //Here we check if a file already exists in the path. This is triggered only by having the same path as 
        //        //another file. Much like other file systems, if the name isn't EXACTLY the same, it'll allow you to save it.
        //        for (int i = 0; i < p.Length; i++)
        //        {
        //            string path = p[i];
        //            string localStorage = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GreenWellLocalStorage\";
        //            string finalPath = localStorage + @"\" + path;
        //            if (System.IO.File.Exists(finalPath)) return StatusCode(500, new { message = "The file exists already.", status = "500" });

        //            //Here we try to make a connection with the Database to begin to save there. If we don't get a connection, we get
        //            //a 500 error.
        //            try
        //            {
        //                Greenwell.Data.Models.Files file = new Greenwell.Data.Models.Files
        //                {
        //                    FullPath = p[i],
        //                    Filename = System.IO.Path.GetFileName(p[i])
        //                };
        //                await _context.Files.AddAsync(file);
        //                await _context.SaveChangesAsync();


        //                using (System.IO.FileStream stream = System.IO.File.Create(finalPath))
        //                {
        //                    f[i].CopyTo(stream);
        //                    stream.Dispose();
        //                }
        //            }
        //            catch (Exception e)
        //            {
        //                return StatusCode(500, new { error = e.Message, status = "500" });
        //            }
        //        }
        //        return Ok(new { message = "Files were added successfully.", success = true, status = "200" });
        //    }
        //    else
        //    {
        //        string path = p[0];
        //        string localStorage = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GreenWellLocalStorage\";
        //        string finalPath = localStorage + @"\" + path;
        //        if (System.IO.File.Exists(finalPath)) return StatusCode(500, new { message = "The file exists already.", status = "500" });

        //        try
        //        {
        //            Greenwell.Data.Models.Files file = new Greenwell.Data.Models.Files
        //            {
        //                FullPath = p[0],
        //                Filename = System.IO.Path.GetFileName(p[0])
        //        };
        //            await _context.Files.AddAsync(file);
        //            await _context.SaveChangesAsync();

        //            using (System.IO.FileStream stream = System.IO.File.Create(finalPath))
        //            {
        //                f[0].CopyTo(stream);
        //                stream.Dispose();
        //            }

        //            return Ok(new { message = "File was added successfully.", success = true, status = "200" });
        //        }
        //        catch (Exception e)
        //        {
        //            return StatusCode(500, new { error = e.Message, status = "500" });
        //        }
        //    }
        //}

        //This function exists for the ability to add a folder.
        [HttpPost("AddAFolder")]
        public async Task<ActionResult> AddAFolder([FromForm] string folderPath)
        {

            //second verse, same as the first...
            //We get the path of local storage, change the path directories from / to \ if needed (epic windows style)
            //Here we check if the folder already exists in the path. This is triggered only by having the same path as 
            //another folder. Much like other file systems, if the name isn't EXACTLY the same, it'll allow you to save it.
            string localStorage = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GreenWellLocalStorage\";
            string finalPath = localStorage + @"\" + folderPath;
            if (Directory.Exists(finalPath)) return StatusCode(500, new { message = "The folder exists already.", status = "500" });

            try
            {
                Greenwell.Data.Models.Files folder = new Greenwell.Data.Models.Files
                {
                    FullPath = folderPath,
                    Approved = true
                };
                await _context.Files.AddAsync(folder);
                await _context.SaveChangesAsync();

                DirectoryInfo di = Directory.CreateDirectory(finalPath);

                return Ok(new { message = "Folder was created successfully.", success = true, status = "200" });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { error = e.Message, status = "500" });
            }
        }

        //Delete folder, authorized only to administrators
        [Authorize(Roles = "Administrator")]
        [HttpPost("DeleteAFolder")]
        public async Task<ActionResult> DeleteAFolder([FromForm] string folderPath)
        {
            try
            {
                //list of all paths with folder in it
                var path = _context.Files.Where(a => a.FullPath.StartsWith(folderPath)).ToList();

                for (int i = 0; i < path.Count; i++)
                {
                    //remove refrences in tagmap to prevent forign key error.
                    int id = _context.Files.SingleOrDefault(a => a.FullPath == path[i].FullPath).FileId;
                    _context.RemoveRange(_context.Tagmap.Where(a => a.FileId == id));
                    await _context.SaveChangesAsync();

                    //They remove the file.
                    _context.Files.Remove(path[i]);
                    await _context.SaveChangesAsync();
                }

                string localStorage = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GreenWellLocalStorage\";
                string finalPath = localStorage + @"\" + folderPath;

                Directory.Delete(finalPath, true);
                return Ok(new { message = "Folder was deleted successfully.", success = true, status = "200" });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { message = "Folder could not be deleted.", error = e.Message, status = "500" });
            }
        }

        //Delete file
        //Authorized only to administrators
        [HttpPost("DeleteAFile")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> DeleteAFile([FromBody] string p)
        {
            try
            {
                //remove all instances of file in database, and delete from local storage
                int id = _context.Files.SingleOrDefault(a => a.FullPath == p).FileId;
                _context.RemoveRange(_context.Tagmap.Where(a => a.FileId == id));
                await _context.SaveChangesAsync();
                _context.Files.Remove(_context.Files.SingleOrDefault(a => a.FullPath == p));
                await _context.SaveChangesAsync();

                string localStorage = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GreenWellLocalStorage\";
                string finalPath = localStorage + @"\" + p;

                System.IO.File.Delete(finalPath);
                return Ok(new { message = "File was deleted successfully.", success = true, status = "200" });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { message = "File could not be deleted.", error = e.Message, status = "500" });
            }
        }

        //Rename folder
        [HttpPost("RenameAFolder")]
        public async Task<ActionResult> RenameAFolder([FromBody] string[] p)
        {
            try
            {
                //list of all paths that include the folder
                var path = _context.Files.Where(a => a.FullPath.StartsWith(p[0])).ToList();

                for (int i = 0; i < path.Count; i++)
                {
                    //replace each old path with new path
                    path[i].FullPath = path[i].FullPath.Replace(p[0], p[1]);
                    _context.Files.Update(path[i]);
                    await _context.SaveChangesAsync();
                }

                string localStorage = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GreenWellLocalStorage\";
                string oldFinalPath = localStorage + @"\" + p[0];
                string newFinalPath = localStorage + @"\" + p[1];

                Directory.Move(oldFinalPath, newFinalPath);
                return Ok(new { message = "Folder was renamed successfully.", success = true, status = "200" });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { message = "Folder could not be renamed.", error = e.Message, status = "500" });
            }
        }

        //Rename file
        [HttpPost("RenameAFile")]
        public async Task<ActionResult> RenameAFile([FromBody] string[] p)
        {
            try
            {
                //Create path with new name, then update database and local files
                var path = _context.Files.FirstOrDefault(a => a.FullPath == p[0]);
                path.FullPath = p[1];
                path.Filename = System.IO.Path.GetFileName(p[1]);
                _context.Files.Update(path);
                await _context.SaveChangesAsync();

                string localStorage = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GreenWellLocalStorage\";
                string oldFinalPath = localStorage + @"\" + p[0];
                string newFinalPath = localStorage + @"\" + p[1];

                Directory.Move(oldFinalPath, newFinalPath);
                return Ok(new { message = "File was renamed successfully.", success = true, status = "200" });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { message = "File could not be renamed.", error = e.Message, status = "500" });
            }
        }

        [HttpPost("DownloadAFile")]
        public IActionResult DownloadFile([FromForm] string filePath)
        {
            var file = _context.Files.SingleOrDefault(a => a.FullPath == filePath);

            //We check to see if the files exists in the database so they download corrupted, non-existent files.
            if (file == null)
            {
                return StatusCode(500, new { message = "File could not be found.", status = "403" });
            }
            //We then check to see if the file is AdminOnly or requires approval, they shouldn't be able to even download files that fail this check, but someone could call this malicously.
            else if (file.AdminOnly == true || (file.Approved == true))
            {
                return StatusCode(500, new { message = "Your not authorized to access this file.", status = "401" });
            }

            //Otherwise return the file
            var net = new System.Net.WebClient();
            string localStorage = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GreenWellLocalStorage";
            filePath = filePath.Replace("/", @"\");
            string finalPath = @localStorage + @"\" + @filePath;
            var data = net.DownloadData(finalPath);
            var content = new System.IO.MemoryStream(data);
            var contentType = "APPLICATION/octet-stream";
            var fileName = "ahmed.jpeg";
            return File(content, contentType, fileName);
        }


        [Authorize(Roles = "Administrator")]
        [HttpPost("AdminDownloadAFile")]
        public IActionResult AdminDownloadFile([FromForm] string filePath)
        {
            var file = _context.Files.SingleOrDefault(a => a.FullPath == filePath);
            //We check to see if the files exists in the database so they download corrupted, non-existent files.
            if (file == null)
            {
                return StatusCode(500, new { message = "File could not be found.", status = "403" });
            }


            var net = new System.Net.WebClient();
            string localStorage = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GreenWellLocalStorage";
            filePath = filePath.Replace("/", @"\");
            string finalPath = @localStorage + @"\" + @filePath;
            var data = net.DownloadData(finalPath);
            var content = new System.IO.MemoryStream(data);
            var contentType = "APPLICATION/octet-stream";
            var fileName = "ahmed.jpeg";
            return File(content, contentType, fileName);
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost("UnapprovedFiles")]
        public ActionResult UnapprovedFiles()
        {
            // return all files without admin only access
            return Ok(new { files = _context.Files.Where(p => p.Approved != true).ToList() });
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost("ResolveApproval")]
        public async Task<ActionResult> ResolveApproval([FromForm] string fullPath, [FromForm] bool approval)
        {
            var path = _context.Files.FirstOrDefault(a => a.FullPath.StartsWith(fullPath));
            if (approval)
            {
                path.Approved = true;
                _context.Files.Update(path);
                await _context.SaveChangesAsync();
                return Ok(new { message = "File was approved.", success = true, status = "200" });
            }
            else {
                try { 
                //remove all instances of file in database, and delete from local storage
                int id = _context.Files.SingleOrDefault(a => a.Filename == System.IO.Path.GetFileName(fullPath)).FileId;
                _context.RemoveRange(_context.Tagmap.Where(a => a.FileId == id));
                await _context.SaveChangesAsync();
                _context.Files.Remove(_context.Files.SingleOrDefault(a => a.Filename == System.IO.Path.GetFileName(fullPath)));
                await _context.SaveChangesAsync();

                string localStorage = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GreenWellLocalStorage\";
                string finalPath = localStorage + @"\" + fullPath;

                System.IO.File.Delete(finalPath);
                return Ok(new { message = "File was deleted successfully.", success = true, status = "200" });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { message = "File could not be found.", error = e.Message, status = "500" });
            }
        }
        }


        //===================================================== For Development /=========================================================// 
        //[HttpPost("GetFilesFromGivenPath")]
        //public async Task<ActionResult> GetFilesFromGivenPath([FromBody] string path)
        //{
        //    try
        //    {
        //        DirectoryInfo dir = new DirectoryInfo(path);
        //        if (!dir.Exists)
        //        {
        //            return StatusCode(500, new { message = "Path does not exist", status = "500" });
        //        }
        //        foreach (var fi in dir.EnumerateFiles("*", SearchOption.AllDirectories))
        //        {
        //            Greenwell.Data.Models.Files folder = new Greenwell.Data.Models.Files
        //            {
        //                FullPath = fi.Directory.ToString().Replace("\\", "/") + "/" + fi.Name
        //            };
        //            await _context.Files.AddAsync(folder);
        //            await _context.SaveChangesAsync();
        //        }

        //        return Ok(new { status = "200", message = "Successfully fetched files.", files = _context.FileDirectories.Select(p => p.path).ToList() });
        //    }
        //    catch (Exception e)
        //    {
        //        return StatusCode(500, new { error = e.Message, status = "500" });
        //    }
        //}

    }
}
