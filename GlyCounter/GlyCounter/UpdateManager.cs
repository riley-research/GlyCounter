using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using Velopack;

namespace GlyCounter
{
    public sealed class UpdateManager
    {
        private static UpdateManager? _instance;
        public static UpdateManager Instance => _instance ??= new UpdateManager();

        private readonly Velopack.UpdateManager _updateManager;
        private readonly IUpdateSource? _updateSource;

        private UpdateManager()
        {
            try
            {
                _updateSource = new GithubSource(
                    "https://github.com/riley-research/GlyCounter",
                    null,
                    false,
                    null
                );

                _updateManager = new Velopack.UpdateManager(_updateSource);

                if (!_updateManager.IsInstalled)
                {
                    Debug.WriteLine("Velopack UpdateManager reports: Not an installed application.");
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize Velopack UpdateManager: {ex}");
                _updateSource = null; 
                _updateManager = new Velopack.UpdateManager(null);
            }
        }

        public async Task CheckForUpdatesAsync(bool silent = false)
        {
            if (_updateSource == null || !_updateManager.IsInstalled)
            {
                string message = _updateSource == null
                    ? "Update manager failed to initialize properly."
                    : "Not an installed build—skipping update check.";
                Debug.WriteLine(message);

                if (!silent && _updateSource != null) 
                {
                    MessageBox.Show(message,
                        "Update Check Skipped", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (!silent && _updateSource == null)
                {
                     MessageBox.Show(message,
                        "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return;
            }


            try
            {
                Debug.WriteLine($"Checking for updates using source: {_updateSource.GetType().Name}");
                var updateInfo = await _updateManager.CheckForUpdatesAsync();

                if (updateInfo?.TargetFullRelease == null)
                {
                    Debug.WriteLine("No updates found or update check failed.");
                    if (!silent)
                    {
                        MessageBox.Show("You are up to date!", "No Updates Found",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    return;
                }

                var newVersion = updateInfo.TargetFullRelease.Version;
                Debug.WriteLine($"Update found: v{newVersion}");

                var result = MessageBox.Show(
                    $"A new version (v{newVersion}) is available. Download & install now?",
                    "Update Available",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        // Download
                        Debug.WriteLine($"Downloading update v{newVersion}...");
                        await _updateManager.DownloadUpdatesAsync(updateInfo);

                        // Apply
                        Debug.WriteLine($"Applying and restarting into v{newVersion}...");
                        _updateManager.ApplyUpdatesAndRestart(updateInfo);
                    }
                    catch (Exception downloadApplyEx)
                    {
                        Debug.WriteLine($"Error downloading/applying updates: {downloadApplyEx}");
                        if (!silent)
                        {
                            MessageBox.Show($"Error downloading or applying updates:\n{downloadApplyEx.Message}",
                                "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("User declined the update.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking for updates: {ex}");
                if (!silent)
                {
                    MessageBox.Show($"Error checking for updates:\n{ex.Message}",
                        "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}