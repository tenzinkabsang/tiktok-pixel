using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Widgets.TikTokPixel.Domain;

namespace Nop.Plugin.Widgets.TikTokPixel.Data;

[NopMigration("2024-04-10 17:00:00", "Widgets.TikTokPixel base schema", MigrationProcessType.Installation)]
public class TikTokPixelSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.TableFor<TikTokPixelConfiguration>();
    }
}
