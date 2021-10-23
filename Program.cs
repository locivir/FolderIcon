using System;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing;
using System.Drawing.IconLib;

namespace Locivir
{
    class FolderIcon
    {
        private const UInt32 FCSM_ICONFILE = 0x00000010;
        private const UInt32 FCS_READ = 0x00000001;
        private const UInt32 FCS_FORCEWRITE = 0x00000002;
        private const UInt32 FCS_WRITE = FCS_READ | FCS_FORCEWRITE;

        static void Main(string[] args)
        {
            if (args == null || args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            {
                PrintUsage();
                return;
            }

            if (string.Compare(args[0],"/remove",true)==0)
            {
                if (args.Length!=2)
                {
                    PrintUsage();
                    return;
                }
                ClearFolderIcon(args[1]);
            }

            if (args.Length != 1)
            {
                PrintUsage();
                return;
            }

            string filename = args[0];

            // if no full path is given and it exists in current directory, add the directory name
            if (!Path.IsPathRooted(filename) && File.Exists(Path.Combine(Directory.GetCurrentDirectory(), filename)))
                filename = Path.Combine(Directory.GetCurrentDirectory(), filename);

            // Set the icon
            if (Path.IsPathRooted(filename) && !File.Exists(filename))
            {
                string ext = Path.GetExtension(filename);
                string newIcon = filename;
                if (ext != ".ico")
                    newIcon = CreateIcon(filename);

                if (newIcon != String.Empty)
                {
                    SetFolderIcon(Path.GetDirectoryName(newIcon), newIcon);
                    WriteIniFile(Path.GetDirectoryName(newIcon), newIcon);
                }
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: FolderIcon \"<filename>\"");
            Console.WriteLine("       Set supplied image as folder icon for the folder it's in.");
            Console.WriteLine("Usage: FolderIcon /remove \"<directory>\"");
            Console.WriteLine("       Reset supplied folder to not use an icon.");
        }

        private static string CreateIcon(string filename)
        {
            string label = Path.GetFileNameWithoutExtension(filename);
            string path = Path.GetDirectoryName(filename);
            if (path.Substring(path.Length - 1) != @"\") path += @"\";

            if (!File.Exists(filename)) return String.Empty;
            try
            {
                Bitmap bmpImage = new Bitmap(filename);
                Rectangle cropArea;
                if (bmpImage.Size.Width > bmpImage.Size.Height)
                    cropArea = new Rectangle((bmpImage.Size.Width - bmpImage.Size.Height) / 2, 0, bmpImage.Size.Height, bmpImage.Size.Height);
                else
                    cropArea = new Rectangle(0, (bmpImage.Size.Height - bmpImage.Size.Width) / 2, bmpImage.Size.Width, bmpImage.Size.Width);
                Bitmap bmpCrop = bmpImage.Clone(cropArea, bmpImage.PixelFormat);

                MultiIcon mIcon = new MultiIcon();
                SingleIcon sIcon = mIcon.Add(label);

                sIcon.Add(new Bitmap(bmpCrop, 256, 256));
                sIcon.Add(new Bitmap(bmpCrop, 128, 128));
                sIcon.Add(new Bitmap(bmpCrop, 96, 96));
                sIcon.Add(new Bitmap(bmpCrop, 64, 64));
                sIcon.Add(new Bitmap(bmpCrop, 48, 48));
                sIcon.Add(new Bitmap(bmpCrop, 32, 32));
                sIcon.Add(new Bitmap(bmpCrop, 24, 24));
                sIcon.Add(new Bitmap(bmpCrop, 16, 16));

                string newFilename = path + label + @".ico";
                if (File.Exists(newFilename))
                {
                    int i = 1;
                    newFilename = path + label + " (" + i.ToString() + ").ico";
                    while (File.Exists(newFilename)) newFilename = path + label + " (" + (++i).ToString() + ").ico";
                }
                sIcon.Save(newFilename);
                return newFilename;
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not create icon");
                Console.WriteLine(e.Message);
            }
            return String.Empty;
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Auto)]
        private static extern UInt32 SHGetSetFolderCustomSettings(ref LPSHFOLDERCUSTOMSETTINGS pfcs, string pszPath, UInt32 dwReadWrite);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct LPSHFOLDERCUSTOMSETTINGS
        {
            public UInt32 dwSize;
            public UInt32 dwMask;
            public IntPtr pvid;
            public string pszWebViewTemplate;
            public UInt32 cchWebViewTemplate;
            public string pszWebViewTemplateVersion;
            public string pszInfoTip;
            public UInt32 cchInfoTip;
            public IntPtr pclsid;
            public UInt32 dwFlags;
            public string pszIconFile;
            public UInt32 cchIconFile;
            public int iIconIndex;
            public string pszLogo;
            public UInt32 cchLogo;
        }

        private static void SetFolderIcon(string path, string iconpath)
        {
            try
            {
                LPSHFOLDERCUSTOMSETTINGS FolderSettings = new LPSHFOLDERCUSTOMSETTINGS
                {
                    //FolderSettings.dwSize = (UInt32)IconPath.Length;
                    dwMask = FCSM_ICONFILE,
                    pszIconFile = iconpath,
                    //FolderSettings.cchIconFile = 0;
                    iIconIndex = 0
                };

                UInt32 HRESULT = SHGetSetFolderCustomSettings(ref FolderSettings, path, FCS_FORCEWRITE);
            }
            catch { }
        }

        private static void ClearFolderIcon(string path)
        {
            // if no full path is given and it exists in current directory, add the directory name
            if (!Path.IsPathRooted(path) && Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), path)))
                path = Path.Combine(Directory.GetCurrentDirectory(), path);
            if (Path.IsPathRooted(path) && !Directory.Exists(path))
            {
                Console.WriteLine("Directory does not exist.");
                return;
            };

