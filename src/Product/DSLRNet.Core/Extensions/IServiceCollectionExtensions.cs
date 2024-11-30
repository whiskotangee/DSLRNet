using DSLRNet.Core.Generators;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DSLRNet.Core.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection InitializeDSLR(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<Configuration>(configuration.GetSection(nameof(Configuration)))
                .Configure<WeaponGeneratorConfig>(configuration.GetSection(nameof(WeaponGeneratorConfig)))
                .Configure<AllowListConfig>(configuration.GetSection(nameof(AllowListConfig)))
                .Configure<LoreConfig>(configuration.GetSection(nameof(LoreConfig)))
                .Configure<AshOfWarConfig>(configuration.GetSection(nameof(AshOfWarConfig)))
                .AddSingleton<ArmorLootGenerator>()
                .AddSingleton<WeaponLootGenerator>()
                .AddSingleton<TalismanLootGenerator>()
                .AddSingleton<ItemLotGenerator>()
                .AddSingleton<LoreGenerator>()
                .AddSingleton<AshofWarHandler>()
                .AddSingleton<DamageTypeHandler>()
                .AddSingleton<RarityHandler>()
                .AddSingleton<SpEffectHandler>()
                .AddSingleton<AllowListHandler>()
                .AddSingleton<DataRepository>()
                .AddSingleton<DSLRNetBuilder>()
                .AddSingleton<ProcessRunner>()
                .AddSingleton((sp) =>
                {
                    return new RandomNumberGetter(sp.GetRequiredService<IOptions<Configuration>>().Value.Settings.RandomSeed);
                });

        return services;
    }
}
