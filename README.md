# Slack-Channel-Management

이 도구는 슬랙 공개 채널 목록을 추출하고, 여러 채널을 한 번에 아카이빙할 수 있는 도구입니다.

실행을 위해서는 .NET 6 이 필요합니다.
[https://dotnet.microsoft.com/download]

## 실행 예

사용자 명령 창 (cmd.exe) 또는 터미널을 열고, src 디렉토리에서 `dotnet run` 으로 실행합니다.

사용자 입력은 `{ }` 로 표시했습니다. 토큰을 얻는 법에 대해서는 이 문서의 '앱 설치하고 유저 토큰 얻기' 섹션을 참고하세요.

### 공개 채널 목록 CSV 파일로 추출하기
```
Slack User Token : {XXXXXXXXXXXXX}

=== Slack Channel Management Tool ===
[1] Export public channel list
[2] Archive specific public channels (by channel name)
[3] Archive specific public channels (by channel ID)
[4] Exit
Enter Selection Number : {1}

Enter output report file path : {output.csv}

100/250 channels exported..
200/250 channels exported..
250/250 channels exported..
Done.
```

### 여러 채널을 한 번에 아카이빙하기

먼저, 다음과 같이 채널 이름을 담은 텍스트 파일을 만듭니다.
```
misc
programming-server
programming-server-old
tech-
tech
```

이후 2번 메뉴를 선택하여 아카이브를 하고 나면 다음과 같은 결과를 얻습니다.

```
Channel Name,Result,Channel ID
misc,Archived,Cxxxxxxxx
programming-server,Archived,Cxxxxxxxx
programming-server-old,Non-Archived Public Channel Not Found,Cxxxxxxxx
tech-,Non-Archived Public Channel Not Found,Cxxxxxxxx
tech,Archived,Cxxxxxxxx
```

다음과 같이 실행합니다.
```
Slack User Token : {XXXXXXXXXXXXX}

=== Slack Channel Management Tool ===
[1] Export public channel list
[2] Archive specific public channels (by channel name)
[3] Archive specific public channels (by channel ID)
[4] Exit
Enter Selection Number : {2}

Enter channel list file path : {channel_list.txt}
Enter archive report file path : {archive_result.csv}

100/120 channels processed..
120/120 channels processed..
Done.
```

만약 채널 이름대신 채널 ID(슬랙 내부 값) 을 이용하고 싶다면, 텍스트 파일에 채널 ID를 기입하고 3번 메뉴를 사용합니다.

## 앱 설치하고 유저 토큰 얻기

주의 : 이 프로그램의 실행을 마친 후에는 반드시 앱을 삭제해주세요. 이 문서 하단의 '앱 삭제'를 참고하세요.

1. https://api.slack.com/apps 로 이동합니다.
2. `Create New App`을 누릅니다.
3. `From an app manifest` 을 선택합니다.
4. 채널 관리를 할 워크스페이스를 선택합니다.
5. `Enter app manifest below` 에서 `JSON` 과 `YAML` 중 `JSON` 을 선택하고, 내용을 다음과 같이 채웁니다.

```JSON
{
    "display_information": {
        "name": "Slack Channel Management App"
    },
    "oauth_config": {
        "scopes": {
            "user": [
                "channels:read",
                "channels:write",
                "channels:history"
            ]
        }
    },
    "settings": {
        "org_deploy_enabled": false,
        "socket_mode_enabled": false,
        "token_rotation_enabled": false
    }
}
```

6. 다음 화면에서 정보가 제대로 들어갔는 지 확인합니다.
- OAuth : `User Scopes (3) : channels:read, channels:write, channels:history`
- Features : 빈 칸 (App Home 만 표시되고 다른 내용 없음)

7. Create를 누릅니다. 앱이 생성됩니다.

8. 화면 왼쪽 메뉴의 `Install App` 을 누르고, `Install to workspace`를 누릅니다.

9. 이제 `Install App` 메뉴에서 유저 토큰을 복사할 수 있습니다. (xoxp- 로 시작)

10. 이 토큰을 토큰 관리 도구 프로그램 사용 시 입력합니다.

## 앱 삭제

프로그램 사용이 끝나면, 앱을 반드시 삭제해 줍니다.

1. https://api.slack.com/apps 으로 이동합니다.

2. 프로그램 목록에서 아까 생성한 `Slack Channel Management App` 을 누릅니다.

3. `Basic Information` 맨 아래의 `Delete App` 을 눌러 앱을 삭제합니다.
