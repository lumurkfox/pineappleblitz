using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Mod;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using System.Text.Json;

namespace PineappleBlitz;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class PineappleBlitzMod(
    DatabaseService databaseService,
    CustomItemService customItemService) : IOnLoad
{
    private const string ITEM_ID = "66a3f5c8d2b1e4a790f3c2d1";
    private const string CLONE_FROM_ID = "5710c24ad2720bc3458b45a3"; // F-1 grenade
    private const string GRENADE_PARENT = "543be6564bdc2df4348b4568"; // Throwable weapon parent
    private const string HANDBOOK_CATEGORY = "5b5f7a2386f774093f2ed3c4"; // Grenades
    private const string PRAPOR_ID = "54cb50c76803fa8b248b4571";
    private const string ROUBLES_ID = "5449016a4bdc2d6f028b456f";

    public async Task OnLoad()
    {
        try
        {
            var configPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "",
                "config", "config.json");
            var config = LoadConfig(configPath);

            var tables = databaseService.GetTables();
            dynamic items = tables.Templates.Items;

            // Create item if it doesn't exist
            if (!items.ContainsKey(ITEM_ID))
            {
                var cloneDetails = new NewItemFromCloneDetails
                {
                    ItemTplToClone = CLONE_FROM_ID,
                    ParentId = GRENADE_PARENT,
                    NewId = ITEM_ID,
                    HandbookParentId = HANDBOOK_CATEGORY,
                    FleaPriceRoubles = config.Price,
                    HandbookPriceRoubles = config.Price,
                    Locales = new Dictionary<string, LocaleDetails>
                    {
                        {
                            "en", new LocaleDetails
                            {
                                Name = "Pineapple Blitz Grenade",
                                ShortName = "PBG",
                                Description = "A short fuse, big bang grenade! Perfect for taking out enemies before they can run away, just don't be too close!"
                            }
                        }
                    }
                };

                customItemService.CreateItemFromClone(cloneDetails);
            }

            // Always apply config properties (even if item already exists)
            dynamic customGrenade = items[ITEM_ID];
            var itemType = customGrenade.GetType();
            var propsProperty = itemType.GetProperty("Properties") ?? itemType.GetProperty("Props");

            if (propsProperty != null)
            {
                dynamic props = propsProperty.GetValue(customGrenade);
                var propsType = props.GetType();

                // Set both ExplDelay and explDelay (game uses lowercase)
                SafeSetProperty(propsType, props, "ExplDelay", config.FuzeTimer);
                SafeSetProperty(propsType, props, "explDelay", config.FuzeTimer);
                SafeSetProperty(propsType, props, "FragmentsCount", config.Fragmentations);
                SafeSetProperty(propsType, props, "MinExplosionDistance", config.ExplosionMinimum);
                SafeSetProperty(propsType, props, "MaxExplosionDistance", config.ExplosionMaximum);
                SafeSetProperty(propsType, props, "HeavyBleedingDelta", config.HeavyBleedPercent);
                SafeSetProperty(propsType, props, "LightBleedingDelta", config.LightBleedPercent);
                SafeSetProperty(propsType, props, "Damage", config.Damage);
                SafeSetProperty(propsType, props, "PenetrationPower", config.Penetration);
                SafeSetProperty(propsType, props, "CanSellOnRagfair", true);
            }

            // Add to Prapor
            AddToTrader(config.Price);

            // Blacklist from bots
            if (config.BlacklistFromBots)
            {
                BlacklistFromBots(tables);
            }
        }
        catch
        {
        }
    }

    private void BlacklistFromBots(dynamic tables)
    {
        try
        {
            // Add to PMC config globalLootBlacklist
            dynamic configs = tables.GetType().GetProperty("Configs")?.GetValue(tables);
            if (configs != null)
            {
                // Try PMC config
                var pmcProperty = configs.GetType().GetProperty("Pmc") ?? configs.GetType().GetProperty("pmc");
                if (pmcProperty != null)
                {
                    dynamic pmcConfig = pmcProperty.GetValue(configs);
                    if (pmcConfig != null)
                    {
                        AddToBlacklist(pmcConfig, "GlobalLootBlacklist");
                        AddToBlacklist(pmcConfig, "globalLootBlacklist");

                        // Also add to vestLoot and backpackLoot blacklists
                        var vestLootProp = pmcConfig.GetType().GetProperty("VestLoot") ?? pmcConfig.GetType().GetProperty("vestLoot");
                        if (vestLootProp != null)
                        {
                            dynamic vestLoot = vestLootProp.GetValue(pmcConfig);
                            if (vestLoot != null) AddToBlacklist(vestLoot, "Blacklist", "blacklist");
                        }

                        var backpackLootProp = pmcConfig.GetType().GetProperty("BackpackLoot") ?? pmcConfig.GetType().GetProperty("backpackLoot");
                        if (backpackLootProp != null)
                        {
                            dynamic backpackLoot = backpackLootProp.GetValue(pmcConfig);
                            if (backpackLoot != null) AddToBlacklist(backpackLoot, "Blacklist", "blacklist");
                        }

                        var pocketLootProp = pmcConfig.GetType().GetProperty("PocketLoot") ?? pmcConfig.GetType().GetProperty("pocketLoot");
                        if (pocketLootProp != null)
                        {
                            dynamic pocketLoot = pocketLootProp.GetValue(pmcConfig);
                            if (pocketLoot != null) AddToBlacklist(pocketLoot, "Blacklist", "blacklist");
                        }
                    }
                }

                // Try Item config
                var itemProperty = configs.GetType().GetProperty("Item") ?? configs.GetType().GetProperty("item");
                if (itemProperty != null)
                {
                    dynamic itemConfig = itemProperty.GetValue(configs);
                    if (itemConfig != null)
                    {
                        AddToBlacklist(itemConfig, "Blacklist", "blacklist");
                    }
                }
            }
        }
        catch { }
    }

    private void AddToBlacklist(dynamic obj, params string[] propertyNames)
    {
        try
        {
            foreach (var propName in propertyNames)
            {
                var prop = obj.GetType().GetProperty(propName);
                if (prop == null) continue;

                dynamic? blacklist = prop.GetValue(obj);
                if (blacklist == null) continue;

                // Check if already in list
                bool found = false;
                foreach (var item in blacklist)
                {
                    if (item?.ToString() == ITEM_ID)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    blacklist.Add(ITEM_ID);
                }
                return;
            }
        }
        catch { }
    }

    private void AddToTrader(int price)
    {
        try
        {
            var traderId = new MongoId(PRAPOR_ID);
            var trader = databaseService.GetTrader(traderId);
            if (trader?.Assort == null) return;

            string uniqueItemId = GenerateShortId("PineappleBlitz_Prapor");

            // Check if already exists
            if (trader.Assort.Items.Any(item => item.Id.ToString() == uniqueItemId))
                return;

            // Add item
            trader.Assort.Items.Add(new Item
            {
                Id = new MongoId(uniqueItemId),
                Template = new MongoId(ITEM_ID),
                ParentId = "hideout",
                SlotId = "hideout",
                Upd = new Upd
                {
                    UnlimitedCount = true,
                    StackObjectsCount = 999999
                }
            });

            // Add barter scheme
            dynamic barterScheme = trader.Assort.BarterScheme;
            foreach (var existingScheme in barterScheme)
            {
                try
                {
                    var schemeList = existingScheme.Value;
                    if (schemeList != null && schemeList.Count > 0 && schemeList[0].Count > 0)
                    {
                        var templateReq = schemeList[0][0];
                        var reqType = templateReq.GetType();

                        var reqJson = JsonSerializer.Serialize(templateReq);
                        dynamic barterReq = JsonSerializer.Deserialize(reqJson, reqType)!;

                        var tplProp = reqType.GetProperty("Tpl") ?? reqType.GetProperty("Template");
                        var countProp = reqType.GetProperty("Count");

                        if (tplProp != null)
                            tplProp.SetValue(barterReq, new MongoId(ROUBLES_ID));
                        if (countProp != null)
                            countProp.SetValue(barterReq, (double)price);

                        var innerListType = typeof(List<>).MakeGenericType(reqType);
                        dynamic innerList = Activator.CreateInstance(innerListType)!;
                        innerList.Add(barterReq);

                        var outerListType = typeof(List<>).MakeGenericType(innerListType);
                        dynamic outerList = Activator.CreateInstance(outerListType)!;
                        outerList.Add(innerList);

                        barterScheme[uniqueItemId] = outerList;
                        break;
                    }
                }
                catch { continue; }
            }

            trader.Assort.LoyalLevelItems.Add(uniqueItemId, 1);
        }
        catch
        {
        }
    }

    private void SafeSetProperty(Type propsType, dynamic props, string propertyName, object value)
    {
        try
        {
            var property = propsType.GetProperty(propertyName);
            if (property == null) return;

            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            object convertedValue;
            if (targetType == typeof(int))
                convertedValue = Convert.ToInt32(value);
            else if (targetType == typeof(float))
                convertedValue = Convert.ToSingle(value);
            else if (targetType == typeof(double))
                convertedValue = Convert.ToDouble(value);
            else if (targetType == typeof(bool))
                convertedValue = Convert.ToBoolean(value);
            else
                convertedValue = value;

            property.SetValue(props, convertedValue);
        }
        catch { }
    }

    private static string GenerateShortId(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hash).Replace("-", "").Substring(0, 24).ToLower();
    }

    private ModConfig LoadConfig(string configPath)
    {
        try
        {
            // Try multiple possible paths
            string[] possiblePaths = new[]
            {
                configPath,
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SPT", "user", "mods", "PineappleBlitz-LumurkFox", "config", "config.json"),
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user", "mods", "PineappleBlitz-LumurkFox", "config", "config.json"),
                System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", "config", "config.json")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var config = JsonSerializer.Deserialize<ModConfig>(File.ReadAllText(path), options);
                    if (config != null) return config;
                }
            }
        }
        catch { }
        return new ModConfig();
    }
}
