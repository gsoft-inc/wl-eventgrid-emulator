using Azure;
using Azure.Identity;
using Azure.Messaging.EventGrid;

var client1 = new EventGridPublisherClient(
    new Uri("https://localhost:5000/comments-eg/api/events"),
    new AzureKeyCredential("dummy"));

var client2 = new EventGridPublisherClient(
    new Uri("https://localhost:5000/activities-eg/api/events"),
    new AzureCliCredential());

var data = new Dictionary<string, string>();
data["foo"] = "bar";

client1.SendEvent(new EventGridEvent("thesubject", "theeventtype", "1.0", data));
client2.SendEvent(new EventGridEvent("thesubject", "theeventtype", "1.0", data));
