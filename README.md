# Workleap Azure Event Grid Emulator

This Docker image provides a local emulator for **Azure Event Grid**, which can be used for testing and development purposes.

> :warning: For now, the emulator only supports push notification model.

## Features
- Event Grid topics endpoints `https://localhost:6500/<topic>/api/events`.
- Push notification to subscribers endpoints defined in `/app/appsettings.json` config file on container image filesystem.
- Retry policies similar to Azure implementation.

## Prerequisites

Before you can use the EventGridEmulator Docker image, you need to have Docker installed on your machine. You can download Docker from the official website: https://www.docker.com/get-started

## Getting started

To use the EventGridEmulator Docker image, you can follow these steps:

1. Create a development certificate in order to allow https communication with the emulator[^1] ([for more info on mkcert used to create certificate](https://github.com/FiloSottile/mkcert)). You can use the script provided in this repo:

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

2. Using `docker-compose` file to start and configure the container:
   
   Create a file named `docker-compose.yaml` in the root source folder of your project.

    ```yaml
    version: '3.4'

    services:
        eventgridemulator:
            image: techplatform0scaffolding0dev0acr.azurecr.io/eventgrid-emulator:main
            environment:
                - KESTREL__CERTIFICATES__DEFAULT__PATH=/etc/ssl/certs/localhost.crt
                - KESTREL__CERTIFICATES__DEFAULT__KEYPATH=/etc/ssl/certs/localhost.key
            ports:
                - "6500:443"
            volumes:
                - "~/.eventgridemulator:/etc/ssl/certs"
    
    ```
    You may need to login to Azure Container Registry with:
    
    ```powershell
    az acr login -n techplatform0scaffolding0dev0acr
    ```
    
    Then run docker compose:
    
    ```powershell
    docker compose pull
    ```
    
    And
    
    ```powershell
    docker compose up
    ```

   This will start the EventGridEmulator container and map port 6500 on your local machine to port 8080 in the container.

3. Now you have to configure your subscriptions. You can use [Docker Desktop](https://www.docker.com/products/docker-desktop/) or [VS Code](https://code.visualstudio.com/) with [Docker extension](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-docker) to connect to your container file system. The configuration is `/app/appsettings.json`. Here is a sample content. We use `host.docker.internal` to access host endpoint outside the Docker container. This will resolve your host ip.
   
   ```json
    {
        "Topics": {
            "topic1": [
                "http://host.docker.internal:6000/webhook-200",
                "http://host.docker.internal:6000/webhook-400",
                "http://host.docker.internal:6000/webhook-missing"
            ],
            "topic2": [
                "http://host.docker.internal:6000/webhook-404",
                "http://host.docker.internal:6000/webhook-401",
                "http://host.docker.internal:6000/webhook-slow-200"
            ]
        }
    }
   ```

## How it works

The following diagram shows how components interact with each other.

![](.docs/diagram-generated.svg)

- Docker compose pull and start an instance of Event Grid Emulator in Docker Desktop.
- Emulator uses ```/app/appsettings.json``` to simulate topics registrations.
- Publisher send event to emulator via ```https://localhost:6500```.
- Emulator will send notifications to all its subscriber's endpoint.
  - It will apply retry policies similar to Azure Event Grid implementation.
- Subscriber receive the event notification via ```http://host.docker.internal``` which is automatically resolved to the host ip.

## appsettings.json file format

Here is the schema of the `appsettings.json` file.

In `appsettings.json` file you can add `Topics` section. This section will contain topics registrations and subscribers. For each topics you can define many subscribers as an array of strings. Here is a sample file that define 2 subscribers on each of the 2 topics:

``` json
{
    "Topics": {
        "topic1": [
            "http://host.docker.internal:6000/subscriber1",
            "http://host.docker.internal:6000/subscriber2"
        ],
        "topics2": [
            "http://host.docker.internal:6000/subscriber3",
            "http://host.docker.internal:6000/subscriber4"
        ]
    }
}
```

To define a subscriber endpoint you simply have to respond to http calls. Here is the simplest .net subscriber endpoint defined using simple API:

``` csharp
var app = WebApplication.CreateBuilder(args).Build();

app.MapPost("/subscriber1", () => Results.Ok());

app.Run();
```

[^1]: The emulator only support https communication to simulate real usage scenario.

## License

This code is licensed under the Apache License, Version 2.0. You may obtain a copy of this license at https://github.com/gsoft-inc/workleap-license/blob/main/LICENSE.
