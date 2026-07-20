using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlyCounter
{
    public partial class Form1
    {

        private readonly string[] HexNAcPos = { "84.0444, HexNAc - C2H8O4", "126.055, HexNAc - C2H6O3", "138.055, HexNAc - CH6O3", "144.0655, HexNAc - C2H4O2", "168.0655, HexNAc - 2H2O", "186.0761, HexNAc - H2O", "204.0867, HexNAc" };
        private readonly string[] HexNAcNeg = { "154.0510, HexNAc - CH2O (B,Z)", "160.0610, HexNAc - C2H2O (Z)", "162.0767, HexNAc - C2H4O2", "166.0511, HexNAc - H2O", "178.0710, HexNAc - C2H2O (Y)", "184.0616, HexNAc (B,Z)", "202.0716, HexNAc, (B)", "204.0878, HexNAc (Z)", "220.0816, HexNAc (C)", "222.0978, HexNAc, (Y)" };

        private readonly string[] HexPos = { "85.0284, Hex - C2H6O3", "97.0284, Hex - CH6O3", "127.0390, Hex - 2H2O", "145.0495, Hex - H2O", "163.0601, Hex" };
        private readonly string[] HexNeg = { "161.0450, Hex (B)", "179.0550, Hex (C)" };

        private readonly string[] ManPos = { "243.0264, Man-P", "405.0798, Man2-P" };
        private readonly string[] ManNeg = { "241.0113, Man-P", "403.0647, Man2-P" };

        private readonly string[] SialicPos = { "214.07099, NeuAc - C2H6O3", "230.06589, NeuGc - C2H6O3", "232.08155, NeuAc - C2H4O2","248.07645, NeuGc - C2H4O2", "256.08155, NeuAc - H2O2", "272.07645, NeuGc - H2O2", "274.0921, NeuAc-H2O", "292.1027, NeuAc", "316.103, NeuAc[Ac] - H2O", "334.113, NeuAc[Ac]",
            "290.0870, NeuGc - H2O", "308.0976, NeuGc", "332.098, NeuGc[Ac] - H2O", "350.1081, NeuGc[Ac]" };
        private readonly string[] SialicNeg = { "290.0876, NeuAc (B)", "308.0976, NeuAc (C)" };

        private readonly string[] FucosePos = { "350.1446, HexNAc-dHex", "512.1974, HexNAc-Hex-dHex (LeX/A)", "674.2502, HexNAc-Hex2-dHex", "803.2928, HexNAc-Hex-dHex-NeuAc (sLeX/A)", "819.2908, HexNAc-Hex-dHex-NeuGc", "877.3296, HexNAc2-Hex2-dHex (diLacNAc-Fuc)" };
        private readonly string[] FucoseNeg = { "163.0601, dHex (C)", "165.0762, dHex (Y)", "350.1457, HexNAc-dHex (Z)", "368.1557, HexNAc-dHex (Y)", "307.1029, Hex-dHex (B)", "325.1129, Hex-dHex (C)", "488.1979, HexNAc-Hex-dHex", "510.1823, HexNAc-Hex-dHex (B)",
            "553.2251, HexNAc2-dHex (Y,Z)", "697.2678, HexNAc2-Hex-dHex (Z,Z)", "715.2778, HexNAc2-Hex-dHex (Z)", "733.2879, HexNAc2-Hex-dHex (Y)", "895.3407, HexNAc2-Hex2-dHex (Y,Y)", "1057.3935, HexNAc2-Hex3 (Y,Y)", "1080.4101, HexNAc3-Hex2-dHex (Z)", "1098.4201, HexNAc3-Hex2-dHex (Y)" };

        private readonly string[] OligoPos = { "325.1129, Hex2", "366.1395, HexNAc-Hex", "407.1660, HexNAc2", "454.1555, Hex-NeuAc", "470.1503, Hex-NeuGc", "495.1821, HexNAc-NeuAc", "511.1769, HexNAc-NeuGc", "528.1923, HexNAc-Hex2",
            "537.1927, HexNAc-NeuAc[Ac]", "553.1875, HexNAc-NeuGc[Ac]", "569.2188, HexNAc2-Hex", "657.2349, HexNAc-Hex-NeuAc", "673.2297, HexNAc-Hex-NeuGc", "690.2451, HexNAc-Hex3", "731.2717, HexNAc2-Hex2 (diLacNAc)",
            "819.2877, HexNAc-Hex2-NeuAc", "835.2825, HexNAc-Hex2-NeuGc", "860.3143, HexNAc2-Hex-NeuAc", "876.3091, HexNAc2-Hex-NeuGc", "893.3245, HexNAc2-Hex3", "948.3303, HexNAc-Hex-NeuAc2", "964.3251, HexNAc-Hex-NeuGc2",
            "1022.3671, HexNAc2-Hex2-NeuAc1", "1038.3619, HexNAc2-Hex2-NeuGc1", "1313.4625, HexNAc2-Hex2-NeuAc2", "1329.4573, HexNAc2-Hex2-NeuGc2" };
        private readonly string[] OligoNeg = { "364.1244, HexNAc-Hex (B)", "366.1406, HexNAc-Hex (Z)", "382.1344, HexNAc-Hex (C)", "384.1506, HexNAc-Hex (Y)", "389.1572, HexNAc2 (Z,Z)", "407.1672, HexNAc2 (Z)",
            "425.1772, HexNAc2 (Y)", "544.4, HexNAc-Hex2 (C,Y)", "551.2100, HexNAc2-Hex (Z,Z)", "569.2200, HexNAc2-Hex (Z)", "587.2300, HexNAc2-Hex (Y)", "675.2460, HexNAc-Hex-NeuAc (Y)", "731.2728, HexNAc2-Hex2 (Z)",
            "749.2828, HexNAc2-Hex2 (Y)", "829.2396, HexNAc2-Hex2 (Y)", "873.2994, HexNAc2-Hex3 (B,Z)", "934.3522, HexNAc3-Hex2 (Y,Z)", "952.3622, HexNAc3-Hex2 (Y,Y)", "1096.4050, HexNAc3-Hex3 (Y,Z)", "1114.415, HexNAc3-Hex3 (Y)",
            "1120.3350, HexNAc2-Hex2-NeuAc (Y)", "1299.4844, HexNAc3-Hex3 (Z)" };

        private readonly string[] ImmoniumPos = { "147.1128, Lysine", "175.119, Arginine" };
        private readonly string[] ImmoniumNeg = { "145.0982, Lysine", "173.1044, Arginine" };
    }
}
