using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace GlyCounter
{
    public class DefaultOutput
    {
        public static GlyCounterSettings? getDefaultOutput(string userOutput, GlyCounterSettings glySettings)
        {
            if (string.IsNullOrEmpty(userOutput) || userOutput == "Select output directory")
            {
                if (glySettings.fileList.Count > 0)
                    glySettings.outputPath = Path.GetDirectoryName(glySettings.fileList[0]) ?? glySettings.defaultOutput;
                else
                    glySettings.outputPath = glySettings.defaultOutput;

                glySettings.outputPath = glySettings.outputPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                return glySettings;
            }
            else if (Directory.Exists(userOutput))
            {
                glySettings.outputPath = userOutput;
                return glySettings;
            }
            else
            {
                // User provided a non-empty path that doesn't exist -> ask to create it (existing behavior)
                DialogResult result = MessageBox.Show(
                    "Folder does not exist. Do you want to create a new one?",
                    "Create Folder",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.OK)
                {
                    try
                    {
                        Directory.CreateDirectory(userOutput);
                        glySettings.outputPath = userOutput.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                    }
                    catch
                    {
                        // If creation fails, fall back to default behavior below (Task.Run also checks)
                        glySettings.outputPath = userOutput + Path.DirectorySeparatorChar;
                    }
                    return glySettings;
                }
                else return null;
            }
        }
    }
}
