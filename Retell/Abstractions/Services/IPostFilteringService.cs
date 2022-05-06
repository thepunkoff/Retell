using Retell.Core.Models;
using Retell.Filtering;
namespace Retell.Abstractions.Services;

public interface IPostFilteringService
{
    FilteringResult Filter(Post post);
}
