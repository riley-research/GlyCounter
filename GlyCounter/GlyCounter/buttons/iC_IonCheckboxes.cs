using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlyCounter
{
    public partial class Form1
    {
        private void iC_tmt11Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(iC_tmt11CBList);
        }

        private void iC_acylButton_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(iC_acylCBList);
        }

        private void iC_tmt16Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(iC_tmt16CBList);
        }

        private void SelectAllBiotin_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(iC_biotinCLB);
        }

        private void iC_clearButton_Click(object sender, EventArgs e)
        {
            while (iC_tmt16CBList.CheckedIndices.Count > 0)
                iC_tmt16CBList.SetItemChecked(iC_tmt16CBList.CheckedIndices[0], false);

            while (iC_acylCBList.CheckedIndices.Count > 0)
                iC_acylCBList.SetItemChecked(iC_acylCBList.CheckedIndices[0], false);

            while (iC_tmt11CBList.CheckedIndices.Count > 0)
                iC_tmt11CBList.SetItemChecked(iC_tmt11CBList.CheckedIndices[0], false);

            while (iC_miscIonsCBList.CheckedIndices.Count > 0)
                iC_miscIonsCBList.SetItemChecked(iC_miscIonsCBList.CheckedIndices[0], false);

            while (iC_biotinCLB.CheckedIndices.Count > 0)
                iC_biotinCLB.SetItemChecked(iC_biotinCLB.CheckedIndices[0], false);

            iC_tmt16CBList.ClearSelected();
            iC_acylCBList.ClearSelected();
            iC_tmt11CBList.ClearSelected();
            iC_miscIonsCBList.ClearSelected();
        }

    }
}
