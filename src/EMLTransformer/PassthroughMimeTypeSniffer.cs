using MimeKit;

namespace EMLTransformer
{
    public sealed class PassthroughMimeTypeSniffer : IMimeTypeSniffer
    {
        public string From(MimePart part)
        {
            return part.ContentType.MimeType;
        }
    }
}