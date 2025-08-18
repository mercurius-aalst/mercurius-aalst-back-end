using AutoFixture;
using AutoFixture.Kernel;
using MercuriusAPI.Exceptions;
using MercuriusAPI.Models.LAN;
using MercuriusAPI.Tests.Customizations;

namespace MercuriusAPI.Tests
{
    public class MatchTests
    {
        [Fact]
        public void TryAssignByeWin_AssignsWinner_WhenOnlyParticipant2FilledIn()
        {
            // Arrange
            var match = CreateMatch();
            match.Participant1 = null;
            // Act
            match.TryAssignByeWin();
            // Assert
            Assert.NotNull(match.Winner);
            Assert.Equal(match.Participant2.Id, match.Winner.Id);          
        }

        [Fact]
        public void TryAssignByeWin_AssignsWinner_WhenOnlyParticipant1FilledIn()
        {
            // Arrange
            var match = CreateMatch();
            match.Participant2 = null;
            // Act
            match.TryAssignByeWin();
            // Assert
            Assert.NotNull(match.Winner);
            Assert.Equal(match.Participant1.Id, match.Winner.Id);
        }

        [Fact]
        public void TryAssignByeWin_DoesNotAssignWinner_WhenBothParticipantsExist()
        {
            // Arrange
            var match = CreateMatch();
            match.Winner = null;
            match.Loser = null;
            // Act
            match.TryAssignByeWin();
            // Assert
            Assert.Null(match.Winner);
        }
        [Fact]
        public void Start_SetsStartTimeToCurrentUtcTime()
        {
            // Arrange
            var match = new Match();
            // Act
            match.Start();
            // Assert
            Assert.True(match.StartTime <= DateTime.UtcNow);
        }
        [Fact]
        public void Finish_SetsEndTimeToCurrentUtcTime_AndUpdatesNextMatch()
        {
            // Arrange
            var match = CreateMatch();

            // Act
            match.Finish();
            // Assert
            Assert.True(match.EndTime <= DateTime.UtcNow);
        }

        [Fact]
        public void UpdateParticipantsNextMatch_SetsWinnerInWinnerNextMatch_Participant1_WhenMatchNumberOdd()
        {
            // Arrange
            var winner = CreatePlayer();
            var match = new Match
            {
                ParticipantType = ParticipantType.Player,
                Winner = winner,
                MatchNumber = 1,
                WinnerNextMatch = new Match()
            };

            // Act
            match.UpdateParticipantsNextMatch();

            // Assert
            Assert.Equal(winner, match.WinnerNextMatch.Participant1);
        }

        [Fact]
        public void UpdateParticipantsNextMatch_SetsWinnerInWinnerNextMatch_Participant2_WhenMatchNumberEven()
        {
            // Arrange
            var winner = CreatePlayer();
            var match = new Match
            {
                ParticipantType = ParticipantType.Player,
                Winner = winner,
                MatchNumber = 2,
                WinnerNextMatch = new Match()
            };

            // Act
            match.UpdateParticipantsNextMatch();

            // Assert
            Assert.Equal(winner, match.WinnerNextMatch.Participant2);
        }

        [Fact]
        public void UpdateParticipantsNextMatch_SetsLoserInLoserNextMatch_Participant1_WhenMatchNumberOdd()
        {
            // Arrange
            var winner = CreatePlayer();
            var loser = CreatePlayer();
            var match = new Match
            {
                ParticipantType = ParticipantType.Player,
                Winner = winner,
                Loser = loser,
                MatchNumber = 1,
                LoserNextMatch = new Match()
            };

            // Act
            match.UpdateParticipantsNextMatch();

            // Assert
            Assert.Equal(loser, match.LoserNextMatch.Participant1);
        }

        [Fact]
        public void UpdateParticipantsNextMatch_DoesNothing_WhenWinnerIsNull()
        {
            // Arrange
            var match = new Match
            {
                Winner = null,
                WinnerNextMatch = CreateMatch(),
                LoserNextMatch = CreateMatch(),
                MatchNumber = 1
            };
            match.WinnerNextMatch.Participant1 = null;
            match.LoserNextMatch.Participant1 = null;
            match.LoserNextMatch.Participant2 = null;
            match.WinnerNextMatch.Participant2 = null;

            // Act
            match.UpdateParticipantsNextMatch();

            // Assert
            Assert.Null(match.WinnerNextMatch.Participant1);
            Assert.Null(match.LoserNextMatch.Participant1);
        }

