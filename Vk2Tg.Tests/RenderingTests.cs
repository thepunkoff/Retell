using System;
using System.Linq;
using NUnit.Framework;
using Vk2Tg.Elements;

namespace Vk2Tg.Tests;

public class Tests
{
    [Test]
    public void Text()
    {
        // Basic
        var shortText = new TgText("short text");

        Assert.AreEqual(shortText.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.ShortText) });
        
        var almostLongText = new TgText(new string(Enumerable.Range(0, 1024).Select(_ => 'a').ToArray()));
        
        Assert.AreEqual(almostLongText.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.ShortText) });
        
        var justLongText = new TgText(new string(Enumerable.Range(0, 1025).Select(_ => 'a').ToArray()));
        
        Assert.AreEqual(justLongText.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.LongText) });
        
        var longText = new TgText(new string(Enumerable.Range(0, 2500).Select(_ => 'a').ToArray()));
        
        Assert.AreEqual(longText.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.LongText) });
    }
    
    [Test]
    public void Photo()
    {
        // Basic
        var photo = new TgPhoto(new Uri("http://localhost/image.png"));

        Assert.AreEqual(photo.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.Photo) });
        
        var photoWithCaption = new TgPhoto(new Uri("http://localhost/image.png"), "caption");

        Assert.AreEqual(photoWithCaption.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.PhotoWithCaption) });

        var photoWithAlmostLongCaption = new TgPhoto(
            new Uri("http://localhost/image.png"),
            new string(Enumerable.Range(0, 1024).Select(_ => 'a').ToArray()));
        
        Assert.AreEqual(photoWithAlmostLongCaption.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.PhotoWithCaption) });
        
        var photoWithjustLongCaption = new TgPhoto(
            new Uri("http://localhost/image.png"),
            new string(Enumerable.Range(0, 1025).Select(_ => 'a').ToArray()));
        
        Assert.AreEqual(photoWithjustLongCaption.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.TextWithHtmlPhoto) });
        
        var photoWithLongCaption = new TgPhoto(
            new Uri("http://localhost/image.png"),
            new string(Enumerable.Range(0, 2500).Select(_ => 'a').ToArray()));
        
        Assert.AreEqual(photoWithLongCaption.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.TextWithHtmlPhoto) });
        
        // Text up
        var photoTextUp = new TgPhoto(new Uri("http://localhost/image.png"));

        Assert.AreEqual(photoTextUp.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.Photo) });

        var photoWithCaptionTextUp = new TgPhoto(new Uri("http://localhost/image.png"), "caption", textUp: true);

        Assert.AreEqual(photoWithCaptionTextUp.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.TextWithHtmlPhoto) });

        var photoWithAlmostLongCaptionTextUp = new TgPhoto(
            new Uri("http://localhost/image.png"),
            new string(Enumerable.Range(0, 1024).Select(_ => 'a').ToArray()));
        
        Assert.AreEqual(photoWithAlmostLongCaptionTextUp.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.PhotoWithCaption) });
        
        var photoWithjustLongCaptionTextUp = new TgPhoto(
            new Uri("http://localhost/image.png"),
            new string(Enumerable.Range(0, 1025).Select(_ => 'a').ToArray()));
        
        Assert.AreEqual(photoWithjustLongCaptionTextUp.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.TextWithHtmlPhoto) });

        var photoWithLongCaptionTextUp = new TgPhoto(
            new Uri("http://localhost/image.png"),
            new string(Enumerable.Range(0, 2500).Select(_ => 'a').ToArray()),
            textUp: true);

        Assert.AreEqual(photoWithLongCaptionTextUp.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.TextWithHtmlPhoto) });
    }

    [Test]
    public void Video()
    {
        // Basic
        var video = new TgVideo(new Uri("http://localhost/video.mp4"));

        Assert.AreEqual(video.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.Video) });
        
        var videoWithCaption = new TgVideo(new Uri("http://localhost/video.mp4"), "caption");

        Assert.AreEqual(videoWithCaption.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.VideoWithCaption) });

        var videoWithAlmostLongCaption = new TgVideo(
            new Uri("http://localhost/video.mp4"),
            new string(Enumerable.Range(0, 1024).Select(_ => 'a').ToArray()));
        
        Assert.AreEqual(videoWithAlmostLongCaption.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.VideoWithCaption) });
        
        var videoWithjustLongCaption = new TgVideo(
            new Uri("http://localhost/video.mp4"),
            new string(Enumerable.Range(0, 1025).Select(_ => 'a').ToArray()));

        var tVideo = new DebugRenderToken(DebugRenderTokenType.Video);
        var tText = new DebugRenderToken(DebugRenderTokenType.LongText, tVideo);
        var tokens = new[] { tVideo, tText };

        Assert.AreEqual(videoWithjustLongCaption.DebugRender(), tokens);
        
        var videoWithLongCaption = new TgVideo(
            new Uri("http://localhost/video.mp4"),
            new string(Enumerable.Range(0, 2500).Select(_ => 'a').ToArray()));
        
        tVideo = new DebugRenderToken(DebugRenderTokenType.Video);
        tText = new DebugRenderToken(DebugRenderTokenType.LongText, tVideo);
        tokens = new[] { tVideo, tText };

        Assert.AreEqual(videoWithLongCaption.DebugRender(), tokens);
        
        // Text up
        var videoTextUp = new TgVideo(new Uri("http://localhost/video.mp4"));

        Assert.AreEqual(videoTextUp.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.Video) });
        
        var videoWithCaptionTextUp = new TgVideo(new Uri("http://localhost/video.mp4"), 
            "caption",
            textUp: true);

        tText = new DebugRenderToken(DebugRenderTokenType.ShortText);
        tVideo = new DebugRenderToken(DebugRenderTokenType.Video, tText);
        tokens = new[] { tText, tVideo };
        
        Assert.AreEqual(videoWithCaptionTextUp.DebugRender(), tokens);
        
        var videoWithAlmostLongCaptionTextUp = new TgVideo(
            new Uri("http://localhost/video.mp4"),
            new string(Enumerable.Range(0, 1024).Select(_ => 'a').ToArray()),
            textUp: true);

        tText = new DebugRenderToken(DebugRenderTokenType.ShortText);
        tVideo = new DebugRenderToken(DebugRenderTokenType.Video, tText);
        tokens = new[] { tText, tVideo };
        
        Assert.AreEqual(videoWithAlmostLongCaptionTextUp.DebugRender(), tokens);
        
        var videoWithjustLongCaptionTextUp = new TgVideo(
            new Uri("http://localhost/video.mp4"),
            new string(Enumerable.Range(0, 1025).Select(_ => 'a').ToArray()),
            textUp: true);

        tText = new DebugRenderToken(DebugRenderTokenType.LongText);
        tVideo = new DebugRenderToken(DebugRenderTokenType.Video, tText);
        tokens = new[] { tText, tVideo };

        Assert.AreEqual(videoWithjustLongCaptionTextUp.DebugRender(), tokens);
        
        var videoWithLongCaptionTextUp = new TgVideo(
            new Uri("http://localhost/video.mp4"),
            new string(Enumerable.Range(0, 2500).Select(_ => 'a').ToArray()),
            textUp: true);
        
        tText = new DebugRenderToken(DebugRenderTokenType.LongText);
        tVideo = new DebugRenderToken(DebugRenderTokenType.Video, tText);
        tokens = new[] { tText, tVideo };

        Assert.AreEqual(videoWithLongCaptionTextUp.DebugRender(), tokens);
    }

    [Test]
    public void Gif()
    {
        // Basic
        var gif = new TgGif(new Uri("http://localhost/gif.gif"));

        Assert.AreEqual(gif.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.Gif) });
        
        var gifWithCaption = new TgGif(new Uri("http://localhost/gif.gif"), "caption");

        Assert.AreEqual(gifWithCaption.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.GifWithCaption) });

        var gifWithAlmostLongCaption = new TgGif(
            new Uri("http://localhost/gif.gif"),
            new string(Enumerable.Range(0, 1024).Select(_ => 'a').ToArray()));
        
        Assert.AreEqual(gifWithAlmostLongCaption.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.GifWithCaption) });
        
        var gifWithjustLongCaption = new TgGif(
            new Uri("http://localhost/gif.gif"),
            new string(Enumerable.Range(0, 1025).Select(_ => 'a').ToArray()));
        
        Assert.AreEqual(gifWithjustLongCaption.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.TextWithHtmlGif) });
        
        var gifWithLongCaption = new TgGif(
            new Uri("http://localhost/gif.gif"),
            new string(Enumerable.Range(0, 2500).Select(_ => 'a').ToArray()));
        
        Assert.AreEqual(gifWithLongCaption.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.TextWithHtmlGif) });
        
        // Text up
        var gifTextUp = new TgGif(new Uri("http://localhost/gif.gif"));

        Assert.AreEqual(gifTextUp.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.Gif) });

        var gifWithCaptionTextUp = new TgGif(new Uri("http://localhost/gif.gif"), "caption", textUp: true);

        Assert.AreEqual(gifWithCaptionTextUp.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.TextWithHtmlGif) });

        var gifWithAlmostLongCaptionTextUp = new TgGif(
            new Uri("http://localhost/gif.gif"),
            new string(Enumerable.Range(0, 1024).Select(_ => 'a').ToArray()));
        
        Assert.AreEqual(gifWithAlmostLongCaptionTextUp.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.GifWithCaption) });
        
        var gifWithjustLongCaptionTextUp = new TgGif(
            new Uri("http://localhost/gif.gif"),
            new string(Enumerable.Range(0, 1025).Select(_ => 'a').ToArray()));
        
        Assert.AreEqual(gifWithjustLongCaptionTextUp.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.TextWithHtmlGif) });

        var gifWithLongCaptionTextUp = new TgGif(
            new Uri("http://localhost/gif.gif"),
            new string(Enumerable.Range(0, 2500).Select(_ => 'a').ToArray()),
            textUp: true);

        Assert.AreEqual(gifWithLongCaptionTextUp.DebugRender(), new [] { new DebugRenderToken(DebugRenderTokenType.TextWithHtmlGif) });
    }

    [Test]
    public void MediaGroup()
    {
        // Basic
        var photo1 = new TgPhoto(new Uri("http://localhost/image1.png"));
        var photo2 = new TgPhoto(new Uri("http://localhost/image2.png"), "caption");
        var video1 = new TgVideo(new Uri("http://localhost/video1.mp4"));
        var video2 = new TgVideo(new Uri("http://localhost/video2.mp4"), "caption");

        var media1 = photo1.AddElement(photo2);
        var media2 = photo1.AddElement(video1);
        var media3 = video1.AddElement(video2);
        var media4 = video1.AddElement(photo1);
        var mediaWithCaption1 = photo2.AddElement(photo1);
        var mediaWithCaption2 = photo2.AddElement(video1);
        var mediaWithCaption3 = video2.AddElement(photo1);
        var mediaWithCaption4 = video2.AddElement(video1);
        
        Assert.AreEqual(media1.DebugRender(), new[] { new DebugRenderToken(DebugRenderTokenType.MediaGroup) });
        Assert.AreEqual(media2.DebugRender(), new[] { new DebugRenderToken(DebugRenderTokenType.MediaGroup) });
        Assert.AreEqual(media3.DebugRender(), new[] { new DebugRenderToken(DebugRenderTokenType.MediaGroup) });
        Assert.AreEqual(media4.DebugRender(), new[] { new DebugRenderToken(DebugRenderTokenType.MediaGroup) });
        Assert.AreEqual(mediaWithCaption1.DebugRender(), new[] { new DebugRenderToken(DebugRenderTokenType.MediaGroupWithCaption) });
        Assert.AreEqual(mediaWithCaption2.DebugRender(), new[] { new DebugRenderToken(DebugRenderTokenType.MediaGroupWithCaption) });
        Assert.AreEqual(mediaWithCaption3.DebugRender(), new[] { new DebugRenderToken(DebugRenderTokenType.MediaGroupWithCaption) });
        Assert.AreEqual(mediaWithCaption4.DebugRender(), new[] { new DebugRenderToken(DebugRenderTokenType.MediaGroupWithCaption) });
    }

    [Test]
    public void CombineWithGif()
    {
        // Two elements
        var photo1 = new TgPhoto(new Uri("http://localhost/image1.png"));
        var video1 = new TgVideo(new Uri("http://localhost/video1.mp4"));
        var gif = new TgGif(new Uri("http://localhost/gif.gif"));

        var result = photo1.AddElement(gif);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.Photo), new DebugRenderToken(DebugRenderTokenType.Gif) }, result.DebugRender());
        result = gif.AddElement(photo1);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.Photo), new DebugRenderToken(DebugRenderTokenType.Gif) }, result.DebugRender());
        
        result = video1.AddElement(gif);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.Video), new DebugRenderToken(DebugRenderTokenType.Gif) }, result.DebugRender());
        result = gif.AddElement(video1);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.Video), new DebugRenderToken(DebugRenderTokenType.Gif) }, result.DebugRender());
        
        // Three elements
        result = photo1.AddElement(video1).AddElement(gif);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.MediaGroup), new DebugRenderToken(DebugRenderTokenType.Gif) }, result.DebugRender());
        
        result = photo1.AddElement(gif).AddElement(video1);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.MediaGroup), new DebugRenderToken(DebugRenderTokenType.Gif) }, result.DebugRender());
        
        result = gif.AddElement(photo1).AddElement(video1);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.MediaGroup), new DebugRenderToken(DebugRenderTokenType.Gif) }, result.DebugRender());
        
        // Captions and modes
        var photoShortCaption = new TgPhoto(new Uri("http://localhost/image1.png"), "caption");
        var videoShortCaption = new TgVideo(new Uri("http://localhost/video1.mp4"), "caption");
        var gifShortCaption = new TgGif(new Uri("http://localhost/gif.gif"), "caption");
        var photoLongCaption = new TgPhoto(new Uri("http://localhost/image1.png"), new string(Enumerable.Range(0, 2500).Select(_ => 'a').ToArray()));
        var videoLongCaption = new TgVideo(new Uri("http://localhost/video1.mp4"), new string(Enumerable.Range(0, 2500).Select(_ => 'a').ToArray()));
        var gifLongCaption = new TgGif(new Uri("http://localhost/gif.gif"), new string(Enumerable.Range(0, 2500).Select(_ => 'a').ToArray()));

        var tMediaGroup = new DebugRenderToken(DebugRenderTokenType.MediaGroup);
        var tVideo = new DebugRenderToken(DebugRenderTokenType.Video);
        var tShortText = new DebugRenderToken(DebugRenderTokenType.ShortText);
        var tLongText = new DebugRenderToken(DebugRenderTokenType.LongText);        
        
        // Short caption (Auto mode)
        result = photoShortCaption.AddElement(gif);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.Photo), new DebugRenderToken(DebugRenderTokenType.GifWithCaption) }, result.DebugRender());

        // Extra thorough
        var t1 = result.AddElement(photo1);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.MediaGroup), new DebugRenderToken(DebugRenderTokenType.GifWithCaption) }, t1.DebugRender());
        var t2 = result.AddElement(video1);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.MediaGroup), new DebugRenderToken(DebugRenderTokenType.GifWithCaption) }, t2.DebugRender());
        var t3 = result.AddElement(gif);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.Photo), new DebugRenderToken(DebugRenderTokenType.Gif), new DebugRenderToken(DebugRenderTokenType.GifWithCaption) }, t3.DebugRender());
        // -----
        
        result = gifShortCaption.AddElement(photo1);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.Photo), new DebugRenderToken(DebugRenderTokenType.GifWithCaption) }, result.DebugRender());

        result = videoShortCaption.AddElement(gif);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.Video), new DebugRenderToken(DebugRenderTokenType.GifWithCaption) }, result.DebugRender());
        
        // Extra thorough
        t1 = result.AddElement(photo1);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.MediaGroup), new DebugRenderToken(DebugRenderTokenType.GifWithCaption) }, t1.DebugRender());
        t2 = result.AddElement(video1);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.MediaGroup), new DebugRenderToken(DebugRenderTokenType.GifWithCaption) }, t2.DebugRender());
        t3 = result.AddElement(gif);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.Video), new DebugRenderToken(DebugRenderTokenType.Gif), new DebugRenderToken(DebugRenderTokenType.GifWithCaption) }, t3.DebugRender());
        // -----
        
        result = gifShortCaption.AddElement(video1);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.Video), new DebugRenderToken(DebugRenderTokenType.GifWithCaption) }, result.DebugRender());

        // Long caption (Auto mode)
        result = photoLongCaption.AddElement(gif);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.TextWithHtmlPhoto), new DebugRenderToken(DebugRenderTokenType.Gif) }, result.DebugRender());

        // Extra thorough
        t1 = result.AddElement(photo1);
        Assert.AreEqual(new[] { tMediaGroup, new DebugRenderToken(DebugRenderTokenType.LongText, tMediaGroup), new DebugRenderToken(DebugRenderTokenType.Gif) }, t1.DebugRender());
        t2 = result.AddElement(video1);
        Assert.AreEqual(new[] { tMediaGroup, new DebugRenderToken(DebugRenderTokenType.LongText, tMediaGroup), new DebugRenderToken(DebugRenderTokenType.Gif) }, t2.DebugRender());
        t3 = result.AddElement(gif);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.TextWithHtmlPhoto), new DebugRenderToken(DebugRenderTokenType.Gif), new DebugRenderToken(DebugRenderTokenType.Gif) }, t3.DebugRender());
        // -----
        
        result = gifLongCaption.AddElement(photo1);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.TextWithHtmlPhoto), new DebugRenderToken(DebugRenderTokenType.Gif) }, result.DebugRender());

        result = videoLongCaption.AddElement(gif);
        Assert.AreEqual(new[] { tVideo, new DebugRenderToken(DebugRenderTokenType.LongText, tVideo), new DebugRenderToken(DebugRenderTokenType.Gif) }, result.DebugRender());
        
        // Extra thorough
        t1 = result.AddElement(photo1);
        Assert.AreEqual(new[] { tMediaGroup, new DebugRenderToken(DebugRenderTokenType.LongText, tMediaGroup), new DebugRenderToken(DebugRenderTokenType.Gif) }, t1.DebugRender());
        t2 = result.AddElement(video1);
        Assert.AreEqual(new[] { tMediaGroup, new DebugRenderToken(DebugRenderTokenType.LongText, tMediaGroup), new DebugRenderToken(DebugRenderTokenType.Gif) }, t2.DebugRender());
        t3 = result.AddElement(gif);
        Assert.AreEqual(new[] { tVideo, new DebugRenderToken(DebugRenderTokenType.LongText, tVideo), new DebugRenderToken(DebugRenderTokenType.Gif), new DebugRenderToken(DebugRenderTokenType.Gif) }, t3.DebugRender());
        // -----
        
        result = gifLongCaption.AddElement(video1);
        Assert.AreEqual(new[] { tVideo, new DebugRenderToken(DebugRenderTokenType.LongText, tVideo), new DebugRenderToken(DebugRenderTokenType.Gif) }, result.DebugRender());
        
        // Short caption (TextUp mode)
        Vk2TgConfig.Current = new Vk2TgConfig { GifMediaGroupMode = GifMediaGroupMode.TextUp };
        
        result = photoShortCaption.AddElement(gif);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.TextWithHtmlPhoto), new DebugRenderToken(DebugRenderTokenType.Gif) }, result.DebugRender());

        // Extra thorough
        t1 = result.AddElement(photo1);
        Assert.AreEqual(new[] { tShortText, new DebugRenderToken(DebugRenderTokenType.MediaGroup, tShortText), new DebugRenderToken(DebugRenderTokenType.Gif) }, t1.DebugRender());
        t2 = result.AddElement(video1);
        Assert.AreEqual(new[] { tShortText, new DebugRenderToken(DebugRenderTokenType.MediaGroup, tShortText), new DebugRenderToken(DebugRenderTokenType.Gif) }, t2.DebugRender());
        t3 = result.AddElement(gif);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.TextWithHtmlPhoto), new DebugRenderToken(DebugRenderTokenType.Gif), new DebugRenderToken(DebugRenderTokenType.Gif), }, t3.DebugRender());
        // -----
        
        result = gifShortCaption.AddElement(photo1);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.TextWithHtmlPhoto), new DebugRenderToken(DebugRenderTokenType.Gif) }, result.DebugRender());
        
        result = videoShortCaption.AddElement(gif);
        Assert.AreEqual(new[] { tShortText, new DebugRenderToken(DebugRenderTokenType.Video, tShortText), new DebugRenderToken(DebugRenderTokenType.Gif) }, result.DebugRender());
        
        // Extra thorough
        t1 = result.AddElement(photo1);
        Assert.AreEqual(new[] { tShortText, new DebugRenderToken(DebugRenderTokenType.MediaGroup, tShortText), new DebugRenderToken(DebugRenderTokenType.Gif) }, t1.DebugRender());
        t2 = result.AddElement(video1);
        Assert.AreEqual(new[] { tShortText, new DebugRenderToken(DebugRenderTokenType.MediaGroup, tShortText), new DebugRenderToken(DebugRenderTokenType.Gif) }, t2.DebugRender());
        t3 = result.AddElement(gif);
        Assert.AreEqual(new[] { tShortText, new DebugRenderToken(DebugRenderTokenType.Video, tShortText), new DebugRenderToken(DebugRenderTokenType.Gif), new DebugRenderToken(DebugRenderTokenType.Gif), }, t3.DebugRender());
        // -----
        
        result = gifShortCaption.AddElement(video1);
        Assert.AreEqual(new[] { tShortText, new DebugRenderToken(DebugRenderTokenType.Video, tShortText), new DebugRenderToken(DebugRenderTokenType.Gif) }, result.DebugRender());

        // Long caption (TextUp mode)
        result = photoLongCaption.AddElement(gif);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.TextWithHtmlPhoto), new DebugRenderToken(DebugRenderTokenType.Gif) }, result.DebugRender());

        // Extra thorough
        t1 = result.AddElement(photo1);
        Assert.AreEqual(new[] { tLongText, new DebugRenderToken(DebugRenderTokenType.MediaGroup, tLongText), new DebugRenderToken(DebugRenderTokenType.Gif) }, t1.DebugRender());
        t2 = result.AddElement(video1);
        Assert.AreEqual(new[] { tLongText, new DebugRenderToken(DebugRenderTokenType.MediaGroup, tLongText), new DebugRenderToken(DebugRenderTokenType.Gif) }, t2.DebugRender());
        t3 = result.AddElement(gif);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.TextWithHtmlPhoto), new DebugRenderToken(DebugRenderTokenType.Gif), new DebugRenderToken(DebugRenderTokenType.Gif), }, t3.DebugRender());
        // -----
        
        result = gifLongCaption.AddElement(photo1);
        Assert.AreEqual(new[] { new DebugRenderToken(DebugRenderTokenType.TextWithHtmlPhoto), new DebugRenderToken(DebugRenderTokenType.Gif) }, result.DebugRender());
        
        result = videoLongCaption.AddElement(gif);
        Assert.AreEqual(new[] { tLongText, new DebugRenderToken(DebugRenderTokenType.Video, tLongText ), new DebugRenderToken(DebugRenderTokenType.Gif) }, result.DebugRender());
        
        // Extra thorough
        t1 = result.AddElement(photo1);
        Assert.AreEqual(new[] { tLongText, new DebugRenderToken(DebugRenderTokenType.MediaGroup, tLongText), new DebugRenderToken(DebugRenderTokenType.Gif) }, t1.DebugRender());
        t2 = result.AddElement(video1);
        Assert.AreEqual(new[] { tLongText, new DebugRenderToken(DebugRenderTokenType.MediaGroup, tLongText), new DebugRenderToken(DebugRenderTokenType.Gif) }, t2.DebugRender());
        t3 = result.AddElement(gif);
        Assert.AreEqual(new[] { tLongText, new DebugRenderToken(DebugRenderTokenType.Video, tLongText), new DebugRenderToken(DebugRenderTokenType.Gif), new DebugRenderToken(DebugRenderTokenType.Gif), }, t3.DebugRender());
        // -----
        
        result = gifLongCaption.AddElement(video1);
        Assert.AreEqual(new[] { tLongText, new DebugRenderToken(DebugRenderTokenType.Video, tLongText), new DebugRenderToken(DebugRenderTokenType.Gif) }, result.DebugRender());
    }
}