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

//File related functionality
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

        //Return all files from the Database and add to a list.
        [HttpGet("[action]")]
        public ActionResult GetAllFiles()
        {
            return Ok(new { files = _context.Files.Select(p => p.FullPath).ToList(), tags = _context.Tags.Select(t => t.TagName).ToList() });
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
                    return Ok(new { status = "empty search bar", files = _context.Files.Select(p => p.FullPath).ToList() });
                }
                //list of all files that begin with the search query
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
            //Search by tag

            //list of files that have the queried tag
            var res1 = _context.Tags.Include(a => a.Tagmap).Where(a => a.TagName == data[0]).Select(a => a.TagId).ToList();
            var res2 = _context.Tagmap.Include(a => a.File).Where(a => res1.Contains(a.TagId)).ToList();
            var files = res2.Select(a => a.File).Select(a => a.Filename).ToList();

            if (files.Count() == 0)
            {
                return Ok(new { status = "no files matched", files = _context.Files.Select(p => p.FullPath).ToList() });
            }
            return Ok(new { status = "200", files });
        }

        //Functionality to add a file from the User.
        [HttpPost("AddFileFromUpload")]
        public async Task<ActionResult> AddFileFromUpload([FromForm] string path, [FromForm] IFormFile f, string[] tags)
        {
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
                string localStorage = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GreenWellLocatStorage";
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

        [HttpPost("AddAFile")]
        public async Task<ActionResult> AddAFile([FromForm] string[] p, [FromForm] List<IFormFile> f)
        {
            if (p.Length > 1)
            {
                //We get the path of local storage, change the path directories from / to \ if needed (epic windows style)
                //Here we check if a file already exists in the path. This is triggered only by having the same path as 
                //another file. Much like other file systems, if the name isn't EXACTLY the same, it'll allow you to save it.
                for (int i = 0; i < p.Length; i++)
                {
                    string path = p[i];
                    string localStorage = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GreenWellLocatStorage\";
                    string finalPath = localStorage + @"\" + path;
                    if (System.IO.File.Exists(finalPath)) return StatusCode(500, new { message = "The file exists already.", status = "500" });
                    
                    //Here we try to make a connection with the Database to begin to save there. If we don't get a connection, we get
                    //a 500 error.
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
        
        //Functionality to download a file from the server
        [HttpPost("DownloadFile")]
        public async Task<IActionResult> Download(string filename)
        {
            if (filename == null)
                return Content("filename not present");

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwwroot", filename);

            var memory = new MemoryStream();
            using (var stream = new FileStream(path, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, GetContentType(path), Path.GetFileName(path));
        }

        //Get file type
        private string GetContentType(string path)
        {
            var types = GetMimeTypes();
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types[ext];
        }

        //File types Dictionary
        private Dictionary<string, string> GetMimeTypes()
        {
            return new Dictionary<string, string>
                {
                    //Common file extensions
                    {".txt", "text/plain"},
                    {".pdf", "application/pdf"},
                    {".doc", "application/vnd.ms-word"},
                    {".docx", "application/vnd.ms-word"},
                    {".xls", "application/vnd.ms-excel"},
                    {".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
                    {".png", "image/png"},
                    {".jpg", "image/jpeg"},
                    {".jpeg", "image/jpeg"},
                    {".gif", "image/gif"},
                    {".csv", "text/csv"},
                    {".zip", "application/x-compressed"},

                    //All of em
                    {".323", "text/h323"},
                    {".3g2", "video/3gpp2"},
                    {".3gp", "video/3gpp"},
                    {".3gp2", "video/3gpp2"},
                    {".3gpp", "video/3gpp"},
                    {".7z", "application/x-7z-compressed"},
                    {".aa", "audio/audible"},
                    {".AAC", "audio/aac"},
                    {".aaf", "application/octet-stream"},
                    {".aax", "audio/vnd.audible.aax"},
                    {".ac3", "audio/ac3"},
                    {".aca", "application/octet-stream"},
                    {".accda", "application/msaccess.addin"},
                    {".accdb", "application/msaccess"},
                    {".accdc", "application/msaccess.cab"},
                    {".accde", "application/msaccess"},
                    {".accdr", "application/msaccess.runtime"},
                    {".accdt", "application/msaccess"},
                    {".accdw", "application/msaccess.webapplication"},
                    {".accft", "application/msaccess.ftemplate"},
                    {".acx", "application/internet-property-stream"},
                    {".AddIn", "text/xml"},
                    {".ade", "application/msaccess"},
                    {".adobebridge", "application/x-bridge-url"},
                    {".adp", "application/msaccess"},
                    {".ADT", "audio/vnd.dlna.adts"},
                    {".ADTS", "audio/aac"},
                    {".afm", "application/octet-stream"},
                    {".ai", "application/postscript"},
                    {".aif", "audio/x-aiff"},
                    {".aifc", "audio/aiff"},
                    {".aiff", "audio/aiff"},
                    {".air", "application/vnd.adobe.air-application-installer-package+zip"},
                    {".amc", "application/x-mpeg"},
                    {".application", "application/x-ms-application"},
                    {".art", "image/x-jg"},
                    {".asa", "application/xml"},
                    {".asax", "application/xml"},
                    {".ascx", "application/xml"},
                    {".asd", "application/octet-stream"},
                    {".asf", "video/x-ms-asf"},
                    {".ashx", "application/xml"},
                    {".asi", "application/octet-stream"},
                    {".asm", "text/plain"},
                    {".asmx", "application/xml"},
                    {".aspx", "application/xml"},
                    {".asr", "video/x-ms-asf"},
                    {".asx", "video/x-ms-asf"},
                    {".atom", "application/atom+xml"},
                    {".au", "audio/basic"},
                    {".avi", "video/x-msvideo"},
                    {".axs", "application/olescript"},
                    {".bas", "text/plain"},
                    {".bcpio", "application/x-bcpio"},
                    {".bin", "application/octet-stream"},
                    {".bmp", "image/bmp"},
                    {".c", "text/plain"},
                    {".cab", "application/octet-stream"},
                    {".caf", "audio/x-caf"},
                    {".calx", "application/vnd.ms-office.calx"},
                    {".cat", "application/vnd.ms-pki.seccat"},
                    {".cc", "text/plain"},
                    {".cd", "text/plain"},
                    {".cdda", "audio/aiff"},
                    {".cdf", "application/x-cdf"},
                    {".cer", "application/x-x509-ca-cert"},
                    {".chm", "application/octet-stream"},
                    {".class", "application/x-java-applet"},
                    {".clp", "application/x-msclip"},
                    {".cmx", "image/x-cmx"},
                    {".cnf", "text/plain"},
                    {".cod", "image/cis-cod"},
                    {".config", "application/xml"},
                    {".contact", "text/x-ms-contact"},
                    {".coverage", "application/xml"},
                    {".cpio", "application/x-cpio"},
                    {".cpp", "text/plain"},
                    {".crd", "application/x-mscardfile"},
                    {".crl", "application/pkix-crl"},
                    {".crt", "application/x-x509-ca-cert"},
                    {".cs", "text/plain"},
                    {".csdproj", "text/plain"},
                    {".csh", "application/x-csh"},
                    {".csproj", "text/plain"},
                    {".css", "text/css"},
                    {".csv", "text/csv"},
                    {".cur", "application/octet-stream"},
                    {".cxx", "text/plain"},
                    {".dat", "application/octet-stream"},
                    {".datasource", "application/xml"},
                    {".dbproj", "text/plain"},
                    {".dcr", "application/x-director"},
                    {".def", "text/plain"},
                    {".deploy", "application/octet-stream"},
                    {".der", "application/x-x509-ca-cert"},
                    {".dgml", "application/xml"},
                    {".dib", "image/bmp"},
                    {".dif", "video/x-dv"},
                    {".dir", "application/x-director"},
                    {".disco", "text/xml"},
                    {".dll", "application/x-msdownload"},
                    {".dll.config", "text/xml"},
                    {".dlm", "text/dlm"},
                    {".doc", "application/msword"},
                    {".docm", "application/vnd.ms-word.document.macroEnabled.12"},
                    {".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
                    {".dot", "application/msword"},
                    {".dotm", "application/vnd.ms-word.template.macroEnabled.12"},
                    {".dotx", "application/vnd.openxmlformats-officedocument.wordprocessingml.template"},
                    {".dsp", "application/octet-stream"},
                    {".dsw", "text/plain"},
                    {".dtd", "text/xml"},
                    {".dtsConfig", "text/xml"},
                    {".dv", "video/x-dv"},
                    {".dvi", "application/x-dvi"},
                    {".dwf", "drawing/x-dwf"},
                    {".dwp", "application/octet-stream"},
                    {".dxr", "application/x-director"},
                    {".eml", "message/rfc822"},
                    {".emz", "application/octet-stream"},
                    {".eot", "application/octet-stream"},
                    {".eps", "application/postscript"},
                    {".etl", "application/etl"},
                    {".etx", "text/x-setext"},
                    {".evy", "application/envoy"},
                    {".exe", "application/octet-stream"},
                    {".exe.config", "text/xml"},
                    {".fdf", "application/vnd.fdf"},
                    {".fif", "application/fractals"},
                    {".filters", "Application/xml"},
                    {".fla", "application/octet-stream"},
                    {".flr", "x-world/x-vrml"},
                    {".flv", "video/x-flv"},
                    {".fsscript", "application/fsharp-script"},
                    {".fsx", "application/fsharp-script"},
                    {".generictest", "application/xml"},
                    {".gif", "image/gif"},
                    {".group", "text/x-ms-group"},
                    {".gsm", "audio/x-gsm"},
                    {".gtar", "application/x-gtar"},
                    {".gz", "application/x-gzip"},
                    {".h", "text/plain"},
                    {".hdf", "application/x-hdf"},
                    {".hdml", "text/x-hdml"},
                    {".hhc", "application/x-oleobject"},
                    {".hhk", "application/octet-stream"},
                    {".hhp", "application/octet-stream"},
                    {".hlp", "application/winhlp"},
                    {".hpp", "text/plain"},
                    {".hqx", "application/mac-binhex40"},
                    {".hta", "application/hta"},
                    {".htc", "text/x-component"},
                    {".htm", "text/html"},
                    {".html", "text/html"},
                    {".htt", "text/webviewhtml"},
                    {".hxa", "application/xml"},
                    {".hxc", "application/xml"},
                    {".hxd", "application/octet-stream"},
                    {".hxe", "application/xml"},
                    {".hxf", "application/xml"},
                    {".hxh", "application/octet-stream"},
                    {".hxi", "application/octet-stream"},
                    {".hxk", "application/xml"},
                    {".hxq", "application/octet-stream"},
                    {".hxr", "application/octet-stream"},
                    {".hxs", "application/octet-stream"},
                    {".hxt", "text/html"},
                    {".hxv", "application/xml"},
                    {".hxw", "application/octet-stream"},
                    {".hxx", "text/plain"},
                    {".i", "text/plain"},
                    {".ico", "image/x-icon"},
                    {".ics", "application/octet-stream"},
                    {".idl", "text/plain"},
                    {".ief", "image/ief"},
                    {".iii", "application/x-iphone"},
                    {".inc", "text/plain"},
                    {".inf", "application/octet-stream"},
                    {".inl", "text/plain"},
                    {".ins", "application/x-internet-signup"},
                    {".ipa", "application/x-itunes-ipa"},
                    {".ipg", "application/x-itunes-ipg"},
                    {".ipproj", "text/plain"},
                    {".ipsw", "application/x-itunes-ipsw"},
                    {".iqy", "text/x-ms-iqy"},
                    {".isp", "application/x-internet-signup"},
                    {".ite", "application/x-itunes-ite"},
                    {".itlp", "application/x-itunes-itlp"},
                    {".itms", "application/x-itunes-itms"},
                    {".itpc", "application/x-itunes-itpc"},
                    {".IVF", "video/x-ivf"},
                    {".jar", "application/java-archive"},
                    {".java", "application/octet-stream"},
                    {".jck", "application/liquidmotion"},
                    {".jcz", "application/liquidmotion"},
                    {".jfif", "image/pjpeg"},
                    {".jnlp", "application/x-java-jnlp-file"},
                    {".jpb", "application/octet-stream"},
                    {".jpe", "image/jpeg"},
                    {".jpeg", "image/jpeg"},
                    {".jpg", "image/jpeg"},
                    {".js", "application/x-javascript"},
                    {".json", "application/json"},
                    {".jsx", "text/jscript"},
                    {".jsxbin", "text/plain"},
                    {".latex", "application/x-latex"},
                    {".library-ms", "application/windows-library+xml"},
                    {".lit", "application/x-ms-reader"},
                    {".loadtest", "application/xml"},
                    {".lpk", "application/octet-stream"},
                    {".lsf", "video/x-la-asf"},
                    {".lst", "text/plain"},
                    {".lsx", "video/x-la-asf"},
                    {".lzh", "application/octet-stream"},
                    {".m13", "application/x-msmediaview"},
                    {".m14", "application/x-msmediaview"},
                    {".m1v", "video/mpeg"},
                    {".m2t", "video/vnd.dlna.mpeg-tts"},
                    {".m2ts", "video/vnd.dlna.mpeg-tts"},
                    {".m2v", "video/mpeg"},
                    {".m3u", "audio/x-mpegurl"},
                    {".m3u8", "audio/x-mpegurl"},
                    {".m4a", "audio/m4a"},
                    {".m4b", "audio/m4b"},
                    {".m4p", "audio/m4p"},
                    {".m4r", "audio/x-m4r"},
                    {".m4v", "video/x-m4v"},
                    {".mac", "image/x-macpaint"},
                    {".mak", "text/plain"},
                    {".man", "application/x-troff-man"},
                    {".manifest", "application/x-ms-manifest"},
                    {".map", "text/plain"},
                    {".master", "application/xml"},
                    {".mda", "application/msaccess"},
                    {".mdb", "application/x-msaccess"},
                    {".mde", "application/msaccess"},
                    {".mdp", "application/octet-stream"},
                    {".me", "application/x-troff-me"},
                    {".mfp", "application/x-shockwave-flash"},
                    {".mht", "message/rfc822"},
                    {".mhtml", "message/rfc822"},
                    {".mid", "audio/mid"},
                    {".midi", "audio/mid"},
                    {".mix", "application/octet-stream"},
                    {".mk", "text/plain"},
                    {".mmf", "application/x-smaf"},
                    {".mno", "text/xml"},
                    {".mny", "application/x-msmoney"},
                    {".mod", "video/mpeg"},
                    {".mov", "video/quicktime"},
                    {".movie", "video/x-sgi-movie"},
                    {".mp2", "video/mpeg"},
                    {".mp2v", "video/mpeg"},
                    {".mp3", "audio/mpeg"},
                    {".mp4", "video/mp4"},
                    {".mp4v", "video/mp4"},
                    {".mpa", "video/mpeg"},
                    {".mpe", "video/mpeg"},
                    {".mpeg", "video/mpeg"},
                    {".mpf", "application/vnd.ms-mediapackage"},
                    {".mpg", "video/mpeg"},
                    {".mpp", "application/vnd.ms-project"},
                    {".mpv2", "video/mpeg"},
                    {".mqv", "video/quicktime"},
                    {".ms", "application/x-troff-ms"},
                    {".msi", "application/octet-stream"},
                    {".mso", "application/octet-stream"},
                    {".mts", "video/vnd.dlna.mpeg-tts"},
                    {".mtx", "application/xml"},
                    {".mvb", "application/x-msmediaview"},
                    {".mvc", "application/x-miva-compiled"},
                    {".mxp", "application/x-mmxp"},
                    {".nc", "application/x-netcdf"},
                    {".nsc", "video/x-ms-asf"},
                    {".nws", "message/rfc822"},
                    {".ocx", "application/octet-stream"},
                    {".oda", "application/oda"},
                    {".odc", "text/x-ms-odc"},
                    {".odh", "text/plain"},
                    {".odl", "text/plain"},
                    {".odp", "application/vnd.oasis.opendocument.presentation"},
                    {".ods", "application/oleobject"},
                    {".odt", "application/vnd.oasis.opendocument.text"},
                    {".one", "application/onenote"},
                    {".onea", "application/onenote"},
                    {".onepkg", "application/onenote"},
                    {".onetmp", "application/onenote"},
                    {".onetoc", "application/onenote"},
                    {".onetoc2", "application/onenote"},
                    {".orderedtest", "application/xml"},
                    {".osdx", "application/opensearchdescription+xml"},
                    {".p10", "application/pkcs10"},
                    {".p12", "application/x-pkcs12"},
                    {".p7b", "application/x-pkcs7-certificates"},
                    {".p7c", "application/pkcs7-mime"},
                    {".p7m", "application/pkcs7-mime"},
                    {".p7r", "application/x-pkcs7-certreqresp"},
                    {".p7s", "application/pkcs7-signature"},
                    {".pbm", "image/x-portable-bitmap"},
                    {".pcast", "application/x-podcast"},
                    {".pct", "image/pict"},
                    {".pcx", "application/octet-stream"},
                    {".pcz", "application/octet-stream"},
                    {".pdf", "application/pdf"},
                    {".pfb", "application/octet-stream"},
                    {".pfm", "application/octet-stream"},
                    {".pfx", "application/x-pkcs12"},
                    {".pgm", "image/x-portable-graymap"},
                    {".pic", "image/pict"},
                    {".pict", "image/pict"},
                    {".pkgdef", "text/plain"},
                    {".pkgundef", "text/plain"},
                    {".pko", "application/vnd.ms-pki.pko"},
                    {".pls", "audio/scpls"},
                    {".pma", "application/x-perfmon"},
                    {".pmc", "application/x-perfmon"},
                    {".pml", "application/x-perfmon"},
                    {".pmr", "application/x-perfmon"},
                    {".pmw", "application/x-perfmon"},
                    {".png", "image/png"},
                    {".pnm", "image/x-portable-anymap"},
                    {".pnt", "image/x-macpaint"},
                    {".pntg", "image/x-macpaint"},
                    {".pnz", "image/png"},
                    {".pot", "application/vnd.ms-powerpoint"},
                    {".potm", "application/vnd.ms-powerpoint.template.macroEnabled.12"},
                    {".potx", "application/vnd.openxmlformats-officedocument.presentationml.template"},
                    {".ppa", "application/vnd.ms-powerpoint"},
                    {".ppam", "application/vnd.ms-powerpoint.addin.macroEnabled.12"},
                    {".ppm", "image/x-portable-pixmap"},
                    {".pps", "application/vnd.ms-powerpoint"},
                    {".ppsm", "application/vnd.ms-powerpoint.slideshow.macroEnabled.12"},
                    {".ppsx", "application/vnd.openxmlformats-officedocument.presentationml.slideshow"},
                    {".ppt", "application/vnd.ms-powerpoint"},
                    {".pptm", "application/vnd.ms-powerpoint.presentation.macroEnabled.12"},
                    {".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation"},
                    {".prf", "application/pics-rules"},
                    {".prm", "application/octet-stream"},
                    {".prx", "application/octet-stream"},
                    {".ps", "application/postscript"},
                    {".psc1", "application/PowerShell"},
                    {".psd", "application/octet-stream"},
                    {".psess", "application/xml"},
                    {".psm", "application/octet-stream"},
                    {".psp", "application/octet-stream"},
                    {".pub", "application/x-mspublisher"},
                    {".pwz", "application/vnd.ms-powerpoint"},
                    {".qht", "text/x-html-insertion"},
                    {".qhtm", "text/x-html-insertion"},
                    {".qt", "video/quicktime"},
                    {".qti", "image/x-quicktime"},
                    {".qtif", "image/x-quicktime"},
                    {".qtl", "application/x-quicktimeplayer"},
                    {".qxd", "application/octet-stream"},
                    {".ra", "audio/x-pn-realaudio"},
                    {".ram", "audio/x-pn-realaudio"},
                    {".rar", "application/octet-stream"},
                    {".ras", "image/x-cmu-raster"},
                    {".rat", "application/rat-file"},
                    {".rc", "text/plain"},
                    {".rc2", "text/plain"},
                    {".rct", "text/plain"},
                    {".rdlc", "application/xml"},
                    {".resx", "application/xml"},
                    {".rf", "image/vnd.rn-realflash"},
                    {".rgb", "image/x-rgb"},
                    {".rgs", "text/plain"},
                    {".rm", "application/vnd.rn-realmedia"},
                    {".rmi", "audio/mid"},
                    {".rmp", "application/vnd.rn-rn_music_package"},
                    {".roff", "application/x-troff"},
                    {".rpm", "audio/x-pn-realaudio-plugin"},
                    {".rqy", "text/x-ms-rqy"},
                    {".rtf", "application/rtf"},
                    {".rtx", "text/richtext"},
                    {".ruleset", "application/xml"},
                    {".s", "text/plain"},
                    {".safariextz", "application/x-safari-safariextz"},
                    {".scd", "application/x-msschedule"},
                    {".sct", "text/scriptlet"},
                    {".sd2", "audio/x-sd2"},
                    {".sdp", "application/sdp"},
                    {".sea", "application/octet-stream"},
                    {".searchConnector-ms", "application/windows-search-connector+xml"},
                    {".setpay", "application/set-payment-initiation"},
                    {".setreg", "application/set-registration-initiation"},
                    {".settings", "application/xml"},
                    {".sgimb", "application/x-sgimb"},
                    {".sgml", "text/sgml"},
                    {".sh", "application/x-sh"},
                    {".shar", "application/x-shar"},
                    {".shtml", "text/html"},
                    {".sit", "application/x-stuffit"},
                    {".sitemap", "application/xml"},
                    {".skin", "application/xml"},
                    {".sldm", "application/vnd.ms-powerpoint.slide.macroEnabled.12"},
                    {".sldx", "application/vnd.openxmlformats-officedocument.presentationml.slide"},
                    {".slk", "application/vnd.ms-excel"},
                    {".sln", "text/plain"},
                    {".slupkg-ms", "application/x-ms-license"},
                    {".smd", "audio/x-smd"},
                    {".smi", "application/octet-stream"},
                    {".smx", "audio/x-smd"},
                    {".smz", "audio/x-smd"},
                    {".snd", "audio/basic"},
                    {".snippet", "application/xml"},
                    {".snp", "application/octet-stream"},
                    {".sol", "text/plain"},
                    {".sor", "text/plain"},
                    {".spc", "application/x-pkcs7-certificates"},
                    {".spl", "application/futuresplash"},
                    {".src", "application/x-wais-source"},
                    {".srf", "text/plain"},
                    {".SSISDeploymentManifest", "text/xml"},
                    {".ssm", "application/streamingmedia"},
                    {".sst", "application/vnd.ms-pki.certstore"},
                    {".stl", "application/vnd.ms-pki.stl"},
                    {".sv4cpio", "application/x-sv4cpio"},
                    {".sv4crc", "application/x-sv4crc"},
                    {".svc", "application/xml"},
                    {".swf", "application/x-shockwave-flash"},
                    {".t", "application/x-troff"},
                    {".tar", "application/x-tar"},
                    {".tcl", "application/x-tcl"},
                    {".testrunconfig", "application/xml"},
                    {".testsettings", "application/xml"},
                    {".tex", "application/x-tex"},
                    {".texi", "application/x-texinfo"},
                    {".texinfo", "application/x-texinfo"},
                    {".tgz", "application/x-compressed"},
                    {".thmx", "application/vnd.ms-officetheme"},
                    {".thn", "application/octet-stream"},
                    {".tif", "image/tiff"},
                    {".tiff", "image/tiff"},
                    {".tlh", "text/plain"},
                    {".tli", "text/plain"},
                    {".toc", "application/octet-stream"},
                    {".tr", "application/x-troff"},
                    {".trm", "application/x-msterminal"},
                    {".trx", "application/xml"},
                    {".ts", "video/vnd.dlna.mpeg-tts"},
                    {".tsv", "text/tab-separated-values"},
                    {".ttf", "application/octet-stream"},
                    {".tts", "video/vnd.dlna.mpeg-tts"},
                    {".txt", "text/plain"},
                    {".u32", "application/octet-stream"},
                    {".uls", "text/iuls"},
                    {".user", "text/plain"},
                    {".ustar", "application/x-ustar"},
                    {".vb", "text/plain"},
                    {".vbdproj", "text/plain"},
                    {".vbk", "video/mpeg"},
                    {".vbproj", "text/plain"},
                    {".vbs", "text/vbscript"},
                    {".vcf", "text/x-vcard"},
                    {".vcproj", "Application/xml"},
                    {".vcs", "text/plain"},
                    {".vcxproj", "Application/xml"},
                    {".vddproj", "text/plain"},
                    {".vdp", "text/plain"},
                    {".vdproj", "text/plain"},
                    {".vdx", "application/vnd.ms-visio.viewer"},
                    {".vml", "text/xml"},
                    {".vscontent", "application/xml"},
                    {".vsct", "text/xml"},
                    {".vsd", "application/vnd.visio"},
                    {".vsi", "application/ms-vsi"},
                    {".vsix", "application/vsix"},
                    {".vsixlangpack", "text/xml"},
                    {".vsixmanifest", "text/xml"},
                    {".vsmdi", "application/xml"},
                    {".vspscc", "text/plain"},
                    {".vss", "application/vnd.visio"},
                    {".vsscc", "text/plain"},
                    {".vssettings", "text/xml"},
                    {".vssscc", "text/plain"},
                    {".vst", "application/vnd.visio"},
                    {".vstemplate", "text/xml"},
                    {".vsto", "application/x-ms-vsto"},
                    {".vsw", "application/vnd.visio"},
                    {".vsx", "application/vnd.visio"},
                    {".vtx", "application/vnd.visio"},
                    {".wav", "audio/wav"},
                    {".wave", "audio/wav"},
                    {".wax", "audio/x-ms-wax"},
                    {".wbk", "application/msword"},
                    {".wbmp", "image/vnd.wap.wbmp"},
                    {".wcm", "application/vnd.ms-works"},
                    {".wdb", "application/vnd.ms-works"},
                    {".wdp", "image/vnd.ms-photo"},
                    {".webarchive", "application/x-safari-webarchive"},
                    {".webtest", "application/xml"},
                    {".wiq", "application/xml"},
                    {".wiz", "application/msword"},
                    {".wks", "application/vnd.ms-works"},
                    {".WLMP", "application/wlmoviemaker"},
                    {".wlpginstall", "application/x-wlpg-detect"},
                    {".wlpginstall3", "application/x-wlpg3-detect"},
                    {".wm", "video/x-ms-wm"},
                    {".wma", "audio/x-ms-wma"},
                    {".wmd", "application/x-ms-wmd"},
                    {".wmf", "application/x-msmetafile"},
                    {".wml", "text/vnd.wap.wml"},
                    {".wmlc", "application/vnd.wap.wmlc"},
                    {".wmls", "text/vnd.wap.wmlscript"},
                    {".wmlsc", "application/vnd.wap.wmlscriptc"},
                    {".wmp", "video/x-ms-wmp"},
                    {".wmv", "video/x-ms-wmv"},
                    {".wmx", "video/x-ms-wmx"},
                    {".wmz", "application/x-ms-wmz"},
                    {".wpl", "application/vnd.ms-wpl"},
                    {".wps", "application/vnd.ms-works"},
                    {".wri", "application/x-mswrite"},
                    {".wrl", "x-world/x-vrml"},
                    {".wrz", "x-world/x-vrml"},
                    {".wsc", "text/scriptlet"},
                    {".wsdl", "text/xml"},
                    {".wvx", "video/x-ms-wvx"},
                    {".x", "application/directx"},
                    {".xaf", "x-world/x-vrml"},
                    {".xaml", "application/xaml+xml"},
                    {".xap", "application/x-silverlight-app"},
                    {".xbap", "application/x-ms-xbap"},
                    {".xbm", "image/x-xbitmap"},
                    {".xdr", "text/plain"},
                    {".xht", "application/xhtml+xml"},
                    {".xhtml", "application/xhtml+xml"},
                    {".xla", "application/vnd.ms-excel"},
                    {".xlam", "application/vnd.ms-excel.addin.macroEnabled.12"},
                    {".xlc", "application/vnd.ms-excel"},
                    {".xld", "application/vnd.ms-excel"},
                    {".xlk", "application/vnd.ms-excel"},
                    {".xll", "application/vnd.ms-excel"},
                    {".xlm", "application/vnd.ms-excel"},
                    {".xls", "application/vnd.ms-excel"},
                    {".xlsb", "application/vnd.ms-excel.sheet.binary.macroEnabled.12"},
                    {".xlsm", "application/vnd.ms-excel.sheet.macroEnabled.12"},
                    {".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
                    {".xlt", "application/vnd.ms-excel"},
                    {".xltm", "application/vnd.ms-excel.template.macroEnabled.12"},
                    {".xltx", "application/vnd.openxmlformats-officedocument.spreadsheetml.template"},
                    {".xlw", "application/vnd.ms-excel"},
                    {".xml", "text/xml"},
                    {".xmta", "application/xml"},
                    {".xof", "x-world/x-vrml"},
                    {".XOML", "text/plain"},
                    {".xpm", "image/x-xpixmap"},
                    {".xps", "application/vnd.ms-xpsdocument"},
                    {".xrm-ms", "text/xml"},
                    {".xsc", "application/xml"},
                    {".xsd", "text/xml"},
                    {".xsf", "text/xml"},
                    {".xsl", "text/xml"},
                    {".xslt", "text/xml"},
                    {".xsn", "application/octet-stream"},
                    {".xss", "application/xml"},
                    {".xtp", "application/octet-stream"},
                    {".xwd", "image/x-xwindowdump"},
                    {".z", "application/x-compress"},
                    {".zip", "application/x-zip-compressed"}
                };
        }

        //This function exists for the ability to add a folder.
        [HttpPost("AddAFolder")]
        public async Task<ActionResult> AddAFolder([FromForm] string folderPath)
        {
        
            //second verse, same as the first...
            //We get the path of local storage, change the path directories from / to \ if needed (epic windows style)
            //Here we check if the folder already exists in the path. This is triggered only by having the same path as 
            //another folder. Much like other file systems, if the name isn't EXACTLY the same, it'll allow you to save it.
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

        //Delete folder
        [HttpPost("DeleteAFolder")]
        public async Task<ActionResult> DeleteAFolder([FromForm] string folderPath)
        {
            try
            {
                //list of all paths with folder in it
                var path = _context.Files.Where(a => a.FullPath.StartsWith(folderPath)).ToList();

                for (int i = 0; i < path.Count; i++)
                {
                    //remove paths
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

        //Delete file
        [HttpPost("DeleteAFile")]
        public async Task<ActionResult> DeleteAFile([FromBody] string p)
        {
            try
            {
                //remove all instances of file in database, and delete from local storage
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

        //Rename file
        [HttpPost("RenameAFile")]
        public async Task<ActionResult> RenameAFile([FromBody] string[] p)
        {
            try
            {
                //Create path with new name, then update database and local files
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
