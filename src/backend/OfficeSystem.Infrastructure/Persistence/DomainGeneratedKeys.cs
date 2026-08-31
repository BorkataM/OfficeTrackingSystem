using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace OfficeSystem.Infrastructure.Persistence;

/// <summary>
/// Identifiers are minted by the domain (<c>Guid.CreateVersion7</c>), never by the
/// store. Saying so explicitly matters: left to its default, EF assumes it owns
/// Guid key generation and then reads an already-set key on a newly created child
/// as "this row exists", issuing an UPDATE where an INSERT belongs.
/// </summary>
internal static class DomainGeneratedKeys
{
    public static void UseDomainGeneratedKeys(this ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entity in modelBuilder.Model.GetEntityTypes())
        {
            IMutableKey? primaryKey = entity.FindPrimaryKey();

            if (primaryKey is null)
            {
                continue;
            }

            foreach (IMutableProperty property in primaryKey.Properties.Where(p => p.ClrType == typeof(Guid)))
            {
                property.ValueGenerated = ValueGenerated.Never;
            }
        }
    }
}
