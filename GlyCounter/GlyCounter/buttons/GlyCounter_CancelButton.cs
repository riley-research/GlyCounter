using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlyCounter
{
    public partial class Form1
    {
        private CancellationTokenSource _cts;

        private void gc_cancelButton_Click(object sender, EventArgs e)
        {
            _cts?.Cancel();
            gc_cancelButton.Enabled = false;
        }
    }
}
