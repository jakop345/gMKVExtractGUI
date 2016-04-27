using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace gMKVToolNix.Controls
{
    public class gDataGridView:DataGridView
    {
        public gDataGridView()
            : base()
        {
            this.RowHeadersVisible = false;
            this.AllowUserToAddRows = false;
            this.AllowUserToDeleteRows = false;
            this.AllowUserToResizeRows = false;
            this.EditMode = DataGridViewEditMode.EditProgrammatically;
            this.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            this.DoubleBuffered = true;
        }
        
    }
}
