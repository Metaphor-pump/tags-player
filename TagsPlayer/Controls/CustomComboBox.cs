using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace TagsPlayer.Controls
{
    public class CustomComboBox : ComboBox
    {
        public CustomComboBox()
        {
            this.IsEditable = true;
            this.Margin = new System.Windows.Thickness(10, 0, 10, 0);
            this.BorderThickness = new System.Windows.Thickness(0.5);
        }
    }
}
