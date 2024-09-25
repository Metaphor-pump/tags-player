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

        private List<MusicInfo> MusicList = new();
        private List<MusicInfo> OriginMusicList = new();
        private readonly WaveOutEvent waveOut = new();
        private PanelViewModel panelModel;
        private DispatcherTimer timer;
        private WaveStream waveStream;
        private readonly Dictionary<string, List<MusicInfo>> tagsMap = new();
        private MusicInfo playedMusic;
        private int playedMusicIndex;

        public MainWindow() {
            InitializeComponent();
            waveOut.PlaybackStopped += WaveOut_PlaybackStopped;
            MusicListView.ItemsSource = this.MusicList;
            panelModel = new PanelViewModel {
                TagsList = new(),
                AuthorTagsList = new()
            };
            DataContext = panelModel;
            ScanDir(ConfigurationManager.AppSettings["WorkDir"]);

            // 定时器，用于更新播放位置
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
                PlayMusic((MusicInfo)MusicListView.Items[nextIndex]);
            }

            bool HasNextMusic() {
                return this.playedMusicIndex + 1 < MusicListView.Items.Count;
            }

            int GetNextMusicIndex() {
                if (LoopModeButton.Kind == MahApps.Metro.IconPacks.PackIconRemixIconKind.RestartLine) {
                    return this.playedMusicIndex + 1;
                }
                else if (LoopModeButton.Kind == MahApps.Metro.IconPacks.PackIconRemixIconKind.RepeatOneFill) {
                    return this.playedMusicIndex;
                }
                else {
                    return new Random().Next(MusicListView.Items.Count);
                }
            }
        }

        private void MusicList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            var selectedMusic = (MusicInfo)MusicListView.SelectedItem;
            if (selectedMusic != null) {
                playedMusicIndex = MusicListView.SelectedIndex;
                waveOut.PlaybackStopped -= WaveOut_PlaybackStopped;
                PlayMusic(selectedMusic);
                waveOut.PlaybackStopped += WaveOut_PlaybackStopped;
            }
        }

        //使用自定义的MusicReader来获取waveStream，是从Naudio里魔改了一些东西，但是我不记得了。
        //Naudio是不支持打开多种格式的。
        private void PlayMusic(MusicInfo musicInfo) {
            waveOut?.Dispose();
            try {
                byte[]? audioData = System.IO.File.ReadAllBytes(musicInfo.File.Name);
                MemoryStream? memoryStream = new(audioData);
                this.waveStream = new MyMusicReader(memoryStream);
                this.waveOut.Init(waveStream);
                this.waveOut.Play();
                this.panelModel.AudioDuration = waveStream.TotalTime.TotalSeconds;
                this.playedMusic = musicInfo;
                this.PlayPauseButton.Kind = MahApps.Metro.IconPacks.PackIconFontAwesomeKind.PauseSolid;
                focusMuiscList();
                this.TotalTimeLabel.Content = waveStream.TotalTime.ToString(@"mm\:ss");
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
                this.panelModel.AudioDuration = waveStream.TotalTime.TotalSeconds;
                this.playedMusic = musicInfo;
                this.PlayPauseButton.Kind = MahApps.Metro.IconPacks.PackIconFontAwesomeKind.PauseSolid;
                this.TotalTimeLabel.Content = waveStream.TotalTime.ToString(@"mm\:ss");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Timer_Tick(object sender, EventArgs e) {
            if (waveOut != null && waveOut.PlaybackState == PlaybackState.Playing) {
                this.panelModel.CurrentPosition = waveStream.CurrentTime.TotalSeconds;
                NowTimeLabel.Content = waveStream.CurrentTime.ToString(@"mm\:ss");
            }
        }


        private void SaveButton_Click(object sender, RoutedEventArgs e) {
            SaveMetaData();
        }

        private void SaveMetaData() {
            var musicInfos = this.MusicListView.SelectedItems.Cast<MusicInfo>().ToList();
            if (musicInfos != null && musicInfos.Count > 0) {
                foreach (MusicInfo music in musicInfos) {
                    music.UpdateTitle(TitleComboBox.Text?.ToString());
                    music.UpdateArtists(ArtistComboBox.Text?.ToString());
                    music.UpdateAlbum(AlbumComboBox.Text?.ToString());
                    music.UpdateComment(CommentComboBox.Text?.ToString());
                    music.File.Save();
                }
                this.MusicListView.Items.Refresh();
            }
        }

        private void TestSave() {
            var music = this.MusicListView.ItemsSource.Cast<MusicInfo>().ToList()[0];
            music.UpdateTitle(TitleComboBox.Text?.ToString());
            music.UpdateArtists(ArtistComboBox.Text?.ToString());
            music.UpdateAlbum(AlbumComboBox.Text?.ToString());
            music.UpdateComment(CommentComboBox.Text?.ToString());
            music.File.Save();
            this.MusicListView.Items.Refresh();
        }

        private void FileName_MouseUp(object sender, MouseEventArgs e) {
            string text = FileNameTextBlock.Text;
            Clipboard.SetText(text);
        }

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


        private void FocusButton_Click(object sender, RoutedEventArgs e) {
            focusMuiscList();
        }

        private void focusMuiscList() {
            if (this.playedMusic != null) {
                this.MusicListView.ScrollIntoView(playedMusic);
                this.MusicListView.SelectedIndex = playedMusicIndex;
            }
        }

        private void LoopModeButton_Click(object sender, RoutedEventArgs e) {
            this.LoopModeButton.Kind = Const.GetNextLoopMode(this.LoopModeButton.Kind);
        }

        private void FilterButton_Click(Object sender, RoutedEventArgs e) {
        }

        private void AddTagButton_Click(object sender, RoutedEventArgs e) {
            string userInput = Microsoft.VisualBasic.Interaction.InputBox("请输入你需要的Tag", "Tag", "");
            foreach (MusicInfo musicInfo in MusicListView.SelectedItems.Cast<MusicInfo>().ToList()) {
                if (musicInfo.File.Tag.Comment != null) {
                    musicInfo.File.Tag.Comment += ("," + userInput);
                }
                else {
                    musicInfo.File.Tag.Comment = userInput;
                }
                musicInfo.File.Save();
                putKsV(this.panelModel.TagsList, userInput.Split(","), musicInfo);
                this.TagsItem.Items.Refresh();
                this.MusicListView.Items.Refresh();
            }
        }

        private void ClearList_Click(object sender, RoutedEventArgs e) { 
            MusicList.Clear();
            MusicListView.Items.Refresh();
        }
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
            this.panelModel.SelectedTitles = titles;
            this.panelModel.SelectedAlbums = albums;
            this.panelModel.SelectedComments = comments;
            this.panelModel.SelectedArtists = artists;
            this.panelModel.SelectedFileName = fileName;
        }

        public void ChangeDirMenuItem_Click(object sender, RoutedEventArgs e) {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["WorkDir"].Value = dialog.SelectedPath;
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                this.MusicList.Clear();
                ScanDir(dialog.SelectedPath);
            }
        }

        private void OpenDirectoryMenuItem_Click(object sender, RoutedEventArgs e) {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                ScanDir(dialog.SelectedPath);
            }
        }
        private void ScanDir(string filePath) {
            string[] audioExtensions = { "*.mp3", "*.flac", "*.ape", "*.aac","*.m4a" };
            var musicFiles = new List<string>();

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
                    var tfile = TagLib.File.Create(file);
                    var musicInfo = new MusicInfo {
                        File = tfile
                    };
                    MusicList.Add(musicInfo);
                    AnalyzeMusicTag(musicInfo);
                }
                catch (Exception ex) {
                    Console.WriteLine("Error reading file " + file + ": " + ex.Message);
                }
            }
            OriginMusicList = MusicList.ToList();
            panelModel.TagsList.Sort(new SizeComparer());
            panelModel.AuthorTagsList.Sort(new SizeComparer());
            TagsItem.Items.Refresh();
            MusicListView.Items.Refresh();
        }

        private void AnalyzeMusicTag(MusicInfo musicInfo) {
            string[] performers = musicInfo.File.Tag.Performers;
            putKsV(this.panelModel.AuthorTagsList, performers, musicInfo);
            string[] comment = musicInfo.File.Tag.Comment?.Split(",");
            putKsV(this.panelModel.TagsList, comment, musicInfo);
            putKV(this.panelModel.TagsList, musicInfo.File.Tag.Album, musicInfo);
        }

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (sender is TextBlock textBlock && textBlock.DataContext is KeyValuePair<string, List<MusicInfo>> item) {
                DragDrop.DoDragDrop(textBlock, item, DragDropEffects.Copy);
            }
        }

        private void MusicList_Drop(object sender, DragEventArgs e) {
            if (e.Data.GetData(typeof(KeyValuePair<string, List<MusicInfo>>)) is KeyValuePair<string, List<MusicInfo>> droppedTagItem) {
                foreach (MusicInfo musicInfo in droppedTagItem.Value) {
                    MusicList.Add(musicInfo);
                }
                MusicListView.Items.Refresh();
            }

        }

        private void MusicList_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Delete) {
                var selectItems = MusicListView.SelectedItems;
                var start = MusicListView.SelectedIndex;
                if (selectItems.Count + start > MusicList.Count) {
                    MusicList.RemoveRange(start, selectItems.Count);
                } else {
                    MusicList.RemoveRange(start, MusicList.Count - start);
                }
                MusicListView.Items.Refresh();
            }
        }

        private void putKsV(List<KeyValuePair<string, List<MusicInfo>>> tagsList, string[] performers, MusicInfo musicInfo) {
            if (performers == null) {
                return;
            }
            foreach (string performer in performers) {
                putKV(tagsList, performer, musicInfo);
            }
        }

        private void putKV(List<KeyValuePair<string, List<MusicInfo>>> tagList, string key, MusicInfo value) {
            if (key == null || key.Length >= 30) {
                return;
            }
            else if (this.tagsMap.ContainsKey(key)) {
                var musicList = this.tagsMap[key];
                musicList.Add(value);
            }
            else {
                var list = new List<MusicInfo>() {
                    {value}
                };
                this.tagsMap[key] = list;
                tagList.Add(new KeyValuePair<string, List<MusicInfo>>(key, list));
            }
        }


        private void Window_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control) {
                SaveMetaData();
            }
        }

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
                        MusicList.RemoveAt(MusicListView.SelectedIndex);
                        MusicListView.Items.Refresh();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("删除文件时发生错误: " + ex.Message);
                    }
                }
            }
        }


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

    }



}
