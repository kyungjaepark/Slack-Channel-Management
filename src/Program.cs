
string slackToken = "";
if (args.Length > 0)
    slackToken = args[0].Trim();
else
{
    Console.Write("Slack User Token : ");
    slackToken = Console.ReadLine()?.Trim() ?? "";
}

while (true)
{
    Console.WriteLine();
    Console.WriteLine("=== Slack Channel Management Tool ===");
    Console.WriteLine("");
    Console.WriteLine("[1] Export public channel list");
    Console.WriteLine("[2] Archive specific public channels (by channel name)");
    Console.WriteLine("[3] Archive specific public channels (by channel ID)");
    Console.WriteLine("[4] Exit");
    Console.WriteLine("");
    Console.Write("Enter Selection Number : ");
    var inputKey = Console.ReadKey().KeyChar;
        Console.WriteLine();
    if (inputKey == '1')
        new SlackChannelManangement.SlackChannel().ExportPublicChannelList(slackToken);
    if (inputKey == '2')
        new SlackChannelManangement.SlackChannel().ArchivePublicChannels(slackToken, false);
    if (inputKey == '3')
        new SlackChannelManangement.SlackChannel().ArchivePublicChannels(slackToken, true);
    if (inputKey == '4')
        break;
}