            try
            {
                LPSHFOLDERCUSTOMSETTINGS FolderSettings = new LPSHFOLDERCUSTOMSETTINGS { dwMask = FCSM_ICONFILE };
                UInt32 HRESULT = SHGetSetFolderCustomSettings(ref FolderSettings, path, FCS_FORCEWRITE);
            }
            catch { }
        }

        private static void WriteIniFile(string path, string iconpath)
        {
            #region [.ShellClassInfo]
            string data = "[.ShellClassInfo]" + Environment.NewLine;
            // specifies an icon for the folder (ico, bmp, exe, dll, icl), eg.: c:\test.ico or text.ico for use of relative path
            data += @"IconFile=" + Path.GetFileName(iconpath) + Environment.NewLine;
            //  specifies the index for an icon (0=first icon), if negative it's a resource ID
            data += @"IconIndex=0" + Environment.NewLine;
            // specifies an icon for the folder, see IconFile and IconIndex (Vista and up)
            data += @"IconResource=" + Path.GetFileName(iconpath) + ",0"+ Environment.NewLine;
            
            // text string or string ID in a resource module, eg.: @shell32.dll,-12689
            //data += @"InfoTip = " + Environment.NewLine;
            
            // default action when you 'Drag and Drop' files in the folder
            //data += @"DefaultDropEffect = " + ((int)DefaultDropEffect.Copy).ToString() + Environment.NewLine;            
 
            // Prevent warning deleting a 'system' directory
            data += @"ConfirmFileOp=0" + Environment.NewLine;
           
            data += Environment.NewLine;
            #endregion

            #region [ViewState]  
            data += @"[ViewState]" + Environment.NewLine;
            data += @"Mode=" + /* ((int)ViewState.Thumbnail).ToString() + */ Environment.NewLine;
            data += @"Vid=" + /* ViewStateGuid[(int)ViewState.Thumbnail] + */ Environment.NewLine;
            data += @"FolderType=Pictures" + Environment.NewLine;
            
            // Show one principal file in folder preview if no icon is assigned
            //data += @"Logo=document.xls" + Environment.NewLine;
            #endregion

            //File.Delete(Path + @"\desktop.ini");
            File.SetAttributes(path + @"\desktop.ini", File.GetAttributes(path + @"\desktop.ini") & ~FileAttributes.System & ~FileAttributes.Hidden);
            File.WriteAllText(path + @"\desktop.ini", data, Encoding.Default);
            File.SetAttributes(path + @"\desktop.ini", File.GetAttributes(path + @"\desktop.ini") | FileAttributes.System | FileAttributes.Hidden);
        }

        private enum FolderType
        {
            Documents,
            Pictures,
            Music,
            Videos,
            Generic
        }

        private enum DefaultDropEffect
        {
            Copy = 1,
            Move = 2,
            Shortcut = 4
        }
    }
}