using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace MarkdownEditor.Services
{
    public static class ImageEmbedder
    {
        private const int MaxSide = 1600;

        private static readonly WebpEncoder Encoder = new WebpEncoder
        {
            FileFormat = WebpFileFormatType.Lossy,
            Quality    = 80,
            Method     = WebpEncodingMethod.Level4
        };

        /// <summary>Encodes a file on disk to a base64 data URI.</summary>
        public static string EncodeFile(string filePath)
        {
            using var fs = File.OpenRead(filePath);
            return EncodeStream(fs);
        }

        /// <summary>
        /// Encodes any <see cref="Stream"/> containing a supported image to a
        /// base64 data URI. The stream must be readable from its current position.
        /// </summary>
        public static string EncodeStream(Stream stream)
        {
            using var image = Image.Load(stream);

            // Resize so the largest side ≤ 1600 px, preserve aspect ratio, never upscale.
            if (image.Width > MaxSide || image.Height > MaxSide)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(MaxSide, MaxSide)
                }));
            }

            using var ms = new MemoryStream();
            image.Save(ms, Encoder);
            var b64 = Convert.ToBase64String(ms.ToArray());
            return "data:image/webp;base64," + b64;
        }

        /// <summary>Returns a human-readable size string, e.g. "1.5 KB" / "1.2 MB".</summary>
        public static string FormatSize(long byteLength)
        {
            if (byteLength < 1024)        return byteLength + " B";
            if (byteLength < 1024 * 1024) return (byteLength / 1024.0).ToString("0.0") + " KB";
            return (byteLength / (1024.0 * 1024.0)).ToString("0.0") + " MB";
        }
    }
}
