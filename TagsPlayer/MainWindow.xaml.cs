using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using System;
using TagsPlayer.ViewModel;
using System.Windows.Threading;
using System.Windows.Input;
using System.Configuration;
using System.Linq;
using TagsPlayer.Utils;
using TagsPlayer.Model;
using System.Diagnostics;

namespace TagsPlayer
{
    public partial class MainWindow : Window
    {

        private List<MusicInfo> musicList = new();
        private readonly WaveOutEvent waveOut = new();
        private MetaInfoPanelModel metaInfoPanelModel;
        private DispatcherTimer timer;
        private WaveStream waveStream;
        private readonly Dictionary<string, List<MusicInfo>> tagsMap = new();
        private MusicInfo playedMusic;
        private int playedMusicIndex;

        public MainWindow() {
            InitializeComponent();
            waveOut.PlaybackStopped += WaveOut_PlaybackStopped;
            MusicListView.ItemsSource = this.musicList;
            metaInfoPanelModel = new MetaInfoPanelModel {
                TagsList = new(),
                AuthorTagsList = new()
            };
            DataContext = metaInfoPanelModel;
            ScanDir(ConfigurationManager.AppSettings["WorkDir"]);
            // 定时器，用于更新UI的播放位置
            timer = new DispatcherTimer {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void WaveOut_PlaybackStopped(object sender, StoppedEventArgs e) {
            PlayNextMusic();
        }
        private void PlayNextButton_Click(object sender, RoutedEventArgs e) {
            waveOut.Stop();
        }
        private void PlayNextMusic() {
            if (HasNextMusic()) {
                int nextIndex = GetNextMusicIndex();
                this.playedMusicIndex = nextIndex;
                PlayMusic(musicList[nextIndex]);
            }

            bool HasNextMusic() {
                return this.playedMusicIndex + 1 < musicList.Count;
            }

            int GetNextMusicIndex() {
                if (LoopModeButton.Kind == MahApps.Metro.IconPacks.PackIconRemixIconKind.RestartLine) {
                    return this.playedMusicIndex + 1;
                }
                else if (LoopModeButton.Kind == MahApps.Metro.IconPacks.PackIconRemixIconKind.RepeatOneFill) {
                    return this.playedMusicIndex;
                }
                else {
                    return new Random().Next(musicList.Count);
                }
            }
        }

        /*菜单：保存*/
        private void SaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveMetaData();
        }


        /*双击歌曲*/
        private void MusicList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            var selectedMusic = (MusicInfo)MusicListView.SelectedItem;
            if (selectedMusic != null) {
                playedMusicIndex = MusicListView.SelectedIndex;
                waveOut.PlaybackStopped -= WaveOut_PlaybackStopped;
                PlayMusic(selectedMusic);
                waveOut.PlaybackStopped += WaveOut_PlaybackStopped;
            }
        }

