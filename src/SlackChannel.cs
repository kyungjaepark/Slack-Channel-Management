using System;
using System.Net;
using System.Text.Json.Nodes;

namespace SlackChannelManangement
{
    public class SlackChannel
    {
        public void ExportPublicChannelList(String slackToken)
        {
            Console.Write("Enter output report file path : ");
            var outputFilePath = Console.ReadLine().Trim();

            var allChannelList = GetPublicChannelList(slackToken);
            if (allChannelList == null)
                return;

            using StreamWriter sw = new StreamWriter(outputFilePath);
            sw.WriteLine("Channel ID, Channel Name, Description, Members, Last Message");

            var wc = GetNewSlackWebClient(slackToken);
            var channelListCount = allChannelList.Count(r => r["is_archived"].GetValue<bool>() == false);
            var exportedChannelCount = 0;

            DateTime nextStatusTime = DateTime.Now;
            foreach (var channelInfo in allChannelList)
            {
                var isArchived = channelInfo["is_archived"].GetValue<bool>();
                if (isArchived)
                    continue;

                List<String> columns = new List<string>();

                var channelId = $"{channelInfo["id"]}";
                columns.Add(channelId);
                columns.Add(GetCsvSafeString($"{channelInfo["name"]}", true));
                columns.Add(GetCsvSafeString($"{channelInfo["purpose"]["value"]}", true));
                columns.Add($"{channelInfo["num_members"]}");

                var targetUrl = $"https://slack.com/api/conversations.history?channel={channelId}&limit=200";
                var historyString = DownloadStringWithApiLimit(wc, targetUrl);
                var messages = JsonObject.Parse(historyString)["messages"] as JsonArray;
                var latestTimestamp = GetLatestConversationTimestamp(messages);
                columns.Add(ConvertFromUnixTimestamp(latestTimestamp).ToString("yyyy-MM-dd HH:mm:ss"));
                sw.WriteLine(String.Join(",", columns));
                sw.Flush();

                exportedChannelCount++;
                if (exportedChannelCount == channelListCount || nextStatusTime < DateTime.Now)
                {
                    Console.WriteLine($"{exportedChannelCount} / {channelListCount} channels exported..");
                    nextStatusTime = DateTime.Now.AddSeconds(5);
                }
            }
        }

        public string GetCsvSafeString(string originalString, bool removeNewLine = false)
        {
            // CSV에서 잘 읽힐 수 있도록, 큰따옴표로 묶인 스트링을 반환합니다.
            if (String.IsNullOrEmpty(originalString))
                return "\"\"";

            var csvString = originalString.Replace("\"", "\"\"");
            if (removeNewLine)
                csvString = csvString.Replace("\n", "").Replace("\r", "");
            return $"\"{csvString}\"";
        }

        public string DownloadStringWithApiLimit(WebClient wc, String url)
        {
            while (true)
            {
                try
                {
                    return wc.DownloadString(url);
                }
                catch (WebException ex)
                {
                    var retryAfter = GetSlackRetryAfterTime(ex);
                    if (retryAfter == -1)
                        throw ex;
                    Console.Error.WriteLine("API reached time limit. wait for " + retryAfter + " seconds..");
                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(retryAfter));
                }
            }
        }

        public Double GetLatestConversationTimestamp(JsonArray messages)
        {
            if (messages.Any() == false)
                return 0;
            var messages_nonChannelJoinLeave = messages.Where(r => $"{r["subtype"]}" != "channel_join" && $"{r["subtype"]}" != "channel_leave");
            if (messages_nonChannelJoinLeave.Any())
                return messages_nonChannelJoinLeave.Select(r => Double.Parse($"{r["ts"]}")).Max();
            return messages.Select(r => Double.Parse($"{r["ts"]}")).Max();
        }

        private JsonArray GetPublicChannelList(String slackToken)
        {
            var allChannelList = new JsonArray();
            var wc = GetNewSlackWebClient(slackToken);
            {
                var baseUrl = "https://slack.com/api/conversations.list?limit=300&types=public_channel";
                var cursor = "";
                while (true)
                {
                    var url = baseUrl;
                    if (cursor != "")
                        url = url + $"&cursor={cursor}";
                    var resultString = wc.DownloadString(url);
                    var result = JsonObject.Parse(resultString) as JsonObject;
                    if (result["ok"].GetValue<bool>() != true)
                    {
                        Console.Error.WriteLine("Error downloading channel list.");
                        return null;
                    }
                    var channelList = (result["channels"] as JsonArray);
                    foreach (var channel in channelList)
                        allChannelList.Add(JsonNode.Parse(channel.ToJsonString()));

                    var nextCursor = result["response_metadata"]["next_cursor"].ToString();
                    if (String.IsNullOrEmpty(nextCursor))
                        return allChannelList;
                    cursor = nextCursor;
                }
            }
        }

