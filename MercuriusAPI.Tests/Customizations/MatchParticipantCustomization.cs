using AutoFixture;
using AutoFixture.Kernel;
using MercuriusAPI.Models.LAN;
using System;
using System.Reflection;

namespace MercuriusAPI.Tests.Customizations;
public class MatchParticipantCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customizations.Add(new ParticipantPropertyBuilder());
    }

    private class ParticipantPropertyBuilder : ISpecimenBuilder
    {
        public object Create(object request, ISpecimenContext context)
        {
            if (request is PropertyInfo pi &&
                pi.DeclaringType == typeof(Match) &&
                (pi.Name == nameof(Match.Participant1) || pi.Name == nameof(Match.Participant2)))
            {
                // Try to get the current Match instance from the context
                var match = context.Resolve(typeof(Match)) as Match;
                if (match != null)
                {
                    if (match.ParticipantType == ParticipantType.Player)
                        return context.Resolve(typeof(Player));
                    else if (match.ParticipantType == ParticipantType.Team)
                        return context.Resolve(typeof(Team));
                }
                // Fallback: just return a Player
                return context.Resolve(typeof(Player));
            }
            return new NoSpecimen();
        }
    }
}
