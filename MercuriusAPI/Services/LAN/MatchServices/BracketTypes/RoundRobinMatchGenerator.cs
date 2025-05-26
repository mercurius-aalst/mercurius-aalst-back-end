using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.Services.LAN.MatchServices.BracketTypes
{
    public class RoundRobinMatchGenerator : IMatchGenerator
    {
        public IEnumerable<Match> GenerateMatchesForGame(Game game)
        {
            var matches = new List<Match>();

            int totalParticipants = game.Participants.Count;
            bool isOdd = totalParticipants % 2 != 0;
            int totalRounds = isOdd ? totalParticipants : totalParticipants - 1;
            int matchesPerRound = totalParticipants / 2;

            var rotation = new List<Participant>(game.Participants);
            if(isOdd)
                rotation.Add(null);

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
            return matches;
        }
    }
}
