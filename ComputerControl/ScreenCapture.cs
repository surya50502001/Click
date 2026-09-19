using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace HeyClicky.ComputerControl
{
    public class ScreenCapture
    {
        // Returns the screen capture as optimized JPEG bytes
        public static byte[] CaptureScreen(int maxDimension = 1280)
        {
            Rectangle bounds = System.Windows.Forms.SystemInformation.VirtualScreen;

            using (Bitmap rawBitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(rawBitmap))
                {
                    g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                }

                // Calculate scaled dimensions to keep image lightweight for Gemini
                int targetWidth = bounds.Width;
                int targetHeight = bounds.Height;

                if (targetWidth > maxDimension || targetHeight > maxDimension)
                {
                    double ratio = Math.Min((double)maxDimension / targetWidth, (double)maxDimension / targetHeight);
                    targetWidth = (int)(targetWidth * ratio);
                    targetHeight = (int)(targetHeight * ratio);
                }

                using (Bitmap scaledBitmap = new Bitmap(targetWidth, targetHeight))
                {
                    using (Graphics g = Graphics.FromImage(scaledBitmap))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawImage(rawBitmap, 0, 0, targetWidth, targetHeight);
                    }

                    using (MemoryStream ms = new MemoryStream())
                    {
                        // Save with 80% JPEG quality
                        var encoder = GetEncoder(ImageFormat.Jpeg);
                        var encoderParams = new EncoderParameters(1);
                        encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 80L);

                        if (encoder != null)
                        {
                            scaledBitmap.Save(ms, encoder, encoderParams);
                        }
                        else
                        {
                            scaledBitmap.Save(ms, ImageFormat.Jpeg);
                        }

                        return ms.ToArray();
                    }
                }
            }
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null!;
        }
    }
}
