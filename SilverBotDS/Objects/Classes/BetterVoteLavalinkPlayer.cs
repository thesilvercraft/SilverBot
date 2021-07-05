﻿using DSharpPlus.Entities;
using Lavalink4NET.Events;
using Lavalink4NET.Player;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

#nullable enable

namespace SilverBotDS.Objects.Classes
{
    public enum LoopSettings
    {
        [EnumMember(Value = "none")]
        NotLooping,

        [EnumMember(Value = "song")]
        LoopingSong,

        [EnumMember(Value = "queue")]
        LoopingQueue
    }

    public class BetterVoteLavalinkPlayer : VoteLavalinkPlayer
    {
        public LoopSettings LoopSettings { get; set; } = LoopSettings.NotLooping;
        public List<Tuple<LavalinkTrack,DateTime>> QueueHistory { get; set; } = new();

        public override Task SkipAsync(int count = 1)
        {
            return SkipAsync(count, true);
        }

        public Task SkipAsync(int count, bool command)
        {
            if (count <= 0)
            {
                return Task.CompletedTask;
            }
            EnsureNotDestroyed();
            EnsureConnected();
            if (!command && LoopSettings == LoopSettings.LoopingSong && CurrentTrack != null)
            {
                return PlayAsync(CurrentTrack, false);
            }
            else if (!Queue.IsEmpty)
            {
                LavalinkTrack? track = null;
                while (count-- > 0)
                {
                    track = Queue.Dequeue();
                    QueueHistory.Add(new(track,DateTime.UtcNow));
                    if (LoopSettings is LoopSettings.LoopingQueue)
                    {
                        Queue.Add(track);
                    }
                }
                return PlayAsync(track!, false);
            }
            else
            {
                StopAsync();
            }
            return Task.CompletedTask;
        }

        public event EventHandler<Tuple<DiscordUser, string>>? OnWebsiteEvent;

        public event EventHandler<TrackStartedEventArgs>? OnNewTrack;

        public void RemoveOnWebsiteEventHandelers()
        {
            OnWebsiteEvent = null;
        }

        public void TriggerWebsiteEvent(DiscordUser user, string action)
        {
            EventHandler<Tuple<DiscordUser, string>>? handler = OnWebsiteEvent;
            handler?.Invoke(this, new Tuple<DiscordUser, string>(user, action));
        }

        public override Task OnTrackStartedAsync(TrackStartedEventArgs eventArgs)
        {
            EventHandler<TrackStartedEventArgs>? handler = OnNewTrack;
            handler?.Invoke(this, eventArgs);
            return base.OnTrackStartedAsync(eventArgs);
        }

        public override Task OnTrackEndAsync(TrackEndEventArgs eventArgs)
        {
            if (eventArgs.MayStartNext)
            {
                return SkipAsync(1, false);
            }
            else
            {
                return Task.CompletedTask;
            }
        }
    }
}