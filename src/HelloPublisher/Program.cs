using Azure;
using Azure.Identity;
using Azure.Messaging.EventGrid;

var client1 = new EventGridPublisherClient(
    new Uri("https://localhost:6000/webhook-200"),
    new AzureCliCredential());

var data = new Dictionary<string, string>();
data["foo"] = "bar";

Response response = client1.SendEvent(new EventGridEvent("thesubject", "theeventtype", "1.0", data));
Console.WriteLine(response.Status);
Console.ReadLine();
