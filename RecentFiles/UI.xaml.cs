using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using Corel.Interop.VGCore;
using Microsoft.Win32;
using System.IO;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using BitmapImage = System.Windows.Media.Imaging.BitmapImage;

namespace RecentFiles
{
    public partial class RFcontrol : UserControl
    {
        // vars
        public static Corel.Interop.VGCore.Application dApp = null;
        public static DB db = null;

        public static List<CdrFile> cdrfiles;
        public static List<OpenedFile> ofiles;

        public static FlyWin fWin;
        public static string uPath;
        public static string uSPath;
        public static string uLangPath;

        public static SolidColorBrush backColor = null;

        public static string curLangFile = @"\Default.xml";
        public static ImageSource noThmb = null;

        public const string mName = "RecentFiles";
        public const string mVer = "1.5";
        public const string mYear = "2019";
        public const string mDate = "12.05.2014";
        public const string mWebSite = @"https://cdrpro.ru";
        public const string mEmail = "info@cdrpro.ru";

        public const int fWinW = 260;
        public const int fWinH = 565;
        public static int DocCount = 0;

        /*
         * TODO:
         * XmlDocument очень медленно работает на больших данных (собенно тормозит .Load)
         * Быстрее всего работает XmlReader
         * http://habrahabr.ru/blogs/net/138848/
         */

