#Requires -Version 7.0

Begin {
    $ErrorActionPreference = 'stop'
}

Process {
    function EnsureCommandInstalled ($commandName, $message = "") {
        if ($null -eq (Get-Command $commandName -ErrorAction SilentlyContinue)) {
            Write-Host $message -ForegroundColor Red
            Exit
        }
    }

    EnsureCommandInstalled "mkcert" "
    MkCert is not installed, See: https://github.com/FiloSottile/mkcert
    "

    Write-Host "Installing localhost certificate..." -ForegroundColor Cyan

    # certificate is stored in user profile directory instead of the git repository
    $workDir = Join-Path $env:USERPROFILE ".eventgridemulator"
    $certPath = Join-Path $workDir "localhost.crt" # certificate only
    $certKeyPath = Join-Path $workDir "localhost.key" # private key only

    # create a trustworthy local certificate authority (CA)
    Invoke-Expression "mkcert -install" | Out-Null

    New-Item -ItemType Directory $workDir -ErrorAction SilentlyContinue

    Remove-Item -Force $certPath -ErrorAction SilentlyContinue
    Remove-Item -Force $certKeyPath -ErrorAction SilentlyContinue

    $certDomains = @(
        "localhost",
        "127.0.0.1",
        "::1",
        "host.docker.internal"
    ) -join " "

    # create the localhost development certificate
    Invoke-Expression "mkcert -cert-file $certPath -key-file $certKeyPath $certDomains" | Out-Null
    Write-Host "Certificate installed successfully." -ForegroundColor Green
}
