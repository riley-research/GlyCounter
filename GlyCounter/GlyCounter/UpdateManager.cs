using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using Velopack;
using Velopack.Sources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GlyCounter
{
    public class UpdateManager
    {
        // --- Singleton Pattern ---
        private static UpdateManager? _instance;
        public static UpdateManager Instance => _instance ??= new UpdateManager();
        // --- --- --- --- --- --- ---

        private const string GitHubRepoUrl = "https://github.com/riley-research/GlyCounter"; 
        private readonly Velopack.UpdateManager _updateManager;
        private readonly ILogger _logger;

        private UpdateManager()
        {
            _logger = NullLogger.Instance; 
            
            try 
            {
                var source = new GithubSource(GitHubRepoUrl, null, prerelease: false, _logger);
                _updateManager = new Velopack.UpdateManager(source, _logger);

                _logger.LogInformation($"UpdateManager initialized. Current version: {_updateManager.CurrentVersion}");
            } 
            catch (Exception ex) 
            {
               _logger.LogError(ex, "Failed to initialize UpdateManager");
               if (_updateManager == null) {
                   MessageBox.Show($"Could not initialize update manager: {ex.Message}. Update checks will be disabled.", "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                   _updateManager = new Velopack.UpdateManager(new NullSource(_logger), _logger); 
               }
            }
        }

        private async Task<UpdateInfo?> CheckForUpdatesInternalAsync()
        {
            if (_updateManager == null || _updateManager.Source is NullSource) {
                _logger.LogWarning("UpdateManager not initialized or using NullSource, skipping check.");
                return null;
            }
            try {
                _logger.LogInformation("Checking for updates...");
                var updateInfo = await _updateManager.CheckForUpdatesAsync();
                if (updateInfo == null) { _logger.LogInformation("No updates found."); } 
                else { _logger.LogInformation($"Update found: v{updateInfo.TargetFullRelease?.Version}"); }
                return updateInfo;
            } catch (Exception ex) {
                _logger.LogError(ex, "Error checking for updates");
                Debug.WriteLine($"Error checking for updates: {ex.Message}\nStack trace: {ex.StackTrace}");
                return null; 
            }
        }

        private async Task DownloadUpdatesInternalAsync(UpdateInfo updateInfo, Action<int> progressHandler)
        {
             if (_updateManager == null) throw new InvalidOperationException("UpdateManager not initialized.");
             _logger.LogInformation($"Downloading update v{updateInfo.TargetFullRelease?.Version}...");
             await _updateManager.DownloadUpdatesAsync(updateInfo, progressHandler);
             _logger.LogInformation("Download complete.");
        }

        private void ApplyUpdatesAndRestartInternal(UpdateInfo updateInfo)
        {
            if (_updateManager == null) throw new InvalidOperationException("UpdateManager not initialized.");
            _logger.LogInformation($"Applying update v{updateInfo.TargetFullRelease?.Version} and restarting...");
            _updateManager.ApplyUpdatesAndRestart(updateInfo);
        }

        public async Task CheckAndPromptForUpdate(bool silent = false)
        {
             if (_updateManager == null || _updateManager.Source is NullSource) {
                _logger.LogWarning("UpdateManager not initialized correctly, cannot check and prompt.");
                 if (!silent) MessageBox.Show("Update manager failed to initialize. Update checks unavailable.", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                 return;
             }

            UpdateInfo? updateInfo = null;
            try {
                updateInfo = await CheckForUpdatesInternalAsync();

                if (updateInfo == null) {
                    if (!silent) { MessageBox.Show("You are running the latest version.", "No Updates Available", MessageBoxButtons.OK, MessageBoxIcon.Information); }
                    return; 
                }

                var newVersion = updateInfo.TargetFullRelease?.Version?.ToString() ?? "new version";
                var result = MessageBox.Show(
                    $"A new version of GlyCounter is available (v{newVersion}).\n\nWould you like to download and install it now?",
                    "Update Available", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    
                if (result == DialogResult.Yes) {
                    Form? updateForm = null;
                    Label? label = null;
                    try {
                        updateForm = new Form { Text = "Updating...", Width=300, Height=100, FormBorderStyle=FormBorderStyle.FixedDialog, StartPosition=FormStartPosition.CenterScreen, ControlBox=false };
                        label = new Label { Text="Initializing...", Dock=DockStyle.Fill, TextAlign=System.Drawing.ContentAlignment.MiddleCenter };
                        updateForm.Controls.Add(label);
                        updateForm.Show();
                        updateForm.Refresh(); 

                        await DownloadUpdatesInternalAsync(updateInfo, p => {
                            if (label?.IsHandleCreated == true) {
                                 label.Invoke((Action)(() => label.Text = $"Downloading update... {p}%"));
                            }
                        });

                        label?.Invoke((Action)(() => label.Text = "Applying update and restarting..."));
                        updateForm?.Refresh(); 
                        await Task.Delay(500); 

                        ApplyUpdatesAndRestartInternal(updateInfo);

                    } catch (Exception ex) {
                        _logger.LogError(ex, "Update process failed");
                        MessageBox.Show($"Update failed: {ex.Message}", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        updateForm?.Close(); 
                    }
                } else {
                     _logger.LogInformation("User declined update.");
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, "CheckAndPromptForUpdate failed");
                Debug.WriteLine($"CheckAndPromptForUpdate error: {ex.Message}\nStack trace: {ex.StackTrace}");
                if (!silent) MessageBox.Show($"Error during update process: {ex.Message}", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}