        //直接将歌曲文件读入内存为memoryStream，使用自定义的MusicReader来获取waveStream，是从Naudio里魔改了一些东西，但是我不记得了
        private void PlayMusic(MusicInfo musicInfo) {
            waveOut?.Dispose();
            try {
                //直接将歌曲文件读入内存，后续不影响文件修改
                byte[]? audioData = System.IO.File.ReadAllBytes(musicInfo.File.Name);
                MemoryStream? memoryStream = new(audioData);
                this.waveStream = new MyMusicReader(memoryStream);
                this.waveOut.Init(waveStream);
                this.waveOut.Play();
                this.metaInfoPanelModel.AudioDuration = waveStream.TotalTime.TotalSeconds;
                this.playedMusic = musicInfo;
                this.PlayPauseButton.Kind = MahApps.Metro.IconPacks.PackIconFontAwesomeKind.PauseSolid;
                SeekPlayedMuisc();
                //this.TotalTimeLabel.Content = waveStream.TotalTime.ToString(@"mm\:ss");
                LyricTextBlock.Text = musicInfo.File.Tag.Lyrics;
                if (musicInfo.File.Tag.Pictures.Length > 0)
                {
                    var cover = new MemoryStream(musicInfo.File.Tag.Pictures?[0]?.Data.Data);
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        //使用原生的MusicReader来获取waveStream
        private void PlayMusicRaw(MusicInfo musicInfo)
        {
            waveOut?.Dispose();
            try
            {
                this.waveStream = new MediaFoundationReader(musicInfo.File.Name);
                this.waveOut.Init(waveStream);
                this.waveOut.Play();
                this.metaInfoPanelModel.AudioDuration = waveStream.TotalTime.TotalSeconds;
                this.playedMusic = musicInfo;
                this.PlayPauseButton.Kind = MahApps.Metro.IconPacks.PackIconFontAwesomeKind.PauseSolid;
                //this.TotalTimeLabel.Content = waveStream.TotalTime.ToString(@"mm\:ss");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Timer_Tick(object sender, EventArgs e) {
            if (waveOut != null && waveOut.PlaybackState == PlaybackState.Playing) {
                this.metaInfoPanelModel.CurrentPosition = waveStream.CurrentTime.TotalSeconds;
                //NowTimeLabel.Content = waveStream.CurrentTime.ToString(@"mm\:ss");
            }
        }

        /*监听键盘：ctrl + s*/
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SaveMetaData();
            }
        }

        private void SaveMetaData() {
            var musicInfos = this.MusicListView.SelectedItems.Cast<MusicInfo>().ToList();
            try {
                if (musicInfos != null && musicInfos.Count > 0)
                {
                    foreach (MusicInfo music in musicInfos)
                    {
                        music.UpdateTitle(TitleComboBox.Text?.ToString());
                        music.UpdateArtists(ArtistComboBox.Text?.ToString());
                        music.UpdateAlbum(AlbumComboBox.Text?.ToString());
                        music.UpdateComment(CommentComboBox.Text?.ToString());
                        music.File.Save();
                    }
                    this.MusicListView.Items.Refresh();
                }
            } catch(Exception ex) 
            {

                MessageBox.Show("保存元信息时发生错误: " + ex.Message);
            }
        }

        private void FileName_MouseUp(object sender, MouseEventArgs e) {
            string text = FileNameTextBlock.Text;
            Clipboard.SetText(text);
        }

        /*开始暂停按钮*/
        private void PlayPauseButton_Click(object sender, RoutedEventArgs e) {
            if (waveOut.PlaybackState == PlaybackState.Playing) {
                this.waveOut.Pause();
                PlayPauseButton.Kind = MahApps.Metro.IconPacks.PackIconFontAwesomeKind.PlaySolid;
            }
            else if (waveOut.PlaybackState == PlaybackState.Paused) {
                this.waveOut.Play();
                PlayPauseButton.Kind = MahApps.Metro.IconPacks.PackIconFontAwesomeKind.PauseSolid;
            }
        }

        /*拖动进度条*/
        private void ProgressSlider_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            Slider slider = (Slider)sender;
            Point position = e.GetPosition(slider);
            double clickedValue = position.X / slider.ActualWidth * (slider.Maximum - slider.Minimum) + slider.Minimum;
            slider.Value = clickedValue;
        }

        private void ProgressSlider_PreviewMouseMove(object sender, MouseEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                Slider slider = (Slider)sender;
                Point position = e.GetPosition(slider);
                double clickedValue = position.X / slider.ActualWidth * (slider.Maximum - slider.Minimum) + slider.Minimum;
                slider.Value = clickedValue;
            }
        }

        private void ProgressSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            Slider slider = (Slider)sender;
            Point position = e.GetPosition(slider);
            double clickedValue = position.X / slider.ActualWidth * (slider.Maximum - slider.Minimum) + slider.Minimum;
            if (waveStream!= null && waveStream.CanSeek) {
                slider.Value = clickedValue;
                waveStream.Seek((long)(clickedValue * waveStream.WaveFormat.AverageBytesPerSecond), SeekOrigin.Begin);
            }

        }


        /*跳转到正在播放的歌曲按钮*/
        private void SeekButton_Click(object sender, RoutedEventArgs e) {
            SeekPlayedMuisc();
        }

        private void SeekPlayedMuisc() {
            if (this.playedMusic != null) {
                this.MusicListView.ScrollIntoView(playedMusic);
                this.MusicListView.SelectedIndex = playedMusicIndex;
            }
        }

        /*循环按钮*/
        private void LoopModeButton_Click(object sender, RoutedEventArgs e) {
            this.LoopModeButton.Kind = Const.GetNextLoopMode(this.LoopModeButton.Kind);
        }

        /*过滤按钮
          这里的过滤不影响实际播放列表，下一曲/上一曲的逻辑依然实际实际播放列表
          过滤后的播放列表只是一个虚拟的视图，仅用于展示
          当过滤文本框为空时，展示原始的实际播放列表
         */
        private void FilterButton_Click(Object sender, RoutedEventArgs e)
        {
            FilterList(FilterTextBox.Text);
        }

        private void FilterTextChanged(object sender, TextChangedEventArgs e)
        {
            FilterList(FilterTextBox.Text);
        }

