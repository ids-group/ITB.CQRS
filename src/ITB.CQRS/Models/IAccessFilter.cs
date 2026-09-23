using ITB.Domain.Interfaces;

namespace ITB.CQRS.Models;

public interface IAccessFilter<T> : IQueryableFilter<T>
    where T : class
{
}
