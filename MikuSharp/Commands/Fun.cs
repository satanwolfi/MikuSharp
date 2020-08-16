﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using HeyRed.Mime;
using System.Collections.Generic;
using MikuSharp.Entities;
using MikuSharp.Utilities;
using System.Net.Http;

namespace MikuSharp.Commands
{
    [Description("Fun commands")]
    class Fun : BaseCommandModule
    {

        [Command("8ball")]
        [Description("Yes? No? Maybe?")]
        public async Task EightBall(CommandContext ctx, [RemainingText]string stuff)
        {
            var responses = new[] { "It is certain.", "It is decidedly so.", "Without a doubt.", "Yes - definitely.", "You may rely on it.", "As I see it, yes.", "Most likely.", "Outlook good.", "Yes.", "Signs point to yes.", "Reply hazy, try again", "Ask again later.", "Better not tell you now.", "Cannot predict now.", "Concentrate and ask again.", "Don't count on it.", "My reply is no.", "My sources say no.", "Outlook not so good.", "Very doubtful.", "No." };
            await ctx.RespondAsync(responses[new Random().Next(0, responses.Length)]);
        }

        [Command("cat")]
        [Description("Get a random cat image!")]
        public async Task Cat(CommandContext ctx)
        {
            var ImgURL = await Web.GetNekos_Life("https://nekos.life/api/v2/img/meow");
            await ctx.RespondWithFileAsync(embed: ImgURL.Embed, fileData: ImgURL.Data, fileName: $"image.{ImgURL.Filetype}");
        }

        [Command("chucknorris")]
        [Description("Random Chuck Norris joke")]
        public async Task ChuckNorris(CommandContext ctx)
        {
            var c = new HttpClient();
            await ctx.RespondAsync(JsonConvert.DeserializeObject<ChuckNorrisObject>(await c.GetStringAsync("https://api.icndb.com/jokes/random")).Value.Joke);
        }

        [Command("clapify")]
        [Description("Clapify your message!")]
        public async Task Clapify(CommandContext ctx, [RemainingText] string text)
        {
            await ctx.RespondAsync($"👏{text.Replace(" ", "👏")}👏");
        } 

        [Command("clyde")]
        [Description("Say something as clyde bot")]
        public async Task Clyde(CommandContext ctx, [RemainingText] string text)
        {
            var c = new HttpClient();
            var e = JsonConvert.DeserializeObject<NekoBot>(await c.GetStringAsync($"https://nekobot.xyz/api/imagegen?type=clyde&text={text}"));
            Stream img = new MemoryStream(await c.GetByteArrayAsync(e.message));
            await ctx.RespondWithFileAsync(fileName: "clyde.png", fileData: img);
        }

        [Command("coinflip")]
        [Description("Flip a coin lol")]
        public async Task Coinflip(CommandContext ctx)
        {
            var flip = new[] { $"Heads {DiscordEmoji.FromName(ctx.Client, ":arrow_up_small:")}", $"Tails {DiscordEmoji.FromName(ctx.Client,":arrow_down_small:")}"};
            await ctx.RespondAsync(flip[new Random().Next(0, flip.Length)]);
        }

        [Command("dadjoke")]
        [Description("Random dadjoke")]
        public async Task DadJoke(CommandContext ctx)
        {
            var wr = (HttpWebRequest)WebRequest.Create("https://icanhazdadjoke.com/");
            wr.Accept = "application/json";
            wr.UserAgent = "Hatsune Miku Discord Bot (speyd3r@meek.moe)";
            await ctx.RespondAsync(JsonConvert.DeserializeObject<Dadjoke>(new StreamReader(wr.GetResponse().GetResponseStream()).ReadToEnd()).joke);
        }

