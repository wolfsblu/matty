﻿using RestSharp.Deserializers;

namespace Model.Server
{
    public class RoomEvent
    {
        [DeserializeAs(Name = "origin_server_ts")]
        public long Timestamp { get; set; }

        public string Sender { get; set; }

        [DeserializeAs(Name = "event_id")]
        public string EventID { get; set; }

        public EventContent Content { get; set; }

        [DeserializeAs(Name = "type")]
        public string Type { get; set; }

        public string Membership { get; set; }

        public string Preview { get; set; }

        // Custom properties (not sent by server)
        public bool IsFollowup { get; set; }
        public bool IsLastFollowup { get; set; }
        public bool IsMine { get; set; }

        public override string ToString()
        {
            return $"{Sender}: {Content.Body}";
        }
    }
}