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
        public int? Participant1Score { get; set; }
        public int? Participant2Score { get; set; }

        public Game Game { get; set; }
        public Participant? Participant1 { get; set; }
        public Participant? Participant2 { get; set; }
        public Participant? Winner { get; set; }

        public void TryAssignByeWin()
        {
            if(Participant1 == null && Participant2 != null)
            {
                Winner = Participant2;
                Participant2 = null;
            }
            else if(Participant2 == null && Participant1 != null)
            {
                Winner = Participant1;
                Participant1 = null;
            }
        }

        public void SetScoresAndWinner(int participant1Score, int participant2Score)
        {
            if(participant1Score < 0 || participant2Score < 0)
                throw new Exception("Scores cannot be negative");
            if(participant1Score == participant2Score)
                throw new Exception("Scores cannot be equal");
            int winsNeeded = Format switch
            {
                GameFormat.BestOf1 => 1,
                GameFormat.BestOf3 => 2,
                GameFormat.BestOf5 => 3,
                _ => 1
            };
            if(participant1Score > winsNeeded || participant2Score > winsNeeded)
                throw new ArgumentException("Scores cannot exceed the required number of wins for the match format.");

            Participant1Score = participant1Score;
            Participant2Score = participant2Score;

            if(participant1Score == winsNeeded && participant1Score > participant2Score)
            {
                Winner = Participant1;
            }
            else if(participant2Score >= winsNeeded && participant2Score > participant1Score)
            {
                Winner = Participant2;
            }
        }
    }
}
