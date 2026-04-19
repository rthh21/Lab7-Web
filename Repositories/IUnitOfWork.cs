using Lab07.Models;

namespace Lab07.Repositories;

public interface IUnitOfWork
{
    IArticleRepository ArticleRepository { get; }
    ICategoryRepository CategoryRepository { get; }
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
