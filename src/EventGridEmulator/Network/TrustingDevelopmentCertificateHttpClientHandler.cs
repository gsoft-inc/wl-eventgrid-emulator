namespace EventGridEmulator.Network;

internal sealed class TrustingDevelopmentCertificateHttpClientHandler : HttpClientHandler
{
    public TrustingDevelopmentCertificateHttpClientHandler()
    {
        this.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }
}