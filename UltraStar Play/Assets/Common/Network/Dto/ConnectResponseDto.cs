﻿public class ConnectResponseDto : JsonSerializable
{
    public string ClientName { get; set; }
    public string ClientId { get; set; }
    public int MicrophonePort { get; set; }
    public int MicrophoneSampleRate { get; set; }
    public string ErrorMessage { get; set; }
    public int HttpServerPort { get; set; }
}
