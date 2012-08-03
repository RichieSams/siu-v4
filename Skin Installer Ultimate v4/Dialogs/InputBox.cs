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
    public partial class InputBox : Form
    {
        public InputBox(String title, String prompt)
        {
            InitializeComponent();

            this.Text = title;
            lbl_prompt.Text = prompt;
        }
    }
}
