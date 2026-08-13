using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Modding.Custom;

namespace PineappleBlitz;

// Runs just after trader registration but before the handbook/trader/ragfair callbacks,
// so the new item, its handbook entry and its Prapor assort are all in place before those read the DB.
[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.TraderRegistration + 1)]
public class PineappleBlitzMod(
    TemplateTable templateTable,
    TradersTable tradersTable,
    PmcConfig pmcConfig,
    ItemConfig itemConfig,
    CustomItemService customItemService,
    ModHelper modHelper,
    ISptLogger<PineappleBlitzMod> logger) : IOnLoad
{
    private static readonly MongoId ItemId = new("66a3f5c8d2b1e4a790f3c2d1");
    private static readonly MongoId CloneFromId = new("5710c24ad2720bc3458b45a3"); // F-1 grenade
    private static readonly MongoId GrenadeParent = new("543be6564bdc2df4348b4568"); // ThrowWeap
    private static readonly MongoId PraporId = new("54cb50c76803fa8b248b4571");
    private static readonly MongoId RoublesId = new("5449016a4bdc2d6f028b456f");
    private const string HandbookCategory = "5b5f7a2386f774093f2ed3c4"; // Grenades

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            var config = LoadConfig();

            CreateItem(config);
            ApplyProperties(config);
            AddToTrader(config.Price);

            if (config.BlacklistFromBots)
            {
                BlacklistFromBots();
            }

            AddToGrenadeKillQuests();
        }
        catch (Exception ex)
        {
            logger.Error($"[PineappleBlitz] Failed to load: {ex.Message}", ex);
        }

        return Task.CompletedTask;
    }

    private void CreateItem(ModConfig config)
    {
        if (templateTable.Items.ContainsKey(ItemId))
        {
            return;
        }

        var cloneDetails = new NewItemFromCloneDetails
        {
            ItemTplToClone = CloneFromId,
            ParentId = GrenadeParent,
            NewId = ItemId,
            NewItemName = "pineapple_blitz_grenade",
            HandbookParentId = HandbookCategory,
            FleaPriceRoubles = config.Price,
            HandbookPriceRoubles = config.Price,
            AddToHandbook = true,
            AddToFleaPriceDb = true,
            Locales = new Dictionary<string, LocaleDetails>
            {
                {
                    "en", new LocaleDetails
                    {
                        Name = "Pineapple Blitz Grenade",
                        ShortName = "PBG",
                        Description =
                            "A short fuse, big bang grenade! Perfect for taking out enemies before they can run away, just don't be too close!"
                    }
                }
            }
        };

        // 4.1 requires the calling assembly so the server can attribute the item to this mod.
        var result = customItemService.CreateItemFromClone(cloneDetails, Assembly.GetExecutingAssembly());
        if (!result.Success)
        {
            logger.Error($"[PineappleBlitz] Could not create item: {string.Join("; ", result.Errors ?? [])}");
        }
    }

    private void ApplyProperties(ModConfig config)
    {
        // Re-applied on every boot so config edits take effect even though the item already exists.
        if (!templateTable.Items.TryGetValue(ItemId, out var grenade) || grenade.Properties is null)
        {
            return;
        }

        var props = grenade.Properties;
        props.ExplDelay = config.FuzeTimer;
        props.explDelay = config.FuzeTimer; // the client reads the lowercase variant
        props.FragmentsCount = config.Fragmentations;
        props.MinExplosionDistance = config.ExplosionMinimum;
        props.MaxExplosionDistance = config.ExplosionMaximum;
        props.HeavyBleedingDelta = config.HeavyBleedPercent;
        props.LightBleedingDelta = config.LightBleedPercent;
        props.Damage = config.Damage;
        props.PenetrationPower = config.Penetration;
        props.CanSellOnRagfair = true;
    }

    private void AddToTrader(int price)
    {
        var trader = tradersTable.GetTrader(PraporId);
        if (trader?.Assort is null)
        {
            logger.Warning("[PineappleBlitz] Prapor assort unavailable, skipping trader offer");
            return;
        }

        var assortItemId = new MongoId(GenerateShortId("PineappleBlitz_Prapor"));
        if (trader.Assort.Items.Any(item => item.Id == assortItemId))
        {
            return;
        }

        trader.Assort.Items.Add(new Item
        {
            Id = assortItemId,
            Template = ItemId,
            ParentId = "hideout",
            SlotId = "hideout",
            Upd = new Upd
            {
                UnlimitedCount = true,
                StackObjectsCount = 999999
            }
        });

        // BarterScheme is now strongly typed: item -> list of alternative barters -> the parts of one barter.
        trader.Assort.BarterScheme[assortItemId] =
        [
            [
                new BarterScheme
                {
                    Template = RoublesId,
                    Count = price
                }
            ]
        ];

        trader.Assort.LoyalLevelItems[assortItemId] = 1;
    }

    private void BlacklistFromBots()
    {
        if (!pmcConfig.GlobalLootBlacklist.Contains(ItemId))
        {
            pmcConfig.GlobalLootBlacklist.Add(ItemId);
        }

        pmcConfig.VestLoot?.Blacklist?.Add(ItemId);
        pmcConfig.PocketLoot?.Blacklist?.Add(ItemId);
        pmcConfig.BackpackLoot?.Blacklist?.Add(ItemId);

        // LootableItemBlacklist keeps it out of generated loot without also pulling it from the flea,
        // which the plain ItemConfig.Blacklist would do.
        itemConfig.LootableItemBlacklist?.Add(ItemId);
    }

    /// <summary>
    ///     Adds the grenade to every quest kill-condition that already whitelists a hand grenade.
    ///     Covers Grenadier, The Art of Explosion and Fearless Beast today, and picks up any future
    ///     or modded grenade quest automatically. Condition IDs are reused across quests in the BSG
    ///     database, so matching on the weapon list rather than on IDs is the reliable approach.
    /// </summary>
    private void AddToGrenadeKillQuests()
    {
        var patchedQuests = new List<string>();

        foreach (var (questId, quest) in templateTable.Quests)
        {
            var conditions = quest.Conditions;
            if (conditions is null)
            {
                continue;
            }

            var buckets = new[]
            {
                conditions.AvailableForFinish,
                conditions.Started,
                conditions.Success,
                conditions.Fail
            };

            var patched = false;

            foreach (var bucket in buckets)
            {
                if (bucket is null)
                {
                    continue;
                }

                foreach (var condition in bucket)
                {
                    var counterConditions = condition.Counter?.Conditions;
                    if (counterConditions is null)
                    {
                        continue;
                    }

                    foreach (var counterCondition in counterConditions)
                    {
                        var weapons = counterCondition.Weapon;

                        // An empty weapon list means "any weapon", so it already counts our grenade.
                        if (weapons is null || weapons.Count == 0 || weapons.Contains(ItemId.ToString()))
                        {
                            continue;
                        }

                        if (!weapons.Any(IsThrowable))
                        {
                            continue;
                        }

                        weapons.Add(ItemId.ToString());
                        patched = true;
                    }
                }
            }

            if (patched)
            {
                patchedQuests.Add(string.IsNullOrEmpty(quest.QuestName) ? questId.ToString() : quest.QuestName);
            }
        }

        if (patchedQuests.Count > 0)
        {
            logger.Success($"[PineappleBlitz] Grenade now counts for {patchedQuests.Count} quest(s): {string.Join(", ", patchedQuests)}");
        }
        else
        {
            logger.Warning("[PineappleBlitz] Found no grenade kill quests to patch");
        }
    }

    private bool IsThrowable(string tpl)
    {
        return MongoId.IsValidMongoId(tpl)
               && templateTable.Items.TryGetValue(new MongoId(tpl), out var item)
               && item.Parent == GrenadeParent;
    }

    private static string GenerateShortId(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(hash)[..24];
    }

    private ModConfig LoadConfig()
    {
        try
        {
            var modFolder = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
            var configPath = System.IO.Path.Combine(modFolder, "config", "config.json");

            if (File.Exists(configPath))
            {
                var config = JsonSerializer.Deserialize<ModConfig>(
                    File.ReadAllText(configPath),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (config is not null)
                {
                    return config;
                }
            }

            logger.Warning("[PineappleBlitz] Could not read config, using defaults");
        }
        catch (Exception ex)
        {
            logger.Warning($"[PineappleBlitz] Could not load config ({ex.Message}), using defaults");
        }

        return new ModConfig();
    }
}
