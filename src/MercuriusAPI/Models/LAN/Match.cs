using MercuriusAPI.Exceptions;

namespace MercuriusAPI.Models.LAN
{
    public class Match
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public BracketType BracketType { get; set; }
        public GameFormat Format { get; set; }
        public ParticipantType ParticipantType { get; set; }

        public int RoundNumber { get; set; }
        public int MatchNumber { get; set; }
        public bool IsLowerBracketMatch { get; set; }

        public int GameId { get; set; }
        public int? Participant1Id { get; set; }
        public int? Participant2Id { get; set; }
        public int? WinnerId { get; set; }
        public int? LoserId { get; set; }

        public int? Participant1Score { get; set; }
        public int? Participant2Score { get; set; }

        public int? WinnerNextMatchId { get; set; }
        public int? LoserNextMatchId { get; set; }

        public bool Participant1IsBYE { get; set; }
        public bool Participant2IsBYE { get; set; }

        public Game Game { get; set; }
        public Participant? Participant1 { get; set; }
        public Participant? Participant2 { get; set; }
        public Participant? Winner { get; set; }
        public Participant? Loser { get; set; }

        public Match? WinnerNextMatch { get; set; }
        public Match? LoserNextMatch { get; set; }

        public void TryAssignByeWin()
        {
            if(Participant1IsBYE || Participant2IsBYE)
            {
                if((Participant1 == null && Participant2 != null))
                {
                    // Participant 1 is BYE, so Participant 2 wins
                    AssignWinner(Participant2, isParticipant1Bye: true);
                }
                else if((Participant2 == null && Participant1 != null))
                {
                    // Participant 2 is BYE, so Participant 1 wins
                    AssignWinner(Participant1, isParticipant2Bye: true);
                }
            }
        }

        public void SetParticipantBYEs(bool participant1BYE, bool participant2BYE)
        {
            if(RoundNumber != 1 && participant1BYE && participant2BYE)
                return;

            Participant1IsBYE = participant1BYE;
            Participant2IsBYE = participant2BYE;
        }

        private void AssignWinner(Participant? winner, bool isParticipant1Bye = false, bool isParticipant2Bye = false)
        {
            Winner = winner;
            UpdateParticipantsNextMatch();
        }

        public void Start()
        {
            StartTime = DateTime.UtcNow;
        }

        public void Finish()
        {
            EndTime = DateTime.UtcNow;
            UpdateParticipantsNextMatch();
        }

        public void SetScoresAndWinner(int participant1Score, int participant2Score)
        {
            if(participant1Score < 0 || participant2Score < 0)
                throw new ValidationException("Scores cannot be negative");
            int winsNeeded = Format switch
            {
                GameFormat.BestOf1 => 1,
                GameFormat.BestOf3 => 2,
                GameFormat.BestOf5 => 3,
                _ => 1
            };
            if(participant1Score > winsNeeded || participant2Score > winsNeeded)
                throw new ValidationException("Scores cannot exceed the required number of wins for the match format.");
            if(participant1Score == participant2Score && participant1Score + participant2Score != 0 && winsNeeded == 1)
                throw new ValidationException("Scores cannot be equal in Bo1 format");

            Participant1Score = participant1Score;
            Participant2Score = participant2Score;

            if(participant1Score == winsNeeded && participant1Score > participant2Score)
            {
                Winner = Participant1;
                Loser = Participant2;
                Finish();
            }
            else if(participant2Score == winsNeeded && participant2Score > participant1Score)
            {
                Winner = Participant2;
                Loser = Participant1;
                Finish();
            }
        }

        public void UpdateParticipantsNextMatch()
        {
            if (Winner == null)
                return;

            // Assign Winner to the next match
            if (WinnerNextMatch != null)
            {
                if (WinnerNextMatch.IsLowerBracketMatch)
                {
                    if (WinnerNextMatch.Participant2 == null || WinnerNextMatch.Participant2?.Id == Winner.Id)
                        WinnerNextMatch.Participant2 = Winner;
                    else
                        WinnerNextMatch.Participant1 = Winner;

                    if(WinnerNextMatch.Participant1IsBYE || WinnerNextMatch.Participant2IsBYE)
                        WinnerNextMatch.TryAssignByeWin();
                }
                else
                {
                    if (MatchNumber % 2 != 0 && !IsLowerBracketMatch)
                        WinnerNextMatch.Participant1 = Winner;
                    else
                        WinnerNextMatch.Participant2 = Winner;
                }          
            }

            // Assign Loser to the next match
            if (LoserNextMatch != null)
            {
                if (RoundNumber == 1)
                {
                    if (MatchNumber % 2 != 0)
                        LoserNextMatch.Participant1 = Loser;
                    else
                        LoserNextMatch.Participant2 = Loser;
                }
                else
                {
                    LoserNextMatch.Participant1 = Loser;
                }
                if(LoserNextMatch.Participant1IsBYE || LoserNextMatch.Participant2IsBYE)
                    LoserNextMatch.TryAssignByeWin();
            }
        }
    }
}
