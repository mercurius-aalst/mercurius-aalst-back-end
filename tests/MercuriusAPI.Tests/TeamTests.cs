using AutoFixture;
using AutoFixture.Kernel;
using MercuriusAPI.Exceptions;
using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.Tests
{
    public class TeamTests
    {
        [Fact]
        public void Team_Creation_Should_Set_Properties_Correctly()
        {
            // Arrange
            var teamName = "Test Team";
            var captain = CreatePlayer();
            var team = new Team(teamName, captain);
            // Act & Assert
            Assert.Equal(teamName, team.Name);
            Assert.Equal(captain.Id, team.CaptainId);
            Assert.Contains(captain, team.Players);
        }

        [Fact]
        public void Update_Team_Should_Update_Properties_Correctly()
        {
            // Arrange
            var team = CreateTeam();
            var newName = "Updated Team Name";
            var newCaptain = CreatePlayer();
            // Act
            team.Update(newName, newCaptain.Id);
            // Assert
            Assert.Equal(newName, team.Name);
            Assert.Equal(newCaptain.Id, team.CaptainId);
        }

        [Fact]
        public void Update_Team_Should_Not_Update_When_New_Name_Is_Null()
        {
            // Arrange
            var team = CreateTeam();
            var newCaptain = CreatePlayer();
            // Act
            team.Update(null, newCaptain.Id);
            // Assert
            Assert.Equal(team.Name, team.Name);
            Assert.Equal(newCaptain.Id, team.CaptainId);
        }

        [Fact]
        public void Update_Team_Should_Not_Update_CaptainId_When_New_CaptainId_Is_Null()
        {
            // Arrange
            var team = CreateTeam();
            var newName = "Updated Team Name";
            // Act
            team.Update(newName, null);
            // Assert
            Assert.Equal(newName, team.Name);
            Assert.Equal(team.CaptainId, team.CaptainId);
        }

        [Fact]
        public void Update_Team_Should_Not_Update_When_Both_Properties_Are_Null()
        {
            // Arrange
            var team = CreateTeam();
            // Act
            team.Update(null, null);
            // Assert
            Assert.Equal(team.Name, team.Name);
            Assert.Equal(team.CaptainId, team.CaptainId);
        }

        [Fact]
        public void RemovePlayer_Should_Remove_Player_From_Team()
        {
            // Arrange
            var team = CreateTeam();
            var playerToRemove = CreatePlayer();
            team.Players.Add(playerToRemove);
            // Act
            team.RemovePlayer(playerToRemove.Id);
            // Assert
            Assert.DoesNotContain(playerToRemove, team.Players);
        }
        [Fact]
        public void RemovePlayer_Should_Not_Remove_Player_If_Not_In_Team()
        {
            // Arrange
            var team = CreateTeam();
            var playerToRemove = CreatePlayer();
            // Act & Assert
            Assert.Throws<NotFoundException>(() => team.RemovePlayer(playerToRemove.Id));
        }

        [Fact]
        public void RemovePlayer_Should_Not_Remove_Captain_From_Team()
        {
            // Arrange
            var team = CreateTeam();
            var captain = team.Captain;
            team.CaptainId = captain.Id;
            team.Players.Add(captain);
            // Act & Assert
            Assert.Throws<ValidationException>(() => team.RemovePlayer(team.CaptainId));
        }



        private Player CreatePlayer()
        {
            var fixture = GetFixture();
            fixture.Customizations.Add(
                new TypeRelay(
                typeof(Participant),
                typeof(Player)));
            return fixture.Create<Player>();
        }

        private Team CreateTeam()
        {
            var fixture = GetFixture();
            fixture.Customizations.Add(
                new TypeRelay(
                typeof(Participant),
                typeof(Team)));
            return fixture.Create<Team>();
        }

        private Fixture GetFixture()
        {
            var fixture = new Fixture();
            fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
               .ForEach(b => fixture.Behaviors.Remove(b));
            fixture.Behaviors.Add(new OmitOnRecursionBehavior(1));

            return fixture;
        }
    }
}