        private void FilterList(string s)
        {
            if (s.Trim().Length > 0)
            {
                //Tag的属性可能为空，必须做空检查
                MusicListView.ItemsSource = musicList.Where(x =>
                    (x.Artists?.ToLower().Contains(s.ToLower()) ?? false)
                    || (x.File?.Tag?.Comment?.ToLower().Contains(s.ToLower()) ?? false)
                    || (x.File?.Tag?.Title?.ToLower().Contains(s.ToLower()) ?? false)
                    || (x.File?.Tag?.Album?.ToLower().Contains(s.ToLower()) ?? false)
                );
            }
            else
            {
                MusicListView.ItemsSource = musicList;
            }
        }


        /*右键菜单：AddTag*/
        private void AddTagButton_Click(object sender, RoutedEventArgs e) {
            string userInput = Microsoft.VisualBasic.Interaction.InputBox("请输入你需要的Tag", "Tag", "");
            foreach (MusicInfo musicInfo in MusicListView.SelectedItems.Cast<MusicInfo>().ToList()) {
                if (musicInfo.File.Tag.Comment == null)
                {
                    musicInfo.File.Tag.Comment = userInput;
                }
                else if (musicInfo.File.Tag.Comment != null && !musicInfo.File.Tag.Comment.Contains(userInput)) 
                {
                    musicInfo.File.Tag.Comment += ("," + userInput);
                }
                musicInfo.File.Save();
                PutKsV(this.metaInfoPanelModel.TagsList, userInput.Split(","), musicInfo);
                this.TagsItem.Items.Refresh();
                this.MusicListView.Items.Refresh();
            }
        }


        /**
         * 播放列表选中改变事件
         * 
         */
        private void MusicList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var selectedMusics = MusicListView.SelectedItems.Cast<MusicInfo>().ToList();
            if (selectedMusics.Count <= 0) {
                return;
            }

            var titles = selectedMusics.Select(x => x.File.Tag.Title).ToList();
            var albums = selectedMusics.Select(x => x.File.Tag.Album).ToList();
            var comments = selectedMusics.Select(x => x.File.Tag.Comment).ToList();
            var artists = selectedMusics.Select(x => x.Artists).ToList();
            var fileName = selectedMusics[0].File.Name.Split("\\")[^1];
            if (selectedMusics.Count == 1) {
                TitleComboBox.Text = titles[0];
                ArtistComboBox.Text = artists[0];
                AlbumComboBox.Text = albums[0];
                CommentComboBox.Text = comments[0];
                FileNameTextBlock.Text = fileName;
                if (selectedMusics[0].File.Tag.Pictures.Length > 0) {
                    CoverImage.Source = TagsUtil.LoadImageFromBytes(selectedMusics[0].File.Tag.Pictures?[0]?.Data.Data);
                }
                else {
                    CoverImage.Source = null;
                }
            }

