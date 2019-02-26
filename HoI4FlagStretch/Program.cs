using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TgaLib;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using System.Windows;


namespace HoI4FlagStretch
{
    public static class ImageExtensions
    {
        public static byte[] ToByteArray(this Image image, System.Drawing.Imaging.ImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, format);
                return ms.ToArray();
            }
        }
    }

    class Program
    {
        static int GetMostCommonColor(Bitmap Image)
        {
            var list = new Dictionary<int, int>();
            for (int x = 0; x < Image.Width; x++)
            {
                for (int y = 0; y < Image.Height; y++)
                {
                    int rgb = Image.GetPixel(x, y).ToArgb();
                    if (!list.ContainsKey(rgb))
                        list.Add(rgb, 1);
                    else
                        list[rgb]++;
                }
            }
            return list.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
        }

        static Bitmap GetBitmap(BitmapSource source)
        {
            Bitmap bmp = new Bitmap(
              source.PixelWidth,
              source.PixelHeight,
              System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            BitmapData data = bmp.LockBits(
              new Rectangle(System.Drawing.Point.Empty, bmp.Size),
              ImageLockMode.WriteOnly,
              System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            source.CopyPixels(
              Int32Rect.Empty,
              data.Scan0,
              data.Height * data.Stride,
              data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Insert path to scan:");
            String FolderPath = Console.ReadLine();
            String[] Files = Directory.GetFiles(FolderPath);
            foreach(string file in Files)
            {
                try
                {
                    if (".tga" == Path.GetExtension(file))
                    {
                        Bitmap outputImage = null;
                        using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                        using (var reader = new BinaryReader(fs))
                        {
                        
                            var tga = new TgaImage(reader);
                            Bitmap source = GetBitmap(tga.GetBitmap());
                            System.Drawing.Color MostCommonColor = System.Drawing.Color.FromArgb(GetMostCommonColor(source));
                            outputImage = new Bitmap(82, 129);
                            var graph = Graphics.FromImage(outputImage);
                            graph.Clear(MostCommonColor);
                            graph.DrawImage(source, new PointF(0, 39));
                            Console.WriteLine("saving to {0}", Path.GetDirectoryName(file) + '/' + Path.GetFileNameWithoutExtension(file) + ".png");
                            //outputImage.Save(Path.GetDirectoryName(file) + '/' + Path.GetFileNameWithoutExtension(file) + ".png", System.Drawing.Imaging.ImageFormat.Png);
                            
                            Console.WriteLine("{0} completed!", Path.GetFileName(file));

                            reader.Close();
                            fs.Close();
                        }
                        if(outputImage != null)
                        {
                            CreateTarga(file, outputImage);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exeption: {0}", e.Message);
                }
            }
            //Console.ReadLine();
        }

        static void CreateTarga(string Filename, Bitmap image)
        {
            using (FileStream fs = new FileStream(Filename, FileMode.Create, FileAccess.Write))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    bw.Seek(0, SeekOrigin.Begin);
                    bw.Write((byte)0); //IdentSize
                    bw.Write((byte)0); //ColorMapType
                    bw.Write((byte)2); //Uncompressed RGB
                    bw.Write((short)0); //Color map start
                    bw.Write((short)0); //Color map length
                    bw.Write((byte)0); //color map bits
                    bw.Write((short)0); //X origin
                    bw.Write((short)0); //Y origin
                    bw.Write((short)image.Width);
                    bw.Write((short)image.Height);
                    bw.Write((byte)32); //32 bit bitmap
                    bw.Write((byte)8); //Descriptor
                    //End of Header

                    image.RotateFlip(RotateFlipType.RotateNoneFlipY);

                    var bitmapdata = image.LockBits(
                        new Rectangle(0, 0, image.Width, image.Height),
                        ImageLockMode.ReadOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    IntPtr ptr = bitmapdata.Scan0;
                    int bytes = bitmapdata.Stride * image.Height;
                    byte[] rgbValues = new byte[bytes - 1];
                    System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes - 1);
                    bw.Write(rgbValues);
                    bw.Write((Int32)0);
                    bw.Write((Int32)0);
                    bw.Write("TRUEVISION-XFILE.");
                    bw.Write((byte)0);

                    //Cleanup and close-up
                    bw.Flush();
                    fs.Flush();
                    bw.Close();
                    fs.Close();
                }
            }
        }
    }
}