        [Command("dog")]
        [Description("Random Dog Image")]
        public async Task Dog(CommandContext ctx)
        {
            var c = new HttpClient();
            var dc = JsonConvert.DeserializeObject<DogCeo>(await c.GetStringAsync("https://dog.ceo/api/breeds/image/random"));
            Stream img = new MemoryStream(await c.GetByteArrayAsync(Other.resizeLink(dc.message)));
            var em = new DiscordEmbedBuilder();
            em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
            em.WithFooter("by dog.ceo", "https://dog.ceo/img/favicon.png");
            em.WithDescription($"[Full Image]({dc.message})");
            await ctx.RespondWithFileAsync(fileData: img, fileName: $"image.{MimeGuesser.GuessExtension(img)}",embed: em.Build());
        }

        [Command("duck")]
        [Description("Random duck image")]
        public async Task Duck(CommandContext ctx)
        {
            var c = new HttpClient();
            var dc = JsonConvert.DeserializeObject<Random_D>(await c.GetStringAsync("https://random-d.uk/api/v1/random"));
            Stream img = new MemoryStream(await c.GetByteArrayAsync(Other.resizeLink(dc.message)));
            var em = new DiscordEmbedBuilder();
            em.WithImageUrl($"attachment://image.{MimeGuesser.GuessExtension(img)}");
            em.WithFooter("by random-d.uk", "https://random-d.uk/favicon.png");
            em.WithDescription($"[Full Image]({dc.message})");
            await ctx.RespondWithFileAsync(fileData: img, fileName: $"image.{MimeGuesser.GuessExtension(img)}", embed: em.Build());
        }

        [Command("eyeify")]
        [Description("eyeify your message :eyes:")]
        public async Task Eyeify(CommandContext ctx, [RemainingText] string text)
        {
            await ctx.RespondAsync(DiscordEmoji.FromUnicode("👀") + text.Replace(" ", "👀") + DiscordEmoji.FromUnicode("👀"));
        }

        [Command("leet")]
        [Description("Convert something to leetspeak")]
        public async Task Leet(CommandContext ctx, [RemainingText] string text)
        {
            //soon
        }

       

        [Command("lizard")]
        [Description("Get a random lizard image")]
        public async Task Lizard(CommandContext ctx)
        {
            var c = new HttpClient();
            var get = await Web.GetNekos_Life("https://nekos.life/api/lizard");
            Stream img = new MemoryStream(await c.GetByteArrayAsync(Other.resizeLink(get.Url)));
            await ctx.RespondWithFileAsync(fileName: "image." + MimeGuesser.GuessExtension(img), fileData: img);
        }

       


        [Command("pirate")]
        [Description("Convert some test into Pirate speech")]
        public async Task Pirate(CommandContext ctx)
        {
            //soon
        }

       

        [Command("rps")]
        [Description("Play rock paper scissors!")]
        public async Task RPS(CommandContext ctx, string rps)
        {
            var rock = new[] { $"Rock {DiscordEmoji.FromName(ctx.Client, ":black_circle:")}", $"Paper {DiscordEmoji.FromName(ctx.Client, ":pencil:")}", $"Scissors {DiscordEmoji.FromName(ctx.Client, ":scissors:")}"};
            await ctx.RespondAsync(rock[new Random().Next(0, rock.Length)]);
        }

      

        [Command("tiny")]
        [Description("Make some text tiny")]
        public async Task Tiny(CommandContext ctx)
        {
            //soon
        }

        [Command("trumptweet")]
        [Description("generate a tweet by Trump")]
        public async Task TrumpTweet(CommandContext ctx, [RemainingText]string text)
        {
            //https://nekobot.xyz/api/imagegen?type=trumptweet&text=
            var c = new HttpClient();
            var e = JsonConvert.DeserializeObject<NekoBot>(await c.GetStringAsync($"https://nekobot.xyz/api/imagegen?type=trumptweet&text={text}"));
            Stream img = new MemoryStream(await c.GetByteArrayAsync(e.message));
            await ctx.RespondWithFileAsync(fileName: "trump.png", fileData: img);
        }

        

        

    }
}
