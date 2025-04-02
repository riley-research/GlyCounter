using Velopack;

namespace GlyCounter
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                VelopackApp.Build()
                    .Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Velopack startup error: {ex.Message}\n\nApplication will still attempt to run.",
                                "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}