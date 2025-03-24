using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

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
                Squirrel.SquirrelAwareApp.HandleEvents(
                    onInitialInstall: OnAppInstall,
                    onAppUninstall: OnAppUninstall,
                    onEveryRun: OnAppRun,
                    onFirstRun: OnFirstRun
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting up Squirrel events: {ex.Message}");
            }
        }

        private static void OnAppInstall(Version version)
        {
            try
            {
                using (var mgr = new Squirrel.UpdateManager(GitHubRepoUrl))
                {
                    // Create desktop and start menu shortcuts
                    mgr.CreateShortcutForThisExe();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during installation: {ex.Message}");
            }
        }

        private static void OnAppUninstall(Version version)
        {
            try
            {
                using (var mgr = new Squirrel.UpdateManager(GitHubRepoUrl))
                {
                    // Remove desktop and start menu shortcuts
                    mgr.RemoveShortcutForThisExe();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during uninstallation: {ex.Message}");
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
        /// <returns>True if updates are available, false otherwise</returns>
        public async Task<bool> CheckForUpdatesAsync(bool showNoUpdatesMessage = false)
        {
            try
            {
                using (_squirrelManager = new Squirrel.UpdateManager(GitHubRepoUrl))
                {
                    var updateInfo = await _squirrelManager.CheckForUpdate();
                    bool updatesAvailable = updateInfo.ReleasesToApply.Count > 0;
                    
                    if (!updatesAvailable && showNoUpdatesMessage)
                    {
                        MessageBox.Show("You have the latest version.", "No Updates Available",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    
                    return updatesAvailable;
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
                return false;
            }
        }

        /// <summary>
        /// Apply updates if available
        /// </summary>
        /// <returns>True if update was successful, false otherwise</returns>
        public async Task<bool> UpdateApplication()
        {
            try
            {
                using (_squirrelManager = new Squirrel.UpdateManager(GitHubRepoUrl))
                {
                    var updateInfo = await _squirrelManager.CheckForUpdate();
                    
                    if (updateInfo.ReleasesToApply.Count > 0)
                    {
                        await _squirrelManager.UpdateApp();
                        return true;
                    }
                    
                    return false;
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
            bool updatesAvailable = await CheckForUpdatesAsync(showNoUpdatesMessage: !silent);
            
            if (updatesAvailable)
            {
                string updateVersion = "new version";
                try 
                {
                    using (_squirrelManager = new Squirrel.UpdateManager(GitHubRepoUrl))
                    {
                        var updateInfo = await _squirrelManager.CheckForUpdate();
                        if (updateInfo.FutureReleaseEntry != null)
                        {
                            updateVersion = updateInfo.FutureReleaseEntry.Version.ToString();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting update version: {ex.Message}");
                }
                
                var result = MessageBox.Show(
                    $"A new version of GlyCounter is available (v{updateVersion}).\n\nWould you like to download and install it now?",
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
                    bool success = await UpdateApplication();
                    
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