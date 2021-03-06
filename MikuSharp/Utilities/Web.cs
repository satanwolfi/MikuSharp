﻿using MikuSharp.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HeyRed.Mime;
using DSharpPlus.Entities;
using System.IO;
using DSharpPlus.CommandsNext;
using Weeb.net;
using System.Net.Http.Headers;

namespace MikuSharp.Utilities
{
    public class Web
    {
        public static async Task<Nekos_Life> GetNekos_Life(string url)
        {
            var hc = new HttpClient();
            var dl = JsonConvert.DeserializeObject<Nekos_Life>(await hc.GetStringAsync(url));
            MemoryStream str = new MemoryStream(await hc.GetByteArrayAsync(Other.resizeLink(dl.Url)));
            str.Position = 0;
            dl.Data = str;
            dl.Filetype = MimeGuesser.GuessExtension(str);
            var em = new DiscordEmbedBuilder();
            em.WithImageUrl($"attachment://image.{dl.Filetype}");
            em.WithFooter("by nekos.life");
            dl.Embed = em.Build();
            return dl;
        }

        public static async Task<KsoftSiRanImg> GetKsoftSiRanImg(string tag, bool nsfw = false)
        {
            var c = new HttpClient();
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Bot.cfg.KsoftSiToken);
            var v = JsonConvert.DeserializeObject<KsoftSiRanImg>(await c.GetStringAsync("https://api.ksoft.si/images/random-image?tag=hentai_gif&nsfw=true"));
            MemoryStream img = new MemoryStream(await c.GetByteArrayAsync(Other.resizeLink(v.url)));
            v.Data = img;
            v.Filetype = MimeGuesser.GuessExtension(img);
            var em = new DiscordEmbedBuilder();
            em.WithImageUrl($"attachment://image.{v.Filetype}");
            em.WithFooter("by KSoft.si");
            v.Embed = em.Build();
            return v;
        }

        public static async Task<NekoBot> GetNekobot(string url)
        {
            var hc = new HttpClient();
            var dl = JsonConvert.DeserializeObject<NekoBot>(await hc.GetStringAsync(url));
            MemoryStream str = new MemoryStream(await hc.GetByteArrayAsync(Other.resizeLink(dl.message)));
            str.Position = 0;
            dl.Data = str;
            dl.Filetype = MimeGuesser.GuessExtension(str);
            var em = new DiscordEmbedBuilder();
            em.WithImageUrl($"attachment://image.{dl.Filetype}");
            em.WithFooter("by nekobot.xyz");
            dl.Embed = em.Build();
            return dl;
        }

        public static async Task<Derpy> GetDerpy(string url)
        {
            var hc = new HttpClient();
            var dl = JsonConvert.DeserializeObject<Derpy>(await hc.GetStringAsync(url));
            MemoryStream str = new MemoryStream(await hc.GetByteArrayAsync(Other.resizeLink(dl.url)));
            str.Position = 0;
            dl.Data = str;
            dl.Filetype = MimeGuesser.GuessExtension(str);
            var em = new DiscordEmbedBuilder();
            em.WithImageUrl($"attachment://image.{dl.Filetype}");
            em.WithFooter("by derpyenterprises.org");
            dl.Embed = em.Build();
            return dl;
        }

        public static async Task<WeebSh> GetWeebSh(CommandContext ctx, string query, string[] tags = null, NsfwSearch nsfw = NsfwSearch.False)
        {
            var weeurl = await Bot._weeb.GetRandomAsync(query, tags, nsfw: nsfw);
            var hc = new HttpClient();
            MemoryStream img = new MemoryStream(await hc.GetByteArrayAsync(weeurl.Url));
            img.Position = 0;
            var em = new DiscordEmbedBuilder();
            //em.WithDescription($"{ctx.Member.Mention} hugs {m.Mention} uwu");
            em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
            em.WithFooter("by weeb.sh");
            //await ctx.RespondWithFileAsync(embed: em.Build(), fileData: img, fileName: $"image.{MimeGuesser.GuessExtension(img)}");
            return new WeebSh {
                ImgData = img,
                Extension = MimeGuesser.GuessExtension(img),
                Embed = em
            };
        }
    }
}
