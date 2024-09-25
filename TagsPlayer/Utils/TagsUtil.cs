using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TagsPlayer.Utils
{
    internal class TagsUtil
    {
        public static ImageSource LoadImageFromBytes(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
                return null;

            using (MemoryStream memoryStream = new MemoryStream(imageData))
            {
                try
                {
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = memoryStream;
                    image.EndInit();
                    return image;
                } catch
                {
                    return null;
                }
            }
        }

    }


}
