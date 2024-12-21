using elFinder.NetCore;
using elFinder.NetCore.Drivers.FileSystem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using webMVC.Data;

namespace webMVC.Namespace
{
    [Area("Files")]
    [Authorize(Roles = RoleName.Administrator + ", " + RoleName.Editor)]
    public class FileManagerController : Controller
    {
        [Route("/file-manager")]
        public IActionResult Index()
        {
            return View();
        }

        IWebHostEnvironment _env;

        public FileManagerController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [Route("/file-manager-connector")]
        public async Task<IActionResult> Connector()
        {
            var connector = GetConnector();
            return await connector.ProcessAsync(Request);
        }

        [Route("/file-manager-thumb/{hash}")]
        public async Task<IActionResult> Thumbs(string hash)
        {
            var connector = GetConnector();
            return await connector.GetThumbnailAsync(HttpContext.Request, HttpContext.Response, hash);
        }

        private Connector GetConnector()
        {
            // Thư mục gốc lưu trữ là wwwwroot/files (đảm bảo có tạo thư mục này)
            string pathRoot = "Uploads";
            string requestUrl = "contents";

            var driver = new FileSystemDriver();

            string absoluteUrl = UriHelper.BuildAbsolute(Request.Scheme, Request.Host);
            var uri = new Uri(absoluteUrl);

            string rootDirectory = Path.Combine(_env.ContentRootPath, pathRoot);
            string url = $"{uri.Scheme}://{uri.Authority}/{requestUrl}/";
            string urlThumb = $"{uri.Scheme}://{uri.Authority}/file-manager-thumb/";

            var root = new RootVolume(rootDirectory, url, urlThumb)
            {
                //IsReadOnly = !User.IsInRole("Administrators")
                IsReadOnly = false,
                IsLocked = false, 
                Alias = "Workspace", 
                //MaxUploadSizeInKb = 2048, 
                //LockedFolders = new List<string>(new string[] { "Folder1" }
                ThumbnailSize = 100,
            };
            driver.AddRoot(root);

            return new Connector(driver)
            {
                MimeDetect = MimeDetectOption.Internal
            };
        }
    }
}
