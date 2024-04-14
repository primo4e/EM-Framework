﻿using Eco.Core.Utils;
using Eco.Gameplay.Items;
using Eco.Gameplay.Objects;
using Eco.Shared.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Eco.EM.Framework.Resolvers
{
    public class EMStackSizeResolver
    {
        internal static int Overriden { get; set; }
        public static void Initialize()
        {
            IEnumerable<Item> locals;
            locals = Item.AllItemsExceptHidden.Where(x => x.Category != "Hidden" && ItemAttribute.Has<MaxStackSizeAttribute>(x.Type) && !ItemAttribute.Has<IgnoreStackSizeAttribute>(x.Type) && x.DisplayName != "Hands");
            locals = locals.OrderBy(x => x.DisplayName);

            MaxStackSizeAttribute.Default = EMStackSizesPlugin.Config.ForceSameStackSizes ? EMStackSizesPlugin.Config.ForcedSameStackAmount : EMStackSizesPlugin.Config.DefaultMaxStackSize;

            BuildStackSizeList(locals);
            OverrideStackSizes(locals);


        }

        // Goes through and loads new items for stack sizes into the dictionary.
        private static void BuildStackSizeList(IEnumerable<Item> locals)
        {
            var config = EMStackSizesPlugin.Config.EMStackSizes;

            // Go through and keep items that are still referenced in the namespace
            SerializedSynchronizedCollection<StackSizeModel> cleanList = new();
            for (int i = 0; i < config.Count; i++)
            {
                if (locals.Any(x => x.DisplayName == config[i].DisplayName))
                {
                    if (!cleanList.Any(x => x.DisplayName == config[i].DisplayName))
                        cleanList.Add(config[i]);
                }
            }

            // Now add anything that is new
            foreach (var i in locals)
            {
                if (!cleanList.Any(x => x.DisplayName == i.DisplayName))
                    cleanList.Add(new StackSizeModel(i.GetType(), i.DisplayName, i.MaxStackSize, false));
            }

            EMStackSizesPlugin.Config.EMStackSizes = cleanList;
        }

        // Overrides the preset stacksizes to those set in the config on load before adding newly created items
        private static void OverrideStackSizes(IEnumerable<Item> locals)
        {
            foreach (var i in locals)
            {
                // Check for the items in the stack size list
                var element = EMStackSizesPlugin.Config.EMStackSizes.FirstOrDefault(x => x.DisplayName == i.DisplayName);
                if (element == null) continue;
                var orThis = element.OverrideThis;
                var forced = EMStackSizesPlugin.Config.ForceSameStackSizes;
                var bforced = EMStackSizesPlugin.Config.CarriedItemsOverride;
                // Get the stacksize attribute and override it.
                var mss = ItemAttribute.Get<MaxStackSizeAttribute>(i.Type);

                // If using Carried Items Override get the attribute
                var bmss = ItemAttribute.Get<CarriedAttribute>(i.Type);
                Overriden++;
                // currently we only change items that have a MaxStackSizeAttribute
                switch (forced)
                {
                    case false:
                        if (mss != null && orThis)
                            mss.GetType().GetProperty("MaxStackSize", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SetValue(mss, element.StackSize);
                        else if (mss != null && bmss != null && bforced)
                            mss.GetType().GetProperty("MaxStackSize", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SetValue(mss, EMStackSizesPlugin.Config.CarriedItemsAmount);
                        break;

                    case true:
                        if (mss != null)
                            mss?.GetType().GetProperty("MaxStackSize", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SetValue(mss, EMStackSizesPlugin.Config.ForcedSameStackAmount);
                        break;
                }
            }
        }
    }
}