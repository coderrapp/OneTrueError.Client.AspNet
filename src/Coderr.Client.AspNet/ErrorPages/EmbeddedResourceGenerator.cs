﻿using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Web;
using System.Web.UI;

namespace codeRR.Client.AspNet.ErrorPages
{
    /// <summary>
    ///     Generate error pages from embedded resources.
    /// </summary>
    public class EmbeddedResourceGenerator : PageGeneratorBase
    {
        private readonly Assembly _assembly;
        private readonly string _path;

        /// <summary>
        ///     Creates a new instance of <see cref="EmbeddedResourceGenerator" />.
        /// </summary>
        /// <param name="assembly">Assembly to load embedded resources from</param>
        /// <param name="path">Namespace path to where the folders are located, example: <c>"YourApp.Views.Errors"</c></param>
        public EmbeddedResourceGenerator(Assembly assembly, string path)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");
            if (path == null) throw new ArgumentNullException("path");
            _assembly = assembly;
            _path = path;
        }

        /// <summary>
        ///     Generate HTML document
        /// </summary>
        /// <param name="context">context information which can be used while deciding which page to generate</param>
        protected override void GenerateHtml(PageGeneratorContext context)
        {
            var html = LoadErrorPage(context.ReporterContext.HttpStatusCodeName);

            if (!html.ToLower().Contains("<form")
                &&
                (Err.Configuration.UserInteraction.AskUserForDetails ||
                 Err.Configuration.UserInteraction.AskUserForPermission))
                throw new ConfigurationErrorsException(
                    "You have to have a <form method=\"post\" action=\"http://yourwebsite/coderr/'\"> in your error page if you would like to collect error information.\r\n(Or set Err.Configuration.AskUserForPermission and Err.Configuration.AskUserForDetails to false)");
            if (!html.Contains("$reportId$"))
                throw new ConfigurationErrorsException(
                    "You have to have a '<input type=\"hidden\" name=\"reportId\" value=\"$reportId$\" />' tag in your HTML. The '$reportId$' will be replaced with the correct report id upon errors by this library.");

            html = html.Replace("$reportId$", context.ReportId)
                .Replace("$URL$", VirtualPathUtility.ToAbsolute("~/coderr/Submit/"));

            if (!Err.Configuration.UserInteraction.AskUserForPermission &&
                (Err.Configuration.UserInteraction.AskForEmailAddress ||
                 Err.Configuration.UserInteraction.AskUserForDetails))
                html = html.Replace("$AllowReportUploading$", "checked");
            else
                html = html.Replace("$AllowReportUploading$", "");

            html = html.Replace("$ExceptionMessage$", context.ReporterContext.ErrorMessage);

            context.SendResponse("text/html", html);
        }

        private string LoadErrorPage(string httpCodeName)
        {
            var paths = new[]
            {
                _path + "." + httpCodeName + ".html",
                _path + ".Error.html"
            };

            var names = _assembly.GetManifestResourceNames();
            var content = "";
            foreach (var path in paths)
                using (var file = _assembly.GetManifestResourceStream(path))
                {
                    if (file == null)
                        continue;

                    var sr = new StreamReader(file);
                    content = sr.ReadToEnd();
                    break;
                }

            var styles = "";
            if (!Err.Configuration.UserInteraction.AskUserForPermission)
                styles += ".AllowSubmissionStyle { display: none; }\r\n";
            if (!Err.Configuration.UserInteraction.AskUserForDetails)
                styles += ".AllowFeedbackStyle { display: none; }\r\n";
            if (!Err.Configuration.UserInteraction.AskForEmailAddress)
                styles += ".AskForEmailAddress { display: none; }\r\n";

            return content
                .Replace("/*CssStyles*/", styles);
        }
    }
}