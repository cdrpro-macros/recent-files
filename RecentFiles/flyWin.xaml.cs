using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using Corel.Interop.VGCore;
using Window = System.Windows.Window;

namespace RecentFiles
{
    public partial class FlyWin : Window
    {
        public FlyWin()
        {
            try
            {
                InitializeComponent();

                mainGrid.Width = (double)RFcontrol.fWinW;
                mainGrid.Height = (double)RFcontrol.fWinH;

                wTitle.Text = "RecentFiles v." + RFcontrol.mVer + " © Sanich, " + RFcontrol.mYear;

                RFcontrol.LoadLang(this, "Lang");
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString(), RFcontrol.mName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.Visibility = Visibility.Hidden;
        }

        // Find
        private void tbFind_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tbFind.Text.Length > 0)
            {
                List<CdrFile> iLst = new List<CdrFile>();
                foreach (CdrFile cdr in RFcontrol.cdrfiles)
                {
                    if (cdr.cdr_filepath.ToLower().IndexOf(tbFind.Text.ToLower(), System.StringComparison.Ordinal) >= 0) iLst.Add(cdr);
                }
                lst.ItemsSource = iLst;
                lst.Items.Refresh();
            }
            else
            {
                lst.ItemsSource = RFcontrol.cdrfiles;
                lst.Items.Refresh();
            }
        }

        public void OpenCdrFile()
        {
            if (lst.SelectedIndex == -1) return;
            CdrFile c = (CdrFile)lst.SelectedItem;

            if (File.Exists(c.cdr_filepath))
            {
                foreach (Document doc in RFcontrol.dApp.Documents)
                {
                    if (doc.FullFileName == c.cdr_filepath)
                    {
                        MessageBox.Show("This file is already open.", RFcontrol.mName, MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                RFcontrol.dApp.OpenDocument(c.cdr_filepath);
                this.Visibility = Visibility.Hidden;
            }
            else
            {
                MessageBox.Show("The file not found", RFcontrol.mName, MessageBoxButton.OK, MessageBoxImage.Information);
                if (RFcontrol.db.DeleteFromDB(c.cdr_filepath)) RFcontrol.db.LoadList();
            }
        }

        private void lst_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (lst.SelectedIndex == -1) return;
            var c = (CdrFile)lst.SelectedItem;

            if (Keyboard.IsKeyDown(Key.LeftShift))
            {
                openFolder(c.cdr_filepath);
                return;
            }

            if (c.isOpen)
            {
                foreach (Corel.Interop.VGCore.Document d in RFcontrol.dApp.Documents)
                {
                    if (d.FullFileName == c.cdr_filepath) { d.Activate(); break; }
                }
            }
            else OpenCdrFile();
        }

        // menu commands
        private void mOpenFile(object sender, RoutedEventArgs e)
        {
            OpenCdrFile();
        }

        private void mReveal(object sender, RoutedEventArgs e)
        {
            if (lst.SelectedIndex == -1) return;
            CdrFile c = (CdrFile)lst.SelectedItem;
            openFolder(c.cdr_filepath);
        }

        private void openFolder(string path)
        {
            if (File.Exists(path)) System.Diagnostics.Process.Start("explorer.exe", @"/select, " + path);
        }

        private void mCloseFile(object sender, RoutedEventArgs e)
        {
            if (lst.SelectedIndex == -1) return;
            CdrFile c = (CdrFile)lst.SelectedItem;

            foreach (Corel.Interop.VGCore.Document d in RFcontrol.dApp.Documents)
            {
                if (c.cdr_filepath == d.FullFileName)
                {
                    if (d.Dirty)
                    {
                        int state = MessageBox.Show(
                            "Save changes to " + c.cdr_file_name + "?",
                            RFcontrol.mName,
                            MessageBoxButton.YesNoCancel,
                            MessageBoxImage.Question
                        ).GetHashCode();

                        switch (state)
                        {
                            //case 2: //Cansel
                            case 6:
                                d.Save();
                                d.Close();
                                break;
                            case 7:
                                d.Close();
                                break;
                        }
                        return;
                    }
                    else
                    {
                        d.Close();
                        return;
                    }
                }
            }
        }

        private void mDelete(object sender, RoutedEventArgs e)
        {
            if (lst.SelectedIndex == -1) return;

            if (MessageBox.Show("Are you sure you want to delete this file from disk?",
                RFcontrol.mName, MessageBoxButton.YesNo, MessageBoxImage.Question).GetHashCode() == 6)
            {
                CdrFile c = (CdrFile)lst.SelectedItem;
                try
                {
                    if (File.Exists(c.cdr_filepath))
                    {
                        File.Delete(c.cdr_filepath);
                        if (RFcontrol.db.DeleteFromDB(c.cdr_filepath)) RFcontrol.db.LoadList();
                    }
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        }

        private void mDeleteItem(object sender, RoutedEventArgs e)
        {
            if (lst.SelectedIndex == -1) return;
            CdrFile c = (CdrFile)lst.SelectedItem;
            if (RFcontrol.db.DeleteFromDB(c.cdr_filepath)) RFcontrol.db.LoadList();
        }
    }
}
