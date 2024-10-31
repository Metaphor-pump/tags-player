using System.Collections.Generic;
using System.ComponentModel;

namespace TagsPlayer.ViewModel
{

    /**
     * 左侧栏ViewModel
     * 包含以下信息：
     * 1.tag相关信息
     * 2.播放器相关信息（播放位置，总时长）
     * 3.选中曲目的元信息
     */
    public class MetaInfoPanelModel : INotifyPropertyChanged
    {
        /**
         * tag统计信息
         */
        private List<KeyValuePair<string, List<MusicInfo>>> tagsList; 
        private List<KeyValuePair<string, List<MusicInfo>>> authorTagsList;
        /**
         * 播放器信息
         */
        private double audioDuration;
        private double currentPosition;
        /**
         * 选中曲目的元信息
         */
        private List<MusicInfo> selectedMusics;
        private List<string> selectedTitles;
        private List<string> selectedComments;
        private List<string> selectArtists;
        private List<string> selectedAlbums;
        private string selectedFileName;

        public string SelectedFileName
        {
            get { return selectedFileName; }
            set
            {
                if (selectedFileName != value) {
                    selectedFileName = value;
                    OnPropertyChanged(nameof(SelectedFileName));
                }
            }
        }
        public List<string> SelectedAlbums
        {
            get { return selectedAlbums; }
            set
            {
                if (selectedAlbums != value)
                {
                    selectedAlbums = value;
                    OnPropertyChanged(nameof(SelectedAlbums));
                }
            }
        }
        public List<string> SelectedArtists
        {
            get { return selectArtists; }
            set
            {
                if (selectArtists != value)
                {
                    selectArtists = value;
                    OnPropertyChanged(nameof(SelectedArtists));
                }
            }
        }
        public List<string> SelectedComments
        {
            get { return selectedComments; }
            set
            {
                if (selectedComments != value)
                {
                    selectedComments = value;
                    OnPropertyChanged(nameof(SelectedComments));
                }
            }
        }

        public List<string> SelectedTitles
        {
            get { return selectedTitles; }
            set
            {
                if (selectedTitles != value)
                {
                    selectedTitles = value;
                    OnPropertyChanged(nameof(SelectedTitles));
                }
            }
        }

        public List<MusicInfo> SelectedMusics
        {
            get { return selectedMusics; }
            set
            {
                if (selectedMusics != value)
                {
                    selectedMusics = value;
                    OnPropertyChanged(nameof(SelectedMusics));
                }
            }
        }

        public double AudioDuration
        {
            get { return audioDuration; }
            set
            {
                if (audioDuration != value)
                {
                    audioDuration = value;
                    OnPropertyChanged(nameof(AudioDuration));
                }
            }
        }

        public double CurrentPosition
        {
            get { return currentPosition; }
            set
            {
                if (currentPosition != value)
                {
                    currentPosition = value;
                    OnPropertyChanged(nameof(CurrentPosition));
                }
            }
        }

        public List<KeyValuePair<string, List<MusicInfo>>> TagsList
        {
            get { return tagsList; }
            set
            {
                tagsList = value;
                OnPropertyChanged(nameof(tagsList));
            }
        }

        public List<KeyValuePair<string, List<MusicInfo>>> AuthorTagsList
        {
            get { return authorTagsList; }
            set
            {
                authorTagsList = value;
                OnPropertyChanged(nameof(authorTagsList));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SizeComparer : IComparer<KeyValuePair<string, List<MusicInfo>>>
    {
        public int Compare(KeyValuePair<string, List<MusicInfo>> x, KeyValuePair<string, List<MusicInfo>> y)
        {
            if (x.Value == null && y.Value != null) return -1;
            if (x.Value != null && y.Value == null) return 1;
            if (x.Value == null && y.Value == null) return 0;
            return y.Value.Count.CompareTo(x.Value.Count);
        }
    }


    public class MusicInfo 
    {
        public TagLib.File File { get; set; }
        public string Artists { get { return string.Join(",", this.File.Tag.Performers); }
            set {
                if (value != null)
                {
                    this.File.Tag.Performers = value.Split(",");
                }
            } 
        }
        public string AlbumArtists { get; set; }

        public void UpdateTitle(string title)
        {
            if ("[keep]".Equals(title) || title == null){ }
            else if ("[blank]".Equals(title))
            {
                this.File.Tag.Title = "";
            }
            else
            {
                this.File.Tag.Title = title;
            }
        }
        
        public void UpdateComment(string comment) {
            if ("[keep]".Equals(comment) || comment == null) { }
            else if ("[blank]".Equals(comment))
            {
                this.File.Tag.Comment = "";
            }
            else
            {
                this.File.Tag.Comment = comment;
            }
        }
        public void UpdateArtists(string artist) {
            if ("[keep]".Equals(artist) || artist == null) { }
            else if ("[blank]".Equals(artist))
            {
                this.Artists = "";
            }
            else
            {
                this.Artists = artist;
            }
        }
        public void UpdateAlbum(string album) {
            if ("[keep]".Equals(album) || album == null) { }
            else if ("[blank]".Equals(album))
            {
                this.File.Tag.Album = "";
            }
            else
            {
                this.File.Tag.Album = album;
            }
        }
    }


}
