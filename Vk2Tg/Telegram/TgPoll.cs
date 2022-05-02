using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Vk2Tg.Elements;

namespace Vk2Tg.Telegram;

public class TgPoll : TgElement
{
    public readonly string Question;
    public readonly string[] Options;
    public readonly bool AllowMultipleOptions;

    public override TgElement AddText(TgText text)
    {
        return new TgPoll(text.Text == Question ? Question : Question + "\n\n" + text.Text, Options, AllowMultipleOptions);
    }

    public override TgElement AddPhoto(TgPhoto photo)
    {
        return new TgCompoundElement(Question == photo.Caption ? new TgPhoto(photo.Url) : photo, this);
    }

    public override TgElement AddVideo(TgVideo video)
    {
        return new TgCompoundElement(Question == video.Caption ? new TgVideo(video.Url) : video, this);
    }

    public override TgElement AddPoll(TgPoll poll)
    {
        return new TgCompoundElement(this, poll);
    }

    public override TgElement AddLink(TgLink link)
    {
        return new TgCompoundElement(link, this);
    }

    public override TgElement AddGif(TgGif gif)
    {
        return new TgCompoundElement(gif, this);
    }

    public TgPoll(string question, string[] options, bool allowMultipleOptions)
    {
        Question = question;
        Options = options;
        AllowMultipleOptions = allowMultipleOptions;
    }

    public override async Task Render(TgRenderContext context, CancellationToken token)
    {
        await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
            async t => await context.BotClient.SendPollAsync(context.ChatId, Question, Options, true, PollType.Regular, AllowMultipleOptions, cancellationToken: t),
            token);
    }

    public override DebugRenderToken[] DebugRender()
    {
        return new[] { new DebugRenderToken(DebugRenderTokenType.Poll) };
    }

    public override string ToString()
    {
        return "[Poll]";
    }
}