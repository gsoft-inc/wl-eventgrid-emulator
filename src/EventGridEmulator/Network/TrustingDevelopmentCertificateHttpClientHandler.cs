namespace EventGridEmulator.Network;

internal sealed class TrustingDevelopmentCertificateHttpClientHandler : HttpClientHandler
{
    public TrustingDevelopmentCertificateHttpClientHandler()
    {
        // This might seems like a security issue, but it's not.
        // We trust the subscribers' certificates as this emulator is intented to be used in a local environment,
        // where certificates are mostly self-signed and not trusted by the Docker container.
        this.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }
}