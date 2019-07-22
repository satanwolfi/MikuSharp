﻿using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using MikuSharp.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace MikuSharp.Entities
{
    public class Guild
    {
        public int shardId { get; set; }
        //CustomPrefix stuff
        public MusicInstance musicInstance{ get; set; }
        public List<Entry> lastPlayedSongs = new List<Entry>();
        public Task AloneCheckThread { get; set; }
        public async Task CheckAlone()
        {
            while (DateTime.UtcNow.Subtract(musicInstance.aloneTime).Minutes != 5 && !musicInstance.aloneCTS.IsCancellationRequested)
            {
                await Task.Delay(1000);
            }
            if (DateTime.UtcNow.Subtract(musicInstance.aloneTime).Minutes == 5 && !musicInstance.aloneCTS.IsCancellationRequested)
            {
                await Task.Run(() => musicInstance.guildConnection.Disconnect());
                await Task.Delay(500);
                musicInstance = null;
            }
        }
    }
}