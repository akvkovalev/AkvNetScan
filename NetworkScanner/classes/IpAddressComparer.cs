using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AkvNetScan.classes
{
    public class IpAddressComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            ListViewItem itemX = (ListViewItem)x;
            ListViewItem itemY = (ListViewItem)y;

            IPAddress ipX = IPAddress.Parse(itemX.SubItems[0].Text);
            IPAddress ipY = IPAddress.Parse(itemY.SubItems[0].Text);

            byte[] bytesX = ipX.GetAddressBytes();
            byte[] bytesY = ipY.GetAddressBytes();

            for (int i = 0; i < bytesX.Length; i++)
            {
                if (bytesX[i] != bytesY[i])
                    return bytesX[i] - bytesY[i];
            }

            return 0;
        }
    }
}
