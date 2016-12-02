using System;
using System.Web;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace CS422
{
    class FilesWebService : WebService
    {
        private readonly StandardFileSystem r_sys;

        public FilesWebService(StandardFileSystem fs)
        {
            r_sys = fs;
        }

        public override void Handler(WebRequest req)
        {
            if (!req.URI.StartsWith(this.ServiceURI))
            {
                throw new InvalidOperationException();
            }

            string requestString = HttpUtility.UrlDecode(req.URI.Substring(ServiceURI.Length));
            string[] pieces = requestString.Split('/');



            Dir422 dir = r_sys.GetRoot();

            for (int i = 0; i < pieces.Length - 1; i++)
            {
                string piece = pieces[i];
                dir = dir.GetDir(piece);
                if (dir == null)
                {
                    req.WriteNotFoundResponse("Invalid path given. Try again.");
                    return;
                }
            }

            File422 file = dir.GetFile(pieces[pieces.Length - 1]);

            if (file != null)
            {
                RespondWithFile(file, req);
            }
            else
            {
                if (dir.Parent != null && pieces[pieces.Length - 1] != "")
                {
                    dir = dir.GetDir(pieces[pieces.Length - 1]);
                }

                if (dir != null)
                {
                    RespondWithList(dir, req);
                }
                else
                {
                    req.WriteNotFoundResponse("Could not find given directory or file. Please try again!");
                }
            }
        }

        private void RespondWithFile(File422 file, WebRequest req)
        {
            try
            {
                Stream stream = file.OpenReadOnly();
                string ContentType;

                if (file.Name.Contains(".jpeg")) { ContentType = "image/jpeg"; }
                else if (file.Name.Contains(".png")) { ContentType = "image/png"; }
                else if (file.Name.Contains(".pdf")) { ContentType = "application/pdf"; }
                else if (file.Name.Contains(".mp4")) { ContentType = "video/mp4"; }
                else if (file.Name.Contains(".txt")) { ContentType = "text/plain"; }
                else if (file.Name.Contains(".html")) { ContentType = "text/html"; }
                else if (file.Name.Contains(".xml")) { ContentType = "application/xml"; }
                else { ContentType = null; }

                if (ContentType == null)
                {
                    req.WriteHTMLResponse("That is not a support file format");
                    return;
                }

                req.WriteStreamResponse(stream, ContentType);
            }
            catch
            {
                Console.WriteLine("Unable to open file stream.");
            }
        }

        private void RespondWithList(Dir422 dir, WebRequest req)
        {
            var html = new StringBuilder("<html>");

            html.AppendLine("<h1>Folders</h1>");
            foreach (Dir422 directory in dir.GetDirs())
            {
                html.AppendFormat("<a href=\"{0}\">{1}</a>", req.URI + directory.Name + "/", directory.Name);
                html.AppendLine("<br>");
            }

            html.AppendLine("<h1>Files</h1>");
            foreach (File422 file in dir.GetFiles())
            {
                html.AppendFormat("<a href=\"{0}\">{1}</a>", req.URI + file.Name, file.Name);
                html.AppendLine("<br>");
            }

            html.Append("</html>");
            req.WriteHTMLResponse(html.ToString());
        }

        public override string ServiceURI
        {
            get
            {
                return "/files/";
            }
        }
    }
}