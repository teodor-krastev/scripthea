using System;
using System.Collections.Generic;
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
//using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;
using Newtonsoft.Json;
using Brushes = System.Windows.Media.Brushes;
using UtilsNS;
using System.Windows.Media.Animation;
using Image = System.Windows.Controls.Image;
using CompactExifLib;

namespace UtilsNS
{
    public static class ImgUtils
    {
        public enum ImageType { Unknown, Bmp, Jpg, Png, Tiff, Gif }
        public static SolidColorBrush ToSolidColorBrush(string hex_code)
        {
            return (SolidColorBrush)new BrushConverter().ConvertFromString(hex_code);
        }
        public static ImageType GetImageType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            switch (extension)
            {
                case ".jpg":
                case ".jpeg": return ImageType.Jpg;
                case ".png": return ImageType.Png;
                case ".gif": return ImageType.Gif;
                case ".bmp": return ImageType.Bmp;
                default: return ImageType.Unknown; // other
            }
        }
        public static string GetImageExt(ImageType iFormat)
        {
            if (iFormat == ImageType.Jpg) return ".jpg";
            if (iFormat == ImageType.Png) return ".png";
            if (iFormat == ImageType.Gif) return ".gif";
            if (iFormat == ImageType.Bmp) return ".bmp";
            return "";
        }
        public static Color ReadPixel(BitmapSource scrBitmap, int X, int Y)
        {
            // Create an array to hold the pixel's color data
            byte[] pixelColor = new byte[4]; // For BGRA32, each pixel is 4 bytes
            // Define the area to copy based on the pixel coordinates
            Int32Rect rect = new Int32Rect(X, Y, 1, 1);
            // Calculate the stride (width * bytes per pixel)
            int stride = (scrBitmap.PixelWidth * scrBitmap.Format.BitsPerPixel + 7) / 8;
            // Copy the pixel data into the array
            scrBitmap.CopyPixels(rect, pixelColor, stride, 0);
            return Color.FromArgb(pixelColor[3], pixelColor[2], pixelColor[1], pixelColor[0]);
        }
        public static void WritePixel(ref WriteableBitmap scrBitmap, int X, int Y, Color clr)
        {
            // Calculate the stride (width * bytes per pixel)
            int bytesPerPixel = (scrBitmap.Format.BitsPerPixel + 7) / 8;
            int stride = scrBitmap.PixelWidth * bytesPerPixel;

            // Create an array for the pixel data
            byte[] pixelData = new byte[4] { clr.B, clr.G, clr.R, clr.A }; // For Bgra32

            // Calculate the offset into the pixel array
            int offset = (Y * stride) + (X * bytesPerPixel);

            // Use WritePixels to update the bitmap
            scrBitmap.WritePixels(new Int32Rect(X, Y, 1, 1), pixelData, stride, 0);
        }
        public static BitmapSource ChangeColor(BitmapSource scrBitmap, Color newColor) //Color.Red;
        {
            //You can change your new color here. Red,Green,LawnGreen any..
            Color actualColor;
            //make an empty bitmap the same size as scrBitmap
            WriteableBitmap newBitmap = new WriteableBitmap(scrBitmap);
            for (int i = 0; i < scrBitmap.Width; i++)
            {
                for (int j = 0; j < scrBitmap.Height; j++)
                {
                    //get the pixel from the scrBitmap image
                    actualColor = ReadPixel(scrBitmap, i, j);
                    // > 150 because.. Images edges can be of low pixel colr. if we set all pixel color to new then there will be no smoothness left.
                    if (actualColor.A > 150)
                        WritePixel(ref newBitmap, i, j, newColor);
                    else
                        WritePixel(ref newBitmap, i, j, actualColor);
                }
            }
            return newBitmap;
        }
        public static Point GetActualSizeInPixels(FrameworkElement element)
        {
            // Retrieve the PresentationSource for the element
            PresentationSource source = PresentationSource.FromVisual(element);

            if (source != null)
            {
                // Get the matrix that converts from DIUs (device-independent units) to device pixels
                Matrix transformToDevice = source.CompositionTarget.TransformToDevice;

                // M11 is the scale factor along the X-axis (width), M22 is for the Y-axis (height)
                double widthInPixels = element.ActualWidth * transformToDevice.M11;
                double heightInPixels = element.ActualHeight * transformToDevice.M22;

                return new Point(widthInPixels, heightInPixels);
            }
            else
            {
                // Fallback if the PresentationSource cannot be retrieved
                // (This will simply return the DIU values in the Point, which may not be accurate for high DPI)
                return new Point(element.ActualWidth, element.ActualHeight);
            }
        }
        public static Color GetColorFromGradient(double intensity, Color color1, Color color2)
        {
            // Clamp intensity between 0 and 1
            intensity = Math.Max(0, Math.Min(1, intensity));

            // Calculate interpolation factor based on intensity
            double factor = intensity;

            // Interpolate between color components of color1 and color2
            byte red = (byte)(color1.R * (1 - factor) + color2.R * factor);
            byte green = (byte)(color1.G * (1 - factor) + color2.G * factor);
            byte blue = (byte)(color1.B * (1 - factor) + color2.B * factor);

            return Color.FromRgb(red, green, blue);
        }
        public static double HueFromPercents(double percents) // from 0 to 100 -> 250 to 0 
        {
            if (!Utils.InRange(percents, 0, 100)) return Double.NaN;
            return Math.Abs(percents - 100) * 2.5;
        }
        public static Color ColorFromHue(double hue)
        {
            if (!Utils.InRange(hue, 0, 360)) return new System.Windows.Media.Color();
            // Convert hue to RGB
            double H = hue / 60;
            int i = (int)Math.Floor(H);
            double f = H - i;
            double p = 1 - f;
            double q = f;
            double t = f;
            double r, g, b;
            switch (i)
            {
                case 0:
                    r = 1; g = t; b = 0;
                    break;
                case 1:
                    r = q; g = 1; b = 0;
                    break;
                case 2:
                    r = 0; g = 1; b = t;
                    break;
                case 3:
                    r = 0; g = q; b = 1;
                    break;
                case 4:
                    r = t; g = 0; b = 1;
                    break;
                case 5:
                    r = 1; g = 0; b = q;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("hue", hue, "Hue must be between 0 and 360.");
            }
            // Create a Color object from the RGB values
            return Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }
        public static BitmapImage UnhookedImageLoad(string filename, ImageType imageFormat = ImageType.Unknown)
        {
            try
            {
                if (!File.Exists(filename)) { Utils.TimedMessageBox("File <" + filename + "> is missing.", "Error:", 3000); return null; }
                ImageType iFormat = imageFormat == ImageType.Unknown ? GetImageType(filename) : imageFormat;
                if (iFormat == ImageType.Unknown) return null; // unrecogn. format; need to specify one
               
                return LoadBitmapImageFromFile(filename);
            }
            catch { return null; }
        }
        public static BitmapImage LoadBitmapImageFromFile(string imagePath)
        {
            BitmapImage bitmap = new BitmapImage();
            try
            {
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // This option helps to load the image faster by caching it immediately.
                bitmap.EndInit();
                bitmap.Freeze(); // This improves performance by making the image read-only and thread-safe.
            }
            catch (Exception ex)
            {
                Utils.TimedMessageBox("Error: loading image-> " + ex.Message);
            }
            return bitmap;
        }
        public static async Task<BitmapImage> LoadBitmapImageFromFileAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                BitmapImage bitmapImage = new BitmapImage();
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = fileStream;
                    bitmapImage.EndInit();
                }
                bitmapImage.Freeze(); // Necessary for cross-thread operations
                return bitmapImage;
            });
        }
        public static string CopyToImageToFormat(string sourceImage, string targetImage, ImageType targetImageType = ImageType.Unknown) // if null use target imageType 
        {
            ImageType SiFormat = GetImageType(sourceImage);
            ImageType TiFormat = targetImageType == ImageType.Unknown ? GetImageType(targetImage) : targetImageType;
            if ((TiFormat == ImageType.Unknown) || (SiFormat == ImageType.Unknown)) return ""; // unrecogn. format; need to specify one

            if (TiFormat == SiFormat) { File.Copy(sourceImage, targetImage); return targetImage; }
            else
            {
                string tImage = Path.ChangeExtension(targetImage, GetImageExt(TiFormat));
                BitmapImage bitmap = LoadBitmapImageFromFile(sourceImage);
                if (!SaveBitmapImageToDisk(bitmap, tImage)) return "";
                return tImage;
            }           
        }
        public static void VisualCompToPng(UIElement element, string filename = "")
        {
            var rect = new Rect(element.RenderSize);
            var visual = new DrawingVisual();

            using (var dc = visual.RenderOpen())
            {
                dc.DrawRectangle(new VisualBrush(element), null, rect);
            }
            var bitmap = new RenderTargetBitmap(
                (int)rect.Width, (int)rect.Height, 96, 96, PixelFormats.Default);
            bitmap.Render(visual);

            if (filename.Equals(""))
            {
                Clipboard.SetImage(bitmap);
                Utils.TimedMessageBox("The image is in the clipboard");
            }
            else
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using (var file = File.OpenWrite(filename))
                {
                    encoder.Save(file);
                }
            }
        }       
        public static bool GetMetadata1111(string imageFilePath, out Dictionary<string, string> itemMap, bool original = false)
        {
            itemMap = new Dictionary<string, string>();
            if (!Path.GetExtension(imageFilePath).ToLower().Equals(".png")) return false;
            var query = "/tEXt/{str=parameters}";
            try
            {
                using (Stream fileStream = File.Open(imageFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var decoder = BitmapDecoder.Create(fileStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
                    BitmapMetadata bitmapMetadata = decoder.Frames[0].Metadata as BitmapMetadata;
                    if (bitmapMetadata == null) return false;
                    var metadata = bitmapMetadata.GetQuery(query);
                    string md = metadata?.ToString(); if (md == null) { return false; }
                    var mda = md.Split((char)10); string[] mdb; string prompt = "";
                    if (mda.Length < 2) return false;
                    if (mda.Length > 2)
                    {
                        List<string> ls = new List<string>(mda);
                        mdb = ls[ls.Count - 1].Split(',');
                        ls.RemoveAt(ls.Count - 1);
                        prompt = String.Join("_", ls.ToArray());
                    }
                    else
                    {
                        prompt = mda[0];
                        mdb = mda[1].Split(',');
                    }
                    itemMap.Add("prompt", prompt);
                    if (mdb.Length.Equals(0)) return false;
                    foreach (var item in mdb)
                    {
                        var mdc = item.Split(':');
                        if (mdc.Length != 2) return false;
                        string nm = mdc[0].Trim();
                        if (!original)
                        {
                            if (nm.Equals("CFG scale")) nm = "scale";
                            if (nm.Equals("Model hash")) nm = "sd_model_hash";
                            else nm = nm.ToLower();
                        }
                        itemMap.Add(nm, mdc[1].Trim());
                    }
                    fileStream.Close();
                }
            }
            catch (Exception e) { Utils.TimedMessageBox("Error (I/O): " + e.Message, "Error message", 3000); return false; }
            return itemMap.Count > 0;
        }
        public static bool GetMetadataStringComfy(string imageFilePath, out string meta)
        {
            meta = "";
            if (!Path.GetExtension(imageFilePath).ToLower().Equals(".png")) return false;
            var query = "/tEXt/{str=prompt}";
            try
            {
                using (Stream fileStream = File.Open(imageFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var decoder = BitmapDecoder.Create(fileStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
                    BitmapMetadata bitmapMetadata = decoder.Frames[0].Metadata as BitmapMetadata;
                    if (bitmapMetadata == null) return false;
                    var metadata = bitmapMetadata.GetQuery(query);
                    meta = metadata?.ToString(); if (meta == null) { return false; }                    
                }
            }
            catch (Exception e) { Utils.TimedMessageBox("Error (I/O): " + e.Message, "Error message", 3000); return false; }
            return true;
        }

        public static int workflow_id = 37510;
        public static string GetJpgMetadata(string imagePath)
        {
            if (!File.Exists(imagePath)) { Utils.TimedMessageBox("Error: file <" + imagePath + "> not found"); return ""; }
            if (GetImageType(imagePath) != ImageType.Jpg) { Utils.TimedMessageBox("Error: file <" + imagePath + "> is not jpeg type"); return ""; }
            using (System.Drawing.Image image = System.Drawing.Image.FromFile(imagePath))
            {
                foreach (PropertyItem property in image.PropertyItems)
                {
                    if (property.Id.Equals(workflow_id)) return Encoding.UTF8.GetString(property.Value).Trim(); //&& property.Type.Equals(2)
                }
            }
            return "";
        }
        public static string GetJpgMetadataExt(string imagePath)
        {
            if (!File.Exists(imagePath)) { Utils.TimedMessageBox("Error: file <" + imagePath + "> not found"); return ""; }
            if (GetImageType(imagePath) != ImageType.Jpg) { Utils.TimedMessageBox("Error: file <" + imagePath + "> is not jpeg type"); return ""; }

            Dictionary<string, string> dct = new Dictionary<string, string>(); 
            using (System.Drawing.Image image = System.Drawing.Image.FromFile(imagePath))
            {
                foreach (PropertyItem property in image.PropertyItems)
                {
                    //if (property.Id.Equals(0x010E) && property.Type.Equals(2)) return Encoding.UTF8.GetString(property.Value).Trim(); // for description
                    dct.Add(property.Id.ToString()+":"+ property.Type.ToString(), Encoding.ASCII.GetString(property.Value).Trim());
                }
            }
            //Utils.writeDict(@"d:/meta.txt", dct);
            return "";
        }           
        public static bool SetJpgMetadata(string imagePath, string workflow)
        {
            if (!File.Exists(imagePath)) { Utils.TimedMessageBox("Error: file <" + imagePath + "> not found"); return false; }
            if (GetImageType(imagePath) != ImageType.Jpg) { Utils.TimedMessageBox("Error: file <" + imagePath + "> is not jpeg type"); return false; }
            if (workflow == "") { Utils.TimedMessageBox("Error: workflow is missing"); return false; }
            ExifData d = new ExifData(imagePath);            
            if (d.SetTagValue(ExifTag.UserComment, workflow+"\0\0", StrCoding.Utf8)) d.Save();
            else return false;
            return true;
        }

        public static bool SaveBitmapImageToDisk(BitmapImage bitmapImage, string filePath) // in PNG format
        {
            // Create a BitmapEncoder object
            BitmapEncoder encoder = null;
            switch (GetImageType(filePath))
            {
                case ImageType.Jpg:
                    encoder = new JpegBitmapEncoder();
                    break;
                case ImageType.Png:
                    encoder = new PngBitmapEncoder();
                    break;
                case ImageType.Bmp:
                    encoder = new BmpBitmapEncoder();
                    break;
                case ImageType.Gif:
                    encoder = new GifBitmapEncoder();
                    break;
                case ImageType.Tiff:
                    encoder = new TiffBitmapEncoder();
                    break;
            }
            if (encoder == null) { Utils.TimedMessageBox("Unknown image type of file: " + filePath); return false; }

            // Create a BitmapFrame object from the BitmapImage
            BitmapFrame frame = BitmapFrame.Create(bitmapImage);

            // Add the frame to the encoder
            encoder.Frames.Add(frame);

            // Create a FileStream object
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                // Save the image to the file
                encoder.Save(fileStream);
            }
            return true;
        }

        public static MemoryStream BitmapSourceToStream(BitmapSource bitmapSource)
        {
            MemoryStream stream = new MemoryStream();

            BitmapEncoder encoder = new PngBitmapEncoder(); // Use PngBitmapEncoder for PNG format
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

            encoder.Save(stream);
            stream.Position = 0; // Reset stream position to the beginning

            return stream;
        }
        public static BitmapSource MemoryStream2BitmapSource(MemoryStream memoryStream)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // Here we set the cache option to OnLoad, which means it will load the image into memory and close the stream automatically.
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.EndInit();
            bitmapImage.Freeze(); // This call is optional but recommended. It makes the image immutable and accessible across threads.

            return bitmapImage;
        }

        public static BitmapImage MemoryStream2BitmapImage(MemoryStream memoryStream)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // Here we set the cache option to OnLoad, which means it will load the image into memory and close the stream automatically.
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.EndInit();
            bitmapImage.Freeze(); // This call is optional but recommended. It makes the image immutable and accessible across threads.

            return bitmapImage;
        }

        public static BitmapImage ConvertBitmapSourceToBitmapImage(BitmapSource bitmapSource)
        {
            BitmapImage bitmapImage = new BitmapImage();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                // Use a PngBitmapEncoder if you want to preserve the transparency of the original image
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(memoryStream);
                memoryStream.Position = 0; // Reset the memory stream position to the beginning

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // Ensures the image is loaded into memory and the stream is closed
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // Optional but recommended for making the image thread-safe
            }
            return bitmapImage;
        }
        public static string GetMD5Checksum(MemoryStream stream)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "");
            }
        }
        public static BitmapSource LoadImage(string imagePath)
        {
            if (!File.Exists(imagePath)) { Utils.TimedMessageBox("Error: No file " + imagePath); return null; }
            ImageType it = GetImageType(imagePath);
            if (it == ImageType.Unknown) { Utils.TimedMessageBox("Error: unklnown type of image <" + imagePath + ">"); return null; }
            FileStream imageStream = File.OpenRead(imagePath);
            switch (it)
            {
                case ImageType.Bmp:
                    BmpBitmapDecoder bmpDecoder = new BmpBitmapDecoder(imageStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                    return bmpDecoder.Frames[0];
                case ImageType.Jpg:
                    JpegBitmapDecoder JpegDecoder = new JpegBitmapDecoder(imageStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                    return JpegDecoder.Frames[0];
                case ImageType.Png:
                    PngBitmapDecoder pngDecoder = new PngBitmapDecoder(imageStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                    return pngDecoder.Frames[0];
                default: return null;
            }
        }
        public static string SaveImage(BitmapSource bitmapSource, string imagePath) // untested
        {
            string FN = Path.ChangeExtension(Utils.timeName(), ".png");
            MemoryStream imageStream = BitmapSourceToStream(bitmapSource);

            // Now you can use 'imageStream' as needed, for example, to save the image to a file:
            using (var fileStream = new FileStream(imagePath, FileMode.Create, FileAccess.Write))
            {
                imageStream.CopyTo(fileStream);
            }
            return FN;
        }
    }
}

/* ===================== SPARE PARTS ====================================
       /// ModifyExifData
        /// </summary>
        /// <param name="inputImagePath"></param>
        /// <param name="outputImagePath"></param>
        /// https://medium.com/@dannyc/get-image-file-metadata-in-c-using-net-88603e6da63f
        /// 

public enum JpgPropertyTag: int
{
    // PropertyTagTypeShort
    ImageWidth = 0x0100, ImageHeight = 0x0101, // JPEGQuality = 0x5010, Adobe private property

    // PropertyTagTypeASCII
    Artist = 0x013B, Copyright = 0x8298, 
    DateTime = 0x0132, 
    SoftwareUsed = 0x0131, UserComment = 0x9286 	
}

public static void ModifyExifData(string inputImagePath, Dictionary<JpgPropertyTag, string> propTags, string outputImagePath = "")
{
    // Load the image
    System.Drawing.Image image = System.Drawing.Image.FromFile(inputImagePath);

    // Get the PropertyItems (EXIF data) of the image
    PropertyItem[] properties = image.PropertyItems;

    // Modify the desired EXIF property
    int propertyId = 0x0132; // 0x0132 is the EXIF tag for DateTime
    string newValue = DateTime.Now.ToString("yyyy:MM:dd HH:mm:ss");

    // Find the PropertyItem with the desired property ID
    PropertyItem targetProperty = null;
    foreach (var property in properties)
    {
        if (property.Id == propertyId)
        {
            targetProperty = property;
            break;
        }
    }

    // If the property exists, modify it. If not, create a new one
    if (targetProperty != null)
    {
        targetProperty.Value = Encoding.ASCII.GetBytes(newValue + '\0');
    }
    else
    {
        targetProperty = image.PropertyItems[0];
        targetProperty.Id = propertyId;
        targetProperty.Type = 2; // 2 is the type for ASCII strings
        targetProperty.Value = Encoding.ASCII.GetBytes(newValue + '\0');
        targetProperty.Len = targetProperty.Value.Length;
    }

    // Set the modified property back to the image
    image.SetPropertyItem(targetProperty);

    // Save the modified image to the output file
    image.Save(outputImagePath == "" ? inputImagePath : outputImagePath , ImageType.Jpeg);
}



public  void Ma1in()
{

    // Load the JPG image.
    System.Drawing.Image image = System.Drawing.Image.FromFile("my_image.jpg");

    // Get the ExifData object.
    ExifLib.IFD ExifData exifData = image.GetPropertyItem(306).Value as ExifData;

    // Modify the EXIF data.
    exifData.Make = "My Camera";
    exifData.Model = "My Model";
    exifData.DateTime = DateTime.Now;
    exifData.Description = "This is my image description";

    // Set the PropertyItem value of the Image object.
    image.GetPropertyItem(306).Value = exifData;

    // Save the image.
    image.Save("my_image_modified.jpg");
=================================================================================================================

using System;
using System.IO;
using System.Windows.Media.Imaging;

class Program
{
    static void Main()
    {
        // Replace with your image path
        string imagePath = @"C:\path\to\your\image.png";

        // Open the image file
        using (FileStream stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            // Load the bitmap image
            BitmapDecoder decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            
            // Get the first frame of the image (since some images can have multiple frames)
            BitmapFrame frame = decoder.Frames[0];

            // Access metadata (if available) If no metadata is found (frame.Metadata is null), then the image doesn't contain any metadata.
            if (frame.Metadata is BitmapMetadata metadata)
            {
                // Example: Reading some common metadata properties
                Console.WriteLine($"Title: {metadata.Title}");
                Console.WriteLine($"Author: {metadata.Author}");
                Console.WriteLine($"Comment: {metadata.Comment}");
                Console.WriteLine($"Date Taken: {metadata.DateTaken}");
                Console.WriteLine($"Camera Model: {metadata.CameraModel}");
                Console.WriteLine($"Application Name: {metadata.ApplicationName}");

                // You can also iterate through all available metadata properties
                foreach (var key in metadata)
                {
                    Console.WriteLine($"{key}: {metadata.GetQuery(key)}");
                }
            }
            else
            {
                Console.WriteLine("No metadata found in the image.");
            }
        }
    }
}
================================================================================================
// meta from/to JPG

using System;
using System.Drawing;
using System.Drawing.Imaging;

class Program
{
    static void Main()
    {
        string imagePath = "path_to_your_image.jpg";
        using (Image image = Image.FromFile(imagePath))
        {
            foreach (PropertyItem property in image.PropertyItems)
            {
                Console.WriteLine($"ID: {property.Id}, Type: {property.Type}, Length: {property.Len}");
            }
        }
    }
}
---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Drawing.Imaging;

class Program
{
    static void Main()
    {
        string imagePath = "path_to_your_image.jpg";
        string outputPath = "output_image.jpg";

        using (Image image = Image.FromFile(imagePath))
        {
            // Get or create a PropertyItem
            PropertyItem prop = image.PropertyItems[0];
            prop.Id = 0x010E; // Example: PropertyTagImageDescription
            prop.Type = 2; // ASCII
            prop.Value = System.Text.Encoding.ASCII.GetBytes("My Description\0");
            prop.Len = prop.Value.Length;

            // Add the property to the image
            image.SetPropertyItem(prop);
            image.Save(outputPath);
        }
    }
}

*/

