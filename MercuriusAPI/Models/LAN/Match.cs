namespace MercuriusAPI.Models.LAN
{
    public class Match{
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public BracketType  BracketType { get; set; }
        public GameFormat Format { get; set; }
        public ParticipantType ParticipantType { get; set; }
       
        public int RoundNumber { get; set; }
        public int MatchNumber { get; set; }
        public bool IsLowerBracketMatch { get; set; }

        public int GameId { get; set; }
        public int? Pariticipant1Id { get; set; }
        public int? Participant2Id { get; set; }
        public int? WinnerId { get; set; }
        public int? LoserId { get; set; }

        public int? Participant1Score { get; set; }
        public int? Participant2Score { get; set; }

        public int? WinnerNextMatchId { get; set; }
        public int? LoserNextMatchId { get; set; }

        public Game Game { get; set; }
        public Participant? Participant1 { get; set; }
        public Participant? Participant2 { get; set; }
        public Participant? Winner { get; set; }
        public Participant? Loser { get; set; }

        public Match? WinnerNextMatch { get; set; }
        public Match? LoserNextMatch { get; set; }

        public void TryAssignByeWin()
        {
            if(Participant1 == null && Participant2 != null)
            {
                Winner = Participant2;
                UpdateParticipantsNextMatch();
            }
            else if(Participant2 == null && Participant1 != null)
            {
                Winner = Participant1;
                UpdateParticipantsNextMatch();
            }
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
                throw new Exception("Scores cannot be negative");
            int winsNeeded = Format switch
            {
                GameFormat.BestOf1 => 1,
                GameFormat.BestOf3 => 2,
                GameFormat.BestOf5 => 3,
                _ => 1
            };
            if(participant1Score > winsNeeded || participant2Score > winsNeeded)
                throw new ArgumentException("Scores cannot exceed the required number of wins for the match format.");
            if(participant1Score == participant2Score && participant1Score+participant2Score != 0 && winsNeeded == 1)
                throw new Exception("Scores cannot be equal in Bo1 format");

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
            if(Winner != null)
            {
                if(WinnerNextMatch is not null)
                {
                    if(MatchNumber % 2 != 0)
                        WinnerNextMatch.Participant1 = Winner;
                    else
                        WinnerNextMatch.Participant2 = Winner;
                }
                if(LoserNextMatch is not null)
                {
                    if(MatchNumber % 2 != 0)
                        LoserNextMatch.Participant1 = Loser;
                    else
                        LoserNextMatch.Participant2 = Loser;
                }
            }
        }
    }
}
