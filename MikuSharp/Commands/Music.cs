﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using MikuSharp.Attributes;
using MikuSharp.Entities;
using MikuSharp.Enums;
using MikuSharp.Events;
using MikuSharp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MikuSharp.Commands
{
    class Music : BaseCommandModule
    {
        [Command("join")]
        [Description("Joins the voice cahnnel you're in")]
        [RequireUserVoicechatConnection]
        public async Task Join(CommandContext ctx)
        {
            if (!Bot.Guilds.Any(x => x.Key == ctx.Guild.Id))
            {
                Bot.Guilds.TryAdd(ctx.Guild.Id, new Guild
                {
                    musicInstance = null,
                    shardId = ctx.Client.ShardId
                });
            }
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null)
            {
                g.musicInstance = new Entities.MusicInstance(Bot.LLEU[ctx.Client.ShardId], ctx.Client.ShardId);
            }
            if (!g.musicInstance.guildConnection?.IsConnected == null || !g.musicInstance.guildConnection.IsConnected == false) await g.musicInstance.ConnectToChannel(ctx.Member.VoiceState.Channel);
            g.musicInstance.usedChannel = ctx.Channel.Id;
            await ctx.RespondAsync($"Heya {ctx.Member.Mention}!");
        }

        [Command("leave")]
        [Description("leaves the channel and optionally keeps the Queue")]
        [Usage("|-> I leave and the current queue will be removed",
            "keep |-> I leave and keep the current queue saved")]
        [RequireUserVoicechatConnection]
        public async Task Leave(CommandContext ctx, string Options = null)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null) return;
            if (Options?.ToLower() == "k" || Options?.ToLower() == "keep")
            {
                await Task.Run(() => g.musicInstance.guildConnection.Stop());
                await Task.Run(() => g.musicInstance.guildConnection.Disconnect());
                await ctx.RespondAsync("cya! 💙");
            }
            else
            {
                await Task.Run(() => g.musicInstance.guildConnection.Disconnect());
                await Task.Delay(500);
                g.musicInstance = null;
                await ctx.RespondAsync("cya! 💙");
            }
        }

        [Command("play"), Aliases("p")]
        [Description("Play or Queue a song!")]
        [Usage("songname |-> Searches for a Song with that name (on youtube)",
            "link |-> Play a song directly from a link",
            "``with a music file attached`` |-> Play the songfile you just sent")]
        [RequireUserVoicechatConnection]
        public async Task Play(CommandContext ctx, [RemainingText]string song = null)
        {
            if (!Bot.Guilds.Any(x => x.Key == ctx.Guild.Id))
            {
                Bot.Guilds.TryAdd(ctx.Guild.Id, new Guild
                {
                    musicInstance = null,
                    shardId = ctx.Client.ShardId
                });
            }
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null)
            {
                g.musicInstance = new Entities.MusicInstance(Bot.LLEU[ctx.Client.ShardId], ctx.Client.ShardId);
            }
            if (!g.musicInstance.guildConnection?.IsConnected == null || !g.musicInstance.guildConnection.IsConnected == false) await g.musicInstance.ConnectToChannel(ctx.Member.VoiceState.Channel);
            if (ctx.Message.Attachments.Count == 0 && song == null) return;
            g.musicInstance.usedChannel = ctx.Channel.Id;
            if (song == null) song = ctx.Message.Attachments.First().ProxyUrl;
            var oldState = g.musicInstance.playstate;
            var q = await g.musicInstance.QueueSong(song, ctx);
            if (q == null) return;
            var emb = new DiscordEmbedBuilder();
            if (oldState == Playstate.Playing)
            {
                emb.AddField(q.Tracks.First().Title + "[" + (q.Tracks.First().Length.Hours != 0 ? q.Tracks.First().Length.ToString(@"hh\:mm\:ss") : q.Tracks.First().Length.ToString(@"mm\:ss")) + "]", $"by {q.Tracks.First().Author}\n" +
                    $"Requested by {ctx.Member.Mention}");
                if (q.Tracks.Count != 1)
                {
                    emb.AddField("Playlist added:", $"added {q.Tracks.Count - 1} more");
                }
                await ctx.RespondAsync(embed: emb.WithTitle("Playing").Build());
            }
            else
            {
                if (q.PlaylistInfo.SelectedTrack == -1 || q.PlaylistInfo.Name == null) emb.AddField(q.Tracks.First().Title + "[" + (q.Tracks.First().Length.Hours != 0 ? q.Tracks.First().Length.ToString(@"hh\:mm\:ss") : q.Tracks.First().Length.ToString(@"mm\:ss")) + "]", $"by {q.Tracks.First().Author}\nRequested by {ctx.Member.Mention}");
                else emb.AddField(q.Tracks[q.PlaylistInfo.SelectedTrack].Title + "[" + (q.Tracks[q.PlaylistInfo.SelectedTrack].Length.Hours != 0 ? q.Tracks[q.PlaylistInfo.SelectedTrack].Length.ToString(@"hh\:mm\:ss") : q.Tracks[q.PlaylistInfo.SelectedTrack].Length.ToString(@"mm\:ss")) + "]", $"by {q.Tracks[q.PlaylistInfo.SelectedTrack].Author}\nRequested by {ctx.Member.Mention}");
                if (q.Tracks.Count != 1)
                {
                    emb.AddField("Playlist added:", $"added {q.Tracks.Count - 1} more");
                }
                await ctx.RespondAsync(embed: emb.WithTitle("Added").Build());
            }
        }

        [Command("playinsert"), Aliases("insertplay", "ip")]
        [Description("Queue a song at a specific position!")]
        [Usage("(number) songname |-> Searches for a Song with that name (on youtube)",
            "(number) link |-> Play a song directly from a link",
            "(number) ``with a music file attached`` |-> Play the songfile you just sent")]
        [RequireUserVoicechatConnection]
        public async Task InsertPlay(CommandContext ctx, int pos, [RemainingText]string song = null)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            if (pos < 1) return;
            if (g.musicInstance == null)
            {
                g.musicInstance = new Entities.MusicInstance(Bot.LLEU[ctx.Client.ShardId], ctx.Client.ShardId);
            }
            if (!g.musicInstance.guildConnection?.IsConnected == null || !g.musicInstance.guildConnection.IsConnected == false) await g.musicInstance.ConnectToChannel(ctx.Member.VoiceState.Channel);
            if (ctx.Message.Attachments.Count == 0 && song == null) return;
            g.musicInstance.usedChannel = ctx.Channel.Id;
            if (song == null) song = ctx.Message.Attachments.First().ProxyUrl;
            var oldState = g.musicInstance.playstate;
            var q = await g.musicInstance.QueueSong(song, ctx, pos);
            if (q == null) return;
            var emb = new DiscordEmbedBuilder();
            if (oldState == Playstate.Playing)
            {
                emb.AddField(q.Tracks.First().Title + "[" + (q.Tracks.First().Length.Hours != 0 ? q.Tracks.First().Length.ToString(@"hh\:mm\:ss") : q.Tracks.First().Length.ToString(@"mm\:ss")) + "]", $"by {q.Tracks.First().Author}\n" +
                    $"Requested by {ctx.Member.Mention}\nAt position: {pos}");
                if (q.Tracks.Count != 1)
                {
                    emb.AddField("Playlist added:", $"added {q.Tracks.Count - 1} more");
                }
                await ctx.RespondAsync(embed: emb.WithTitle("Playing").Build());
            }
            else
            {
                if (q.PlaylistInfo.SelectedTrack == -1 || q.PlaylistInfo.Name == null) emb.AddField(q.Tracks.First().Title + "[" + (q.Tracks.First().Length.Hours != 0 ? q.Tracks.First().Length.ToString(@"hh\:mm\:ss") : q.Tracks.First().Length.ToString(@"mm\:ss")) + "]", $"by {q.Tracks.First().Author}\nRequested by {ctx.Member.Mention}");
                else emb.AddField(q.Tracks[q.PlaylistInfo.SelectedTrack].Title + "[" + (q.Tracks[q.PlaylistInfo.SelectedTrack].Length.Hours != 0 ? q.Tracks[q.PlaylistInfo.SelectedTrack].Length.ToString(@"hh\:mm\:ss") : q.Tracks[q.PlaylistInfo.SelectedTrack].Length.ToString(@"mm\:ss")) + "]", $"by {q.Tracks[q.PlaylistInfo.SelectedTrack].Author}\nRequested by {ctx.Member.Mention}At position: {pos}");
                if (q.Tracks.Count != 1)
                {
                    emb.AddField("Playlist added:", $"added {q.Tracks.Count - 1} more");
                }
                await ctx.RespondAsync(embed: emb.WithTitle("Added").Build());
            }
        }

        [Command("skip")]
        [Description("Skip the current song")]
        [RequireUserVoicechatConnection]
        public async Task Skip(CommandContext ctx)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel.Id;
            g.musicInstance.guildConnection.PlaybackFinished -= Lavalink.LavalinkTrackFinish;
            if (g.musicInstance.repeatMode != RepeatMode.On && g.musicInstance.repeatMode != RepeatMode.All) g.musicInstance.queue.Remove(g.musicInstance.currentSong);
            if (g.lastPlayedSongs.Count == 0)
            {
                g.lastPlayedSongs.Add(g.musicInstance.currentSong);
                await Database.AddToLPL(ctx.Guild.Id, g.musicInstance.currentSong.track.TrackString);
            }
            else if (g.lastPlayedSongs[0]?.track.Uri != g.musicInstance.currentSong.track.Uri)
            {
                g.lastPlayedSongs.Insert(0, g.musicInstance.currentSong);
                await Database.AddToLPL(ctx.Guild.Id, g.musicInstance.currentSong.track.TrackString);
            }
            g.musicInstance.lastSong = g.musicInstance.currentSong;
            g.musicInstance.currentSong = null;
            if (g.musicInstance.queue.Count != 0) await g.musicInstance.PlaySong();
            else
            {
                g.musicInstance.playstate = Playstate.NotPlaying;
                g.musicInstance.guildConnection.Stop();
            }
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription($"**Skipped:**\n{g.musicInstance.lastSong.track.Title}").Build());
        }

        [Command("stop")]
        [Description("Stop Playback")]
        [RequireUserVoicechatConnection]
        public async Task Stop(CommandContext ctx)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel.Id;
            await Task.Run(() => g.musicInstance.guildConnection.Stop());
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription("**Stopped**\n(use m%%resume to start playback again)").Build());
        }

        [Command("volume"), Aliases("vol")]
        [Description("Change the music volume")]
        [Usage("(0-150) |-> Changed the volume to the specified amount",
            "|-> Changes the volume to the default setting of ``100``")]
        [RequireUserVoicechatConnection]
        public async Task Volume(CommandContext ctx, int vol = 100)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel.Id;
            if (vol > 150) vol = 150;
            g.musicInstance.guildConnection.SetVolume(vol);
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription($"**Set volume to {vol}**").Build());
        }

        [Command("pause")]
        [Description("Pauses or unpauses playback")]
        [RequireUserVoicechatConnection]
        public async Task Pause(CommandContext ctx)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel.Id;
            if (g.musicInstance.playstate == Playstate.Stopped)
            {
                await g.musicInstance.PlaySong();
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription("**Started Playback**").Build());
            }
            else if (g.musicInstance.playstate == Playstate.Playing)
            {
                g.musicInstance.guildConnection.Pause();
                g.musicInstance.playstate = Playstate.Paused;
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription("**Paused**").Build());
            }
            else
            {
                g.musicInstance.guildConnection.Resume();
                g.musicInstance.playstate = Playstate.Playing;
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription("**Resumed**").Build());
            }
        }

        [Command("resume"), Aliases("unpause")]
        [Description("Resumes paused playback")]
        [RequireUserVoicechatConnection]
        public async Task Resume(CommandContext ctx)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel.Id;
            if (g.musicInstance.playstate == Playstate.Stopped)
            {
                await g.musicInstance.PlaySong();
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription("**Started Playback**").Build());
            }
            else
            {
                g.musicInstance.guildConnection.Resume();
                g.musicInstance.playstate = Playstate.Playing;
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription("**Resumed**").Build());
            }
        }

        [Command("queuerclear"), Aliases("qc")]
        [Description("Clears the queue")]
        [RequireUserVoicechatConnection]
        public async Task QueuecClear(CommandContext ctx)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel.Id;
            g.musicInstance.queue.Clear();
            g.musicInstance.queue.Add(g.musicInstance.currentSong);
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription("**Cleared queue!**").Build());
        }

        [Command("queuemove"), Aliases("qm", "qmv")]
        [Description("Moves a specific song in the queue")]
        [Usage("(positionOfSong(number)) (newPosition(number)) |-> Moves a desired song to the specified position (refer to m%queue for position numbers)")]
        [RequireUserVoicechatConnection]
        public async Task QueueMove(CommandContext ctx, int old, int newpos)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel.Id;
            if (old < 1 || newpos < 1 || old == newpos || newpos >= g.musicInstance.queue.Count) return;
            var oldSong = g.musicInstance.queue[old];
            g.musicInstance.queue = ListExtension.Swap(g.musicInstance.queue, old, newpos);
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription($"**Moved**:\n" +
                $"**{oldSong.track.Title}**\nby {oldSong.track.Author}\n" +
                $"from position **{old}** to **{newpos}**!"));
        }

        [Command("queueremove"), Aliases("qr")]
        [Description("Removes a song from queue")]
        [Usage("(number) |-> Removes a song from queue, you can get the number from the queue list command")]
        [RequireUserVoicechatConnection]
        public async Task QueueRemove(CommandContext ctx, int r)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel.Id;
            var old = g.musicInstance.queue[r];
            g.musicInstance.queue.RemoveAt(r);
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription($"**Removed:\n{old.track.Title}**\nby {old.track.Author}").Build());
        }

        [Command("repeat"), Aliases("r")]
        [Description("Repeat the current song or the entire queue")]
        [Usage("|-> If Repeatmode is on it will be turned off, in any other case it will be turned to single song repeat mode",
            "(0,1,2) |-> 0:Off 1:Repeat only the current song 2:Repeat the entire queue",
            "(off, on, all) |-> off:Off on:Repeat only the current song all:Repeat the entire queue")]
        [RequireUserVoicechatConnection]
        public async Task Repeat(CommandContext ctx, int e)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel.Id;
            switch (e)
            {
                case 0: g.musicInstance.repeatMode = RepeatMode.Off; break;
                case 1: g.musicInstance.repeatMode = RepeatMode.On; break;
                case 2:
                    {
                        g.musicInstance.repeatMode = RepeatMode.All;
                        if (g.musicInstance.queue.Count != 0 && g.musicInstance.playstate == Playstate.Playing)
                            g.musicInstance.repeatAllPos = g.musicInstance.queue.FindIndex(x => x.track.Uri == g.musicInstance.currentSong.track.Uri);
                        else
                            g.musicInstance.repeatAllPos = 0;
                        break;
                    }
                default: g.musicInstance.repeatMode = RepeatMode.Off; break;
            }
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription($"Set repeatmode to:\n**{g.musicInstance.repeatMode}**").Build());
        }

        [Command("repeat")]
        public async Task Repeat(CommandContext ctx, string e)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel.Id;
            switch (e)
            {
                case "off": g.musicInstance.repeatMode = RepeatMode.Off; g.musicInstance.repeatAllPos = 0;  break;
                case "on": g.musicInstance.repeatMode = RepeatMode.On; g.musicInstance.repeatAllPos = 0; break;
                case "all":
                    {
                        g.musicInstance.repeatMode = RepeatMode.All;
                        if (g.musicInstance.queue.Count != 0 && g.musicInstance.playstate == Playstate.Playing)
                            g.musicInstance.repeatAllPos = g.musicInstance.queue.FindIndex(x => x.track.Uri == g.musicInstance.currentSong.track.Uri);
                        else
                            g.musicInstance.repeatAllPos = 0;
                        break;
                    }
                default: g.musicInstance.repeatMode = RepeatMode.Off; break;
            }
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription($"Set repeatmode to:\n**{g.musicInstance.repeatMode}**").Build());
        }

        [Command("repeat")]
        public async Task Repeat(CommandContext ctx)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel.Id;
            if (g.musicInstance.repeatMode != RepeatMode.On) g.musicInstance.repeatMode = RepeatMode.On;
            else g.musicInstance.repeatMode = RepeatMode.Off;
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription($"Set repeatmode to:\n**{g.musicInstance.repeatMode}**").Build());
        }

        [Command("repeatall"), Aliases("ra")]
        [Description("Repeat the entire queue")]
        [Usage("|-> If Repeatmode is set to all it will be turned off, in any other case it will be turned to \"all\" repeat mode")]
        [RequireUserVoicechatConnection]
        public async Task RepeatAll(CommandContext ctx)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel.Id;
            if (g.musicInstance.repeatMode != RepeatMode.All) g.musicInstance.repeatMode = RepeatMode.All;
            else g.musicInstance.repeatMode = RepeatMode.Off;
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription($"Set repeatmode to:\n**{g.musicInstance.repeatMode}**").Build());
        }

        [Command("shuffle"), Aliases("s")]
        [Description("Play the queue in shuffle mode")]
        [RequireUserVoicechatConnection]
        public async Task Shuffle(CommandContext ctx)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel.Id;
            if (g.musicInstance.shuffleMode == ShuffleMode.Off) g.musicInstance.shuffleMode = ShuffleMode.On;
            else g.musicInstance.shuffleMode = ShuffleMode.Off;
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription($"Set Shufflemode to:\n**{g.musicInstance.shuffleMode}**").Build());
        }

        [Command("queue"), Aliases("q")]
        [Description("Show the current queue")]
        public async Task Queue(CommandContext ctx)
        {
            try
            {
                var g = Bot.Guilds[ctx.Guild.Id];
                if (g.musicInstance.queue.Count == 0)
                {
                    await ctx.RespondAsync("Queue empty");
                    return;
                }
                var inter = ctx.Client.GetInteractivity();
                int songsPerPage = 0;
                int currentPage = 1;
                int songAmount = 0;
                int totalP = g.musicInstance.queue.Count / 5;
                if ((g.musicInstance.queue.Count % 5) != 0) totalP++;
                var emb = new DiscordEmbedBuilder();
                List<Page> Pages = new List<Page>();
                if (g.musicInstance.repeatMode == RepeatMode.All)
                {
                    songAmount = g.musicInstance.repeatAllPos;
                    foreach (var Track in g.musicInstance.queue)
                    {
                        if (songsPerPage == 0 && currentPage == 1)
                        {
                            emb.WithTitle("Current Song");
                            string time = "";
                            if (g.musicInstance.currentSong.track.Length.Hours < 1) time = g.musicInstance.currentSong.track.Length.ToString(@"mm\:ss");
                            else time = g.musicInstance.currentSong.track.Length.ToString(@"hh\:mm\:ss");
                            string time2 = "";
                            if (g.musicInstance.guildConnection.CurrentState.PlaybackPosition.Hours < 1) time2 = g.musicInstance.guildConnection.CurrentState.PlaybackPosition.ToString(@"mm\:ss");
                            else time2 = g.musicInstance.guildConnection.CurrentState.PlaybackPosition.ToString(@"hh\:mm\:ss");
                            emb.AddField($"**{songAmount}.{g.musicInstance.currentSong.track.Title.Replace("*", "").Replace("|", "")}** by {g.musicInstance.currentSong.track.Author.Replace("*", "").Replace("|", "")} [{time2}/{time}]",
                                $"Requested by {g.musicInstance.currentSong.addedBy.Username} [Link]({g.musicInstance.currentSong.track.Uri.AbsoluteUri})\nˉˉˉˉˉ");
                        }
                        else
                        {
                            string time = "";
                            if (g.musicInstance.queue.ElementAt(songAmount).track.Length.Hours < 1) time = g.musicInstance.queue.ElementAt(songAmount).track.Length.ToString(@"mm\:ss");
                            else time = g.musicInstance.queue.ElementAt(songAmount).track.Length.ToString(@"hh\:mm\:ss");
                            emb.AddField($"**{songAmount}.{g.musicInstance.queue.ElementAt(songAmount).track.Title.Replace("*", "").Replace("|", "")}** by {g.musicInstance.queue.ElementAt(songAmount).track.Author.Replace("*", "").Replace("|", "")} [{time}]",
                                $"Requested by {g.musicInstance.queue.ElementAt(songAmount).addedBy.Username} [Link]({g.musicInstance.queue.ElementAt(songAmount).track.Uri.AbsoluteUri})");
                        }
                        songsPerPage++;
                        songAmount++;
                        if (songAmount == g.musicInstance.queue.Count)
                        {
                            songAmount = 0;
                        }
                        if (songsPerPage == 5)
                        {
                            songsPerPage = 0;
                            var opts = "";
                            if (g.musicInstance.repeatMode == RepeatMode.On) opts += DiscordEmoji.FromUnicode("🔂");
                            if (g.musicInstance.repeatMode == RepeatMode.All) opts += DiscordEmoji.FromUnicode("🔁");
                            if (g.musicInstance.shuffleMode == ShuffleMode.On) opts += DiscordEmoji.FromUnicode("🔀");
                            if (opts != "")
                            {
                                emb.AddField("Playback Options", opts);
                            }
                            emb.WithFooter($"Page {currentPage}/{totalP}");
                            Pages.Add(new Page(embed: emb));
                            emb.ClearFields();
                            emb.WithTitle("more™");
                            currentPage++;
                        }
                        if (songAmount == g.musicInstance.repeatAllPos)
                        {
                            var opts = "";
                            if (g.musicInstance.repeatMode == RepeatMode.On) opts += DiscordEmoji.FromUnicode("🔂");
                            if (g.musicInstance.repeatMode == RepeatMode.All) opts += DiscordEmoji.FromUnicode("🔁");
                            if (g.musicInstance.shuffleMode == ShuffleMode.On) opts += DiscordEmoji.FromUnicode("🔀");
                            if (opts != "")
                            {
                                emb.AddField("Playback Options", opts);
                            }
                            emb.WithFooter($"Page {currentPage}/{totalP}");
                            Pages.Add(new Page(embed: emb));
                            emb.ClearFields();
                        }
                    }
                }
                else
                {
                    foreach (var Track in g.musicInstance.queue)
                    {
                        if (songsPerPage == 0 && currentPage == 1)
                        {
                            emb.WithTitle("Current Song");
                            string time = "";
                            if (g.musicInstance.currentSong.track.Length.Hours < 1) time = g.musicInstance.currentSong.track.Length.ToString(@"mm\:ss");
                            else time = g.musicInstance.currentSong.track.Length.ToString(@"hh\:mm\:ss");
                            string time2 = "";
                            if (g.musicInstance.guildConnection.CurrentState.PlaybackPosition.Hours < 1) time2 = g.musicInstance.guildConnection.CurrentState.PlaybackPosition.ToString(@"mm\:ss");
                            else time2 = g.musicInstance.guildConnection.CurrentState.PlaybackPosition.ToString(@"hh\:mm\:ss");
                            emb.AddField($"**{g.musicInstance.currentSong.track.Title.Replace("*", "").Replace("|", "")}** by {g.musicInstance.currentSong.track.Author.Replace("*", "").Replace("|", "")} [{time2}/{time}]",
                                $"Requested by {g.musicInstance.currentSong.addedBy.Username} [Link]({g.musicInstance.currentSong.track.Uri.AbsoluteUri})\nˉˉˉˉˉ");
                        }
                        else
                        {
                            string time = "";
                            if (Track.track.Length.Hours < 1) time = Track.track.Length.ToString(@"mm\:ss");
                            else time = Track.track.Length.ToString(@"hh\:mm\:ss");
                            emb.AddField($"**{songAmount}.{Track.track.Title.Replace("*", "").Replace("|", "")}** by {Track.track.Author.Replace("*", "").Replace("|", "")} [{time}]",
                                $"Requested by {Track.addedBy.Username} [Link]({Track.track.Uri.AbsoluteUri})");
                        }
                        songsPerPage++;
                        songAmount++;
                        if (songsPerPage == 5)
                        {
                            songsPerPage = 0;
                            emb.WithFooter($"Page {currentPage}/{totalP}");
                            var opts = "";
                            if (g.musicInstance.repeatMode == RepeatMode.On) opts += DiscordEmoji.FromUnicode("🔂");
                            if (g.musicInstance.repeatMode == RepeatMode.All) opts += DiscordEmoji.FromUnicode("🔁");
                            if (g.musicInstance.shuffleMode == ShuffleMode.On) opts += DiscordEmoji.FromUnicode("🔀");
                            if (opts != "")
                            {
                                emb.AddField("Playback Options", opts);
                            }
                            Pages.Add(new Page(embed: emb));
                            emb.ClearFields();
                            emb.WithTitle("more™");
                            currentPage++;
                        }
                        if (songAmount == g.musicInstance.queue.Count)
                        {
                            emb.WithFooter($"Page {currentPage}/{totalP}");
                            var opts = "";
                            if (g.musicInstance.repeatMode == RepeatMode.On) opts += DiscordEmoji.FromUnicode("🔂");
                            if (g.musicInstance.repeatMode == RepeatMode.All) opts += DiscordEmoji.FromUnicode("🔁");
                            if (g.musicInstance.shuffleMode == ShuffleMode.On) opts += DiscordEmoji.FromUnicode("🔀");
                            if (opts != "")
                            {
                                emb.AddField("Playback Options", opts);
                            }
                            Pages.Add(new Page(embed: emb));
                            //Console.WriteLine(emb.Fields.Count);
                            emb.ClearFields();
                        }
                    }
                }
                if (currentPage == 1)
                {
                    var opts = "";
                    if (g.musicInstance.repeatMode == RepeatMode.On) opts += DiscordEmoji.FromUnicode("🔂");
                    if (g.musicInstance.repeatMode == RepeatMode.All) opts += DiscordEmoji.FromUnicode("🔁");
                    if (g.musicInstance.shuffleMode == ShuffleMode.On) opts += DiscordEmoji.FromUnicode("🔀");
                    if (opts != "")
                    {
                        emb.AddField("Playback Options", opts);
                    }
                    //Console.WriteLine(emb.Fields.Count);
                    await ctx.RespondAsync(embed: Pages.First().Embed);
                    return;
                }
                else if (currentPage == 2 && songsPerPage == 0)
                {
                    await ctx.RespondAsync(embed: Pages.First().Embed);
                    return;
                }
                foreach (var eP in Pages.Where(x => x.Embed.Fields.Where(y => y.Name != "Playback Options").Count() == 0).ToList())
                {
                    Pages.Remove(eP);
                }
                await inter.SendPaginatedMessageAsync(ctx.Channel, ctx.User, Pages, timeoutoverride: TimeSpan.FromMinutes(5));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        [Command("nowplaying"), Aliases("np")]
        [Description("Show whats currently playing")]
        public async Task NowPlayling(CommandContext ctx)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            g.shardId = ctx.Client.ShardId;
            var eb = new DiscordEmbedBuilder();
            eb.WithTitle("Now Playing");
            eb.WithDescription("**__Current Song:__**");
            if (g.musicInstance.currentSong.track.Uri.ToString().Contains("youtu"))
            {
                try
                {
                    var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                    {
                        ApiKey = Bot.cfg.YoutubeApiToken,
                        ApplicationName = this.GetType().ToString()
                    });
                    var searchListRequest = youtubeService.Search.List("snippet");
                    searchListRequest.Q = g.musicInstance.currentSong.track.Title + " " + g.musicInstance.currentSong.track.Author;
                    searchListRequest.MaxResults = 1;
                    searchListRequest.Type = "video";
                    string time1, time2;
                    if (g.musicInstance.currentSong.track.Length.Hours < 1)
                    {
                        time1 = g.musicInstance.guildConnection.CurrentState.PlaybackPosition.ToString(@"mm\:ss");
                        time2 = g.musicInstance.currentSong.track.Length.ToString(@"mm\:ss");
                    }
                    else
                    {
                        time1 = g.musicInstance.guildConnection.CurrentState.PlaybackPosition.ToString(@"hh\:mm\:ss");
                        time2 = g.musicInstance.currentSong.track.Length.ToString(@"hh\:mm\:ss");
                    }
                    var searchListResponse = await searchListRequest.ExecuteAsync();
                    eb.AddField($"{g.musicInstance.currentSong.track.Title} ({time1}/{time2})", $"[Video Link]({g.musicInstance.currentSong.track.Uri})\n" +
                        $"[{g.musicInstance.currentSong.track.Author}](https://www.youtube.com/channel/" + searchListResponse.Items[0].Snippet.ChannelId + ")");
                    if (searchListResponse.Items[0].Snippet.Description.Length > 500) eb.AddField("Description", searchListResponse.Items[0].Snippet.Description.Substring(0, 500) + "...");
                    else eb.AddField("Description", searchListResponse.Items[0].Snippet.Description);
                    eb.WithImageUrl(searchListResponse.Items[0].Snippet.Thumbnails.High.Url);
                    var opts = "";
                    if (g.musicInstance.repeatMode == RepeatMode.On) opts += DiscordEmoji.FromUnicode("🔂");
                    if (g.musicInstance.repeatMode == RepeatMode.All) opts += DiscordEmoji.FromUnicode("🔁");
                    if (g.musicInstance.shuffleMode == ShuffleMode.On) opts += DiscordEmoji.FromUnicode("🔀");
                    if (opts != "")
                    {
                        eb.AddField("Playback Options", opts);
                    }
                }
                catch
                {
                    if (eb.Fields.Count != 1)
                    {
                        eb.AddField($"{g.musicInstance.currentSong.track.Title} ({g.musicInstance.currentSong.track.Length})", $"By {g.musicInstance.currentSong.track.Author}\n[Link]({g.musicInstance.currentSong.track.Uri})\nRequested by {g.musicInstance.currentSong.addedBy.Mention}");
                        var opts = "";
                        if (g.musicInstance.repeatMode == RepeatMode.On) opts += DiscordEmoji.FromUnicode("🔂");
                        if (g.musicInstance.repeatMode == RepeatMode.All) opts += DiscordEmoji.FromUnicode("🔁");
                        if (g.musicInstance.shuffleMode == ShuffleMode.On) opts += DiscordEmoji.FromUnicode("🔀");
                        if (opts != "")
                        {
                            eb.AddField("Playback Options", opts);
                        }
                    }
                }
            }
            else
            {
                string time1,time2;
                if (g.musicInstance.currentSong.track.Length.Hours < 1)
                {
                    time1 = g.musicInstance.guildConnection.CurrentState.PlaybackPosition.ToString(@"mm\:ss");
                    time2 = g.musicInstance.currentSong.track.Length.ToString(@"mm\:ss");
                }
                else
                {
                    time1 = g.musicInstance.guildConnection.CurrentState.PlaybackPosition.ToString(@"hh\:mm\:ss");
                    time2 = g.musicInstance.currentSong.track.Length.ToString(@"hh\:mm\:ss");
                }
                eb.AddField($"{g.musicInstance.currentSong.track.Title} ({time1}/{time2})", $"By {g.musicInstance.currentSong.track.Author}\n[Link]({g.musicInstance.currentSong.track.Uri})\nRequested by {g.musicInstance.currentSong.addedBy.Mention}");
                var opts = "";
                if (g.musicInstance.repeatMode == RepeatMode.On) opts += DiscordEmoji.FromUnicode("🔂");
                if (g.musicInstance.repeatMode == RepeatMode.All) opts += DiscordEmoji.FromUnicode("🔁");
                if (g.musicInstance.shuffleMode == ShuffleMode.On) opts += DiscordEmoji.FromUnicode("🔀");
                if (opts != "")
                {
                    eb.AddField("Playback Options", opts);
                }
            }
            await ctx.RespondAsync(embed: eb.Build());
        }

        [Command("lastplaying"), Aliases("lp")]
        [Description("Show what played before")]
        public async Task LastPlayling(CommandContext ctx)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            g.shardId = ctx.Client.ShardId;
            var eb = new DiscordEmbedBuilder();
            eb.WithTitle("Now Playing");
            eb.WithDescription("**__Current Song:__**");
            if (g.lastPlayedSongs[0].track.Uri.ToString().Contains("youtu"))
            {
                try
                {
                    var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                    {
                        ApiKey = Bot.cfg.YoutubeApiToken,
                        ApplicationName = this.GetType().ToString()
                    });
                    var searchListRequest = youtubeService.Search.List("snippet");
                    searchListRequest.Q = g.lastPlayedSongs[0].track.Title + " " + g.lastPlayedSongs[0].track.Author;
                    searchListRequest.MaxResults = 1;
                    searchListRequest.Type = "video";
                    string time2 = "";
                    if (g.lastPlayedSongs[0].track.Length.Hours < 1)
                    {
                        time2 = g.lastPlayedSongs[0].track.Length.ToString(@"mm\:ss");
                    }
                    else
                    {
                        time2 = g.lastPlayedSongs[0].track.Length.ToString(@"hh\:mm\:ss");
                    }
                    var searchListResponse = await searchListRequest.ExecuteAsync();
                    eb.AddField($"{g.lastPlayedSongs[0].track.Title} ({time2})", $"[Video Link]({g.lastPlayedSongs[0].track.Uri})\n" +
                        $"[{g.lastPlayedSongs[0].track.Author}](https://www.youtube.com/channel/" + searchListResponse.Items[0].Snippet.ChannelId + ")");
                    if (searchListResponse.Items[0].Snippet.Description.Length > 500) eb.AddField("Description", searchListResponse.Items[0].Snippet.Description.Substring(0, 500) + "...");
                    else eb.AddField("Description", searchListResponse.Items[0].Snippet.Description);
                    eb.WithImageUrl(searchListResponse.Items[0].Snippet.Thumbnails.High.Url);
                    var opts = "";
                    if (g.musicInstance.repeatMode == RepeatMode.On) opts += DiscordEmoji.FromUnicode("🔂");
                    if (g.musicInstance.repeatMode == RepeatMode.All) opts += DiscordEmoji.FromUnicode("🔁");
                    if (g.musicInstance.shuffleMode == ShuffleMode.On) opts += DiscordEmoji.FromUnicode("🔀");
                    if (opts != "")
                    {
                        eb.AddField("Playback Options", opts);
                    }
                }
                catch
                {
                    if (eb.Fields.Count != 1)
                    {
                        eb.AddField($"{g.lastPlayedSongs[0].track.Title} ({g.lastPlayedSongs[0].track.Length})", $"By {g.lastPlayedSongs[0].track.Author}\n[Link]({g.lastPlayedSongs[0].track.Uri})");
                        var opts = "";
                        if (g.musicInstance.repeatMode == RepeatMode.On) opts += DiscordEmoji.FromUnicode("🔂");
                        if (g.musicInstance.repeatMode == RepeatMode.All) opts += DiscordEmoji.FromUnicode("🔁");
                        if (g.musicInstance.shuffleMode == ShuffleMode.On) opts += DiscordEmoji.FromUnicode("🔀");
                        if (opts != "")
                        {
                            eb.AddField("Playback Options", opts);
                        }
                    }
                }
            }
            else
            {
                string time2 = "";
                if (g.lastPlayedSongs[0].track.Length.Hours < 1)
                {
                    time2 = g.lastPlayedSongs[0].track.Length.ToString(@"mm\:ss");
                }
                else
                {
                    time2 = g.lastPlayedSongs[0].track.Length.ToString(@"hh\:mm\:ss");
                }
                eb.AddField($"{g.lastPlayedSongs[0].track.Title} ({time2})", $"By {g.lastPlayedSongs[0].track.Author}\n[Link]({g.lastPlayedSongs[0].track.Uri})");
                var opts = "";
                if (g.musicInstance.repeatMode == RepeatMode.On) opts += DiscordEmoji.FromUnicode("🔂");
                if (g.musicInstance.repeatMode == RepeatMode.All) opts += DiscordEmoji.FromUnicode("🔁");
                if (g.musicInstance.shuffleMode == ShuffleMode.On) opts += DiscordEmoji.FromUnicode("🔀");
                if (opts != "")
                {
                    eb.AddField("Playback Options", opts);
                }
            }
            await ctx.RespondAsync(embed: eb.Build());
        }

        [Command("lastplayinglist"), Aliases("lpl", "lpq")]
        [Description("Show what song were played before")]
        public async Task LastPlaylingList(CommandContext ctx)
        {
            try
            {
                var g = Bot.Guilds[ctx.Guild.Id];
                if (g.lastPlayedSongs.Count == 0)
                {
                    await ctx.RespondAsync("Queue empty");
                    return;
                }
                var inter = ctx.Client.GetInteractivity();
                int songsPerPage = 0;
                int currentPage = 1;
                int songAmount = 0;
                int totalP = g.lastPlayedSongs.Count / 10;
                if ((g.lastPlayedSongs.Count % 10) != 0) totalP++;
                var emb = new DiscordEmbedBuilder();
                List<Page> Pages = new List<Page>();
                foreach (var Track in g.lastPlayedSongs)
                {

                    string time = "";
                    if (Track.track.Length.Hours < 1) time = Track.track.Length.ToString(@"mm\:ss");
                    else time = Track.track.Length.ToString(@"hh\:mm\:ss");
                    emb.AddField($"{songAmount+1}.{Track.track.Title.Replace("*", "").Replace("|", "")}",$"by {Track.track.Author.Replace("*", "").Replace("|", "")} [{time}] [Link]({Track.track.Uri})");
                    songsPerPage++;
                    songAmount++;
                    if (songsPerPage == 10)
                    {
                        songsPerPage = 0;
                        emb.WithTitle("Last played songs in this server:\n");
                        emb.WithFooter($"Page {currentPage}/{totalP}");
                        Pages.Add(new Page(embed: emb));
                        emb.ClearFields();
                        emb.WithTitle("more™");
                        currentPage++;
                    }
                    if (songAmount == g.lastPlayedSongs.Count)
                    {
                        emb.WithTitle("Last played songs in this server:\n");
                        emb.WithFooter($"Page {currentPage}/{totalP}");
                        Pages.Add(new Page(embed: emb));
                        emb.ClearFields();
                    }
                }
                if (currentPage == 1)
                {
                    await ctx.RespondAsync(embed: Pages.First().Embed);
                    return;
                }
                else if (currentPage == 2 && songsPerPage == 0)
                {
                    await ctx.RespondAsync(embed: Pages.First().Embed);
                    return;
                }
                foreach (var eP in Pages.Where(x => x.Embed.Fields.Count == 0).ToList())
                {
                    Pages.Remove(eP);
                }
                await inter.SendPaginatedMessageAsync(ctx.Channel, ctx.User, Pages, timeoutoverride: TimeSpan.FromMinutes(5));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}