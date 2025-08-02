using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
//using System.Text.Json;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using UtilsNS;

namespace ExtCollMng
{
    public class CLIP
    {
        #region TEXT  
        public bool GPUenabled = false;
        public float[] TextEmbeds(string prompt)
        {
            if (!LoadTextEnv()) return null;
            List<string> tkns = AllTokensOfLine(prompt);
            List<int> ti = new List<int>();
            foreach (string t in tkns)
            {
                if (!tokenIds.ContainsKey(t)) continue;
                foreach (int tj in tokenIds[t])
                {
                    ti.Add(tj);
                }
            }
            int[] tIds = BuildPaddedClipInput(ti.ToArray());
            return TextEmbeds(tIds);
        }
        private InferenceSession clipTextModel = null;
        private Dictionary<string, int[]> tokenIds = null;
        private bool LoadTextEnv()
        {
            if (clipTextModel != null && tokenIds != null) return true;
            string clipTextFile = Path.Combine(Utils.configPath, "clip-text-vit-32-float32-int32.onnx");
            if (!File.Exists(clipTextFile)) return false;
            if (GPUenabled)
            {
                var options = new SessionOptions();
                options.AppendExecutionProvider_CUDA();  // 👈 Enables GPU
                clipTextModel = new InferenceSession(clipTextFile, options);
            }
            else clipTextModel = new InferenceSession(clipTextFile);
            string tokensPath = Path.Combine(Utils.configPath, "token_ids.tsv");
            if (!File.Exists(tokensPath)) return false;
            tokenIds = LoadTokenIds(tokensPath);
            return clipTextModel != null && tokenIds != null;
        }
        public List<string> AllTokensOfLine(string line)
        {
            List<string> words = new List<string>();
            string[] sa = line.ToLower().Split(
                new char[] { ' ', '\t', '\r', '\n', '-', '.', '!', '?', '(', ')', ',', ':', '\'', '`', '\"', '–', '—', '/' },
                StringSplitOptions.RemoveEmptyEntries
            );
            foreach (string ss in sa)
            {
                if (ss.Length == 0) continue;
                if (!char.IsLetter(ss[0])) continue;
                if (ss.EndsWith("'s")) words.Add(ss.Substring(0, ss.Length - 2));
                else words.Add(ss);
            }
            return words;
        }
        public Dictionary<string, int[]> LoadTokenIds(string path)
        {
            var result = new Dictionary<string, int[]>();
            foreach (var line in File.ReadAllLines(path))
            {
                var parts = line.Split('\t');
                if (parts.Length != 2) continue;

                var word = parts[0].Trim();
                var tokenStr = parts[1].Trim();

                int[] tokenIds;

                if (tokenStr.StartsWith("MULTI:"))
                {
                    tokenIds = tokenStr.Substring(6)
                                       .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(int.Parse)
                                       .ToArray();
                }
                else
                {
                    tokenIds = new[] { int.Parse(tokenStr) };
                }
                result[word] = tokenIds;
            }
            return result;
        }
        private int[] BuildPaddedClipInput(int[] tokenIds)
        {
            var result = new int[77];
            result[0] = 49406; // <|startoftext|>
            Array.Copy(tokenIds, 0, result, 1, Math.Min(tokenIds.Length, 75));
            result[1 + Math.Min(tokenIds.Length, 75)] = 49407; // <|endoftext|>
                                                               // Remaining values are already 0 (padded)
            return result;
        }
        public float[] TextEmbeds(int[] tokenIds)
        {
            var inputTensor = new DenseTensor<int>(tokenIds, new[] { 1, 77 });
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("input", inputTensor) };

            // Run the model, and get the output back as an Array of floats
            var outputData = clipTextModel.Run(inputs).ToList().Last().AsTensor<float>().ToArray();

            // Write the array serialized as JSON
            //Console.WriteLine(JsonSerializer.Serialize(outputData));
            return outputData;
        }
        #endregion

