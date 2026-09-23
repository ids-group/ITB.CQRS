namespace ITB.Domain.Entities;

public abstract class Entity : IEntity
{
    public override string ToString() => $"[ENTITY: {GetType().Name}] Keys = {string.Join(", ", GetKeys())}";

    public abstract object[] GetKeys();
}

public abstract class Entity<TKey> : Entity, IEntity<TKey>
{
    protected Entity()
    {
    }

    protected Entity(TKey id)
    {
        Id = id;
    }

    public virtual TKey Id { get; protected set; }

    public override object[] GetKeys()
    {
        if (Id != null)
        {
            return new object[] { Id };
        }

        return Array.Empty<object>();
    }

    public override string ToString()
    {
        return $"[ENTITY: {GetType().Name}] Id = {Id}";
    }
}
