﻿using AutoMapper;
using Macli.Synapse.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Globalization.NumberFormatting;
using Macli.Processing;
using Macli.Processing.Comparers;
using Macli.Storage;
using Macli.Views;

namespace Macli.Synapse
{
    sealed class SynapseClient
    {
        public static SynapseClient Instance => instance.Value;
        private static readonly Lazy<SynapseClient> instance = new Lazy<SynapseClient>(() => new SynapseClient());

        public User User { get; set; }
        public string EndpointUrl { get; set; }

        private SynapseClient() { }

        public void Initialize(InitialState state)
        {
            User = state.User;
            EndpointUrl = state.EndpointUrl;
            SynapseAPI.SetHomeserver(state.EndpointUrl);
        }

        public async Task LoginAsync(LoginViewModel viewModel)
        {
            if (!string.IsNullOrEmpty(viewModel.HomeserverUrl))
            {
                AppStorage.SaveEndpointUrl(viewModel.HomeserverUrl);
                SynapseAPI.SetHomeserver(viewModel.HomeserverUrl);
            }

            var credentials = Mapper.Map<Credentials>(viewModel);
            User = await SynapseAPI.LoginAsync(credentials);
        }

        public async Task<IEnumerable<Views.Models.Room>> LoadRoomsAsync(RoomViewModel viewModel)
        {
            var syncState = await SynapseAPI.SyncAsync(User.AccessToken);
            IEnumerable<Room> rooms = syncState.JoinedRooms.Values;

            var messageProcessor = new SequenceProcessor<RoomEvent>();
            messageProcessor.DefineSequence("FollowupMessages", new FollowupComparer());
            messageProcessor.AddSequenceRule("FollowupMessages", sequence => sequence.Tail.ForEach(item => item.IsFollowup = true));
            messageProcessor.AddSequenceRule("FollowupMessages", sequence => sequence.Last.IsLastFollowup = true);

            var roomProcessor = new SequenceProcessor<Room>();
            roomProcessor.AddRule(r => messageProcessor.Process(r.History.Events));
            roomProcessor.Process(rooms.ToList());

            return Mapper.Map<IEnumerable<Room>, IEnumerable<Views.Models.Room>>(rooms);
        }

        public string GetPreviewUrl(string mxcUrl, int width, int height)
        {
            var uri = new Uri(mxcUrl);
            return SynapseAPI.GetPreviewUrl(uri.Host, uri.Segments[1], width, height);
        }
    }
}