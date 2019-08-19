using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MimeKit;
using Xunit;

namespace EMLTransformer.Tests
{
    public class MailInlineImageEmbedderTests
    {
        private readonly MailInlineImageEmbedder sut;

        public MailInlineImageEmbedderTests()
        {
            sut = new MailInlineImageEmbedder();
        }
        
        [Theory]
        [InlineData("Resources/hello.eml", @"""(?s)data:.*Y\+LUukZZAJ7Ij8AHO7(.*?)""/>", 1)]
        [InlineData("Resources/hello.eml", @"""(?s)data:.*rhU3i8axP1RJA3hxL3(.*?)""/>", 1)]
        [InlineData("Resources/hello.eml", @"""(?s)data:.*ihO6JqbIS8Iyrooroh(.*?)""/>", 1)]
        public void CanEmbedImagesAndStripAttachmentsFromStream(string pathToFile, string expectedImageStringFragment, int attachmentCount)
        {
            using (var file = File.OpenRead(pathToFile))
            using (var eml = sut.InlineEmbedImagesAndStripFromAttachments(file)) AssertEmbeddings(expectedImageStringFragment, attachmentCount, eml);
        }

        [Theory]
        [InlineData("Resources/hello.eml", @"""(?s)data:.*Y\+LUukZZAJ7Ij8AHO7(.*?)""/>", 1)]
        [InlineData("Resources/hello.eml", @"""(?s)data:.*rhU3i8axP1RJA3hxL3(.*?)""/>", 1)]
        [InlineData("Resources/hello.eml", @"""(?s)data:.*ihO6JqbIS8Iyrooroh(.*?)""/>", 1)]
        public void CanEmbedImagesAndStripAttachmentsFromPath(string pathToFile, string expectedImageStringFragment, int attachmentCount)
        {
            using (var eml = sut.InlineEmbedImagesAndStripFromAttachments(pathToFile)) AssertEmbeddings(expectedImageStringFragment, attachmentCount, eml);
        }
        [Theory]
        [InlineData("Resources/hello.eml", @"""(?s)data:.*Y\+LUukZZAJ7Ij8AHO7(.*?)""/>", 1)]
        [InlineData("Resources/hello.eml", @"""(?s)data:.*rhU3i8axP1RJA3hxL3(.*?)""/>", 1)]
        [InlineData("Resources/hello.eml", @"""(?s)data:.*ihO6JqbIS8Iyrooroh(.*?)""/>", 1)]
        public void CanEmbedImagesAndStripAttachmentsFromPathAndOutputToFile(string pathToFile, string expectedImageStringFragment, int attachmentCount)
        {
            var tempFile = Path.GetTempFileName();
            sut.InlineEmbedImagesAndStripFromAttachments(pathToFile, tempFile);
            using (var output = File.OpenRead(tempFile))
            {
                AssertEmbeddings(expectedImageStringFragment, attachmentCount, output);
            }
        }

        private static void AssertEmbeddings(string expectedImageStringFragment, int attachmentCount, Stream eml)
        {
            using (var m = new MemoryStream())
            {
                eml.CopyTo(m);
                m.Seek(0, SeekOrigin.Begin);
                var mimeParser = new MimeParser(m);
                var msg = mimeParser.ParseMessage();
                m.Seek(0, SeekOrigin.Begin);
                var content = System.Text.Encoding.UTF8.GetString(m.ToArray());
                Assert.True(new Regex(expectedImageStringFragment).Matches(content).Count == 1);
                Assert.Equal(attachmentCount, msg.Attachments.Count());
            }
        }

        [Theory]
        [InlineData("Resources/hello.eml", @"""(?s)data:.*Y\+LUukZZAJ7Ij8AHO7(.*?)""/>")]
        [InlineData("Resources/hello.eml", @"""(?s)data:.*rhU3i8axP1RJA3hxL3(.*?)""/>")]
        [InlineData("Resources/hello.eml", @"""(?s)data:.*ihO6JqbIS8Iyrooroh(.*?)""/>")]
        public void CanEmbedAndGetBodyOnly(string pathToFile, string expectedImageStringFragment)
        {
            var content = sut.InlineEmbedImagesAndReturnBodyOnly(pathToFile);
            Assert.True(new Regex(expectedImageStringFragment).Matches(content).Count == 1);
        }

        [Theory]
        [InlineData("Resources/hello_octetstream.eml", @"image/png")]
        public void CanSetMimeTypeByFilename(string pathToFile, string expectedMimeType)
        {
            var mySut = new MailInlineImageEmbedder(new FileExtensionBasedMimeTypeSniffer());
            using (var m = new MemoryStream())
            {
                var s = mySut.InlineEmbedImagesAndStripFromAttachments(pathToFile);
                s.CopyTo(m);
                m.Seek(0, SeekOrigin.Begin);
                var mimeParser = new MimeParser(m);
                var msg = mimeParser.ParseMessage();
                Assert.Contains(expectedMimeType, msg.HtmlBody);
            }
        }
    }
}