        [Fact]
        public void UpdateParticipantsNextMatch_SetsWinnerInLowerBracketMatch()
        {
            // Arrange
            var winner = CreatePlayer();
            var match = new Match
            {
                Winner = winner,
                WinnerNextMatch = new Match { IsLowerBracketMatch = true }
            };

            // Act
            match.UpdateParticipantsNextMatch();

            // Assert
            Assert.Equal(winner, match.WinnerNextMatch.Participant2);
        }

        [Fact]
        public void UpdateParticipantsNextMatch_SetsWinnerInUpperBracketMatch_Participant1_WhenMatchNumberOdd()
        {
            // Arrange
            var winner = CreatePlayer();
            var match = new Match
            {
                Winner = winner,
                MatchNumber = 1,
                WinnerNextMatch = new Match { IsLowerBracketMatch = false }
            };

            // Act
            match.UpdateParticipantsNextMatch();

            // Assert
            Assert.Equal(winner, match.WinnerNextMatch.Participant1);
        }

        [Fact]
        public void UpdateParticipantsNextMatch_SetsWinnerInUpperBracketMatch_Participant2_WhenMatchNumberEven()
        {
            // Arrange
            var winner = CreatePlayer();
            var match = new Match
            {
                Winner = winner,
                MatchNumber = 2,
                WinnerNextMatch = new Match { IsLowerBracketMatch = false }
            };

            // Act
            match.UpdateParticipantsNextMatch();

            // Assert
            Assert.Equal(winner, match.WinnerNextMatch.Participant2);
        }

        [Fact]
        public void UpdateParticipantsNextMatch_AssignsLoserToParticipant1_WhenMatchNumberOdd()
        {
            // Arrange
            var loser = CreatePlayer();
            var winner = CreatePlayer();
            var match = new Match
            {
                Loser = loser,
                Winner = winner,
                MatchNumber = 1,
                LoserNextMatch = CreateMatch()
            };

            // Act
            match.UpdateParticipantsNextMatch();

            // Assert
            Assert.Equal(loser, match.LoserNextMatch.Participant1);
        }

        [Fact]
        public void UpdateParticipantsNextMatch_AssignsLoserToParticipant1_WhenNotFirstRound()
        {
            // Arrange
            var loser = CreatePlayer();
            var winner = CreatePlayer();

            var match = new Match
            {
                Loser = loser,
                Winner = winner,
                MatchNumber = 2,
                LoserNextMatch = CreateMatch()
            };

            // Act
            match.UpdateParticipantsNextMatch();

            // Assert
            Assert.Equal(loser, match.LoserNextMatch.Participant1);
        }

        [Theory]
        [InlineData(GameFormat.BestOf1, 1, 0)]
        [InlineData(GameFormat.BestOf3, 2, 1)]
        [InlineData(GameFormat.BestOf5, 3, 2)]
        public void SetScoresAndWinner_SetsWinnerAndLoser_WhenParticipant1Wins(GameFormat format, int p1Score, int p2Score)
        {
            // Arrange
            var match = CreateMatch(format);

            // Act
            match.SetScoresAndWinner(p1Score, p2Score);

            // Assert
            Assert.Equal(p1Score, match.Participant1Score);
            Assert.Equal(p2Score, match.Participant2Score);
            Assert.Equal(match.Participant1Id, match.WinnerId);
            Assert.Equal(match.Participant2Id, match.LoserId);
        }

