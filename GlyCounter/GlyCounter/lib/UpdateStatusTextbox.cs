using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlyCounter
{
    public partial class Form1
    {
        private void LogMessage(string message)
        {
            if (statusTB.InvokeRequired)
            {
                statusTB.Invoke(() => LogMessage(message));
                return;
            }
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            statusTB.AppendText($"[{timestamp}] {message}\n");
            statusTB.ScrollToCaret();
        }
    }
}