            else if (selectedMusics?.Count > 1) {
                var keepAndBlank = new List<string>() { "[keep]", "[blank]" };
                titles.InsertRange(0, keepAndBlank);
                albums.InsertRange(0, keepAndBlank);
                artists.InsertRange(0, keepAndBlank);
                comments.InsertRange(0, keepAndBlank);
                fileName = null;
                TitleComboBox.SelectedItem = "[keep]";
                ArtistComboBox.SelectedItem = "[keep]";
                AlbumComboBox.SelectedItem = "[keep]";
                CommentComboBox.SelectedItem = "[keep]";
                CoverImage.Source = null;
            }
            this.metaInfoPanelModel.SelectedTitles = titles;
            this.metaInfoPanelModel.SelectedAlbums = albums;
            this.metaInfoPanelModel.SelectedComments = comments;
            this.metaInfoPanelModel.SelectedArtists = artists;
            this.metaInfoPanelModel.SelectedFileName = fileName;
        }

        /*菜单：改变/扫描工作目录
         此举会初始化Tag表*/
        public void ChangeDirMenuItem_Click(object sender, RoutedEventArgs e) {

            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["WorkDir"].Value = dialog.SelectedPath;
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                this.musicList.Clear();
                ScanDir(dialog.SelectedPath);
            }
        }

        private void ScanDir(string filePath) {
            string[] audioExtensions = { "*.mp3", "*.flac", "*.ape", "*.aac","*.m4a" };
            var musicFiles = new List<string>();
            metaInfoPanelModel.AuthorTagsList.Clear();
            metaInfoPanelModel.TagsList.Clear();
            foreach (var extension in audioExtensions) {
                if (filePath != null && !"".Equals(filePath)) {
                    try {
                        musicFiles.AddRange(System.IO.Directory.GetFiles(filePath, extension, System.IO.SearchOption.AllDirectories));
                    }
                    catch { }
                }
            }

            foreach (string file in musicFiles) {
                try {
                    var musicInfo = new MusicInfo {
                        File = TagLib.File.Create(file)
                    };
                    musicList.Add(musicInfo);
                    AnalyzeMusicTag(musicInfo);
                }
                catch (Exception ex) {
                    Console.WriteLine("Error reading file " + file + ": " + ex.Message);
                }
            }
            metaInfoPanelModel.TagsList.Sort(new SizeComparer());
            metaInfoPanelModel.AuthorTagsList.Sort(new SizeComparer());
            TagsItem.Items.Refresh();
            MusicListView.Items.Refresh();
        }

        private void AnalyzeMusicTag(MusicInfo musicInfo) {
            PutKV(this.metaInfoPanelModel.TagsList,"all", musicInfo);
            string[] performers = musicInfo.File.Tag.Performers;
            PutKsV(this.metaInfoPanelModel.AuthorTagsList, performers, musicInfo);
            string[] comment = musicInfo.File.Tag.Comment?.Split(",");
            PutKsV(this.metaInfoPanelModel.TagsList, comment, musicInfo);
            //PutKV(this.panelModel.TagsList, musicInfo.File.Tag.Album, musicInfo);
        }


        private void PutKsV(List<KeyValuePair<string, List<MusicInfo>>> tagsList, string[] performers, MusicInfo musicInfo)
        {
            if (performers == null)
            {
                return;
            }
            foreach (string performer in performers)
            {
                PutKV(tagsList, performer, musicInfo);
            }
        }

        private void PutKV(List<KeyValuePair<string, List<MusicInfo>>> tagList, string key, MusicInfo value)
        {
            if (key == null || key.Length >= 30)
            {
                return;
            }
            else if (this.tagsMap.ContainsKey(key))
            {
                var musicList = this.tagsMap[key];
                musicList.Add(value);
            }
            else
            {
                var list = new List<MusicInfo>() {
                    {value}
                };
                this.tagsMap[key] = list;
                tagList.Add(new KeyValuePair<string, List<MusicInfo>>(key, list));
            }
        }

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (sender is TextBlock textBlock && textBlock.DataContext is KeyValuePair<string, List<MusicInfo>> item) {
                DragDrop.DoDragDrop(textBlock, item, DragDropEffects.Copy);
            }
        }


        /*拖动事件结束：拖动tags进播放列表*/
        private void MusicList_Drop(object sender, DragEventArgs e) {
            if (e.Data.GetData(typeof(KeyValuePair<string, List<MusicInfo>>)) is KeyValuePair<string, List<MusicInfo>> droppedTagItem) {
                foreach (MusicInfo musicInfo in droppedTagItem.Value) {
                    musicList.Add(musicInfo);
                }
                MusicListView.Items.Refresh();
            }

        }

        /*监听键盘事件：监听delete按钮*/
        private void MusicList_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Delete) {
                var selectItems = MusicListView.SelectedItems;
                var start = MusicListView.SelectedIndex;
                if (selectItems.Count + start > musicList.Count) {
                    musicList.RemoveRange(start, selectItems.Count);
                } else {
                    musicList.RemoveRange(start, musicList.Count - start);
                }
                MusicListView.Items.Refresh();
            }
        }



        /*右键菜单：删除文件*/
        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            var filePath = MusicListView.SelectedItems.Cast<MusicInfo>().ToList()?[0]?.File.Name;
            if (File.Exists(filePath))
            {
                MessageBoxResult result = MessageBox.Show("您确定要删除这个文件吗？", "删除确认", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        File.Delete(filePath);
                        musicList.RemoveAt(MusicListView.SelectedIndex);
                        MusicListView.Items.Refresh();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("删除文件时发生错误: " + ex.Message);
                    }
                }
            }
        }

        /*右键菜单：选择文件夹*/
        private void MenuItemOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var filePath = MusicListView.SelectedItems.Cast<MusicInfo>().ToList()?[0]?.File.Name;
            if (Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                try
                {
                    Process.Start("explorer.exe", "/select, \"" + filePath + "\"");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("打开文件夹时发生错误: " + ex.Message);
                }
            }
        }

        /*右键菜单：清空*/
        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            musicList.Clear();
            MusicListView.Items.Refresh();
        }

    }



}
