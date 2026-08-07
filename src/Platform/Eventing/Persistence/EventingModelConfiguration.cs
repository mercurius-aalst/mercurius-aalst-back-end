using Microsoft.EntityFrameworkCore;

namespace Platform.Eventing.Persistence;

public static class EventingModelConfiguration
{
    public static ModelBuilder ApplyEventingModelConfiguration(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());

        return modelBuilder;
    }
}
