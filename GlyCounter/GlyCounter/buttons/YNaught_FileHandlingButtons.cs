using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlyCounter
{
    public partial class Form1
    {
        //find glycopeptide ID .txt file to know what peptides to look for to make Y-ions
        private void BrowseGlycoPepIDs_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Title = "C# Corner Open File Dialog";
            if (!string.IsNullOrEmpty(Properties.Settings1.Default.LastOpenFolder) && Directory.Exists(Properties.Settings1.Default.LastOpenFolder))
            {
                fdlg.InitialDirectory = Properties.Settings1.Default.LastOpenFolder;
            }
            else
            {
                fdlg.InitialDirectory = @"c:\"; // Default directory if no previous directory is found
            }
            fdlg.Filter = "*.txt|*.txt|*.tsv|*.tsv";
            fdlg.FilterIndex = 2;
            fdlg.RestoreDirectory = true;
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                LoadInGlycoPepIDs_TextBox.Text = fdlg.FileName;
                yNsettings.pepIDFilePath = fdlg.FileName;

                Properties.Settings1.Default.LastOpenFolder = Path.GetDirectoryName(fdlg.FileName);
                Properties.Settings1.Default.Save();
            }

        }

        //find the raw file to look for Y-ions
        private void BrowseGlycoPepRawFiles_Button_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Title = "C# Corner Open File Dialog";
            if (!string.IsNullOrEmpty(Properties.Settings1.Default.LastOpenFolder) && Directory.Exists(Properties.Settings1.Default.LastOpenFolder))
            {
                fdlg.InitialDirectory = Properties.Settings1.Default.LastOpenFolder;
            }
            else
            {
                fdlg.InitialDirectory = @"c:\"; // Default directory if no previous directory is found
            }
            fdlg.Filter = "RAW files (*.raw*)|*.raw*|mzML files (*.mzML)|*.mzML";
            fdlg.FilterIndex = 1;
            fdlg.RestoreDirectory = true;
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                LoadInGlycoPepRawFile_TextBox.Text = fdlg.FileName;
                yNsettings.rawFilePath = fdlg.FileName;

                Properties.Settings1.Default.LastOpenFolder = Path.GetDirectoryName(fdlg.FileName);
                Properties.Settings1.Default.Save();
            }
        }

        //set up output directory
        private void Ynaught_outputButton_Click(object sender, EventArgs e)
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
                    Ynaught_outputTextBox.Text = dialog.SelectedPath;

                    Properties.Settings1.Default.LastOpenFolder = Path.GetDirectoryName(dialog.SelectedPath);
                    Properties.Settings1.Default.Save();
                }
            }
        }

        //set up custom additions for Y-ion upload
        private void BrowseCustomAdditions_Button_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Title = "C# Corner Open File Dialog";
            if (!string.IsNullOrEmpty(Properties.Settings1.Default.LastOpenFolder) && Directory.Exists(Properties.Settings1.Default.LastOpenFolder))
            {
                fdlg.InitialDirectory = Properties.Settings1.Default.LastOpenFolder;
            }
            else
            {
                fdlg.InitialDirectory = @"c:\"; // Default directory if no previous directory is found
            }
            fdlg.Filter = "*.csv|*.csv";
            fdlg.FilterIndex = 2;
            fdlg.RestoreDirectory = true;
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                Ynaught_CustomAdditions_TextBox.Text = fdlg.FileName;
                yNsettings.csvCustomAdditions = fdlg.FileName;

                Properties.Settings1.Default.LastOpenFolder = Path.GetDirectoryName(fdlg.FileName);
                Properties.Settings1.Default.Save();

                glySettings.defaultOutput = Path.GetDirectoryName(fdlg.FileName);
            }

        }

        //set up custom substractions for Y-ion upload
        private void BrowseCustomSubtractions_Button_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Title = "C# Corner Open File Dialog";
            if (!string.IsNullOrEmpty(Properties.Settings1.Default.LastOpenFolder) && Directory.Exists(Properties.Settings1.Default.LastOpenFolder))
            {
                fdlg.InitialDirectory = Properties.Settings1.Default.LastOpenFolder;
            }
            else
            {
                fdlg.InitialDirectory = @"c:\"; // Default directory if no previous directory is found
            }
            fdlg.Filter = "*.csv|*.csv";
            fdlg.FilterIndex = 2;
            fdlg.RestoreDirectory = true;
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                Ynaught_CustomSubtractions_TextBox.Text = fdlg.FileName;
                yNsettings.csvCustomSubtractions = fdlg.FileName;

                Properties.Settings1.Default.LastOpenFolder = Path.GetDirectoryName(fdlg.FileName);
                Properties.Settings1.Default.Save();
            }

        }
    }
}
