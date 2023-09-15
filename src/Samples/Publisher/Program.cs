using Azure;
using Azure.Messaging.EventGrid;

var client1 = new EventGridPublisherClient(
    new Uri("http://localhost:6500/activities-eg/api/events"),
    new AzureKeyCredential("dummyAccessKey"));

var client2 = new EventGridPublisherClient(
    new Uri("http://localhost:6500/comments-eg/api/events"),
    new AzureKeyCredential("dummyAccessKey"));

var data = new Dictionary<string, string>();
data["foo"] = "bar";

var response1 = client1.SendEvent(new EventGridEvent("thesubject", "theeventtype", "1.0", data));
Console.WriteLine(response1.Status);

var response2 = client2.SendEvent(new EventGridEvent("thesubject", "theeventtype", "1.0", data));
Console.WriteLine(response2.Status);
