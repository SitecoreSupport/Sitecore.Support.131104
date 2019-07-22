using Sitecore.Data.Items;
using Sitecore.IO;
using Sitecore.Resources.Media;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Shell.Framework;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.HtmlControls;

namespace Sitecore.Support.Shell.Applications.ContentEditor
{
    // The creator of the original patch is Alexey Chigarkov
    public class File : Web.UI.HtmlControls.Edit, IContentField
    {
        // Methods
        public File()
        {
            Class = "scContentControl";
            Change = "#";
            Activation = true;
        }

        private void ClearFile()
        {
            if (!Disabled)
            {
                if (!string.IsNullOrEmpty(Value))
                {
                    SetModified();
                }
                XmlValue = new XmlValue(string.Empty, "file");
                Value = string.Empty;
            }
        }

        protected override void DoChange(Message message)
        {
            base.DoChange(message);
            XmlValue.SetAttribute("mediapath", Value);
            string str = Value;
            if (!str.StartsWith("/sitecore", StringComparison.InvariantCulture))
            {
                str = "/sitecore/media library" + str;
            }
            MediaItem item = Sitecore.Context.ContentDatabase.Items[str];
            if (item == null)
            {
                XmlValue.SetAttribute("mediaid", string.Empty);
                XmlValue.SetAttribute("mediapath", string.Empty);
                XmlValue.SetAttribute("src", string.Empty);
            }
            else
            {
                string mediaUrl = MediaManager.GetMediaUrl(item, MediaUrlOptions.GetShellOptions());
                XmlValue.SetAttribute("mediaid", item.ID.ToString());
                XmlValue.SetAttribute("mediapath", item.MediaPath);
                XmlValue.SetAttribute("src", mediaUrl);
            }
            SetModified();
            Sitecore.Context.ClientPage.ClientResponse.SetReturnValue(true);
        }

        public string GetValue() =>
            XmlValue.ToString();

        public override void HandleMessage(Message message)
        {
            string str;
            base.HandleMessage(message);
            if ((message["id"] == ID) && ((str = message.Name) != null))
            {
                if (str == "contentfile:open")
                {
                    Sitecore.Context.ClientPage.Start(this, "OpenFile");
                }
                else if (str == "contentfile:download")
                {
                    string attribute = XmlValue.GetAttribute("src");
                    if (string.IsNullOrEmpty(attribute))
                    {
                        Sitecore.Context.ClientPage.ClientResponse.Alert("No file has been selected.");
                    }
                    else
                    {
                        if (attribute.StartsWith("~/", StringComparison.InvariantCulture))
                        {
                            attribute = FileUtil.MakePath(Sitecore.Context.Site.VirtualFolder, attribute);
                        }
                        Files.Download(new UrlString(attribute).ToString());
                    }
                }
                else if (str != "contentfile:preview")
                {
                    if (str == "contentfile:clear")
                    {
                        ClearFile();
                    }
                }
                else
                {
                    string attribute = XmlValue.GetAttribute("src");
                    if (attribute.Length > 0)
                    {
                        Sitecore.Context.ClientPage.ClientResponse.Eval("window.open('" + attribute + "', '_blank')");
                    }
                    else
                    {
                        Sitecore.Context.ClientPage.ClientResponse.Alert("No file has been selected.");
                    }
                }
            }
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            base.ServerProperties["Value"] = base.ServerProperties["Value"];
            base.ServerProperties["XmlValue"] = base.ServerProperties["XmlValue"];
        }

        protected void OpenFile(ClientPipelineArgs args)
        {
            if (!args.IsPostBack)
            {
                string[] values = new string[] { Source, "/sitecore/media library" };
                string root = StringUtil.GetString(values);
                Dialogs.BrowseImage(XmlValue.GetAttribute("mediaid"), root, false);
                args.WaitForPostBack();
            }
            else if (!string.IsNullOrEmpty(args.Result) && (args.Result != "undefined"))
            {
                MediaItem item = Sitecore.Context.ContentDatabase.Items[args.Result];
                if (item == null)
                {
                    Sitecore.Context.ClientPage.ClientResponse.Alert("Item not found.");
                }
                else
                {
                    string mediaUrl = MediaManager.GetMediaUrl(item, MediaUrlOptions.GetShellOptions());
                    XmlValue.SetAttribute("mediaid", item.ID.ToString());
                    XmlValue.SetAttribute("src", mediaUrl);
                    Value = item.MediaPath;
                    SetModified();
                }
            }
        }

        protected override void SetModified()
        {
            base.SetModified();
            if (TrackModified)
            {
                Sitecore.Context.ClientPage.Modified = true;
            }
        }

        public void SetValue(string value)
        {
            XmlValue = new XmlValue(value, "file");
            if (XmlValue.GetAttribute("mediaid").Length > 0)
            {
                Item item = Sitecore.Context.ContentDatabase.Items[XmlValue.GetAttribute("mediaid")];
                if (item != null)
                {
                    Value = item.Paths.MediaPath;
                }
            }
        }

        // Properties
        public string Source
        {
            get { return GetViewStateString("Source"); }
            set
            {
                string str = MainUtil.UnmapPath(value);
                if (str.EndsWith("/", StringComparison.InvariantCulture))
                {
                    str = str.Substring(0, str.Length - 1);
                }
                SetViewStateString("Source", str);
            }
        }

        private XmlValue XmlValue
        {
            get
            {
                XmlValue viewStateProperty = base.GetViewStateProperty("XmlValue", null) as XmlValue;
                if (viewStateProperty == null)
                {
                    viewStateProperty = new XmlValue(string.Empty, "file");
                    XmlValue = viewStateProperty;
                }
                return viewStateProperty;
            }
            set
            {
                SetViewStateProperty("XmlValue", value, null);
            }
        }

    }
}

