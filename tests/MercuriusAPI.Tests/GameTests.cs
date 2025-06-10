using MercuriusAPI.Models.LAN;
using System.ComponentModel.DataAnnotations;

namespace MercuriusAPI.Tests
{
    public class GameTests
    {
        private Game CreateGame(
            string name = "Test Game",
            BracketType bracketType = BracketType.SingleElimination,
            GameFormat format = GameFormat.BestOf1,
            GameFormat finalsFormat = GameFormat.BestOf1,
            ParticipantType participantType = ParticipantType.Player)
        {
            return new Game(name, bracketType, format, finalsFormat, participantType);
        }

        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            var game = CreateGame("LAN", BracketType.RoundRobin, GameFormat.BestOf3, GameFormat.BestOf5, ParticipantType.Team);

            Assert.Equal("LAN", game.Name);
            Assert.Equal(BracketType.RoundRobin, game.BracketType);
            Assert.Equal(GameFormat.BestOf3, game.Format);
            Assert.Equal(GameFormat.BestOf5, game.FinalsFormat);
            Assert.Equal(GameStatus.Scheduled, game.Status);
            Assert.Equal(ParticipantType.Team, game.ParticipantType);
            Assert.NotNull(game.Placements);
            Assert.NotNull(game.Matches);
            Assert.NotNull(game.Participants);
        }

        [Fact]
        public void Update_UpdatesProperties_WhenStatusIsScheduled()
        {
            var game = CreateGame();
            game.Update("Updated", BracketType.DoubleElimination, GameFormat.BestOf3, GameFormat.BestOf5);

            Assert.Equal("Updated", game.Name);
            Assert.Equal(BracketType.DoubleElimination, game.BracketType);
            Assert.Equal(GameFormat.BestOf3, game.Format);
            Assert.Equal(GameFormat.BestOf5, game.FinalsFormat);
        }

        [Theory]
        [InlineData(GameStatus.InProgress)]
        [InlineData(GameStatus.Completed)]
        public void Update_ThrowsException_WhenStatusIsInProgressOrCompleted(GameStatus status)
        {
            var game = CreateGame();
            game.Status = status;

            Assert.Throws<ValidationException>(() =>
                game.Update("New", BracketType.Swiss, GameFormat.BestOf1, GameFormat.BestOf3));
        }

        [Fact]
        public void Cancel_SetsStatusToCanceled_WhenNotCompleted()
        {
            var game = CreateGame();
            game.Status = GameStatus.InProgress;

            game.Cancel();

            Assert.Equal(GameStatus.Canceled, game.Status);
        }

        [Fact]
        public void Cancel_ThrowsException_WhenStatusIsCompleted()
        {
            var game = CreateGame();
            game.Status = GameStatus.Completed;

            Assert.Throws<ValidationException>(() => game.Cancel());
        }

        [Fact]
        public void Start_SetsStatusAndStartTime_WhenScheduledAndEnoughParticipants()
        {
            var game = CreateGame();
            game.Status = GameStatus.Scheduled;
            game.Participants.Add(new TestParticipant());
            game.Participants.Add(new TestParticipant());

            game.Start();

            Assert.Equal(GameStatus.InProgress, game.Status);
            Assert.True(game.StartTime <= DateTime.UtcNow && game.StartTime > DateTime.UtcNow.AddMinutes(-1));
        }

        [Fact]
        public void Start_ThrowsException_WhenNotScheduled()
        {
            var game = CreateGame();
            game.Status = GameStatus.InProgress;
            game.Participants.Add(new TestParticipant());
            game.Participants.Add(new TestParticipant());

            Assert.Throws<ValidationException>(() => game.Start());
        }

        [Fact]
        public void Start_ThrowsException_WhenNotEnoughParticipants()
        {
            var game = CreateGame();
            game.Status = GameStatus.Scheduled;
            game.Participants.Add(new TestParticipant());

            Assert.Throws<ValidationException>(() => game.Start());
        }

        [Fact]
        public void Complete_SetsStatusAndEndTime_WhenInProgress()
        {
            var game = CreateGame();
            game.Status = GameStatus.InProgress;

            game.Complete();

            Assert.Equal(GameStatus.Completed, game.Status);
            Assert.True(game.EndTime <= DateTime.UtcNow && game.EndTime > DateTime.UtcNow.AddMinutes(-1));
        }

        [Fact]
        public void Complete_ThrowsException_WhenNotInProgress()
        {
            var game = CreateGame();
            game.Status = GameStatus.Scheduled;

            Assert.Throws<ValidationException>(() => game.Complete());
        }

        [Theory]
        [InlineData(GameStatus.Completed)]
        [InlineData(GameStatus.Canceled)]
        public void Reset_SetsStatusAndClearsCollections_WhenCompletedOrCanceled(GameStatus status)
        {
            var game = CreateGame();
            game.Status = status;
            game.StartTime = DateTime.UtcNow;
            game.EndTime = DateTime.UtcNow;
            game.Matches.Add(new Match());
            game.Participants.Add(new TestParticipant());

            game.Reset();

            Assert.Equal(GameStatus.Scheduled, game.Status);
            Assert.Equal(DateTime.MinValue, game.StartTime);
            Assert.Equal(DateTime.MinValue, game.EndTime);
            Assert.Empty(game.Matches);
            Assert.Empty(game.Participants);
        }

        [Fact]
        public void Reset_ThrowsException_WhenNotCompletedOrCanceled()
        {
            var game = CreateGame();
            game.Status = GameStatus.InProgress;

            Assert.Throws<ValidationException>(() => game.Reset());
        }

        // Minimal stub for Participant
        private class TestParticipant : Participant
        {
            public TestParticipant() { Games = new List<Game>(); }
        }
    }
}
