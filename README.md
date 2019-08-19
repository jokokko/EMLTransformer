# EMLTransformer [![Build status](https://ci.appveyor.com/api/projects/status/ves8tx7kwwog4hte?svg=true)](https://ci.appveyor.com/project/jokokko/EMLTransformer) [![NuGet Version](http://img.shields.io/nuget/v/EMLTransformer.svg?style=flat)](https://www.nuget.org/packages/EMLTransformer/)
Inline embed images in emails (MIME) and strip out attached images.

**Package** [EMLTransformer](https://www.nuget.org/packages/EMLTransformer) | **Platforms** .NET 4.6, .NET Standard 2.0

### Sample usage
```csharp
var embedder = new MailInlineImageEmbedder();
embedder.InlineEmbedImagesAndStripFromAttachments("email.eml", "out.eml");
```

If images are attached as application/octet-stream, the `MailInlineImageEmbedder` can be passed an implementation of `IMimeTypeSniffer`.

### Set type of embedded inline images based on file extension
```csharp
var embedder = new MailInlineImageEmbedder(new FileExtensionBasedMimeTypeSniffer());
var streamOut = embedder.InlineEmbedImagesAndStripFromAttachments(streamIn);
```