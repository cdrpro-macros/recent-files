using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Threading;

namespace RecentFiles
{
    public class DB
    {
        public DB() { }

        // Load Items in List
        public void LoadList()
        {
            try
            {
                var xDoc = new XmlDocument();
                xDoc.Load(RFcontrol.uPath);

                if (xDoc.ChildNodes.Count == 0) return;
                RFcontrol.cdrfiles = new List<CdrFile>();

                foreach (XmlNode n in xDoc.ChildNodes[1].ChildNodes)
                {
                    string fPath = n.Attributes["FilePath"].Value;

                    if (File.Exists(fPath))
                    {
                        string fName = n.Attributes["Title"].Value;

                        bool isOpen = false;
                        Brush brush = RFcontrol.backColor;
                        foreach (OpenedFile of in RFcontrol.ofiles)
                        {
                            if (fPath == of.filepath)
                            {
                                brush = new SolidColorBrush(Color.FromRgb(102, 153, 51));
                                isOpen = true;
                            }
                        }

                        var fi = new FileInfo(fPath);

                        if (n.Attributes["Img"].Value.Length > 0)
                        {
                            RFcontrol.cdrfiles.Add(new CdrFile(
                                fName,
                                SetImageData(n.Attributes["Img"].Value),
                                fPath,
                                fi.LastWriteTime.ToString(CultureInfo.InvariantCulture) + "\nver." + n.Attributes["Ver"].Value,
                                brush,
                                isOpen
                            ));
                        }
                        else
                        {
                            RFcontrol.cdrfiles.Add(new CdrFile(
                                fName,
                                RFcontrol.noThmb,
                                fPath,
                                fi.LastWriteTime.ToString() + "\nver." + n.Attributes["Ver"].Value,
                                brush,
                                isOpen
                            ));
                        }

                    }
                    else
                    {
                        DeleteFromDB(fPath);
                    }
                }

                RFcontrol.DocCount = RFcontrol.cdrfiles.Count;

                RFcontrol.fWin.lst.ItemsSource = RFcontrol.cdrfiles;
                RFcontrol.fWin.lst.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        // InDB
        public bool InDB(string fpath)
        {
            var xDoc = new XmlDocument();
            xDoc.Load(RFcontrol.uPath);
            var d = xDoc.SelectSingleNode("//Doc[@FilePath = \"" + fpath + "\"]");
            return d != null;
        }

        // Add Doc in DB
        public void addInDB(Corel.Interop.VGCore.Document Doc, string FileName)
        {
            var xDoc = new XmlDocument();
            xDoc.Load(RFcontrol.uPath);
            var root = xDoc.ChildNodes[1];

            string fName = FileName.Length > 0 ? FileName : Doc.FullFileName;

            if (!IsFileLocked(fName))
            {
                var d = xDoc.CreateElement("Doc");

                d.SetAttribute("Title", Path.GetFileName(fName));
                d.SetAttribute("FilePath", fName);
                d.SetAttribute("Ver", GetCDRversion(fName));
                d.SetAttribute("Img", ImageToBase64(fName));

                if (root.ChildNodes.Count == 0) root.AppendChild(d);
                else root.InsertBefore(d, root.FirstChild);

                xDoc.Save(RFcontrol.uPath);
                LoadList();
            }
        }

        bool IsFileLocked(string file)
        {
            while (true)
            {
                Thread.Sleep(100);
                try
                {
                    var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                    stream.Close();
                    return false;
                }
                catch (IOException) { /*MessageBox.Show(er.Message);*/ DoEvents(); }
            }
        }

        // Update Doc in DB
        public void updateInDB(Corel.Interop.VGCore.Document Doc, string FileName)
        {
            var xDoc = new XmlDocument();
            xDoc.Load(RFcontrol.uPath);
            string fName = FileName.Length > 0 ? FileName : Doc.FullFileName;
            var d = xDoc.SelectSingleNode("//Doc[@FilePath = \"" + fName + "\"]");
            if (d != null)
            {
                var root = xDoc.ChildNodes[1];
                root.RemoveChild(d);
                xDoc.Save(RFcontrol.uPath);
                addInDB(Doc, FileName);
            }
            else MessageBox.Show("Item not found!", RFcontrol.mName, MessageBoxButton.OK);
        }

        // Delete From DB
        public bool DeleteFromDB(string fpath)
        {
            var xDoc = new XmlDocument();
            xDoc.Load(RFcontrol.uPath);
            var d = xDoc.SelectSingleNode("//Doc[@FilePath = \"" + fpath + "\"]");
            if (d != null)
            {
                xDoc.ChildNodes[1].RemoveChild(d);
                xDoc.Save(RFcontrol.uPath);
                return true;
            }
            MessageBox.Show("Item not found!", RFcontrol.mName, MessageBoxButton.OK);
            return false;
        }

        private ImageSource SetImageData(string b64)
        {
            using (var ms = new MemoryStream(Convert.FromBase64String(b64)))
            {
                var source = new BitmapImage();
                source.BeginInit();
                source.StreamSource = new MemoryStream(ms.ToArray()); //source.StreamSource = ms;
                source.EndInit();
                return source;
            }
        }

        private string ImageToBase64(string fileInput)
        {
            var x = new Parser();
            x.OpenFile(fileInput);

            if (x.ReadRiff() == 0)
            {
                x.CloseFile();

                try
                {
                    using (ZipStorer zip = ZipStorer.Open(fileInput, FileAccess.Read))
                    {
                        List<ZipStorer.ZipFileEntry> dir = zip.ReadCentralDir();
                        foreach (ZipStorer.ZipFileEntry entry in dir)
                        {
                            if (Path.GetFileName(entry.FilenameInZip) == "thumbnail.bmp" || Path.GetFileName(entry.FilenameInZip) == "thumbnail.png")
                            {
                                var sw = new MemoryStream();
                                zip.ExtractFile(entry, sw);

                                var newImg = new BitmapImage();
                                newImg.BeginInit();
                                newImg.CacheOption = BitmapCacheOption.None;
                                newImg.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                                newImg.DecodePixelWidth = 60;
                                newImg.DecodePixelHeight = 60;
                                newImg.StreamSource = sw; //newImg.StreamSource = new MemoryStream(sw.ToArray());
                                newImg.Rotation = Rotation.Rotate0;
                                newImg.EndInit();

                                var ms = new MemoryStream(getJPGFromImageControl(newImg as BitmapImage));
                                return Convert.ToBase64String(ms.ToArray());
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: #1\n" + ex.ToString(), RFcontrol.mName, MessageBoxButton.OK);
                }

                return "";
            }

            int size, fourcc;
            x.SkipData(10);
            x.ReadTwoInts(out fourcc, out size);
            if (x.FourCC(fourcc) == "DISP")
            {
                x.ReadCustomInts(out fourcc, size);
                byte[] bmpbyte = new byte[x.cbyte.Length + 10];
                //42 4D 16 05 00 00 00 00 00 00 36 04
                bmpbyte[0] = 0x42;
                bmpbyte[1] = 0x4D;
                bmpbyte[2] = 0xEE;
                bmpbyte[3] = 0x04;
                for (int i = 4; i < 10; i++) bmpbyte[i] = 0x00;
                bmpbyte[10] = 0x3E;
                bmpbyte[11] = 0x00;
                Array.Copy(x.cbyte, 2, bmpbyte, 12, (x.cbyte.Length - 2));

                var newImg = new BitmapImage();
                newImg.BeginInit();
                newImg.CacheOption = BitmapCacheOption.None;
                newImg.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                newImg.DecodePixelWidth = 60;
                newImg.DecodePixelHeight = 60;
                newImg.StreamSource = new MemoryStream(bmpbyte);
                newImg.Rotation = Rotation.Rotate0;
                newImg.EndInit();

                x.CloseFile();
                var ms = new MemoryStream(getJPGFromImageControl(newImg as BitmapImage));
                return Convert.ToBase64String(ms.ToArray());

            }
            x.CloseFile();
            return "";

        }

        public byte[] getJPGFromImageControl(BitmapImage imageC)
        {
            var memStream = new MemoryStream();
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(imageC));
            encoder.QualityLevel = 50;
            encoder.Save(memStream);
            return memStream.GetBuffer();
        }

        private string GetCDRversion(string fileInput)
        {
            var x = new Parser();
            x.OpenFile(fileInput);

            if (x.ReadRiff() == 0)
            {
                x.CloseFile();

                try
                {
                    using (ZipStorer zip = ZipStorer.Open(fileInput, FileAccess.Read))
                    {
                        List<ZipStorer.ZipFileEntry> dir = zip.ReadCentralDir();
                        foreach (ZipStorer.ZipFileEntry entry in dir)
                        {
                            string fileName = Path.GetFileName(entry.FilenameInZip);
                            if (fileName == "metadata.xml")
                            {
                                var sw = new MemoryStream();
                                zip.ExtractFile(entry, sw);
                                var xDoc = new XmlDocument();
                                xDoc.Load(new MemoryStream(sw.ToArray()));
                                var n = xDoc.GetElementsByTagName("CoreVersion")[0];
                                if (n != null) return n.InnerText.Substring(0, 2);
                                return "";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: #2\n" + ex.ToString(), RFcontrol.mName, MessageBoxButton.OK);
                }
                return "";
            }

            int size, fourcc;
            x.ReadTwoInts(out fourcc, out size);
            string ver = x.FourCC(fourcc);
            x.ReadCustomInts(out fourcc, size);
            string oldVers = (fourcc / 100).ToString(CultureInfo.InvariantCulture);
            x.CloseFile();
            return oldVers;
        }

        private void DoEvents()
        {
            var f = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
            (SendOrPostCallback)delegate (object arg) { var fr = arg as DispatcherFrame; fr.Continue = false; }, f);
            Dispatcher.PushFrame(f);
        }
    }
}
