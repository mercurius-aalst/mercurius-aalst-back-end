﻿namespace MercuriusAPI.Models.LAN
{
    public class Player: Participant{
        public string Username { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Email { get; set; }

        public string? DiscordId { get; set; }
        public string? SteamId { get; set; }
        public string? RiotId { get; set; }

        public IList<Team> Teams { get; set; } = new List<Team>();

        public Player()
        {
            
        }

        public Player(string username, string firstname, string lastname, string email, string? discordId, string? steamId, string? riotId)
        {
            Username = username;
            Firstname = firstname;
            Lastname = lastname;
            Email = email;
            DiscordId = discordId;
            SteamId = steamId;
            RiotId = riotId;

        }

        public void Update(string firstname, string lastname, string username, string? discordId, string? steamId, string? riotId)
        {
            Firstname = firstname;
            Lastname = lastname;
            Username = username;
            DiscordId = discordId;
            SteamId = steamId;
            RiotId = riotId;
        }
    }
}
