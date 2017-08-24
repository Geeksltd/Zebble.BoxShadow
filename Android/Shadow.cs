using System;
using System.Threading.Tasks;
using Android.Graphics;
using System.IO;

namespace Zebble
{
    public partial class Shadow
    {
        bool Save(FileInfo savePath, byte[] buffer)
        {
            // var sdCardPath = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
            // var path = System.IO.Path.Combine(sdCardPath, fileName);
            var bitmap = BitmapFactory.DecodeByteArray(buffer, 0, buffer.Length);

            using (var filestream = new FileStream(savePath.FullName, FileMode.OpenOrCreate))
            {
                if (bitmap.Compress(Bitmap.CompressFormat.Png, 50, filestream))
                {
                    filestream.Flush();
                }
                else { } // handle failure case...
            }
            bitmap.Recycle();
            bitmap.Dispose();
            return true;
        }
    }
}