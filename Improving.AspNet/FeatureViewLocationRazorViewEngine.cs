using System.Web.Mvc;

namespace Improving.AspNet
{
    /// <summary>
    /// Modified from the suggestion at
    /// http://timgthomas.com/2013/10/feature-folders-in-asp-net-mvc/
    /// </summary>
    public class FeatureViewLocationRazorViewEngine : RazorViewEngine
    {
        public FeatureViewLocationRazorViewEngine()
        {
            var featureFolderViewLocationFormats = new[]
            {
                // First: Look in the feature folder
                "~/Features/{1}/{0}/View.cshtml",
                "~/Features/{1}/Shared/{0}.cshtml",
                "~/Features/Shared/{0}.cshtml",
                // If needed: standard  locations
                "~/Views/{1}/{0}.cshtml",
                "~/Views/Shared/{0}.cshtml"
            };

            ViewLocationFormats        = featureFolderViewLocationFormats;
            MasterLocationFormats      = featureFolderViewLocationFormats;
            PartialViewLocationFormats = featureFolderViewLocationFormats;

            var defaultAreaViewLocations = new[]
            {     
                "~/Areas/{2}/Views/{1}/{0}.cshtml",
                "~/Areas/{2}/Views/{1}/{0}.vbhtml",
                "~/Areas/{2}/Views/Shared/{0}.cshtml",
                "~/Areas/{2}/Views/Shared/{0}.vbhtml"
            };

            AreaMasterLocationFormats      = defaultAreaViewLocations;
            AreaPartialViewLocationFormats = defaultAreaViewLocations;
            AreaViewLocationFormats        = defaultAreaViewLocations;

        }
    }
}