using TagsPlayer.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TagsPlayer.Controls
{
    /// <summary>
    /// MusicListView.xaml 的交互逻辑
    /// </summary>
    public partial class MusicListView : UserControl
    {
        public MusicListView()
        {
            InitializeComponent();
        }

        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                var selectedItem = menuItem.DataContext as MusicInfo;

                if (selectedItem != null)
                {
                    var result = MessageBox.Show("确定删除 " + selectedItem.File.Name + " 吗？", "删除确认", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            File.Delete(selectedItem.File.Name);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("删除文件时发生错误: " + ex.Message);
                        }
                    }
                }
            }
        }

        private void MenuItemOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                var selectedItem = menuItem.DataContext as MusicInfo;
                if (selectedItem != null)
                {
                    var directoryPath = System.IO.Path.GetDirectoryName(selectedItem.File.Name);
                    if (Directory.Exists(directoryPath))
                    {
                        try
                        {
                            Process.Start("explorer.exe", directoryPath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("打开文件夹时发生错误: " + ex.Message);
                        }
                    }
                }
            }
        }
    }
}
