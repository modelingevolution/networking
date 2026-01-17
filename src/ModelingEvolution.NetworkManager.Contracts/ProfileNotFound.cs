namespace ModelingEvolution.NetworkManager.Contracts;

public class ProfileNotFound
{
    public Guid Id { get; init; }
    public static implicit operator ProfileNotFound(Guid id) => new ProfileNotFound { Id = id };
}
