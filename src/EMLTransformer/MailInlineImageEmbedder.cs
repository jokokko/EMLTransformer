using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MimeKit;
using static System.String;

namespace EMLTransformer
{
    public sealed class MailInlineImageEmbedder
    {
        private static readonly string PatternCid = $@"(?is)('|"")?{MagicStrings.Cid}:(\s)*({{0}})\1?";
        private readonly IMimeTypeSniffer mimeTypeSniffer;

        public MailInlineImageEmbedder()
        {
            mimeTypeSniffer = new PassthroughMimeTypeSniffer();
        }

        public MailInlineImageEmbedder(IMimeTypeSniffer mimeTypeSniffer)
        {
            this.mimeTypeSniffer = mimeTypeSniffer ?? throw new ArgumentNullException(nameof(mimeTypeSniffer));
        }

        private void EmbedImages(TextPart[] textParts, MimeMessage message, List<MimeEntity> entitiesToRemove)
        {
            IEnumerable<Tuple<MatchCollection, TextPart>> MatchBy(string pattern, string matchBy)
            {
                var matchContentId = new Regex(Format(pattern, Regex.Escape(matchBy)), RegexOptions.Compiled);

                var matched = textParts.Select(x => Tuple.Create(matchContentId.Matches(x.Text), x))
                    .Where(x => x.Item1.Count > 0);

                return matched;
            }

            IEnumerable<Tuple<MatchCollection, TextPart>> MatchByCid(string matchBy)
            {
                return MatchBy(PatternCid, matchBy);
            }

            foreach (var part in message.BodyParts.OfType<MimePart>())
            {
                var all = new List<Tuple<MatchCollection, TextPart>>();

                if (!IsNullOrEmpty(part.ContentId))
                {
                    var matched = MatchByCid(part.ContentId);

                    all.AddRange(matched);
                }

                if (!IsNullOrEmpty(part.FileName))
                {
                    var matched = MatchByCid(part.FileName);

                    all.AddRange(matched);
                }

                if (part.ContentLocation != null)
                {
                    var matched = MatchBy(@"(?s)('|""){0}\1", part.ContentLocation.ToString());

                    all.AddRange(matched);
                }

                foreach (var entry in all)
                {
                    foreach (var match in entry.Item1.OfType<Match>())
                    {
                        var data = ToBase64EncodedDataString(part);
                        entry.Item2.Text = entry.Item2.Text.Replace(match.Value, data);
                        entitiesToRemove.Add(part);
                    }
                }
            }
        }

        public Stream InlineEmbedImagesAndStripFromAttachments(Stream email)
        {
            var mimeParser = new MimeParser(email);
            var message = mimeParser.ParseMessage();
            var entitiesToRemove = new List<MimeEntity>();
            var textParts = message.BodyParts.OfType<TextPart>().ToArray();

            EmbedImages(textParts, message, entitiesToRemove);

            if (message.Body is Multipart parts)
            {
                var multiparts = parts.OfType<Multipart>().ToArray();
                foreach (var t in entitiesToRemove)
                {
                    parts.Remove(t);
                    // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                    multiparts.Any(x => x.Remove(t));
                }
            }

            var messageStream = new MemoryStream();
            message.WriteTo(messageStream);
            messageStream.Seek(0, SeekOrigin.Begin);
            return messageStream;
        }

        public Stream InlineEmbedImagesAndStripFromAttachments(string sourceEml)
        {
            using (var file = File.OpenRead(sourceEml))
            {
                return InlineEmbedImagesAndStripFromAttachments(file);
            }
        }

        public void InlineEmbedImagesAndStripFromAttachments(string sourceEml, string writeTo)
        {
            using (var file = File.OpenRead(sourceEml))
            {
                using (var eml = InlineEmbedImagesAndStripFromAttachments(file))
                using (var fileWriteTo = File.OpenWrite(writeTo))
                {
                    eml.CopyTo(fileWriteTo);
                }
            }
        }

        public string InlineEmbedImagesAndReturnBodyOnly(Stream email)
        {
            var mimeParser = new MimeParser(email);
            var message = mimeParser.ParseMessage();
            var entitiesToRemove = new List<MimeEntity>();
            var textParts = message.BodyParts.OfType<TextPart>().ToArray();

            EmbedImages(textParts, message, entitiesToRemove);

            return message.HtmlBody;
        }

        public string InlineEmbedImagesAndReturnBodyOnly(string sourceEml)
        {
            using (var file = File.OpenRead(sourceEml))
            {
                return InlineEmbedImagesAndReturnBodyOnly(file);
            }
        }

        private string ToBase64EncodedDataString(MimePart part)
        {
            using (var stream = new MemoryStream())
            {
                part.Content.DecodeTo(stream);
                var dataArray = stream.ToArray();
                var mimeType = mimeTypeSniffer.From(part);
                var data = $@"""{MagicStrings.Data}:{mimeType};{MagicStrings.Base64},{Convert.ToBase64String(dataArray)}""";
                return data;
            }
        }
    }
}