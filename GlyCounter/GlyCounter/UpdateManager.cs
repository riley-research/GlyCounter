using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using Squirrel;
using NuGet;

namespace GlyCounter
{
    public class UpdateManager
    {
        private const string GitHubRepoUrl = "https://github.com/Glyco/GlyCounter";
        private static UpdateManager? _instance;
        private Squirrel.UpdateManager? _squirrelManager;
        
        public static UpdateManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new UpdateManager();
                }
                return _instance;
            }
        }

        private UpdateManager()
        {
            // Private constructor for singleton pattern
        }

        /// <summary>
        /// Sets up Squirrel.Windows events for install, uninstall, and update
        /// </summary>
        public static void SetupEvents()
        {
            try
            {
                SquirrelAwareApp.HandleEvents(
                    onInitialInstall: v => OnAppInstall(v),
                    onAppUninstall: v => OnAppUninstall(v),
                    onEveryRun: (v, d) => OnAppRun(v, d),
                    onFirstRun: () => OnFirstRun()
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting up Squirrel events: {ex.Message}");
            }
        }

        private static void OnAppInstall(Version version)
        {
            using (var mgr = new Squirrel.UpdateManager(GitHubRepoUrl))
            {
                // Create desktop and start menu shortcuts
                mgr.CreateShortcutForThisExe();
            }
        }

        private static void OnAppUninstall(Version version)
        {
            using (var mgr = new Squirrel.UpdateManager(GitHubRepoUrl))
            {
                // Remove desktop and start menu shortcuts
                mgr.RemoveShortcutForThisExe();
            }
        }

        private static void OnAppRun(Version version, bool firstRun)
        {
            // This method is called on every application run
        }

        private static void OnFirstRun()
        {
            // This method is called on the first run after installation
            MessageBox.Show("Thank you for installing GlyCounter!", "Installation Complete", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Check for updates asynchronously
        /// </summary>
        /// <param name="showNoUpdatesMessage">Whether to show a message when no updates are available</param>
        /// <returns>UpdateInfo if an update is available, null otherwise</returns>
        public async Task<ReleaseEntry?> CheckForUpdatesAsync(bool showNoUpdatesMessage = false)
        {
            try
            {
                using (_squirrelManager = new Squirrel.UpdateManager(GitHubRepoUrl))
                {
                    var updateInfo = await _squirrelManager.CheckForUpdate();
                    
                    if (updateInfo.ReleasesToApply.Count > 0)
                    {
                        return updateInfo.FutureReleaseEntry;
                    }
                    else if (showNoUpdatesMessage)
                    {
                        MessageBox.Show("You have the latest version.", "No Updates Available",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking for updates: {ex.Message}");
                if (showNoUpdatesMessage)
                {
                    MessageBox.Show($"Error checking for updates: {ex.Message}", "Update Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return null;
            }
        }

        /// <summary>
        /// Apply updates if available
        /// </summary>
        /// <param name="releaseEntry">ReleaseEntry from CheckForUpdatesAsync</param>
        /// <returns>True if update was successful, false otherwise</returns>
        public async Task<bool> UpdateApplication(ReleaseEntry releaseEntry)
        {
            try
            {
                using (_squirrelManager = new Squirrel.UpdateManager(GitHubRepoUrl))
                {
                    var updateInfo = await _squirrelManager.CheckForUpdate();
                    await _squirrelManager.DownloadReleases(updateInfo.ReleasesToApply);
                    await _squirrelManager.ApplyReleases(updateInfo);
                    await _squirrelManager.CreateUninstallerRegistryEntry();
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating application: {ex.Message}");
                MessageBox.Show($"Error updating application: {ex.Message}", "Update Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Check for updates and prompt user to install if available
        /// </summary>
        public async Task CheckAndPromptForUpdate(bool silent = false)
        {
            var releaseEntry = await CheckForUpdatesAsync(showNoUpdatesMessage: !silent);
            
            if (releaseEntry != null)
            {
                var result = MessageBox.Show(
                    $"A new version of GlyCounter is available (v{releaseEntry.Version}).\n\nWould you like to download and install it now?",
                    "Update Available",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);
                
                if (result == DialogResult.Yes)
                {
                    // Show an updating message
                    var updateForm = new Form
                    {
                        Text = "Updating",
                        Width = 300,
                        Height = 100,
                        FormBorderStyle = FormBorderStyle.FixedDialog,
                        StartPosition = FormStartPosition.CenterScreen,
                        MaximizeBox = false,
                        MinimizeBox = false,
                        ControlBox = false
                    };
                    
                    var label = new Label
                    {
                        Text = "Downloading and installing update...",
                        Width = 280,
                        Height = 50,
                        TextAlign = System.Drawing.ContentAlignment.MiddleCenter
                    };
                    
                    updateForm.Controls.Add(label);
                    updateForm.Show();
                    
                    // Apply the update
                    bool success = await UpdateApplication(releaseEntry);
                    
                    updateForm.Close();
                    
                    if (success)
                    {
                        MessageBox.Show(
                            "Update has been installed successfully. The application will now restart.",
                            "Update Complete",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        
                        // Restart the application
                        UpdateManager.RestartApp();
                    }
                }
            }
        }

        /// <summary>
        /// Restart the application after update
        /// </summary>
        public static void RestartApp()
        {
            var currentExecutablePath = Application.ExecutablePath;
            Process.Start(currentExecutablePath);
            Application.Exit();
        }
    }
}