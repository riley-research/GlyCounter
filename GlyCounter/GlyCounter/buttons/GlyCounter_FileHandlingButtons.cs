using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace GlyCounter
{
    public partial class Form1
    {
        public void ClearFiles_Click(object sender, EventArgs e)
        {
            glySettings.fileList = new ObservableCollection<string>();
            FileCounter.Text = "Total Files Uploaded: " + glySettings.fileList.Count;
            textBox1.Text = "Cleared Files";
        }
        public void Button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Multiselect = true;
            fdlg.Title = "C# Corner Open File Dialog";

            // Set the initial directory to the last open folder, if it exists
            if (!string.IsNullOrEmpty(Properties.Settings1.Default.LastOpenFolder) && Directory.Exists(Properties.Settings1.Default.LastOpenFolder))
            {
                fdlg.InitialDirectory = Properties.Settings1.Default.LastOpenFolder;
            }
            else
            {
                fdlg.InitialDirectory = @"c:\"; // Default directory if no previous directory is found
            }

            fdlg.Filter = "RAW and mzML files (*.raw;*.mzML)|*.raw;*.mzML|RAW files (*.raw*)|*.raw*|mzML files (*.mzML)|*.mzML";
            fdlg.FilterIndex = 1;
            fdlg.RestoreDirectory = true;
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = "Successfully uploaded " + fdlg.FileNames.Count() + " file(s)";

                //set the most recent folder to the path of the last file selected
                Properties.Settings1.Default.LastOpenFolder = Path.GetDirectoryName(fdlg.FileNames.LastOrDefault());
                Properties.Settings1.Default.Save();

                //also set a default output directory to the path of the last file saved
                glySettings.defaultOutput = Path.GetDirectoryName(fdlg.FileNames.LastOrDefault());
            }

            //add file paths to file list
            foreach (string filePath in fdlg.FileNames)
                glySettings.fileList.Add(filePath);
        }

        public void ButtonAddFolder_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select timsTOF .d folder";
                dialog.UseDescriptionForTitle = true;
                dialog.ShowNewFolderButton = false;

                if (!string.IsNullOrEmpty(Properties.Settings1.Default.LastOpenFolder) && Directory.Exists(Properties.Settings1.Default.LastOpenFolder))
                {
                    dialog.SelectedPath = Properties.Settings1.Default.LastOpenFolder;
                }
                else
                {
                    dialog.SelectedPath = @"c:\"; // Default directory if no previous directory is found
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var selected = dialog.SelectedPath;
                    // Accept directories that end with ".d" as timsTOF folders; otherwise scan for raw/mzML files
                    if (Path.GetExtension(selected).Equals(".d", StringComparison.OrdinalIgnoreCase))
                    {
                        glySettings.fileList.Add(selected);
                        textBox1.Text = $"Added folder: {selected.Split('\\').Last()}";
                    }
                    else
                    {
                        // add files inside the folder if present
                        var files = Directory.EnumerateFiles(selected, "*.*", SearchOption.AllDirectories)
                            .Where(f => f.EndsWith(".raw", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".mzML", StringComparison.OrdinalIgnoreCase));
                        foreach (var f in files) glySettings.fileList.Add(f);

                        textBox1.Text = $"Added {files.Count()} file(s) from folder";
                    }

                    Properties.Settings1.Default.LastOpenFolder = selected;
                    Properties.Settings1.Default.Save();
                }
            }
        }

        private void Gly_outputButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select Output Directory";
                dialog.UseDescriptionForTitle = true;
                dialog.ShowNewFolderButton = true;

                if (!string.IsNullOrEmpty(Properties.Settings1.Default.LastOpenFolder) && Directory.Exists(Properties.Settings1.Default.LastOpenFolder))
                {
                    dialog.InitialDirectory = Properties.Settings1.Default.LastOpenFolder;
                }
                else
                {
                    dialog.InitialDirectory = @"c:\"; // Default directory if no previous directory is found
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    Gly_outputTextBox.Text = dialog.SelectedPath;
                    Properties.Settings1.Default.LastOpenFolder = Path.GetDirectoryName(dialog.SelectedPath);
                    Properties.Settings1.Default.Save();
                }
            }
        }

        private void FileList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            FileCounter.Text = "Total Files Uploaded: " + glySettings.fileList.Count;
        }

        //set up upload custom oxonium ions
        private void UploadCustomBrowseButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Title = "C# Corner Open File Dialog";
            if (!string.IsNullOrEmpty(Properties.Settings1.Default.LastOpenFolder) && Directory.Exists(Properties.Settings1.Default.LastOpenFolder))
            {
                fdlg.InitialDirectory = Properties.Settings1.Default.LastOpenFolder;
            }
            else
            {
                fdlg.InitialDirectory = @"c:\";
            }
            fdlg.Filter = "*.csv|*.csv";
            fdlg.FilterIndex = 2;
            fdlg.RestoreDirectory = true;
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                uploadCustomTextBox.Text = fdlg.FileName;
                glySettings.csvCustomFile = fdlg.FileName;

                Properties.Settings1.Default.LastOpenFolder = Path.GetDirectoryName(fdlg.FileName);
                Properties.Settings1.Default.Save();
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            int added = 0;

            foreach (var p in paths)
            {
                if (Directory.Exists(p))
                {
                    // if it's a .d folder, add the folder itself
                    if (Path.GetExtension(p).Equals(".d", StringComparison.OrdinalIgnoreCase))
                    {
                        glySettings.fileList.Add(p);
                        added++;
                    }
                    else
                    {
                        // otherwise, scan for raw/mzML files inside the folder
                        var files = Directory.EnumerateFiles(p, "*.*", SearchOption.AllDirectories)
                            .Where(f => f.EndsWith(".raw", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".mzML", StringComparison.OrdinalIgnoreCase));
                        foreach (var f in files)
                        {
                            glySettings.fileList.Add(f);
                            added++;
                        }
                    }
                }
                else if (File.Exists(p))
                {
                    glySettings.fileList.Add(p);
                    added++;
                }
            }

            if (added > 0)
            {
                textBox1.Text = $"Added {added} item(s) via drag-and-drop";
                // Update last folder
                var first = paths.First();
                Properties.Settings1.Default.LastOpenFolder = Directory.Exists(first) ? first : Path.GetDirectoryName(first);
                Properties.Settings1.Default.Save();
            }
        }
    }
}
