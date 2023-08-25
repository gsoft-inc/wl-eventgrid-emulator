using Azure;
using Azure.Identity;
using Azure.Messaging.EventGrid;

var client1 = new EventGridPublisherClient(
    new Uri("https://localhost:6500/activities-eg/api/events"),
    new AzureCliCredential());

var client2 = new EventGridPublisherClient(
    new Uri("https://localhost:6500/comments-eg/api/events"),
    new AzureCliCredential());

var data = new Dictionary<string, string>();
data["foo"] = "bar";

Response response1 = client1.SendEvent(new EventGridEvent("thesubject", "theeventtype", "1.0", data));
Console.WriteLine(response1.Status);

Response response2 = client2.SendEvent(new EventGridEvent("thesubject", "theeventtype", "1.0", data));
Console.WriteLine(response2.Status);
