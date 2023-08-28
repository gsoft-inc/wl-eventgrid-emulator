# Workleap Azure Event Grid Emulator

This Docker image provides a local emulator for **Azure Event Grid**, which can be used for testing and development purposes.

## Prerequisites

Before you can use the EventGridEmulator Docker image, you need to have Docker installed on your machine. You can download Docker from the official website: https://www.docker.com/get-started

## Getting started

To use the EventGridEmulator Docker image, you can follow these steps:

1. Create a development certificate ([for more info](https://github.com/FiloSottile/mkcert)):

    ```powershell
    .\devtools\Install-Certificate.ps1
    ```

    You should see something like:
    ```
    Installing localhost certificate...
    The local CA is already installed in the system trust store! 👍


    Created a new certificate valid for the following names 📜
    - "localhost"
    - "127.0.0.1"
    - "::1"
    - "host.docker.internal"

    The certificate is at "C:\Users\<user>\.eventgridemulator\localhost.crt" and the key at "C:\Users\<user>\.eventgridemulator\localhost.key" ✅

    It will expire on <dd mmmm yy> 📅

    Certificate installed successfully.
    ```

2. Option a) Using docker-compose file (**recommended**):

    ```yaml
    version: '3.4'

    services:
        eventgridemulator:
            image: eventgridemulator
            environment:
                - ASPNETCORE_ENVIRONMENT=Development
                - ASPNETCORE_URLS=https://+:443;http://+:8080
                - KESTREL__CERTIFICATES__DEFAULT__PATH=/etc/ssl/certs/localhost.crt
                - KESTREL__CERTIFICATES__DEFAULT__KEYPATH=/etc/ssl/certs/localhost.key
            ports:
                - "6500:443"
            extra_hosts:
                - "host.docker.internal:host-gateway"
            volumes:
                - "~/.eventgridemulator:/etc/ssl/certs"
    ``` 
    You have to mount a volume to provide development certificates for https communication

    Option b) Using Docker Cli (*not recommended*):

    ```powershell
    docker run `
        --name eventgridemulator `
        -e "ASPNETCORE_ENVIRONMENT=Development" `
        -e "ASPNETCORE_URLS=https://+:443;http://+:8080" `
        -e "KESTREL__CERTIFICATES__DEFAULT__PATH=/etc/ssl/certs/localhost.crt" `
        -e "KESTREL__CERTIFICATES__DEFAULT__KEYPATH=/etc/ssl/certs/localhost.key" `
        -p "6500:8080" `
        -v "$env:USERPROFILE\.eventgridemulator:/etc/ssl/certs" `
        eventgridemulator
    ```

   This will start the EventGridEmulator container and map port 6500 on your local machine to port 8080 in the container.

3. Now you have to configure your subscriptions. You can use [Docker Desktop](https://www.docker.com/products/docker-desktop/) or [VS Code](https://code.visualstudio.com/) With [Docker extension](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-docker) to connect to your container file system. The configuration is `/app/appsetings.json`. Here is a sample content. We use `host.docker.internal` to access host endpoint outside the Docker container. This will resolve your host ip.
   
   ```json
    {
        "Topics": {
            "activities-eg": [
            "http://host.docker.internal:6000/webhook-200",
            "http://host.docker.internal:6000/webhook-400",
            "http://host.docker.internal:6000/webhook-missing"
            ],
            "comments-eg": [
            "http://host.docker.internal:6000/webhook-404",
            "http://host.docker.internal:6000/webhook-401",
            "http://host.docker.internal:6000/webhook-slow-200"
            ]
        }
    }
   ```

## How it works

The following diagram shows how components interact with each other.

``` plantuml
@startuml
' https://github.com/plantuml-stdlib/Azure-PlantUML
!include <azure/AzureCommon.puml>
!include <azure/AzureSimplified.puml>
!include <azure/Compute/AzureAppService.puml>
!include <azure/Integration/AzureEventGrid.puml>
!include <azure/Containers/AzureContainerRegistry.puml>
left to right direction
frame "Host Computer" {
	AzureAppService(pub, "Publisher", "")
	AzureAppService(sub, "Subscriber", "")
	frame "Docker Desktop" as docker {
		AzureEventGrid(ege, "Event Grid Emulator", "")
	}
}
frame Azure {
    AzureContainerRegistry(acr, "Container Registry", "")
}
note top of ege
    /app/appsetings.json
end note

pub --> ege : "https://localhost:6500"
sub <-- ege : "http://host.docker.internal:6000"
ege <-- acr : "docker pull"
@enduml
```

- Docker compose pull and start an instance of Event Grid Emulator in Docker Desktop.
- Emulator uses ```/app/appsettings.json``` to simulate topics registrations.
- Publisher send event to emulator via ```https://localhost:6500```.
- Subscriber recive the event notification via ```http://host.docker.internal``` which is automatically resolved to the host ip.

## Building

The project can be built by running `Build.ps1`.

## License

This code is licensed under the Apache License, Version 2.0. You may obtain a copy of this license at https://github.com/gsoft-inc/workleap-license/blob/main/LICENSE.


I hope this helps! Let me know if you have any questions.