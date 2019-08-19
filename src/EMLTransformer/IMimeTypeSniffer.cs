using MimeKit;

namespace EMLTransformer
{
    public interface IMimeTypeSniffer
    {
        string From(MimePart part);
    }
}