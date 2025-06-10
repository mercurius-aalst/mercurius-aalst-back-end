using System.ComponentModel.DataAnnotations;

namespace MercuriusAPI.Models.LAN
{
    public class Game
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PictureUrl { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public GameStatus Status { get; set; }
        public BracketType BracketType { get; set; }
        public GameFormat Format { get; set; }
        public GameFormat FinalsFormat { get; set; }
        public ParticipantType ParticipantType { get; set; }

        public IList<Placement> Placements { get; set; } = [];

        public IList<Match> Matches { get; set; } = new List<Match>();
        public IList<Participant> Participants { get; set; } = [];

        public Game(string name, string pictureUrl, BracketType bracketType, GameFormat format, GameFormat finalsFormat, ParticipantType participantType)
        {
            Name = name;
            PictureUrl = pictureUrl;
            BracketType = bracketType;
            Format = format;
            FinalsFormat = finalsFormat;
            Status = GameStatus.Scheduled;
            ParticipantType = participantType;
            Placements = new List<Placement>();
        }
        public Game()
        {
        }

        public void Update(string name, string pictureUrl, BracketType bracketType, GameFormat format, GameFormat finalsFormat)
        {
            if(Status == GameStatus.InProgress || Status == GameStatus.Completed)
                throw new ValidationException("Game cannot be updated when it's in progress or completed.");
            Name = name;
            PictureUrl = pictureUrl;
            BracketType = bracketType;
            Format = format;
            FinalsFormat = finalsFormat;
        }
        public void Cancel()
        {
            if(Status == GameStatus.Completed)
                throw new ValidationException("Game cannot be canceled when it's already completed.");
            Status = GameStatus.Canceled;
        }

        public void Start()
        {
            if(Status != GameStatus.Scheduled)
                throw new ValidationException("Game has to be scheduled to be able to start");
            if(Participants.Count < 2)
                throw new ValidationException("At least 2 participants required.");
            StartTime = DateTime.UtcNow; 
            Status = GameStatus.InProgress;
        }

        public void Complete()
        {
            if(Status != GameStatus.InProgress)
                throw new ValidationException("Game has to be in progress to be able to complete");
            EndTime = DateTime.UtcNow;
            Status = GameStatus.Completed;
        }

        public void Reset()
        {
            if(Status != GameStatus.Completed && Status != GameStatus.Canceled)
                throw new ValidationException("Game has to be completed or canceled to be able to reset");
            Status = GameStatus.Scheduled;
            StartTime = DateTime.MinValue;
            EndTime = DateTime.MinValue;
            Matches.Clear();
            Participants.Clear();
        }
    }
}