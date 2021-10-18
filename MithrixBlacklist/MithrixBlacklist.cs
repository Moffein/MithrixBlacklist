using RoR2;
using R2API;
using BepInEx;
using R2API.Utils;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using System.Linq;

namespace MithrixBlacklist
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.Moffein.MithrixBlacklist", "Michael Blacklist", "1.0.0")]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class MithrixBlacklist : BaseUnityPlugin
    {
        public static Dictionary<ItemIndex, int> itemLimits;
        string blacklistString;
        public void Awake()
        {
            blacklistString = base.Config.Bind<string>(new ConfigDefinition("Settings", "Blacklist"), "", new ConfigDescription("List item codenames separated by commas (ex. Behemoth, ShockNearby, Clover). To specify an item cap instead, enter a - followed by the max cap (ex. Behemoth - 5, ShockNearby, Clover - 1). List of item codenames can be found at https://github.com/risk-of-thunder/R2Wiki/wiki/Item-&-Equipment-IDs-and-Names")).Value;
            itemLimits = new Dictionary<ItemIndex, int>();

            On.RoR2.ItemCatalog.Init += (orig) =>
            {
                orig();

                blacklistString = new string(blacklistString.ToCharArray().Where(c => !System.Char.IsWhiteSpace(c)).ToArray());
                string[] splitBlacklist = blacklistString.Split(',');
                foreach (string str in splitBlacklist)
                {
                    string[] current = str.Split('-');

                    if (current.Length == 2 && int.TryParse(current[1], out int cap) && cap > 0)
                    {
                        ItemIndex index = ItemCatalog.FindItemIndex(current[0]);
                        if (index != ItemIndex.None)
                        {
                            itemLimits.Add(index, cap);
                        }
                    }
                    else if (current.Length > 0)
                    {
                        AddToBlacklist(current[0]);
                    }
                }
            };

            On.RoR2.ItemStealController.FixedUpdate += (orig, self) =>
            {
                orig(self);
                if (self.lendeeInventory)
                {
                    foreach (KeyValuePair<ItemIndex, int> pair in itemLimits)
                    {
                        int count = self.lendeeInventory.GetItemCount(pair.Key);
                        if (count > pair.Value)
                        {
                            self.lendeeInventory.RemoveItem(pair.Key, count - pair.Value);
                        }
                    }
                }
            };
        }

        public void AddToBlacklist(string itemName)
        {
            ItemIndex i = ItemCatalog.FindItemIndex(itemName);
            if (i != ItemIndex.None)
            {
                AddToBlacklist(i);
            }
        }

        public static void AddToBlacklist(ItemIndex index)
        {
            //Debug.Log("Adding BrotherBlacklist tag to ItemIndex " + index);
            ItemDef itemDef = ItemCatalog.GetItemDef(index);
            if (itemDef.DoesNotContainTag(ItemTag.BrotherBlacklist))
            {
                System.Array.Resize(ref itemDef.tags, itemDef.tags.Length + 1);
                itemDef.tags[itemDef.tags.Length - 1] = ItemTag.BrotherBlacklist;
            }
        }
    }
}