        #region IMAGE
        public float[] ImageEmbeds(string imagePath)
        {
            if (!LoadImageEnv() || !File.Exists(imagePath)) return null;
            BitmapSource bitmapSource = ImgUtils.LoadImage(imagePath);

            return ImageEmbeds(bitmapSource);
        }
        private InferenceSession clipImageModel = null;
        private bool LoadImageEnv()
        {
            if (clipImageModel != null) return true;
            string clipImageFile = Path.Combine(Utils.configPath, "clip-image-vit-32-float32.onnx");
            if (!File.Exists(clipImageFile)) return false;
            if (GPUenabled)
            {
                var options = new SessionOptions();
                options.AppendExecutionProvider_CUDA();  // 👈 Enables GPU
                clipImageModel = new InferenceSession(clipImageFile, options);

            }
            else clipImageModel = new InferenceSession(clipImageFile);
            
            return clipImageModel != null;
        }
        /// <summary>
        /// Resizes a BitmapSource to the specified dimensions.
        /// </summary>
        /// <param name="source">The original BitmapSource.</param>
        /// <param name="newWidth">The desired width.</param>
        /// <param name="newHeight">The desired height.</param>
        /// <returns>A new, resized BitmapSource.</returns>
        public BitmapSource Resize(BitmapSource source, int newWidth, int newHeight)
        {
            // Calculate the scaling factors for width and height
            double scaleX = (double)newWidth / source.PixelWidth;
            double scaleY = (double)newHeight / source.PixelHeight;

            // Create the ScaleTransform
            var scaleTransform = new ScaleTransform(scaleX, scaleY);

            // Create the TransformedBitmap
            var resizedBitmap = new TransformedBitmap(source, scaleTransform);

            // It's a good practice to freeze the new bitmap for performance benefits
            resizedBitmap.Freeze();

            return resizedBitmap;
        }
        public static BitmapSource CenterCrop(BitmapSource source, int cropWidth, int cropHeight)
        {
            int x = (source.PixelWidth - cropWidth) / 2;
            int y = (source.PixelHeight - cropHeight) / 2;

            // Ensure bounds are valid
            x = Math.Max(x, 0);
            y = Math.Max(y, 0);
            cropWidth = Math.Min(cropWidth, source.PixelWidth);
            cropHeight = Math.Min(cropHeight, source.PixelHeight);

            return new CroppedBitmap(source, new Int32Rect(x, y, cropWidth, cropHeight));
        }
        public static Color GetPixelColor(BitmapSource bitmap, int x, int y)
        {
            // Ensure pixel is within bounds
            if (x < 0 || x >= bitmap.PixelWidth || y < 0 || y >= bitmap.PixelHeight)
                return Color.FromArgb(0, 0, 0, 0);

            // Define pixel format - assuming 32bpp (BGRA)
            if (bitmap.Format != PixelFormats.Bgra32)
            {
                // Convert to Bgra32 format if not already
                bitmap = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
            }

            // Allocate buffer
            byte[] pixels = new byte[4]; // 4 bytes per pixel (B, G, R, A)

            // Copy one pixel
            bitmap.CopyPixels(new Int32Rect(x, y, 1, 1), pixels, 4, 0);

            // Create Color from bytes
            byte blue = pixels[0];
            byte green = pixels[1];
            byte red = pixels[2];
            byte alpha = pixels[3];

            return Color.FromArgb(alpha, red, green, blue);
        }
        private double cropSize = 224; // may change after new model
        public BitmapSource controlImage = null;
        public float[] ImageEmbeds(BitmapSource bitmapSource)
        {
            int pw = bitmapSource.PixelWidth; int ph = bitmapSource.PixelHeight;
            BitmapSource img = bitmapSource.Clone();
            // equalize width and height
            if (pw > ph)
            {
                img = Resize(bitmapSource, (int)(pw * (cropSize / ph)), (int)cropSize);
            }
            if (pw < ph)
            {
                img = Resize(bitmapSource, (int)cropSize, (int)(ph * (cropSize / pw)));
            }
            if (pw == ph)
            {
                img = Resize(bitmapSource, (int)cropSize, (int)cropSize);
            }
            img = CenterCrop(img.Clone(), (int)cropSize, (int)cropSize);
            controlImage = img.Clone();
            // Create a new array for 1 picture, 3 channels (RGB) and 224 pixels height and width
            int w = (int)cropSize; int h = w; int iw = img.PixelWidth; int ih = img.PixelHeight;
            var inputTensor = new DenseTensor<float>(new[] { 1, 3, h, w });

            // Put all the pixels in the input tensor
            for (var x = 0; x < w; x++) // width
            {
                for (var y = 0; y < h; y++)
                {
                    Color clr = Brushes.Black.Color;
                    if (x < iw || y < ih) clr = ImgUtils.ReadPixel(img, x, y);
                    // Normalize from bytes (0-255) to floats (constants borrowed from CLIP repository)
                    inputTensor[0, 0, y, x] = Convert.ToSingle((((float)clr.R / 255) - 0.48145466) / 0.26862954);
                    inputTensor[0, 1, y, x] = Convert.ToSingle((((float)clr.G / 255) - 0.4578275) / 0.26130258);
                    inputTensor[0, 2, y, x] = Convert.ToSingle((((float)clr.B / 255) - 0.40821073) / 0.27577711);
                }
            }
            // Prepare the inputs as a named ONNX variable, name should be "input"
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("input", inputTensor) };

            // Run the model, and get the output back as an Array of floats
            var outputData = clipImageModel.Run(inputs).ToList().Last().AsTensor<float>().ToArray();

            // Write the array serialized as JSON
            //Console.WriteLine(JsonSerializer.Serialize(outputData));
            return outputData;
        }
        #endregion IMAGE

        #region DISTANCE
        // Normalize an embedding to unit length (L2 norm = 1)
        public float[] Normalize(float[] vector)
        {
            float norm = (float)Math.Sqrt(vector.Sum(x => x * x));
            if (norm == 0) return vector.ToArray(); // avoid div-by-zero
            return vector.Select(x => x / norm).ToArray();
        }
        const int vectorLenght = 512;
        // Cosine similarity: assumes both vectors are already normalized
        public float CosineSimilarity(float[] vecA, float[] vecB)
        {
            if (vecA == null || vecB == null) return -1;
            if (vecA.Length != vecB.Length || vecA.Length != vectorLenght) return -1;
            float dot = 0f;
            for (int i = 0; i < vecA.Length; i++)
                dot += vecA[i] * vecB[i];
            return dot;
        }
        // Cosine similarity with auto-normalization
        public float CosineSimilarityNormalized(float[] vecA, float[] vecB)
        {
            if (vecA == null || vecB == null) return -1;
            if (vecA.Length != vecB.Length || vecA.Length != vectorLenght) return -1;
            var normA = Normalize(vecA);
            var normB = Normalize(vecB);
            return CosineSimilarity(normA, normB);
        }
        public float ArcCosineSimilarityNormalized(float[] vecA, float[] vecB)
        {
            if (vecA == null || vecB == null) return -1;
            if (vecA.Length != vecB.Length || vecA.Length != vectorLenght) return -1;
            var normA = Normalize(vecA);
            var normB = Normalize(vecB);
            return (float)Math.Acos(CosineSimilarity(normA, normB));
        }
        #endregion DISTANCE
    }

}
