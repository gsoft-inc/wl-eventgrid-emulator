namespace EventGridEmulator.Network;

internal interface ISubscriberCancellationTokenRegistry
{
    void Register(string topic, string subscriber);

    CancellationToken Get(string topic, string subscriber);

    void Unregister(string topic, string subscriber);
}