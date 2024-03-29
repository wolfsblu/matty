﻿using System;
using System.Linq;
using AutoMapper;
using Model.Client;
using Model.Server;
using Synapse;
using UI.Views;
using Profile = Model.Client.Profile;
using Room = Model.Server.Room;

namespace UI
{
    static class MapperConfig
    {
        private static bool isInitialized;

        public static void Initialize()
        {
            if (isInitialized)
                return;

            Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<LoginViewModel, Credentials>()
                    .ForMember(dst => dst.user, opts => opts.MapFrom(src => src.Username))
                    .ForMember(dst => dst.password, opts => opts.MapFrom(src => src.Password))
                    .ForAllOtherMembers(dst => dst.Ignore());

                cfg.CreateMap<RoomEvent, Message>()
                    .ForMember(dst => dst.Sender, opts => opts.MapFrom(src => src.Sender))
                    .ForMember(dst => dst.Text, opts => opts.MapFrom(src => src.Content.Body))
                    .ForMember(dst => dst.Url, opts => opts.MapFrom(src => src.Content.Url))
                    .ForMember(dst => dst.IsFollowup, opts => opts.MapFrom(src => src.IsFollowup))
                    .ForMember(dst => dst.IsLastFollowup, opts => opts.MapFrom(src => src.IsLastFollowup))
                    .ForMember(dst => dst.IsMine, opts => opts.MapFrom(src => src.IsMine))
                    .ForMember(dst => dst.MessageType, opts => opts.MapFrom(src => src.Content.Type))
                    .ForPath(dst => dst.Image.Height, opts => opts.MapFrom(src => src.Content.Height))
                    .ForPath(dst => dst.Image.Width, opts => opts.MapFrom(src => src.Content.Width))
                    .ForPath(dst => dst.Image.ThumbnailUrl, opts => opts.MapFrom(src => src.Content.ThumbnailUrl))
                    .ForPath(dst => dst.Image.Url, opts => opts.MapFrom(src => src.Content.Url))
                    .ForMember(dst => dst.Timestamp,
                        opts => opts.MapFrom(src => DateTimeOffset.FromUnixTimeMilliseconds(src.Timestamp)))
                    .ForAllOtherMembers(mem => mem.Ignore());

                cfg.CreateMap<Model.Server.Room, Model.Client.Room>()
                    .ForMember(dst => dst.Messages,
                        opts => opts.MapFrom(src => src.History.Events.Where(e => e.Type.Equals("m.room.message"))))
                    .ForMember(dst => dst.Name, opts => opts.ResolveUsing(ResolveRoomName))
                    .ForMember(dst => dst.MessagePreview, opts => opts.ResolveUsing(GenerateMessagePreview))
                    .ForMember(dst => dst.AvatarUrl, opts => opts.ResolveUsing(GetRoomAvatarUrl))
                    .ForAllOtherMembers(mem => mem.Ignore());

                cfg.CreateMap<UserProfile, Profile>()
                    .ForMember(dst => dst.AvatarUrl, opts => opts.MapFrom(src => src.AvatarUrl))
                    .ForMember(dst => dst.DisplayName, opts => opts.MapFrom(src => src.DisplayName))
                    .ForAllOtherMembers(mem => mem.Ignore());
            });
            isInitialized = true;
        }

        private static string GetRoomAvatarUrl(Room room)
        {
            var avatarEvent = room.State.Events.LastOrDefault(e => e.Type.Equals("m.room.avatar"));
            if (avatarEvent != null) return avatarEvent.Content.Url;

            // Direct chats usually don't have an avatar event, use the profile picture of the other user
            var user = SynapseClient.Instance.User.ID;
            var joinEvent = room.State.Events.FirstOrDefault(e => 
                e.Type.Equals("m.room.member") && e.Membership.Equals("join") && !e.Sender.Equals(user));
            return joinEvent?.Content.UserAvatarUrl;
        }

        private static string GenerateMessagePreview(Room room)
        {
            var lastMessage = room.History.Events.LastOrDefault(e => e.Type.Equals("m.room.message"));
            return lastMessage?.Content.Body;
        }

        private static string ResolveRoomName(Room room)
        {
            var nameEvent = room.State.Events.LastOrDefault(e => e.Type.Equals("m.room.name"));
            if (nameEvent != null) return nameEvent.Content.Name;

            var user = SynapseClient.Instance.User.ID;
            var joinEvent = room.State.Events.FirstOrDefault(e =>
                e.Type.Equals("m.room.member") && e.Membership.Equals("join") && !e.Sender.Equals(user));
            return joinEvent?.Content.DisplayName;

        }
    }
}
