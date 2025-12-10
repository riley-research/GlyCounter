using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlyCounter
{
    public partial class Form1
    {
        private void SelectAllItems_CheckedBox(CheckedListBox cListBox)
        {
            for (int i = 0; i < cListBox.Items.Count; i++)
            {
                cListBox.SetItemChecked(i, true);
            }
        }

        //set up check all button
        private void CheckAll_Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(HexNAcCheckedListBox);
            SelectAllItems_CheckedBox(HexCheckedListBox);
            SelectAllItems_CheckedBox(SialicAcidCheckedListBox);
            SelectAllItems_CheckedBox(M6PCheckedListBox);
            SelectAllItems_CheckedBox(OligosaccharideCheckedListBox);
            SelectAllItems_CheckedBox(FucoseCheckedListBox);
        }

        //set up check common buttom
        private void MostCommonButton_Click(object sender, EventArgs e)
        {
            //hexnac
            for (int i = 0; i < HexNAcCheckedListBox.Items.Count; i++)
            {
                if (HexNAcCheckedListBox.Items[i].ToString().Contains("126."))
                {
                    HexNAcCheckedListBox.SetItemChecked(i, true);
                }
                if (HexNAcCheckedListBox.Items[i].ToString().Contains("138."))
                {
                    HexNAcCheckedListBox.SetItemChecked(i, true);
                }
                if (HexNAcCheckedListBox.Items[i].ToString().Contains("144."))
                {
                    HexNAcCheckedListBox.SetItemChecked(i, true);
                }
                if (HexNAcCheckedListBox.Items[i].ToString().Contains("168."))
                {
                    HexNAcCheckedListBox.SetItemChecked(i, true);
                }
                if (HexNAcCheckedListBox.Items[i].ToString().Contains("186."))
                {
                    HexNAcCheckedListBox.SetItemChecked(i, true);
                }
                if (HexNAcCheckedListBox.Items[i].ToString().Contains("204."))
                {
                    HexNAcCheckedListBox.SetItemChecked(i, true);
                }
            }

            //hexose
            for (int i = 0; i < HexCheckedListBox.Items.Count; i++)
            {
                if (HexCheckedListBox.Items[i].ToString().Contains("163."))
                {
                    HexCheckedListBox.SetItemChecked(i, true);
                }
            }

            //sialic
            for (int i = 0; i < SialicAcidCheckedListBox.Items.Count; i++)
            {
                if (SialicAcidCheckedListBox.Items[i].ToString().Contains("274."))
                {
                    SialicAcidCheckedListBox.SetItemChecked(i, true);
                }
                if (SialicAcidCheckedListBox.Items[i].ToString().Contains("292."))
                {
                    SialicAcidCheckedListBox.SetItemChecked(i, true);
                }
                if (SialicAcidCheckedListBox.Items[i].ToString().Contains("290."))
                {
                    SialicAcidCheckedListBox.SetItemChecked(i, true);
                }
                if (SialicAcidCheckedListBox.Items[i].ToString().Contains("308."))
                {
                    SialicAcidCheckedListBox.SetItemChecked(i, true);
                }
            }

            //fucose
            for (int i = 0; i < FucoseCheckedListBox.Items.Count; i++)
            {
                if (FucoseCheckedListBox.Items[i].ToString().Contains("512."))
                {
                    FucoseCheckedListBox.SetItemChecked(i, true);
                }
            }

            //oligo
            for (int i = 0; i < OligosaccharideCheckedListBox.Items.Count; i++)
            {
                if (OligosaccharideCheckedListBox.Items[i].ToString().Contains("366."))
                {
                    OligosaccharideCheckedListBox.SetItemChecked(i, true);
                }
                if (OligosaccharideCheckedListBox.Items[i].ToString().Contains("657."))
                {
                    OligosaccharideCheckedListBox.SetItemChecked(i, true);
                }
                if (OligosaccharideCheckedListBox.Items[i].ToString().Contains("673."))
                {
                    OligosaccharideCheckedListBox.SetItemChecked(i, true);
                }
                if (OligosaccharideCheckedListBox.Items[i].ToString().Contains("893."))
                {
                    OligosaccharideCheckedListBox.SetItemChecked(i, true);
                }
            }

            //M6P
            for (int i = 0; i < M6PCheckedListBox.Items.Count; i++)
            {
                if (M6PCheckedListBox.Items[i].ToString().Contains("243."))
                {
                    M6PCheckedListBox.SetItemChecked(i, true);
                }
            }
        }

        //set up clear all button
        private void ClearButton_Click(object sender, EventArgs e)
        {
            while (HexNAcCheckedListBox.CheckedIndices.Count > 0)
                HexNAcCheckedListBox.SetItemChecked(HexNAcCheckedListBox.CheckedIndices[0], false);

            while (HexCheckedListBox.CheckedIndices.Count > 0)
                HexCheckedListBox.SetItemChecked(HexCheckedListBox.CheckedIndices[0], false);

            while (SialicAcidCheckedListBox.CheckedIndices.Count > 0)
                SialicAcidCheckedListBox.SetItemChecked(SialicAcidCheckedListBox.CheckedIndices[0], false);

            while (M6PCheckedListBox.CheckedIndices.Count > 0)
                M6PCheckedListBox.SetItemChecked(M6PCheckedListBox.CheckedIndices[0], false);

            while (OligosaccharideCheckedListBox.CheckedIndices.Count > 0)
                OligosaccharideCheckedListBox.SetItemChecked(OligosaccharideCheckedListBox.CheckedIndices[0], false);

            while (FucoseCheckedListBox.CheckedIndices.Count > 0)
                FucoseCheckedListBox.SetItemChecked(FucoseCheckedListBox.CheckedIndices[0], false);

            HexNAcCheckedListBox.ClearSelected();
            HexCheckedListBox.ClearSelected();
            SialicAcidCheckedListBox.ClearSelected();
            M6PCheckedListBox.ClearSelected();
            OligosaccharideCheckedListBox.ClearSelected();
            FucoseCheckedListBox.ClearSelected();

        }

        //uncheck specific boxes
        private void OligosaccharideCheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            OligosaccharideCheckedListBox.ClearSelected();
        }

        private void HexNAcCheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            HexNAcCheckedListBox.ClearSelected();
        }

        private void HexCheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            HexCheckedListBox.ClearSelected();
        }

        private void SialicAcidCheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            SialicAcidCheckedListBox.ClearSelected();
        }

        private void M6PCheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            M6PCheckedListBox.ClearSelected();
        }

        private void FucoseCheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            FucoseCheckedListBox.ClearSelected();
        }

        //set up check all buttons for specific types
        private void CheckAll_HexNAc_Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(HexNAcCheckedListBox);
        }

        private void CheckAll_Hex_Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(HexCheckedListBox);
        }

        private void CheckAll_Sialic_Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(SialicAcidCheckedListBox);
        }

        private void CheckAll_M6P_Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(M6PCheckedListBox);
        }

        private void CheckAll_Oligo_Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(OligosaccharideCheckedListBox);
        }

        private void CheckAll_Fucose_Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(FucoseCheckedListBox);
        }

        private void SetNegativeMode()
        {
            HexNAcCheckedListBox.BackColor = alternateBackColor;
            HexCheckedListBox.BackColor = alternateBackColor;
            M6PCheckedListBox.BackColor = alternateBackColor;
            FucoseCheckedListBox.BackColor = alternateBackColor;
            SialicAcidCheckedListBox.BackColor = alternateBackColor;
            OligosaccharideCheckedListBox.BackColor = alternateBackColor;

            HexNAcCheckedListBox.Items.Clear();
            HexCheckedListBox.Items.Clear();
            M6PCheckedListBox.Items.Clear();
            FucoseCheckedListBox.Items.Clear();
            SialicAcidCheckedListBox.Items.Clear();
            OligosaccharideCheckedListBox.Items.Clear();

            HexNAcCheckedListBox.Items.AddRange(HexNAcNeg);
            HexCheckedListBox.Items.AddRange(HexNeg);
            M6PCheckedListBox.Items.AddRange(ManNeg);
            FucoseCheckedListBox.Items.AddRange(FucoseNeg);
            SialicAcidCheckedListBox.Items.AddRange(SialicNeg);
            OligosaccharideCheckedListBox.Items.AddRange(OligoNeg);
        }

        private void SetPositiveMode()
        {
            HexNAcCheckedListBox.BackColor = normalBackColor;
            HexCheckedListBox.BackColor = normalBackColor;
            M6PCheckedListBox.BackColor = normalBackColor;
            FucoseCheckedListBox.BackColor = normalBackColor;
            SialicAcidCheckedListBox.BackColor = normalBackColor;
            OligosaccharideCheckedListBox.BackColor = normalBackColor;

            HexNAcCheckedListBox.Items.Clear();
            HexCheckedListBox.Items.Clear();
            M6PCheckedListBox.Items.Clear();
            FucoseCheckedListBox.Items.Clear();
            SialicAcidCheckedListBox.Items.Clear();
            OligosaccharideCheckedListBox.Items.Clear();

            HexNAcCheckedListBox.Items.AddRange(HexNAcPos);
            HexCheckedListBox.Items.AddRange(HexPos);
            M6PCheckedListBox.Items.AddRange(ManPos);
            FucoseCheckedListBox.Items.AddRange(FucosePos);
            SialicAcidCheckedListBox.Items.AddRange(SialicPos);
            OligosaccharideCheckedListBox.Items.AddRange(OligoPos);
        }

        private void polarityCB_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox toggle = sender as CheckBox;

            if (toggle.Checked)
            {
                SetNegativeMode();
            }
            else
            {
                SetPositiveMode();
            }
        }
    }
}
