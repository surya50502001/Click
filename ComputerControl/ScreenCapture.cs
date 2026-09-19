using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace HeyClicky.ComputerControl
{
    public class ScreenCapture
    {
        // Returns the screen capture as PNG bytes
        public static byte[] CaptureScreen()
        {
            // Note: In a multi-monitor setup, you might want to capture VirtualScreen instead.
            // For V1, capturing PrimaryScreen is simplest.
            Rectangle bounds = new Rectangle(0, 0, 
                System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, 
                System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);

            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }
    }
}
