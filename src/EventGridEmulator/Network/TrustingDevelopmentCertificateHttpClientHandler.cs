namespace EventGridEmulator.Network;

public class TrustingDevelopmentCertificateHttpClientHandler : HttpClientHandler
{
    private TrustingDevelopmentCertificateHttpClientHandler()
    {
        this.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }

    public static TrustingDevelopmentCertificateHttpClientHandler Instance { get; } = new TrustingDevelopmentCertificateHttpClientHandler();
}