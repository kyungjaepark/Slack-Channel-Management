using KayJay.WebCli;

return WebConsole.Init(args, WebMain);

async Task<int> WebMain(string[] args)
{

    string slackToken = "";
    if (args.Length > 0)
        slackToken = args[0].Trim();
    else
    {
        Console.WriteLine("Slack User Token을 입력해주세요. ");
        Console.WriteLine("Token을 얻는 방법은 https://github.com/kyungjaepark/Slack-Channel-Management 의 ");
        Console.WriteLine("'앱 설치하고 유저 토큰 얻기' 항목을 참고하세요. ");
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
        var inputLine = Console.ReadLine();
        if (String.IsNullOrEmpty(inputLine))
            continue;
        var inputKey = inputLine[0];
        if (inputKey == '1')
            new SlackChannelManangement.SlackChannel().ExportPublicChannelList(slackToken);
        if (inputKey == '2')
            new SlackChannelManangement.SlackChannel().ArchivePublicChannels(slackToken, false);
        if (inputKey == '3')
            new SlackChannelManangement.SlackChannel().ArchivePublicChannels(slackToken, true);
        if (inputKey == '4')
            break;
    }
    return 0;
}