﻿using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MikuSharp.Events;
using MikuSharp.Enums;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using System.IO;
using HeyRed.Mime;
using System.Net.Http.Headers;
using AngleSharp.Html.Parser;
using System.Text.RegularExpressions;
using FluentFTP;
using System.Diagnostics;
using AngleSharp.Dom;
using System.Threading;

namespace MikuSharp.Entities
{
    public class MusicInstance
    {
        public int shardID { get; set; }
        public ulong usedChannel { get; set; }
        public Playstate playstate { get; set; }
        public RepeatMode repeatMode { get; set; }
        public int repeatAllPos { get; set; }
        public ShuffleMode shuffleMode { get; set; }
        public DateTime aloneTime { get; set; }
        public CancellationTokenSource aloneCTS { get; set; }
        public LavalinkNodeConnection nodeConnection { get; set; }
        public LavalinkGuildConnection guildConnection { get; set; }
        public List<QueueEntry> queue { get; set; }
        public QueueEntry currentSong { get; set; }
        public QueueEntry lastSong { get; set; }

        public MusicInstance(LavalinkNodeConnection node, int shard)
        {
            shardID = shard;
            nodeConnection = node;
            usedChannel = 0;
            playstate = Playstate.NotPlaying;
            repeatMode = RepeatMode.Off;
            repeatAllPos = 0;
            shuffleMode = ShuffleMode.Off;
            queue = new List<QueueEntry>();
        }
        public async Task<LavalinkGuildConnection> ConnectToChannel(DiscordChannel channel)
        {
            switch (channel.Type)
            {
                case DSharpPlus.ChannelType.Voice:
                    {
                        guildConnection = await nodeConnection.ConnectAsync(channel);
                        return guildConnection;
                    }
                default: return null;
            }
        }
        public async Task<TrackResult> QueueSong(string n, CommandContext ctx, int pos = -1)
        {
            var inter = ctx.Client.GetInteractivity();
            if (n.ToLower().StartsWith("http://nicovideo.jp")
                || n.ToLower().StartsWith("http://sp.nicovideo.jp")
                || n.ToLower().StartsWith("https://nicovideo.jp")
                || n.ToLower().StartsWith("https://sp.nicovideo.jp")
                || n.ToLower().StartsWith("http://www.nicovideo.jp")
                || n.ToLower().StartsWith("https://www.nicovideo.jp"))
            {
                var msg = await ctx.RespondAsync("Processing NND Video...");
                var split = n.Split("/".ToCharArray());
                var nndID = split.First(x => x.StartsWith("sm") || x.StartsWith("nm")).Split("?")[0];
                FtpClient client = new FtpClient(Bot.cfg.NndConfig.FtpConfig.Hostname, new NetworkCredential(Bot.cfg.NndConfig.FtpConfig.User, Bot.cfg.NndConfig.FtpConfig.Password));
                await client.ConnectAsync();
                if (!await client.FileExistsAsync($"{nndID}.mp3"))
                {
                    await msg.ModifyAsync("Preparing download...");
                    var ex = await Utilities.NND.GetNND(nndID, msg);
                    if (ex == null)
                    {
                        await msg.ModifyAsync("Please try again or verify the link");
                        return null;
                    }
                    await msg.ModifyAsync("Uploading");
                    await client.UploadAsync(ex, $"{nndID}.mp3", FtpExists.Skip, true);
                }
                var Track = await nodeConnection.GetTracksAsync(new Uri($"https://nnd.meek.moe/new/{nndID}.mp3"));
                if (pos == -1) queue.Add(new QueueEntry(Track.Tracks.First(), ctx.Member));
                else queue.Insert(pos, new QueueEntry(Track.Tracks.First(), ctx.Member));
                if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped)) await PlaySong();
                return new TrackResult(Track.PlaylistInfo, Track.Tracks.First());
            }
            if (n.StartsWith("http://") | n.StartsWith("https://"))
            {
                var s = await nodeConnection.GetTracksAsync(new Uri(n));
                switch (s.LoadResultType)
                {
                    case LavalinkLoadResultType.LoadFailed: return null;
                    case LavalinkLoadResultType.NoMatches: return null;
                    case LavalinkLoadResultType.PlaylistLoaded:
                        {
                            if (s.PlaylistInfo.SelectedTrack == -1)
                            {
                                var msg = await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
                                    .WithTitle("Playlist link detected!")
                                    .WithDescription("Please respond with either:\n" +
                                    "``yes``, ``y`` or ``1`` to add the **entire** playlist or\n" +
                                    "``no``, ``n``, ``0`` or let this time out to cancel")
                                    .WithAuthor($"Requested by {ctx.Member.Username}#{ctx.Member.Discriminator} || Timeout 25 seconds", iconUrl: ctx.Member.AvatarUrl)
                                    .Build());
                                var resp = await inter.WaitForMessageAsync(x => x.Author.Id == ctx.User.Id , TimeSpan.FromSeconds(25));
                                await msg.DeleteAsync();
                                if (resp.TimedOut)
                                {
                                    return null;
                                }
                                if (resp.Result.Content == "y" || resp.Result.Content == "yes" || resp.Result.Content == "1")
                                {
                                    foreach (var e in s.Tracks)
                                        queue.Add(new QueueEntry(e, ctx.Member));
                                    if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped)) await PlaySong();
                                    return new TrackResult(s.PlaylistInfo, s.Tracks);
                                }
                                else return null;
                            }
                            else
                            {
                                var msg = await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
                                    .WithTitle("Link with Playlist detected!")
                                    .WithDescription("Please respond with either:\n" +
                                    "``yes``, ``y`` or ``1`` to add only the referred song in the link or\n" +
                                    "``all`` or ``a`` to add the entire playlistor\n" +
                                    "``no``, ``n``, ``0`` or let this time out to cancel")
                                    .WithAuthor($"Requested by {ctx.Member.Username}#{ctx.Member.Discriminator} || Timeout 25 seconds", iconUrl: ctx.Member.AvatarUrl)
                                    .Build());
                                var resp = await inter.WaitForMessageAsync(x => x.Author.Id == ctx.User.Id, TimeSpan.FromSeconds(25));
                                await msg.DeleteAsync();
                                if (resp.TimedOut)
                                {
                                    return null;
                                }
                                if (resp.Result.Content == "y" || resp.Result.Content == "yes" || resp.Result.Content == "1")
                                {
                                    if (pos == -1) queue.Add(new QueueEntry(s.Tracks.ElementAt(s.PlaylistInfo.SelectedTrack), ctx.Member));
                                    else queue.Insert(pos, new QueueEntry(s.Tracks.ElementAt(s.PlaylistInfo.SelectedTrack), ctx.Member));
                                    if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped)) await PlaySong();
                                    return new TrackResult(s.PlaylistInfo, s.Tracks.ElementAt(s.PlaylistInfo.SelectedTrack));
                                }
                                if (resp.Result.Content == "a" || resp.Result.Content == "all")
                                {
                                    if (pos == -1)
                                        foreach (var e in s.Tracks)
                                            queue.Add(new QueueEntry(e, ctx.Member));
                                    else
                                        foreach (var e in s.Tracks.Reverse())
                                            queue.Insert(pos, new QueueEntry(e, ctx.Member));
                                    if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped)) await PlaySong();
                                    return new TrackResult(s.PlaylistInfo, s.Tracks);
                                }
                                else return null;
                            }
                        };
                    default:
                        {
                            if (pos == -1) queue.Add(new QueueEntry(s.Tracks.First(), ctx.Member));
                            else queue.Insert(pos, new QueueEntry(s.Tracks.First(), ctx.Member));
                            if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped)) await PlaySong();
                            return new TrackResult(s.PlaylistInfo, s.Tracks.First());
                        };
                }
            }
            else
            {
                var s = await nodeConnection.GetTracksAsync(n);
                switch (s.LoadResultType)
                {
                    case LavalinkLoadResultType.LoadFailed: return null;
                    case LavalinkLoadResultType.NoMatches: return null;
                    default:
                        {
                            var em = new DiscordEmbedBuilder()
                                .WithTitle("Results!")
                                .WithDescription("Please select a track by responding to this with:\n")
                                .WithAuthor($"Requested by {ctx.Member.Username}#{ctx.Member.Discriminator} || Timeout 25 seconds", iconUrl: ctx.Member.AvatarUrl);
                            for (int i = 0; i < 5; i++)
                            {
                                em.AddField($"{i + 1}.{s.Tracks.ElementAt(i).Title} [{s.Tracks.ElementAt(i).Length}]", $"by {s.Tracks.ElementAt(i).Author} [Link]({s.Tracks.ElementAt(i).Uri})");
                            }
                            var msg = await ctx.RespondAsync(embed: em.Build());
                            var resp = await inter.WaitForMessageAsync(x => x.Author.Id == ctx.User.Id, TimeSpan.FromSeconds(25));
                            await msg.DeleteAsync();
                            if (resp.TimedOut)
                            {
                                return null;
                            }
                            if (resp.Result.Content == "1")
                            {
                                if (pos == -1)queue.Add(new QueueEntry(s.Tracks.ElementAt(0), ctx.Member));
                                else queue.Insert(pos,new QueueEntry(s.Tracks.ElementAt(0), ctx.Member));
                                if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped)) await PlaySong();
                                return new TrackResult(s.PlaylistInfo, s.Tracks.ElementAt(0));
                            }
                            if (resp.Result.Content == "2")
                            {
                                if (pos == -1) queue.Add(new QueueEntry(s.Tracks.ElementAt(1), ctx.Member));
                                else queue.Insert(pos, new QueueEntry(s.Tracks.ElementAt(1), ctx.Member));
                                if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped)) await PlaySong();
                                return new TrackResult(s.PlaylistInfo, s.Tracks.ElementAt(1));
                            }
                            if (resp.Result.Content == "3")
                            {
                                if (pos == -1) queue.Add(new QueueEntry(s.Tracks.ElementAt(2), ctx.Member));
                                else queue.Insert(pos, new QueueEntry(s.Tracks.ElementAt(2), ctx.Member));
                                if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped)) await PlaySong();
                                return new TrackResult(s.PlaylistInfo, s.Tracks.ElementAt(2)); ;
                            }
                            if (resp.Result.Content == "4")
                            {
                                if (pos == -1) queue.Add(new QueueEntry(s.Tracks.ElementAt(3), ctx.Member));
                                else queue.Insert(pos, new QueueEntry(s.Tracks.ElementAt(3), ctx.Member));
                                if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped)) await PlaySong();
                                return new TrackResult(s.PlaylistInfo, s.Tracks.ElementAt(3));
                            }
                            if (resp.Result.Content == "5")
                            {
                                if (pos == -1) queue.Add(new QueueEntry(s.Tracks.ElementAt(4), ctx.Member));
                                else queue.Insert(pos, new QueueEntry(s.Tracks.ElementAt(4), ctx.Member));
                                if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped)) await PlaySong();
                                return new TrackResult(s.PlaylistInfo, s.Tracks.ElementAt(4));
                            }
                            else return null;
                        };
                }
            }
        } 
        public async Task<QueueEntry> PlaySong()
        {
            var cur = lastSong;
            if (queue.Count != 1 && repeatMode == RepeatMode.All)
                repeatAllPos++;
            if (repeatAllPos >= queue.Count)
                repeatAllPos = 0;
            if (shuffleMode == ShuffleMode.Off)
                currentSong = queue[0];
            else
                currentSong = queue[new Random().Next(0, queue.Count)];
            if (repeatMode == RepeatMode.All)
                currentSong = queue[repeatAllPos];
            if (repeatMode == RepeatMode.On)
                currentSong = cur;
            guildConnection.PlaybackFinished += Lavalink.LavalinkTrackFinish;
            playstate = Playstate.Playing;
            await Task.Run(() => guildConnection.Play(currentSong.track));
            return currentSong;
        }
    }

    public class TrackResult
    {
        public LavalinkPlaylistInfo PlaylistInfo { get; set; }
        public List<LavalinkTrack> Tracks { get; set; }
        public TrackResult(LavalinkPlaylistInfo pl, IEnumerable<LavalinkTrack> tr)
        {
            PlaylistInfo = pl;
            Tracks = new List<LavalinkTrack>();
            Tracks.AddRange(tr);
        }
        public TrackResult(LavalinkPlaylistInfo pl, LavalinkTrack tr)
        {
            PlaylistInfo = pl;
            Tracks = new List<LavalinkTrack>();
            Tracks.Add(tr);
        }
    }

    public class Entry
    {
        public LavalinkTrack track { get; protected set; }
        public DateTimeOffset additionDate { get; protected set; }
        public Entry(LavalinkTrack t)
        {
            track = t;
            additionDate = DateTimeOffset.UtcNow;
        }
    }

    public class QueueEntry : Entry
    {
        public DiscordMember addedBy { set; get; }
        public QueueEntry(LavalinkTrack t, DiscordMember m) : base(t)
        {
            addedBy = m;
        }
    }

    //     B/S(｀・ω・´) ❤️ (´ω｀)U/C
}