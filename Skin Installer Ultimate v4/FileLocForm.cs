using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace SkinInstaller
{
    public partial class FileLocForm : Form
    {
        public FileLocForm(FileInfo file, List<String> options)
        {
            InitializeComponent();

            this.fileName.Text = file.Name;
            this.origLoc.Text = file.FullName;

            foreach (String potentialFile in options)
                possibleLocs.Items.Add(potentialFile);
            possibleLocs.Sorted = true;
        }
    }
}
