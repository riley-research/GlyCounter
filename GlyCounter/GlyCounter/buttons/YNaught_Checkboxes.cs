using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlyCounter
{
    public partial class Form1
    {
        //set up buttons to check all of certain types of Y-ions
        private void CheckAllNglyco_Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(Yions_NlinkedCheckBox);
        }

        private void CheckAllFucose_Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(Yions_FucoseNlinkedCheckedBox);
        }

        private void CheckAllNeutralLosses_Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(Yions_LossFromPepChecklistBox);
        }

        private void CheckAllOglyco_Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(Yions_OlinkedChecklistBox);
        }

        private void Yions_CheckAllButton_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(Yions_NlinkedCheckBox);
            SelectAllItems_CheckedBox(Yions_FucoseNlinkedCheckedBox);
            SelectAllItems_CheckedBox(Yions_LossFromPepChecklistBox);
            SelectAllItems_CheckedBox(Yions_OlinkedChecklistBox);
        }

        private void Yions_NglycoMannoseButton_Click(object sender, EventArgs e)
        {
            //Common Nglyco
            for (int i = 0; i < Yions_NlinkedCheckBox.Items.Count; i++)
            {
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("Y0"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("203.07"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("406.15"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("568.21"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("730.26"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("892.31"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
            }
            //Glycan losses
            for (int i = 0; i < Yions_LossFromPepChecklistBox.Items.Count; i++)
            {
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("Intact Mass"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("162.05"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("324.10"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("486.15"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("648.21"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("810.26"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("972.31"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
            }

        }

        private void Yions_CheckNglycoSialylButton_Click(object sender, EventArgs e)
        {
            //Common Nglyco
            for (int i = 0; i < Yions_NlinkedCheckBox.Items.Count; i++)
            {
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("Y0"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("203.07"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("406.15"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("568.21"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("730.26"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("892.31"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
            }
            //Glycan losses
            for (int i = 0; i < Yions_LossFromPepChecklistBox.Items.Count; i++)
            {
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("Intact Mass"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("291.09"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("453.14"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("656.22"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("582.19"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("906.29"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("1312.45"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
            }
        }

        private void Yions_CheckNglycoFucoseButton_Click(object sender, EventArgs e)
        {
            //Glycan losses
            for (int i = 0; i < Yions_LossFromPepChecklistBox.Items.Count; i++)
            {
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("Intact Mass"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("802.28"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("511.19"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
            }
            //check all fucose
            SelectAllItems_CheckedBox(Yions_FucoseNlinkedCheckedBox);
            //Common Nglyco
            for (int i = 0; i < Yions_NlinkedCheckBox.Items.Count; i++)
            {
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("Y0"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("203.07"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("406.15"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("568.21"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("730.26"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("892.31"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
            }
        }

        //set up clearing of selections
        private void ClearAllSelections_Button_Click(object sender, EventArgs e)
        {
            while (Yions_NlinkedCheckBox.CheckedIndices.Count > 0)
                Yions_NlinkedCheckBox.SetItemChecked(Yions_NlinkedCheckBox.CheckedIndices[0], false);

            while (Yions_FucoseNlinkedCheckedBox.CheckedIndices.Count > 0)
                Yions_FucoseNlinkedCheckedBox.SetItemChecked(Yions_FucoseNlinkedCheckedBox.CheckedIndices[0], false);

            while (Yions_LossFromPepChecklistBox.CheckedIndices.Count > 0)
                Yions_LossFromPepChecklistBox.SetItemChecked(Yions_LossFromPepChecklistBox.CheckedIndices[0], false);

            while (Yions_OlinkedChecklistBox.CheckedIndices.Count > 0)
                Yions_OlinkedChecklistBox.SetItemChecked(Yions_OlinkedChecklistBox.CheckedIndices[0], false);

            Yions_NlinkedCheckBox.ClearSelected();
            Yions_OlinkedChecklistBox.ClearSelected();
            Yions_FucoseNlinkedCheckedBox.ClearSelected();
            Yions_LossFromPepChecklistBox.ClearSelected();
        }

        private void Yions_NlinkedCheckBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Yions_NlinkedCheckBox.ClearSelected();
        }

        //set up charge states
        private void GroupChargeStates_CheckedChanged(object sender, EventArgs e)
        {
            yNsettings.condenseChargeStates = true;
        }

        private void SeparateChargeStates_CheckedChanged(object sender, EventArgs e)
        {
            yNsettings.condenseChargeStates = false;
        }
    }
}
