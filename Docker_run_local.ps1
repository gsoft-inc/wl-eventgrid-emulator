#Requires -Version 5.0

Begin {
    $ErrorActionPreference = "stop"
}

Process {
    function Exec([scriptblock]$Command) {
        & $Command
        if ($LASTEXITCODE -ne 0) {
            throw ("An error occurred while executing command: {0}" -f $Command)
        }
    }
    
    $workingDir = Join-Path $PSScriptRoot "src"

    try {
        Push-Location $workingDir
    
        Exec { & docker run -p 5000:8080 eventgridemulator:local }
    }
    finally {
        Pop-Location
    }
}