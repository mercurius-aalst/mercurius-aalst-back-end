using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.Services.LAN.MatchServices.BracketTypes
{
    public class RoundRobinMatchModerator : IMatchModerator
    {
        public void DeterminePlacements(Game game)
        {
            if(game.Participants.Count == 0)
                throw new Exception("No participants in the game to determine placements.");

            // Count wins for each participant
            var winCounts = game.Participants.ToDictionary(
                p => p.Id,
                p => game.Matches.Count(m => m.WinnerId == p.Id)
            );

            // Get head-to-head all possible matches f
            var headToHead = game.Matches
                .Where(m => m.Participant1 != null && m.Participant2 != null && m.WinnerId.HasValue)
                .ToDictionary(
                    m => (Math.Min(m.Participant1.Id, m.Participant2.Id), Math.Max(m.Participant1.Id, m.Participant2.Id)),
                    m => m.WinnerId.Value
                );

            // Sort participants by wins
            var ordered = game.Participants
                .OrderByDescending(p => winCounts[p.Id])
                .ThenBy(p => p.Id)
                .ToList();

            // Apply head-to-head tiebreaker for ties
            for(int i = 0; i < ordered.Count - 1; i++)
            {
                for(int j = i + 1; j < ordered.Count; j++)
                {
                    var p1 = ordered[i];
                    var p2 = ordered[j];
                    if(winCounts[p1.Id] == winCounts[p2.Id])
                    {
                        var match = game.Matches.FirstOrDefault(m =>
                   ((m.Participant1?.Id == p1.Id && m.Participant2?.Id == p2.Id) ||
                    (m.Participant1?.Id == p2.Id && m.Participant2?.Id == p1.Id))
                   && m.WinnerId.HasValue);
                        if(match is not null && match.WinnerId == p2.Id && i < j)
                        {
                            // Swap so winner comes first
                            ordered[i] = p2;
                            ordered[j] = p1;
                        }
                    }
                }
            }

            // Assign placements
            for(int i = 0; i < ordered.Count; i++)
                game.Placements.Add(new Placement
                {
                    GameId = game.Id,
                    Participants = [ordered[i]],
                    Place = i + 1
                });
        }

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
