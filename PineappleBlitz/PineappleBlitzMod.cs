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

            // Check if already exists
            if (items.ContainsKey(ITEM_ID))
                return;

            // Create item using CustomItemService
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

            // Get the created item and modify properties
            dynamic customGrenade = items[ITEM_ID];
            var itemType = customGrenade.GetType();
            var propsProperty = itemType.GetProperty("Properties") ?? itemType.GetProperty("Props");

            if (propsProperty != null)
            {
                dynamic props = propsProperty.GetValue(customGrenade);
                var propsType = props.GetType();

                SafeSetProperty(propsType, props, "ExplDelay", config.FuzeTimer);
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

        }
        catch
        {
        }
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
            if (File.Exists(configPath))
            {
                var config = JsonSerializer.Deserialize<ModConfig>(File.ReadAllText(configPath));
                if (config != null) return config;
            }
        }
        catch { }
        return new ModConfig();
    }
}
