using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;
using StardewValley.Objects;

namespace OvercrowdedPremiumPatch
{
    public interface IContentPatcherAPI
    {
        bool IsConditionsApiReady { get; }
        void RegisterToken(IManifest mod, string name, Func<IEnumerable<string>> getValue);
    }
    public class ModEntry : Mod
    {
        public static ModEntry? modInstance;
        public static IContentPack? cpPack;
        private const string PremiumVPP = "bobkalonger.PremiumPatchVPP_";
        private const string PremiumBarn = $"{PremiumVPP}PremiumBarn";
        private const string PremiumCoop = $"{PremiumVPP}PremiumCoop";
        private const string VppItemKey = "Premium/vppItems";
        private const string OvercrowdingKey = "bobkalonger.PremiumPatch_code/OvercrowdingActive";
        private bool _overcrowdingActive = false;
        public override void Entry(IModHelper helper)
        {
            modInstance = this;

            // helper.Events.GameLoop.ReturnedToTitle += (s, e) =>
            // {
            //     _cachedBarnFloorConfig = null;
            //     _cachedCoopFloorConfig = null;
            // };

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Player.Warped += PlayerOnWarped;

            var harmony = new Harmony(this.ModManifest.UniqueID);

            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private bool IsVppOvercrowdingActive()
        {
            if (!Context.IsWorldReady) return false;
            if (!Helper.ModRegistry.IsLoaded("KediDili.VanillaPlusProfessions")) return false;
            if (!Helper.ModRegistry.IsLoaded("Esca.EMP")) return false;
            return GameStateQuery.CheckConditions(
                "KediDili.VanillaPlusProfessions_PlayerHasTalent Any Overcrowding",
                Game1.getFarm(),
                Game1.player
            );
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            var cp = Helper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");

            cp?.RegisterToken(ModManifest, "OvercrowdingActive", () => new[] { _overcrowdingActive ? "true" : "false" });
        }

        private void PlayerOnWarped(object? sender, WarpedEventArgs e)
        {
            if (e.NewLocation is AnimalHouse)
            {
                Utility.ForEachBuilding(building =>
                {
                    if (building.GetIndoors() == e.NewLocation &&
                        building.buildingType.Value is PremiumBarn or PremiumCoop)
                    {
                        bool vppActive = _overcrowdingActive;
                        string targetVppState = vppActive ? "BPP" : "Base";
                    }
                    return true;
                });
            }
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            if (!Game1.player.modData.ContainsKey(OvercrowdingKey))
                Game1.player.modData[OvercrowdingKey] = IsVppOvercrowdingActive() ? "true" : "false";
            _overcrowdingActive = Game1.player.modData[OvercrowdingKey] == "true";

            Helper.GameContent.InvalidateCache("Data/Buildings");
        }

        // private string? _cachedBarnFloorConfig = null;
        // private string GetBarnFloorConfig()
        // {
        //     if (_cachedBarnFloorConfig != null) return _cachedBarnFloorConfig;
        //     var config = cpPack?.ReadJsonFile<Dictionary<string, string>>("config.json");
        //     if (config != null && config.TryGetValue("Barn Floor", out string? value))
        //         _cachedBarnFloorConfig = value;
        //     return _cachedBarnFloorConfig ?? "Clean";
        // }

        // private string? _cachedCoopFloorConfig = null;
        // private string GetCoopFloorConfig()
        // {
        //     if (_cachedCoopFloorConfig != null) return _cachedCoopFloorConfig;
        //     var config = cpPack?.ReadJsonFile<Dictionary<string, string>>("config.json");
        //     if (config != null && config.TryGetValue("Coop Floor", out string? value))
        //         _cachedCoopFloorConfig = value;
        //     return _cachedCoopFloorConfig ?? "Clean";
        // }

        // private static void BarnItemMoves(GameLocation interior)
        // {
        //     if (interior.map == null) return;

        //     var namedDestinations = new Dictionary<string, Vector2>
        //     {
        //         { "(BC)99",  new Vector2(26, 19) },
        //         { "(BC)104", new Vector2(41, 19) },
        //         { "(BC)165", new Vector2(36, 19) },
        //         { "(BC)272", new Vector2(21, 19) }
        //     };

        //     var spawnIfMissing = new HashSet<string> { "(BC)104", "(BC)165", "(BC)272" };

        //     var haySlots = new List<Rectangle>
        //     {
        //         new Rectangle(4,  29, 12, 1),
        //         new Rectangle(4,  39, 12, 1),
        //         new Rectangle(47, 29, 12, 1),
        //         new Rectangle(47, 39, 12, 1)
        //     };

        //     Vector2 startCenter = new Vector2(
        //         interior.map.Layers[0].LayerWidth / 2,
        //         interior.map.Layers[0].LayerHeight / 2
        //     );

        //     var foundHay = SpiralSearch(interior, "(O)178", startCenter, maxRadius: 50);

        //     foreach (var (sourceTile, obj) in foundHay)
        //         interior.removeObject(sourceTile, false);

        //     int haySlotIndex = 0;
        //     int haySlotX = haySlots[0].Left;

        //     foreach (var (sourceTile, obj) in foundHay)
        //     {
        //         if (haySlotIndex >= haySlots.Count) break;
        //         Vector2 dest = new Vector2(haySlotX, haySlots[haySlotIndex].Top);
        //         obj.TileLocation = dest;
        //         interior.objects[dest] = obj;

        //         haySlotX++;
        //         if (haySlotX >= haySlots[haySlotIndex].Right)
        //         {
        //             haySlotIndex++;
        //             if (haySlotIndex < haySlots.Count)
        //                 haySlotX = haySlots[haySlotIndex].Left;
        //         }
        //     }

        //     var excludedIds = new HashSet<string>(namedDestinations.Keys) { "(O)178" };

        //     foreach (var kvp in namedDestinations)
        //     {
        //         var found = SpiralSearch(interior, kvp.Key, startCenter, maxRadius: 50);

        //         if (found.Count == 0)
        //         {
        //             if (spawnIfMissing.Contains(kvp.Key))
        //             {
        //                 if (interior.objects.TryGetValue(kvp.Value, out StardewValley.Object blocking))
        //                 {
        //                     interior.objects.Remove(kvp.Value);
        //                     Game1.player.team.returnedDonations.Add(blocking);
        //                     Game1.player.team.newLostAndFoundItems.Value = true;
        //                 }

        //                 var newObj = ItemRegistry.Create(kvp.Key) as StardewValley.Object;
        //                 if (newObj != null)
        //                 {
        //                     newObj.TileLocation = kvp.Value;
        //                     interior.objects[kvp.Value] = newObj;
        //                 }
        //             }
        //             continue;
        //         }

        //         var (sourceTile, obj) = found[0];
        //         interior.removeObject(sourceTile, false);
        //         obj.TileLocation = kvp.Value;
        //         interior.objects[kvp.Value] = obj;
        //     }

        //     var landingPad = new Rectangle(x: 21, y: 21, width: 21, height: 24);

        //     var barnItemMoves = interior.objects.Pairs
        //         .Where(p => !excludedIds.Contains(p.Value.QualifiedItemId))
        //         .ToList();

        //     foreach (var pair in barnItemMoves)
        //     {
        //         Vector2 dest = LandingPadRect(interior, landingPad);
        //         if (dest == Vector2.Zero) continue;
        //         interior.removeObject(pair.Key, false);
        //         pair.Value.TileLocation = dest;
        //         interior.objects[dest] = pair.Value;
        //     }

        //     Vector2 correctFeederTile = namedDestinations["(BC)99"];
        //     var extraFeeders = SpiralSearch(interior, "(BC)99", startCenter, maxRadius: 50)
        //         .Where(f => f.tile != correctFeederTile)
        //         .ToList();
        //     foreach (var (tile, _) in extraFeeders)
        //         interior.removeObject(tile, false);
        // }

        // private static void CoopItemMoves(GameLocation interior)
        // {
        //     if (interior.map == null) return;

        //     var namedDestinations = new Dictionary<string, Vector2>
        //     {
        //         { "(BC)99",  new Vector2(38, 36) },
        //         { "(BC)104", new Vector2(28, 6)  },
        //         { "(BC)165", new Vector2(39, 36) },
        //         { "(BC)272", new Vector2(29, 6)  }
        //     };

        //     var spawnIfMissing = new HashSet<string> { "(BC)104", "(BC)165", "(BC)272" };

        //     var haySlots = new List<Rectangle>
        //     {
        //         new Rectangle(4,  14, 12, 1),
        //         new Rectangle(4,  22, 12, 1),
        //         new Rectangle(4,  30, 12, 1),
        //         new Rectangle(4,  38, 12, 1)
        //     };

        //     Vector2 startCenter = new Vector2(
        //         interior.map.Layers[0].LayerWidth / 2,
        //         interior.map.Layers[0].LayerHeight / 2
        //     );

        //     var foundHay = SpiralSearch(interior, "(O)178", startCenter, maxRadius: 50);

        //     foreach (var (sourceTile, obj) in foundHay)
        //         interior.removeObject(sourceTile, false);

        //     int haySlotIndex = 0;
        //     int haySlotX = haySlots[0].Left;

        //     foreach (var (sourceTile, obj) in foundHay)
        //     {
        //         if (haySlotIndex >= haySlots.Count) break;

        //         Vector2 dest = new Vector2(haySlotX, haySlots[haySlotIndex].Top);
        //         obj.TileLocation = dest;
        //         interior.objects[dest] = obj;

        //         haySlotX++;
        //         if (haySlotX >= haySlots[haySlotIndex].Right)
        //         {
        //             haySlotIndex++;
        //             if (haySlotIndex < haySlots.Count)
        //                 haySlotX = haySlots[haySlotIndex].Left;
        //         }
        //     }

        //     Vector2[] incubatorDestinations =
        //     {
        //         new Vector2(2, 14),
        //         new Vector2(2, 22),
        //         new Vector2(2, 30),
        //         new Vector2(2, 38)
        //     };

        //     var foundIncubators = SpiralSearch(interior, "(BC)101", startCenter, maxRadius: 50);

        //     for (int i = 0; i < foundIncubators.Count && i < incubatorDestinations.Length; i++)
        //     {
        //         var (sourceTile, obj) = foundIncubators[i];
        //         Vector2 dest = incubatorDestinations[i];
        //         interior.removeObject(sourceTile, false);
        //         obj.TileLocation = dest;
        //         interior.objects[dest] = obj;
        //     }

        //     for (int i = foundIncubators.Count; i < incubatorDestinations.Length; i++)
        //     {
        //         Vector2 dest = incubatorDestinations[i];

        //         if (interior.objects.TryGetValue(dest, out StardewValley.Object blocking))
        //         {
        //             interior.objects.Remove(dest);
        //             Game1.player.team.returnedDonations.Add(blocking);
        //             Game1.player.team.newLostAndFoundItems.Value = true;

        //         }

        //         var newIncubator = ItemRegistry.Create("(BC)101") as StardewValley.Object;
        //         if (newIncubator != null)
        //         {
        //             newIncubator.TileLocation = dest;
        //             interior.objects[dest] = newIncubator;
        //         }
        //     }

        //     var excludedIds = new HashSet<string>(namedDestinations.Keys) { "(O)178", "(BC)101" };

        //     foreach (var kvp in namedDestinations)
        //     {
        //         var found = SpiralSearch(interior, kvp.Key, startCenter, maxRadius: 50);

        //         if (found.Count == 0)
        //         {
        //             if (spawnIfMissing.Contains(kvp.Key))
        //             {
        //                 if (interior.objects.TryGetValue(kvp.Value, out StardewValley.Object blocking))
        //                 {
        //                     interior.objects.Remove(kvp.Value);
        //                     Game1.player.team.returnedDonations.Add(blocking);
        //                     Game1.player.team.newLostAndFoundItems.Value = true;
        //                 }

        //                 var newObj = ItemRegistry.Create(kvp.Key) as StardewValley.Object;
        //                 if (newObj != null)
        //                 {
        //                     newObj.TileLocation = kvp.Value;
        //                     interior.objects[kvp.Value] = newObj;
        //                 }
        //             }
        //             continue;
        //         }

        //         var (sourceTile, obj) = found[0];
        //         interior.removeObject(sourceTile, false);
        //         obj.TileLocation = kvp.Value;
        //         interior.objects[kvp.Value] = obj;
        //     }

        //     var landingPad = new Rectangle(x: 20, y: 7, width: 16, height: 36);

        //     var coopItemMoves = interior.objects.Pairs
        //         .Where(p => !excludedIds.Contains(p.Value.QualifiedItemId))
        //         .ToList();

        //     foreach (var pair in coopItemMoves)
        //     {
        //         Vector2 dest = LandingPadRect(interior, landingPad);
        //         if (dest == Vector2.Zero) continue;
        //         interior.removeObject(pair.Key, false);
        //         pair.Value.TileLocation = dest;
        //         interior.objects[dest] = pair.Value;
        //     }

        //     Vector2 correctFeederTile = namedDestinations["(BC)99"];
        //     var extraFeeders = SpiralSearch(interior, "(BC)99", startCenter, maxRadius: 50)
        //         .Where(f => f.tile != correctFeederTile)
        //         .ToList();
        //     foreach (var (tile, _) in extraFeeders)
        //         interior.removeObject(tile, false);
        // }

        // private static void ReturnHayToSilo(GameLocation interior, Rectangle zone)
        // {
        //     var farm = Game1.getFarm();
        //     var hayInZone = interior.objects.Pairs
        //         .Where(p => zone.Contains((int)p.Key.X, (int)p.Key.Y) && p.Value.QualifiedItemId == "(O)178")
        //         .ToList();

        //     foreach (var (tile, obj) in hayInZone)
        //     {
        //         interior.removeObject(tile, false);
        //         int leftover = farm.tryToAddHay(obj.Stack);
        //         if (leftover > 0)
        //         {
        //             var leftoverHay = ItemRegistry.Create("(O)178", leftover) as StardewValley.Object;
        //             if (leftoverHay != null)
        //             {
        //                 Game1.player.team.returnedDonations.Add(leftoverHay);
        //                 Game1.player.team.newLostAndFoundItems.Value = true;
        //             }
        //         }
        //     }
        // }
    }
}