        // https://stackoverflow.com/a/49621777
        static DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddSeconds(timestamp);
        }

        private WebClient GetNewSlackWebClient(String slackToken)
        {
            WebClient wc = new WebClient();
            wc.Headers["Authorization"] = $"Bearer {slackToken}";
            wc.Headers["Content-type"] = "application/json";
            return wc;
        }

        private double GetSlackRetryAfterTime(Exception ex)
        {
            try
            {
                if (ex is WebException wex)
                {
                    if ((int)wex.Status == 429)
                    {
                        var retryAfter = Double.Parse(wex?.Response?.Headers["Retry-After"] ?? "");
                        return retryAfter;
                    }
                }

            }
            catch
            {
                return -1;
            }
            return -1;
        }

        public void ArchivePublicChannels(String slackToken, bool byChannelId = true)
        {
            Console.Write("Enter channel list file path : ");
            var channelListFilePath = Console.ReadLine().Trim();
            List<String> candidateList = new List<string>();
            try
            {
                var srcLines = File.ReadAllLines(channelListFilePath);
                candidateList.AddRange(
                    srcLines.Select(r => r.Trim()).Where(r => String.IsNullOrEmpty(r) == false)
                );
            }
            catch
            {
                Console.Error.WriteLine("Error : error reading file - " + channelListFilePath);
                return;
            }

            if (candidateList.Any() == false)
            {
                Console.Error.WriteLine("Error : file has empty list - " + channelListFilePath);
                return;
            }

            Console.Write("Enter archive report file path : ");
            var archiveReportFilePath = Console.ReadLine().Trim();
            using StreamWriter sw = new StreamWriter(archiveReportFilePath);
            sw.WriteLine("Channel ID, Channel Name, Result");
            var previousResult = new Dictionary<string, string>();

            var allChannelList = GetPublicChannelList(slackToken);
            if (allChannelList == null)
                return;

            var activeChannelList = allChannelList.Where(r => r["is_archived"].GetValue<bool>() == false).ToList();
            var channelIdToChannelNameMap = activeChannelList.ToDictionary(r => $"{r["id"]}", r => $"{r["name"]}");
            var channelNameToChannelIdMap = activeChannelList.ToDictionary(r => $"{r["name"]}", r => $"{r["id"]}");

            DateTime nextStatusTime = DateTime.Now;
            int processedChannelCount = 0;
            foreach (var candidateChannel in candidateList)
            {
                string resultLine = "";
                if (previousResult.ContainsKey(candidateChannel))
                {
                    resultLine = previousResult[candidateChannel];
                }
                else
                {
                    string channelName = null;
                    string channelId = null;
                    string result = null;
                    if (byChannelId)
                    {
                        channelId = candidateChannel;
                        if (channelIdToChannelNameMap.ContainsKey(channelId))
                            channelName = channelNameToChannelIdMap[channelId];

                    }
                    else
                    {
                        channelName = candidateChannel;
                        if (channelNameToChannelIdMap.ContainsKey(channelName))
                            channelId = channelNameToChannelIdMap[channelName];
                    }

                    if (channelName == null || channelId == null)
                    {
                        result = "Non-Archived Public Channel Not Found";
                    }
                    else
                    {
                        int retryCount = 3;
                        while (true)
                        {
                            try
                            {
                                using var wc = GetNewSlackWebClient(slackToken);

                                var url = $"https://slack.com/api/conversations.archive";
                                var payload = new JsonObject();
                                payload["channel"] = channelId;
                                var archiveResultString = wc.UploadString(url, payload.ToString());
                                var archiveResult = JsonNode.Parse(archiveResultString);
                                if (archiveResult["ok"].GetValue<bool>() == true)
                                    result = "Archived";
                                else
                                    result = $"Error : {archiveResult["error"]}";
                                break;
                            }
                            catch (Exception ex)
                            {
                                var retryAfter = GetSlackRetryAfterTime(ex);
                                if (retryAfter == -1 || --retryCount < 0)
                                {
                                    result = "Error while archiving channel";
                                    break;
                                }
                                Console.Error.WriteLine("API reached time limit. wait for " + retryAfter + " seconds..");
                                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(retryAfter));
                            }
                        }
                    }
                    resultLine = $"{channelName},{result},{channelId}";
                }
                sw.WriteLine(resultLine);
                sw.Flush();
                previousResult[candidateChannel] = resultLine;
                processedChannelCount++;
                if (processedChannelCount == candidateList.Count || nextStatusTime < DateTime.Now)
                {
                    Console.WriteLine($"{processedChannelCount} / {candidateList.Count} channels processed..");
                    nextStatusTime = DateTime.Now.AddSeconds(5);
                }
            }
        }
    }
}