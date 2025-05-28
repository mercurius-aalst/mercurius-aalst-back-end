using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.Services.LAN.MatchServices.BracketTypes
{
    public class RoundRobinMatchModerator : IMatchModerator
    {
        public IEnumerable<Match> GenerateMatchesForGame(Game game)
        {
            var matches = new List<Match>();                    

            var rotation = new List<Participant>(game.Participants);
            bool isOdd = rotation.Count % 2 != 0;
            if(isOdd)
                rotation.Add(null);

            int totalParticipants = rotation.Count;
            int totalRounds = totalParticipants - 1;
            int matchesPerRound = totalParticipants / 2;

            int matchNumber = 0;

            for(int round = 1; round <= totalRounds; round++)
            {
                for(int i = 0; i < matchesPerRound; i++)
                {
                    var participant1 = rotation[i];
                    var participant2 = rotation[rotation.Count - 1 - i];

                    if(participant1 == null || participant2 == null)
                        continue;

                    matches.Add(new Match
                    {
                        GameId = game.Id,
                        RoundNumber = round,
                        MatchNumber = matchNumber++,
                        BracketType = game.BracketType,
                        Format = game.Format,
                        ParticipantType = game.ParticipantType,
                        Participant1 = participant1,
                        Participant2 = participant2
                    });
                }

                var last = rotation[rotation.Count - 1];
                rotation.RemoveAt(rotation.Count - 1);
                rotation.Insert(1, last);
            }

            //Winner and Loser next matches don't need to be linked, no binary tree here.
            return matches;
        }
    }
}
