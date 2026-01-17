namespace ModelingEvolution.NetworkManager.Contracts;

public interface IStatefulStream<in TIdentifier>
{
    static abstract string FullStreamName(TIdentifier id);
}