        public RFcontrol() { InitializeComponent(); }
        public RFcontrol(object app)
        {
            try
            {
                InitializeComponent();
                dApp = (Corel.Interop.VGCore.Application)app;

                Stream imgStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("RecentFiles.no-thumb.png");
                var nImg = new BitmapImage();
                nImg.BeginInit();
                nImg.StreamSource = imgStream;
                nImg.EndInit();
                noThmb = nImg;

                Load1();
                LoadLangMenu();

                db = new DB();
                fWin = new FlyWin();

                var uriSource = new Uri("pack://application:,,,/RecentFiles;component/Images/save_off.png");
                btnIcon.Source = new BitmapImage(uriSource);

                dApp.DocumentOpen += new Corel.Interop.VGCore.DIVGApplicationEvents_DocumentOpenEventHandler(dApp_DocumentOpen);
                dApp.DocumentAfterSave += new Corel.Interop.VGCore.DIVGApplicationEvents_DocumentAfterSaveEventHandler(dApp_DocumentAfterSave);
                dApp.DocumentBeforeSave += new Corel.Interop.VGCore.DIVGApplicationEvents_DocumentBeforeSaveEventHandler(dApp_DocumentBeforeSave);
                dApp.DocumentClose += new Corel.Interop.VGCore.DIVGApplicationEvents_DocumentCloseEventHandler(dApp_DocumentClose);

                backColor = new SolidColorBrush(Color.FromRgb(90, 90, 90));

                ofiles = new List<OpenedFile>();

                if (!File.Exists(uPath))
                {
                    var xDoc = new XmlDocument();
                    var dec = xDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                    xDoc.AppendChild(dec);
                    var root = xDoc.CreateElement("Documents");
                    xDoc.AppendChild(root);
                    xDoc.Save(uPath);
                }

                db.LoadList();
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString(), mName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Load1()
        {
            try
            {
                string uFolderPath = Environment.GetEnvironmentVariable("APPDATA") + @"\Corel\" + mName;
                if (!Directory.Exists(uFolderPath)) Directory.CreateDirectory(uFolderPath);
                uPath = uFolderPath + @"\History.xml";
                uSPath = uFolderPath + @"\Settings.xml";

                uLangPath = uFolderPath + curLangFile;
                if (!File.Exists(uLangPath)) UploadLangFile(uFolderPath, "DefaultLanguage", "Default");
                if (!File.Exists(uSPath)) UploadLangFile(uFolderPath, "Settings", "Settings");

                //other languages
                var uRusLangPath = uFolderPath + @"\1049.xml";
                if (!File.Exists(uRusLangPath)) UploadLangFile(uFolderPath, "Languages.1049", "1049");

                var uTurLangPath = uFolderPath + @"\1055.xml";
                if (!File.Exists(uTurLangPath)) UploadLangFile(uFolderPath, "Languages.1055", "1055");

                var uChLangPath = uFolderPath + @"\2052.xml";
                if (!File.Exists(uChLangPath)) UploadLangFile(uFolderPath, "Languages.2052", "2052");
                //other languages

                UpdateFiles(uFolderPath); // update files

                string uiLng = @"\" + dApp.UILanguage.GetHashCode().ToString() + @".xml";
                if (File.Exists(uFolderPath + uiLng)) curLangFile = uiLng;

                var key = Registry.CurrentUser.OpenSubKey("Software\\CDRPRO MACROS\\" + mName);
                if (key != null)
                {
                    var sKey = (string)key.GetValue("Lang", ""); key.Close();
                    if (sKey != "") curLangFile = @"\" + sKey + @".xml";
                }

                uLangPath = uFolderPath + curLangFile;
                LoadLang(this, "Lang");
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString(), mName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateFiles(string uFolderPath)
        {
            try
            {
                var xDoc = new XmlDocument();
                xDoc.Load(uFolderPath + curLangFile);

                var isLng = xDoc.SelectSingleNode(@"/Lng");
                if (isLng != null)
                {
                    UploadLangFile(uFolderPath, "DefaultLanguage", "Default");
                    UploadLangFile(uFolderPath, "Languages.1049", "1049");
                    //UploadLangFile(uFolderPath, "Languages.1055", "1055");
                    UploadLangFile(uFolderPath, "Languages.2052", "2052");
                }

            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString(), mName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UploadLangFile(string uFolderPath, string name, string fileName)
        {
            try
            {
                var uCostLangPath = uFolderPath + @"\" + fileName + @".xml";
                Stream inFile = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("RecentFiles." + name + ".xml");
                var xDocDef = new XmlDocument();
                xDocDef.Load(inFile);
                xDocDef.Save(uCostLangPath);
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString(), mName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void LoadLang(FrameworkElement obj, string Key)
        {
            var xd = (XmlDataProvider)obj.Resources[Key];
            xd.Source = new Uri(uLangPath);
        }

        private void LoadLangMenu()
        {
            try
            {
                string uFolderPath = Environment.GetEnvironmentVariable("APPDATA") + @"\Corel\" + mName;
                var xDoc = new XmlDocument();
                xDoc.Load(uSPath);
                foreach (XmlNode n in xDoc.SelectSingleNode(@"/App/Languages").ChildNodes)
                {
                    var id = n.Attributes["Id"].Value;
                    var uAddLangPath = uFolderPath + @"\" + id + @".xml";
                    if (File.Exists(uAddLangPath))
                    {
                        var mi = new MenuItem { Header = n.Attributes["Name"].Value, Tag = id };
                        mi.Click += new RoutedEventHandler(ChangeLang);
                        LangMenu.Items.Add(mi);
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString(), mName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChangeLang(object sender, RoutedEventArgs e)
        {
            try
            {
                var mi = (MenuItem)sender;
                RegistryKey Key;
                string id = mi.Tag.ToString();

                Key = Registry.CurrentUser.CreateSubKey("Software\\CDRPRO MACROS\\" + mName);
                if (Key == null) Key = Registry.CurrentUser.CreateSubKey("Software\\CDRPRO MACROS\\" + mName);
                Key.SetValue("Lang", id, RegistryValueKind.String);

                string uFolderPath = Environment.GetEnvironmentVariable("APPDATA") + @"\Corel\" + mName;
                uLangPath = uFolderPath + @"\" + id + @".xml";
                LoadLang(this, "Lang"); // update ui
                LoadLang(fWin, "Lang"); // update ui in float window
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString(), mName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Document After Open
        private void dApp_DocumentOpen(Document doc, string FileName)
        {
            UpdIcon(false);
            if (IsNotSup(doc.SourceFormat)) return;

            if (!db.InDB(FileName)) db.addInDB(doc, "");
            else db.updateInDB(doc, "");

            foreach (CdrFile c in cdrfiles)
            {
                if (FileName == c.cdr_filepath)
                {
                    c.bg_color = new SolidColorBrush(Color.FromRgb(102, 153, 51)); //c.bg_color = Brushes.Orange;
                    c.isOpen = true;
                    fWin.lst.Items.Refresh();
                    ofiles.Add(new OpenedFile(FileName));
                    break;
                }
            }
        }

        private void dApp_DocumentBeforeSave(Document Doc, bool SaveAs, string FileName)
        {
            if (IsNotSup(Doc.SourceFormat)) return;
            UpdIcon(true);
        }

        // DocumentAfterSave
        private void dApp_DocumentAfterSave(Document Doc, bool SaveAs, string FileName)
        {
            if (IsNotSup(Doc.SourceFormat)) return;

            if (!db.InDB(FileName)) db.addInDB(Doc, FileName);
            else db.updateInDB(Doc, FileName);

            UpdIcon(false);

            if (SaveAs)
            {
                foreach (CdrFile c in cdrfiles)
                {
                    if (c.cdr_filepath == FileName)
                    {
                        c.bg_color = new SolidColorBrush(Color.FromRgb(102, 153, 51)); //c.bg_color = Brushes.Orange;
                        c.isOpen = true;
                        ofiles.Add(new OpenedFile(FileName));
                    }
                    else
                    {
                        foreach (OpenedFile of in ofiles)
                        {
                            if (c.cdr_filepath == of.filepath)
                            {
                                ofiles.Remove(of);
                                c.bg_color = backColor;
                                c.isOpen = false;
                                fWin.lst.Items.Refresh();
                                return;
                            }
                        }
                    }
                }
            }
        }

        // DocumentClose
        private void dApp_DocumentClose(Document Doc)
        {
            UpdIcon(false);
            if (IsNotSup(Doc.SourceFormat)) return;

            if (Doc.FullFileName.Length == 0) return;
            foreach (OpenedFile of in ofiles)
            {
                if (Doc.FullFileName == of.filepath)
                {
                    ofiles.Remove(of);
                    foreach (CdrFile c in cdrfiles)
                    {
                        if (Doc.FullFileName == c.cdr_filepath)
                        {
                            c.bg_color = backColor;
                            c.isOpen = false;
                            fWin.lst.Items.Refresh();
                            return;
                        }
                    }
                }
            }
        }

        private bool IsNotSup(cdrFilter flt)
        {
            if (flt != cdrFilter.cdrCDR) return true;
            return false;
        }

        private void UpdIcon(bool isSave)
        {
            Uri uriSource = null;

            if (isSave == true) uriSource = new Uri("pack://application:,,,/RecentFiles;component/Images/save_on.png");
            else uriSource = new Uri("pack://application:,,,/RecentFiles;component/Images/save_off.png");

            btnIcon.Source = new BitmapImage(uriSource);
            btnIcon.Refresh();
        }

        //Show window
        private void btn_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // TODO: remove WindowStartupLocation and fix detection of mouse point

            //Point mousePoint = this.PointToScreen(Mouse.GetPosition(this));
            //fWin.Left = (mousePoint.X + fWinW) >= SystemParameters.VirtualScreenWidth ? mousePoint.X - fWinW : mousePoint.X;
            //fWin.Top = (mousePoint.Y + fWinH) >= SystemParameters.VirtualScreenHeight ? mousePoint.Y - fWinH : mousePoint.Y;

            var wih = new System.Windows.Interop.WindowInteropHelper(fWin);
            wih.Owner = (IntPtr)dApp.AppWindow.Handle;

            fWin.Show();
        }

        private void mAbout_Click(object sender, RoutedEventArgs e)
        {
            var w = new wAbout();
            var wih = new System.Windows.Interop.WindowInteropHelper(w);
            wih.Owner = (IntPtr)dApp.AppWindow.Handle;
            w.ShowDialog();
        }
    }

    public static class ExtensionMethods
    {
        private static Action EmptyDelegate = delegate () { };

        public static void Refresh(this UIElement uiElement)
        {
            uiElement.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, EmptyDelegate);
        }
    }

    public class CdrFile
    {
        public string cdr_file_name { get; set; }
        public ImageSource cdr_icon { get; set; }
        public string cdr_filepath { get; set; }
        public string cdr_info { get; set; }
        public Brush bg_color { get; set; }
        public bool isOpen { get; set; }

        public CdrFile(string fName, ImageSource img, string fPath, string info, Brush bg, bool isFileOpen)
        {
            this.cdr_file_name = fName;
            this.cdr_icon = img;
            this.cdr_filepath = fPath;
            this.cdr_info = info;
            this.bg_color = bg;
            this.isOpen = isFileOpen;
        }
    }

    public class OpenedFile
    {
        public string filepath { get; set; }
        public OpenedFile(string p)
        {
            this.filepath = p;
        }
    }

    public class Parser
    {
        private byte[] vbyte = new byte[8];
        private byte[] fbyte = new byte[4];
        public byte[] cbyte;
        FileStream stream;

        public FileStream OpenFile(string filename)
        {
            stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            return stream;
        }

        public void CloseFile()
        {
            stream.Close();
        }

        public unsafe void ReadTwoInts(out int fourcc, out int size)
        {
            int read = stream.Read(vbyte, 0, 8);
            if (read != 8)
            {
                MessageBox.Show("Ошибка чтения файла", "Ошибка!", MessageBoxButton.OK);
            }
            fixed (byte* bp = &vbyte[0])
            {
                fourcc = *((int*)bp);
                size = *((int*)(bp + 4));
            }
        }

        public unsafe void ReadOneInt(out int fourcc)
        {
            int read = stream.Read(fbyte, 0, 4);
            if (read != 4)
            {
                MessageBox.Show("Ошибка чтения файла", "Ошибка!", MessageBoxButton.OK);
            }
            fixed (byte* bp = &fbyte[0])
            {
                fourcc = *((int*)bp);
            }
        }

        public unsafe void ReadCustomInts(out int fourcc, int size)
        {
            cbyte = new byte[size];
            int read = stream.Read(cbyte, 0, size);
            if (read != size)
            {
                MessageBox.Show("Ошибка чтения файла", "Ошибка!", MessageBoxButton.OK);
            }
            fixed (byte* bp = &cbyte[0])
            {
                fourcc = *((int*)bp);
            }
        }

        public string FourCC(int fourcc)
        {
            char[] chars = new char[4];
            chars[0] = (char)(fourcc & 0xFF);
            chars[1] = (char)((fourcc >> 8) & 0xFF);
            chars[2] = (char)((fourcc >> 16) & 0xFF);
            chars[3] = (char)((fourcc >> 24) & 0xFF);
            return new string(chars);
        }

        public void SkipData(int count)
        {
            stream.Seek(count, SeekOrigin.Current);
        }

        public int ReadRiff()
        {
            int fourcc, size, filetype;
            ReadTwoInts(out fourcc, out size);
            ReadOneInt(out filetype);
            if ((FourCC(fourcc) != "RIFF") || (FourCC(filetype).Substring(0, 3) != "CDR"))
            {
                CloseFile();
                return 0;
            }
            return size;
        }

    }
}
