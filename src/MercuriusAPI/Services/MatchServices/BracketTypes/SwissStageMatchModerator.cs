using Mercurius.LAN.API.Models;
using Mercurius.LAN.API.Services.MatchServices.Helpers;

namespace Mercurius.LAN.API.Services.MatchServices.BracketTypes;

public class SwissStageMatchModerator : IMatchModerator
{

    private const int _maxRounds = 5;
    private const int _maxParticipants = 16;
    private const int _playOffSize = 8;


    public IEnumerable<Match> GenerateMatchesForGame(Game game)
    {
        var matches = new List<Match>();
        GenerateSwissMatches(game, matches);
        GeneratePlayoffMatches(game, matches);

        return matches;
    }


    private void GenerateSwissMatches(Game game, List<Match> matches)
    {
        var paddedParticipants = PadTo16(MatchModeParticipantHelper.GetParticipantsForBracket(game));
        int matchCounter = 0;

        for (int round = 1; round <= _maxRounds; round++)
        {
            int matchCount = paddedParticipants.Count / 2;

            for (int i = 0; i < matchCount; i++)
            {
                var match = new Match
                {
                    GameId = game.Id,
                    RoundNumber = round,
                    MatchNumber = matchCounter++,
                    BracketType = BracketType.Swiss,
                    Format = game.Format,
                    ParticipationMode = game.ParticipationMode
                };
                if (round == 1)
                {
                    int p1Index = i * 2;
                    int p2Index = p1Index + 1;

                    MatchModeParticipantHelper.AssignParticipants(
                        match,
                        paddedParticipants[p1Index],
                        p2Index < paddedParticipants.Count ? paddedParticipants[p2Index] : null);

                    match.TryAssignByeWin();
                }

                matches.Add(match);
            }
        }
    }
    private void GeneratePlayoffMatches(Game game, List<Match> matches)
    {
        int startingRound = _maxRounds + 1;
        int matchIndex = 0;
        int roundSize = _playOffSize / 2;

        int totalRounds = (int)Math.Ceiling(Math.Log2(_playOffSize));

        for (int r = 0; r < totalRounds; r++)
        {
            int round = startingRound + r;

            if (r == totalRounds - 1)
                break;

            for (int i = 0; i < roundSize; i++)
            {
                var match = new Match
                {
                    GameId = game.Id,
                    RoundNumber = round,
                    MatchNumber = matchIndex++,
                    BracketType = game.BracketType,
                    Format = game.Format,
                    ParticipationMode = game.ParticipationMode
                };

                matches.Add(match);
            }

            roundSize /= 2;
        }

        var grandFinalMatch = new Match
        {
            GameId = game.Id,
            RoundNumber = startingRound + totalRounds - 1,
            MatchNumber = matchIndex++,
            BracketType = BracketType.SingleElimination,
            Format = game.FinalsFormat,
            ParticipationMode = game.ParticipationMode
        };

        matches.Add(grandFinalMatch);
    }
    private List<Participant?> PadTo16(List<Participant> participants)
    {
        var padded = new List<Participant?>(participants);
        while (padded.Count < _maxParticipants)
            padded.Add(null);

        return padded;
    }

    public void DeterminePlacements(Game game) => throw new NotImplementedException();
}

