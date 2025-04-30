using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using Velopack;
using Velopack.Sources; // Keep this for GithubSource and the cast

namespace GlyCounter
{
    public sealed class UpdateManager
    {
        private static UpdateManager? _instance;
        public static UpdateManager Instance => _instance ??= new UpdateManager();

        // Make readonly as it's initialized only once
        private readonly Velopack.UpdateManager _updateManager;

        private UpdateManager()
        {
            try
            {
                // Define the source directly
                var githubSource = new GithubSource(
                    "https://github.com/riley-research/GlyCounter", // Correct repo URL if this is it
                    null, // No prerelease tag filter
                    false, // Not fetching prereleases
                    null // No custom access token needed for public repo
                );

                // Initialize the manager with the source
                _updateManager = new Velopack.UpdateManager(githubSource);

                // Basic check if the manager thinks it's installed.
                if (!_updateManager.IsInstalled)
                {
                    Debug.WriteLine("Velopack UpdateManager reports: Not an installed application.");
                }
            }
            catch (Exception ex)
            {
                // Log the failure and create a disabled manager
                Debug.WriteLine($"Failed to initialize Velopack UpdateManager: {ex}");
                // Create a disabled manager by explicitly casting null to the IUpdateSource interface
                _updateManager = new Velopack.UpdateManager((Velopack.Sources.IUpdateSource?)null);
            }
        }

        // This is the method you call from Form1
        public async Task CheckForUpdatesAsync(bool silent = false)
        {
            // The primary check is whether the app is installed via Velopack.
            // If initialization failed in the constructor, the manager was created with a null source.
            // Calling CheckForUpdatesAsync on such a manager should fail gracefully or return null below.
            if (!_updateManager.IsInstalled)
            {
                string message = "Not an installed build—skipping update check.";
                Debug.WriteLine(message);

                if (!silent)
                {
                    MessageBox.Show(message,
                        "Update Check Skipped", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }

            // Proceed with the update check attempt.
            try
            {
                // Removed the check/use of the inaccessible 'Source' property here
                Debug.WriteLine($"Checking for updates...");
                var updateInfo = await _updateManager.CheckForUpdatesAsync();

                // Check if updateInfo or the TargetFullRelease is null
                if (updateInfo?.TargetFullRelease == null)
                {
                    Debug.WriteLine("No updates found or update check failed (potentially due to initialization issue).");
                    if (!silent)
                    {
                        MessageBox.Show("You are up to date!", "No Updates Found",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    return;
                }

                // Correctly access the version from TargetFullRelease
                var newVersion = updateInfo.TargetFullRelease.Version;
                Debug.WriteLine($"Update found: v{newVersion}");

                // Prompt the user (only if not silent)
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
                // Catch errors during the check process itself
                // This will also catch issues if the manager wasn't initialized properly (e.g., null source)
                Debug.WriteLine($"Error checking for updates: {ex}");

                string errorMessage = $"Error checking for updates:\n{ex.Message}";
                string errorTitle = "Update Error";

                // Simple heuristic: If the message mentions "source", it *might* be the initialization issue.
                if (ex.Message.ToLowerInvariant().Contains("source")) {
                   errorMessage = $"Update manager may not have initialized correctly or failed to check source.\nError: {ex.Message}";
                   errorTitle = "Update Initialization/Check Error";
                }

                if (!silent)
                {
                    MessageBox.Show(errorMessage, errorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}