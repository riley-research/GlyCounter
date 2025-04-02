using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using Velopack;
using Velopack.Sources;

namespace GlyCounter
{
    public sealed class UpdateManager
    {
        private static UpdateManager? _instance;
        public static UpdateManager Instance => _instance ??= new UpdateManager();

        private readonly Velopack.UpdateManager _updateManager;

        private UpdateManager()
        {
            try
            {
                var githubSource = new GithubSource(
                    "https://github.com/riley-research/GlyCounter", // repo URL
                    null,
                    false, 
                    null 
                );

                _updateManager = new Velopack.UpdateManager(githubSource);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize Velopack: {ex}");
                _updateManager = new Velopack.UpdateManager(null, true);
            }
        }

        public async Task CheckForUpdatesAsync(bool silent = false)
        {
            if (_updateManager.IsDisabled)
            {
                if (!silent)
                {
                    MessageBox.Show("UpdateManager is disabled, no updates available.",
                        "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return;
            }

            try
            {
                if (!_updateManager.IsInstalled)
                {
                    Debug.WriteLine("Not an installed build—skipping update check.");
                    return;
                }

                Debug.WriteLine("Checking for updates on GitHub...");
                var updateInfo = await _updateManager.CheckForUpdatesAsync();
                if (updateInfo == null)
                {
                    if (!silent)
                    {
                        MessageBox.Show("You are up to date!", "No Updates Found",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    return;
                }

                var newVersion = updateInfo.Version; 
                var result = MessageBox.Show(
                    $"A new version (v{newVersion}) is available. Download & install now?",
                    "Update Available",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );
                if (result == DialogResult.Yes)
                {
                    // Download
                    Debug.WriteLine($"Downloading update v{newVersion}...");
                    await _updateManager.DownloadUpdatesAsync(updateInfo);
                    // Apply
                    Debug.WriteLine($"Applying and restarting into v{newVersion}...");
                    _updateManager.ApplyUpdatesAndRestart(updateInfo);
                }
                else
                {
                    Debug.WriteLine("User declined the update.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking/applying updates: {ex}");
                if (!silent)
                {
                    MessageBox.Show($"Error checking or applying updates:\n{ex.Message}",
                        "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
