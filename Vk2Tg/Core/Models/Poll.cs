namespace Vk2Tg.Core.Models;

/// <summary>
/// Domain model of a generic poll.
/// </summary>
public class Poll
{
    /// <summary>
    /// Poll question.
    /// </summary>
    public string Question { get; }

    /// <summary>
    /// Poll answer options.
    /// </summary>
    public string[] Options { get; }

    /// <summary>
    /// Allow multiple options.
    /// </summary>
    public bool AllowMultipleOptions { get; }

    public Poll(string question, string[] options, bool allowMultipleOptions)
    {
        Question = question;
        Options = options;
        AllowMultipleOptions = allowMultipleOptions;
    }
}