        [Theory]
        [InlineData(GameFormat.BestOf1, 0, 1)]
        [InlineData(GameFormat.BestOf3, 1, 2)]
        [InlineData(GameFormat.BestOf5, 2, 3)]
        public void SetScoresAndWinner_SetsWinnerAndLoser_WhenParticipant2Wins(GameFormat format, int p1Score, int p2Score)
        {
            // Arrange
            var match = CreateMatch(format);

            // Act
            match.SetScoresAndWinner(p1Score, p2Score);

            // Assert
            Assert.Equal(p1Score, match.Participant1Score);
            Assert.Equal(p2Score, match.Participant2Score);
            Assert.Equal(match.Participant2Id, match.WinnerId);
            Assert.Equal(match.Participant1Id, match.LoserId);
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        [InlineData(-5, -5)]
        public void SetScoresAndWinner_ThrowsValidationException_WhenScoreIsNegative(int p1Score, int p2Score)
        {
            // Arrange
            var match = CreateMatch();

            // Act & Assert
            var ex = Assert.Throws<ValidationException>(() => match.SetScoresAndWinner(p1Score, p2Score));
            Assert.Equal("Scores cannot be negative", ex.Message);
        }

        [Theory]
        [InlineData(GameFormat.BestOf1, 2, 0)]
        [InlineData(GameFormat.BestOf3, 3, 0)]
        [InlineData(GameFormat.BestOf3, 0, 3)]
        [InlineData(GameFormat.BestOf5, 4, 0)]
        [InlineData(GameFormat.BestOf5, 0, 4)]
        public void SetScoresAndWinner_ThrowsValidationException_WhenScoreExceedsWinsNeeded(GameFormat format, int p1Score, int p2Score)
        {
            // Arrange
            var match = CreateMatch(format);

            // Act & Assert
            Assert.Throws<ValidationException>(() => match.SetScoresAndWinner(p1Score, p2Score));
        }

        [Fact]
        public void SetScoresAndWinner_ThrowsValidationException_WhenScoresAreEqualInBo1()
        {
            // Arrange
            var match = CreateMatch(GameFormat.BestOf1);

            // Act & Assert
            var ex = Assert.Throws<ValidationException>(() => match.SetScoresAndWinner(1, 1));
            Assert.Equal("Scores cannot be equal in Bo1 format", ex.Message);
        }

        [Fact]
        public void SetScoresAndWinner_DoesNotSetWinner_WhenNoOneHasEnoughWins()
        {
            // Arrange
            var match = CreateMatch(GameFormat.BestOf3);

            // Act
            match.SetScoresAndWinner(1, 1);

            // Assert
            Assert.Null(match.Winner);
            Assert.Null(match.Loser);
            Assert.Equal(1, match.Participant1Score);
            Assert.Equal(1, match.Participant2Score);
        }

        [Fact]
        public void SetScoresAndWinner_DoesNotSetWinner_WhenScoresAreZero()
        {
            // Arrange
            var match = CreateMatch(GameFormat.BestOf3);

            // Act
            match.SetScoresAndWinner(0, 0);

            // Assert
            Assert.Null(match.Winner);
            Assert.Null(match.Loser);
            Assert.Equal(0, match.Participant1Score);
            Assert.Equal(0, match.Participant2Score);
        }

        private Match CreateMatch()
        {
            var fixture = GetFixture();
            fixture.Customizations.Add(new TypeRelay(typeof(Participant), typeof(Player)));
            fixture.Customizations.Add(new TypeRelay(typeof(Participant), typeof(Team)));
            fixture.Customize(new MatchParticipantCustomization());
            return fixture.Create<Match>();
        }

        private Match CreateMatch(GameFormat format = GameFormat.BestOf1)
        {
            var fixture = GetFixture();
            fixture.Customizations.Add(new TypeRelay(typeof(Participant), typeof(Player)));
            fixture.Customizations.Add(new TypeRelay(typeof(Participant), typeof(Team)));
            fixture.Customize(new MatchParticipantCustomization());
            var match = fixture.Build<Match>()
                .Without(m => m.Winner)
                .Without(m => m.Loser)
                .Without(m => m.WinnerId)
                .Without(m => m.LoserId)
                .With(m => m.Format, format)
                .Create();

            return match;
        }

        private Fixture GetFixture()
        {
            var fixture = new Fixture();
            fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
               .ForEach(b => fixture.Behaviors.Remove(b));
            fixture.Behaviors.Add(new OmitOnRecursionBehavior(1));

            return fixture;
        }

        private Player CreatePlayer()
        {
            var fixture = GetFixture();
            fixture.Customizations.Add(new TypeRelay(typeof(Participant), typeof(Player)));
            fixture.Customize(new MatchParticipantCustomization());
            return fixture.Create<Player>();
        }
    }
}
