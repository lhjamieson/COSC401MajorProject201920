using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Greenwell.Data;
using Greenwell.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace Greenwell.Controllers
{
    [Route("api/[controller]")]
    public class GreenWellFilesController : Controller
    {
        private readonly greenwelldatabaseContext _context;
        public GreenWellFilesController(greenwelldatabaseContext context)
        {
            _context = context;
        }

        [HttpGet("[action]")]
        public ActionResult GetAllFiles()
        {
            return Ok(new { files = _context.Files.Select(p => p.FullPath).ToList(), tags = _context.Tags.Select(t => t.TagName).ToList() });
        }

        [HttpGet("[action]")]
        public ActionResult CreateLocalStorage()
        {
            string localStorage = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GreenWellLocatStorage\";
            if (Directory.Exists(localStorage))
            {
                return Ok(new { status = "Local Storage already exists." });
            }
            System.IO.Directory.CreateDirectory(localStorage);
            return Ok(new { files = _context.Files.Select(p => p.FullPath).ToList() });
        }

        [HttpPost("Search")]
        public ActionResult Search([FromBody] string[] data)
        {
            if (data[1].Trim() == "fileName")
            {
                if (data[0].Trim() == "")
                {
                    return Ok(new { status = "empty search bar", files = _context.Files.Select(p => p.FullPath).ToList() });
                }
                var fs = _context.Files.Where(a => a.Filename.StartsWith(data[0].Trim())).Select(p => p.Filename).ToList();
                if (fs.Count() == 0)
                {
                    return Ok(new { status = "no files matched", files = _context.Files.Select(p => p.FullPath).ToList() });
                }
                return Ok(new { status = "200", files = fs });
            }
            if (data[0].Trim() == "")
            {
                return Ok(new { status = "empty search bar", files = _context.Files.Select(p => p.FullPath).ToList() });
            }
            var res1 = _context.Tags.Include(a => a.Tagmap).Where(a => a.TagName == data[0]).Select(a => a.TagId).ToList();
            var res2 = _context.Tagmap.Include(a => a.File).Where(a => res1.Contains(a.TagId)).ToList();
            var files = res2.Select(a => a.File).Select(a => a.Filename).ToList();

            if (files.Count() == 0)
            {
                return Ok(new { status = "no files matched", files = _context.Files.Select(p => p.FullPath).ToList() });
            }
            return Ok(new { status = "200", files });
        }

        [HttpPost("AddFileFromUpload")]
        public async Task<ActionResult> AddFileFromUpload([FromForm] string path, [FromForm] IFormFile f, string[] tags)
        {
            try
            {
                List<int> ids = new List<int>();
                if (!string.IsNullOrEmpty(tags[0]))
                {
                    if (tags[0].Contains(",")) tags = tags[0].Split(",");
                    Greenwell.Data.Models.Tags ts; 
                    for (int i = 0; i < tags.Length; i++)
                    {
                        ts = new Greenwell.Data.Models.Tags
                        {
                            TagName = tags[i]
                        };
                        await _context.Tags.AddAsync(ts);
                        await _context.SaveChangesAsync();
                        ids.Add(ts.TagId);
                    }
                }

                Greenwell.Data.Models.Files file = new Greenwell.Data.Models.Files
                {
                    FullPath = path,
                    Filename = System.IO.Path.GetFileName(path)
                };

                await _context.Files.AddAsync(file);
                await _context.SaveChangesAsync();

                int fileId = file.FileId;

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
                string localStorage = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GreenWellLocatStorage";
                path = path.Replace("/", @"\");
                string finalPath = @localStorage + @"\" + @path;

                using (System.IO.FileStream stream = System.IO.File.Create(finalPath))
                {
                    f.CopyTo(stream);
                    stream.Dispose();
                }
            }
            catch (Exception e)
            {
                return StatusCode(500, new { error = e.Message, status = "500" });
            }
            return Ok(new { message = "File was added successfully.", status = "200" });
        }

        [HttpPost("AddAFile")]
        public async Task<ActionResult> AddAFile([FromForm] string[] p, [FromForm] List<IFormFile> f)
        {
            if (p.Length > 1)
            {
                for (int i = 0; i < p.Length; i++)
                {
                    string path = p[i];
                    string localStorage = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GreenWellLocatStorage\";
                    string finalPath = localStorage + @"\" + path;
                    if (System.IO.File.Exists(finalPath)) return StatusCode(500, new { message = "The file exists already.", status = "500" });
                    try
                    {
                        Greenwell.Data.Models.Files file = new Greenwell.Data.Models.Files
                        {
                            FullPath = p[i],
                            Filename = System.IO.Path.GetFileName(p[i])
                        };
                        await _context.Files.AddAsync(file);
                        await _context.SaveChangesAsync();


                        using (System.IO.FileStream stream = System.IO.File.Create(finalPath))
                        {
                            f[i].CopyTo(stream);
                            stream.Dispose();
                        }
                    }
                    catch (Exception e)
                    {
                        return StatusCode(500, new { error = e.Message, status = "500" });
                    }
                }
                return Ok(new { message = "Files were added successfully.", success = true, status = "200" });
            }
            else
            {
                string path = p[0];
                string localStorage = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GreenWellLocatStorage\";
                string finalPath = localStorage + @"\" + path;
                if (System.IO.File.Exists(finalPath)) return StatusCode(500, new { message = "The file exists already.", status = "500" });

                try
                {
                    Greenwell.Data.Models.Files file = new Greenwell.Data.Models.Files
                    {
                        FullPath = p[0],
                        Filename = System.IO.Path.GetFileName(p[0])
                };
                    await _context.Files.AddAsync(file);
                    await _context.SaveChangesAsync();

                    using (System.IO.FileStream stream = System.IO.File.Create(finalPath))
                    {
                        f[0].CopyTo(stream);
                        stream.Dispose();
                    }

                    return Ok(new { message = "File was added successfully.", success = true, status = "200" });
                }
                catch (Exception e)
                {
                    return StatusCode(500, new { error = e.Message, status = "500" });
                }
            }
        }


        [HttpPost("AddAFolder")]
        public async Task<ActionResult> AddAFolder([FromForm] string folderPath)
        {
            string localStorage = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GreenWellLocatStorage\";
            string finalPath = localStorage + @"\" + folderPath;
            if (Directory.Exists(finalPath)) return StatusCode(500, new { message = "The folder exists already.", status = "500" });

            try
            {
                Greenwell.Data.Models.Files folder = new Greenwell.Data.Models.Files
                {
                    FullPath = folderPath
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

        [HttpPost("DeleteAFolder")]
        public async Task<ActionResult> DeleteAFolder([FromForm] string folderPath)
        {
            try
            {
                var path = _context.Files.Where(a => a.FullPath.StartsWith(folderPath)).ToList();

                for (int i = 0; i < path.Count; i++)
                {
                    _context.Files.Remove(path[i]);
                    await _context.SaveChangesAsync();
                }

                string localStorage = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GreenWellLocatStorage\";
                string finalPath = localStorage + @"\" + folderPath;

                Directory.Delete(finalPath, true);
                return Ok(new { message = "Folder was deleted successfully.", success = true, status = "200" });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { message = "Folder could not be deleted.", error = e.Message, status = "500" });
            }
        }

        [HttpPost("DeleteAFile")]
        public async Task<ActionResult> DeleteAFile([FromBody] string p)
        {
            try
            {
                int id = _context.Files.SingleOrDefault(a => a.Filename == System.IO.Path.GetFileName(p)).FileId;
                _context.RemoveRange(_context.Tagmap.Where(a => a.FileId == id));
                await _context.SaveChangesAsync();
                _context.Files.Remove(_context.Files.SingleOrDefault(a => a.Filename == System.IO.Path.GetFileName(p)));
                await _context.SaveChangesAsync();

                string localStorage = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GreenWellLocatStorage\";
                string finalPath = localStorage + @"\" + p;

                System.IO.File.Delete(finalPath);
                return Ok(new { message = "File was deleted successfully.", success = true, status = "200" });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { message = "File could not be deleted.", error = e.Message, status = "500" });
            }
        }

        [HttpPost("RenameAFolder")]
        public async Task<ActionResult> RenameAFolder([FromBody] string[] p)
        {
            try
            {
                var path = _context.Files.Where(a => a.FullPath.StartsWith(p[0])).ToList();

                for (int i = 0; i < path.Count; i++)
                {
                    path[i].FullPath = path[i].FullPath.Replace(p[0], p[1]);
                    _context.Files.Update(path[i]);
                    await _context.SaveChangesAsync();
                }

                string localStorage = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GreenWellLocatStorage\";
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

        [HttpPost("RenameAFile")]
        public async Task<ActionResult> RenameAFile([FromBody] string[] p)
        {
            try
            {
                var path = _context.Files.FirstOrDefault(a => a.FullPath.StartsWith(p[0]));
                path.FullPath = p[1];
                path.Filename = System.IO.Path.GetFileName(p[1]);
                _context.Files.Update(path);
                await _context.SaveChangesAsync();

                string localStorage = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GreenWellLocatStorage\";
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
