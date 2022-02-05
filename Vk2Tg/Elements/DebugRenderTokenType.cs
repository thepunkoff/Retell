namespace Vk2Tg.Elements;

[Flags]
public enum DebugRenderTokenType
{
    ShortText,
    LongText,
    Photo,
    PhotoWithCaption,
    TextWithHtmlPhoto,
    Video,
    VideoWithCaption,
    Gif,
    GifWithCaption,
    TextWithHtmlGif,
    MediaGroup,
    MediaGroupWithCaption
}