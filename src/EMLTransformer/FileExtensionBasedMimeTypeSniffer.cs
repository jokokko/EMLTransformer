using System.Collections.Generic;
using System.IO;
using MimeKit;

namespace EMLTransformer
{
    public sealed class FileExtensionBasedMimeTypeSniffer : IMimeTypeSniffer
    {
        private static readonly Dictionary<string, string> TypeByExtension = new Dictionary<string, string>
        {
            {"bmp", "image/bmp"},
            {"gif", "image/gif"},
            {"jpeg", "image/jpeg"},
            {"jpg", "image/jpeg"},
            {"png", "image/png"},
            {"svgxml", "image/svg+xml"},
            {"tiff", "image/tiff"},
            {"webp", "image/webp"}
        };

        public string From(MimePart part)
        {
            if (!string.IsNullOrEmpty(part.FileName))
            {
                var extension = Path.GetExtension(part.FileName).ToLowerInvariant().TrimStart('.');
                if (TypeByExtension.TryGetValue(extension, out var type))
                {
                    return type;
                }
            }

            return part.ContentType.MimeType;
        }
    }
}