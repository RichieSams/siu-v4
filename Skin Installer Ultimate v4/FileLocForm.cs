using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SkinInstaller
{
    public partial class FileLocForm : Form
    {
        public FileLocForm(List<String> options)
        {
            InitializeComponent();

            foreach (String file in options)
                possibleLocs.Items.Add(file);
            possibleLocs.Sorted = true;
        }
    }
}
