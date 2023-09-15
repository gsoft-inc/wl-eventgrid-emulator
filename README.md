# Workleap Azure Event Grid Emulator

This is an open source emulator for [Azure Event Grid](https://learn.microsoft.com/en-us/azure/event-grid/overview) that supports only the [push delivery](https://learn.microsoft.com/en-us/azure/event-grid/push-delivery-overview) model. Based on ASP.NET Core, this emulator provides a cross-platform experience for developers wanting to try Azure Event Grid easily in a local environment without having to deploy to Azure.

This project is not affiliated, associated, authorized, endorsed by, or in any way officially connected with Microsoft.

## Features

- Support for multiple Event Grid topics by sending events to `http://127.0.0.1:6500/<topic-name>/api/events`.
- Push delivery to configured webhooks defined in the emulator configuration file (more details below).
- Simple but durable message delivery and retry based on the [Azure Event Grid documentation](https://learn.microsoft.com/en-us/azure/event-grid/delivery-and-retry).
- Ability to add and remove topics and webhooks at runtime without having to restart the emulator.
- As the emulator is built on top of ASP.NET Core, you can follow this [Microsoft documentation](https://learn.microsoft.com/en-us/aspnet/core/security/docker-compose-https) to run on HTTPS.

## Prerequisites

You must have [Docker](https://www.docker.com/get-started/) installed. This Event Grid emulator is only distributed as a Docker image.

## Getting started

The first step is to **create a configuration file** for the emulator to know the topics, and for each topic, the webhooks to call when an event is published.
Create a configuration file named `appsettings.json` somewhere on your computer, for instance: `C:\eventgridemulator\appsettings.json`.

It should look like this:

```json
{
  "Topics": {
    "topic1": [
      "https://host.docker.internal:5122/my-webhook",
      "http://host.docker.internal:7221/eventgrid"
    ],
    "topic2": [
      "https://mydockercontainer:5122/eventgrid/domainevents",
    ]
  }
}
```

In this example, we have two topics, `topic1` and `topic2`. If an event is sent to the emulator on this URL `http://127.0.0.1:6500/topic1/api/events`, the emulator would forward the events to `https://host.docker.internal:5122/my-webhook` and `http://host.docker.internal:7221/eventgrid` on your host machine. As the emulator runs on Docker, you must use the `host.docker.internal` host whenever you want to call a webhook on your host machine.

**Run the Event Grid emulator with docker run**

```bash
docker run -p 6500:6500 -v "C:/eventgridemulator/appsettings.json:/app/appsettings.json" --add-host=host.docker.internal:host-gateway workleap/eventgridemulator
```

> `--add-host=host.docker.internal:host-gateway` is required for the emulator to be able to reach the webhooks on the host machine.

**Run the Event Grid emulator with Docker Compose**

Create a file named `docker-compose.yaml` and add this content:

```yaml
version: '3'

services:
  eventgridemulator:
    image: workleap/eventgridemulator:latest
    ports:
      - "6500:6500"
    volumes:
      - "C:/eventgridemulator/appsettings.json:/app/appsettings.json"
    extra_hosts:
      - "host.docker.internal:host-gateway"
```

From the directory in which the file resides, run the `docker compose up` command.

**Sending and receiving events**

Now that the emulator is running, you can send events to it and receive them in your webhooks. If you're using C#, follow [these steps from the Microsoft documentation](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/messaging.eventgrid-readme?view=azure-dotnet):

```csharp
// Change "my-topic" to the name of your topic.
// The authentication mechanism is actually ignored by the emulator.
// If you must provide a TokenCredential instead of an access key, the emulator must be running on HTTPS.
var client = new EventGridPublisherClient(
    new Uri("http://127.0.0.1:6500/my-topic/api/events"),
    new AzureKeyCredential("fakeAccessKey"));
```

## Additional information

- As mentioned above, the `EventGridPublisherClient` requires an authentication mechanism, but the actual value is ignored by the emulator. You can use any value you want.
- Using `TokenCredential` (Azure Identity) instead of an access key requires the emulator to be running on HTTPS. The `EventGridPublisherClient` will throw an exception otherwise.
- The Event Grid validation mechanism is not implemented in the emulator. You can send events without having to validate your webhooks. This is because the emulator is not meant to be used in a production environment.
- The emulator tries to replicate the original Event Grid behavior when it comes to retry and HTTP header values. However, it is not guaranteed to be 100% accurate. If you find a bug, please open an issue.
- There's no persistence layer in the emulator, the messages are stored in memory. If you restart the emulator, all pending messages will be lost.

## License

This code is licensed under the Apache License, Version 2.0. You may obtain a copy of this license at https://github.com/gsoft-inc/workleap-license/blob/main/LICENSE.