using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using Velopack;
using Velopack.Sources;
using Velopack.Logging; 

namespace GlyCounter
{
    // UpdateManager using Velopack
    public class UpdateManager
    {
        // --- Singleton Pattern ---
        private static UpdateManager? _instance;
        public static UpdateManager Instance => _instance ??= new UpdateManager();
        // --- --- --- --- --- --- ---

        private const string GitHubRepoUrl = "https://github.com/riley-research/GlyCounter"; // Your repo URL
        private readonly Velopack.UpdateManager _updateManager;
        private readonly ILogger _logger; // Velopack's logger interface

        private UpdateManager()
        {
            // Optional: Configure logging (writes to %LocalAppData%\[YourAppId]\Logs)
            _logger = new VelopackLogger(); 

            try 
            {
                // Configure the source (GitHub)
                var source = new GithubSource(GitHubRepoUrl, null, prerelease: false, _logger);

                // Initialize the Velopack UpdateManager
                _updateManager = new Velopack.UpdateManager(source, _logger);

                _logger.Info($"UpdateManager initialized. Current version: {_updateManager.CurrentVersion}");
            } 
            catch (Exception ex) 
            {
               _logger?.Error($"Failed to initialize UpdateManager: {ex.Message}");
               // Handle critical initialization failure if necessary
               // For simplicity, we might allow the app to continue without update checks
               // Re-throw if _updateManager must be valid: throw; 
               // Or ensure _updateManager is handled as potentially null later
               if (_updateManager == null) {
                   MessageBox.Show($"Could not initialize update manager: {ex.Message}. Update checks will be disabled.", "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                   // Create a dummy manager or handle null checks later
                   _updateManager = new Velopack.UpdateManager(new NullSource(_logger), _logger); // Prevents null refs later
               }
            }
        }

        /// <summary>
        /// Checks Velopack source for available updates.
        /// </summary>
        /// <returns>UpdateInfo if an update is available, null otherwise or on error.</returns>
        private async Task<UpdateInfo?> CheckForUpdatesInternalAsync()
        {
            if (_updateManager == null || _updateManager.Source is NullSource)
            {
                _logger?.Warn("UpdateManager not initialized or using NullSource, skipping check.");
                return null;
            }

            try
            {
                _logger?.Info("Checking for updates...");
                // Check for updates relative to the current version
                var updateInfo = await _updateManager.CheckForUpdatesAsync();

                if (updateInfo == null)
                {
                    _logger?.Info("No updates found.");
                } else {
                    _logger?.Info($"Update found: v{updateInfo.TargetFullRelease?.Version}");
                }
                return updateInfo; // Returns null if no updates available
            }
            catch (Exception ex)
            {
                _logger?.Error($"Error checking for updates: {ex.Message}");
                Debug.WriteLine($"Error checking for updates: {ex.Message}\nStack trace: {ex.StackTrace}");
                return null; // Indicate failure
            }
        }

        /// <summary>
        /// Downloads the specified update package.
        /// </summary>
        private async Task DownloadUpdatesInternalAsync(UpdateInfo updateInfo, Action<int> progressHandler)
        {
             if (_updateManager == null) throw new InvalidOperationException("UpdateManager not initialized.");
             _logger?.Info($"Downloading update v{updateInfo.TargetFullRelease?.Version}...");
             await _updateManager.DownloadUpdatesAsync(updateInfo, progressHandler);
             _logger?.Info("Download complete.");
        }

        /// <summary>
        /// Applies the downloaded updates and restarts the application.
        /// </summary>
        private void ApplyUpdatesAndRestartInternal(UpdateInfo updateInfo)
        {
            if (_updateManager == null) throw new InvalidOperationException("UpdateManager not initialized.");
            _logger?.Info($"Applying update v{updateInfo.TargetFullRelease?.Version} and restarting...");
            // This command will close the current application instance
            _updateManager.ApplyUpdatesAndRestart(updateInfo);
        }

        /// <summary>
        /// Checks for updates and prompts the user interactively.
        /// Downloads and applies if the user consents.
        /// </summary>
        /// <param name="silent">If true, only shows dialogs on error or if an update is found.</param>
        public async Task CheckAndPromptForUpdate(bool silent = false)
        {
             if (_updateManager == null || _updateManager.Source is NullSource)
             {
                _logger?.Warn("UpdateManager not initialized correctly, cannot check and prompt.");
                 if (!silent) MessageBox.Show("Update manager failed to initialize. Update checks unavailable.", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                 return;
             }

            UpdateInfo? updateInfo = null;
            try
            {
                updateInfo = await CheckForUpdatesInternalAsync();

                if (updateInfo == null) {
                    // No update available or error during check
                    if (!silent) 
                    {
                       // Only show "up to date" if the check itself didn't fail (Check logs for errors)
                       MessageBox.Show("You have the latest version.", "No Updates Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    return; // Exit if no updates or check failed
                }

                // Update Available - Proceed to Prompt
                var newVersion = updateInfo.TargetFullRelease?.Version?.ToString() ?? "new version";
                var result = MessageBox.Show(
                    $"A new version of GlyCounter is available (v{newVersion}).\n\nWould you like to download and install it now?",
                    "Update Available", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    // --- Show Progress Form ---
                    Form? updateForm = null;
                    Label? label = null;
                    try 
                    {
                        updateForm = new Form { /* Setup basic form properties */ Text = "Updating...", Width=300, Height=100, FormBorderStyle=FormBorderStyle.FixedDialog, StartPosition=FormStartPosition.CenterScreen, ControlBox=false };
                        label = new Label { /* Setup basic label properties */ Text="Initializing...", Dock=DockStyle.Fill, TextAlign=System.Drawing.ContentAlignment.MiddleCenter };
                        updateForm.Controls.Add(label);
                        updateForm.Show();
                        updateForm.Refresh(); // Ensure it's drawn

                        // --- Download ---
                        await DownloadUpdatesInternalAsync(updateInfo, p => {
                            // Update progress label (ensure thread safety)
                            if (label?.IsHandleCreated == true) {
                                 label.Invoke((Action)(() => label.Text = $"Downloading update... {p}%"));
                            }
                        });

                        // --- Apply and Restart ---
                        label?.Invoke((Action)(() => label.Text = "Applying update and restarting..."));
                        updateForm?.Refresh(); 
                        await Task.Delay(500); // Small delay so user sees message

                        ApplyUpdatesAndRestartInternal(updateInfo);
                        // Application should exit and restart here. Code below might not run.

                    } catch (Exception ex) {
                        _logger?.Error($"Update process failed: {ex.Message}");
                        MessageBox.Show($"Update failed: {ex.Message}", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        // Explicitly close form if update fails before restart
                        updateForm?.Close(); 
                    }
                } else {
                     _logger?.Info("User declined update.");
                }
            }
            catch (Exception ex) // Catch errors during the check/prompt phase itself
            {
                _logger?.Error($"CheckAndPromptForUpdate failed: {ex.Message}");
                Debug.WriteLine($"CheckAndPromptForUpdate error: {ex.Message}\nStack trace: {ex.StackTrace}");
                if (!silent) MessageBox.Show($"Error checking for updates: {ex.Message}", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // Helper simple logger for Velopack (optional, but good practice)
    internal class VelopackLogger : ILogger
    {
        public LogLevel Level { get; set; } = LogLevel.Info;

        public void Write(string message, LogLevel level)
        {
            if (level < Level) return;
            Debug.WriteLine($"[Velopack {level}] {message}");
            // You could also write to a file here
        }
    }
}