using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;
using UnityEngine;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Help
{
    [BepInPlugin("com.thetimesweeper.help", "help", "1.0.0")]
    public class HelpPluginPlugin : BaseUnityPlugin {

        public static InventoryItem.ITEM_TYPE[] cachedAllMeals;

        public static Dictionary<InventoryItem.ITEM_TYPE, List<List<InventoryItem>>> cachedAllRecipes = new Dictionary<InventoryItem.ITEM_TYPE, List<List<InventoryItem>>>();

        void Awake()
        {
            //Log.Init(Logger);        

            bool recipeCaching = Config.Bind(
                "Hello",
                "RecipeCaching",
                true,
                "caches GetAllMeals and GetRecipe. These optimizations take advantage of the fact that these collections are hard coded, so if the community ever adds new meals or recipes, disable this").Value;

            if (recipeCaching)
            {
                cachedAllMeals = CookingData.GetAllMeals();

                //store the lists of recipeses once and reference this.
                //calling new List<> multiple times per frame can create an unfathomable amount of garbage to collect
                for (int i = 0; i < cachedAllMeals.Length; i++)
                {
                    InventoryItem.ITEM_TYPE itemType = cachedAllMeals[i];
                    cachedAllRecipes[itemType] = CookingData.GetRecipe(itemType);
                }
                IL.CookingData.GetAllMeals += CookingData_GetAllMeals;
                IL.CookingData.GetRecipe += CookingData_GetRecipe;
            }

            On.CookingData.GetCookableRecipeAmount += CookingData_GetCookableRecipeAmount1;
        }

        private static void CookingData_GetAllMeals(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.Index = 0;
            //Log.Warning(cursor);
            cursor.EmitDelegate<Func<InventoryItem.ITEM_TYPE[]>>(() => {
                return cachedAllMeals;
            });
            cursor.Emit(OpCodes.Ret);
        }

        private static void CookingData_GetRecipe(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.Index = 1;
            //Log.Warning(cursor);
            cursor.EmitDelegate<Func<InventoryItem.ITEM_TYPE, List<List<InventoryItem>>>>((meal) => {
                return cachedAllRecipes[meal];
            });
            cursor.Emit(OpCodes.Ret);
        }

        private static int CookingData_GetCookableRecipeAmount1(On.CookingData.orig_GetCookableRecipeAmount orig, InventoryItem.ITEM_TYPE mealType, List<InventoryItem> ingredients)
        {
            //long story short, regular fors instead of foreaches whenever I can, avoiding new dictionaries and new lists as they create garbage, and avoiding linq on something that runs many times in one frame
            List<List<InventoryItem>> recipes = CookingData.GetRecipe(mealType);
            int cookableRecipeAmount = 0;

            for (int r = 0; r < recipes.Count; r++)
            {
                //just gonna create a fake dictionary here where instead of ints for keys, the "keys" are just the index of the array
                int highest = 0;
                for (int i = 0; i < ingredients.Count; i++)
                {
                    if (ingredients[i].type > highest)
                    {
                        highest = ingredients[i].type;
                    }
                }
                int[] notDictionary = new int[highest + 1];
                for (int i = 0; i < notDictionary.Length; i++)
                {
                    notDictionary[i] = -1;
                }
                for (int i = 0; i < ingredients.Count; i++)
                {
                    InventoryItem ingredient = ingredients[i];

                    notDictionary[ingredient.type] += ingredient.quantity;
                }

                List<InventoryItem> recipe = recipes[r];

                while (recipes.Count != 0)
                {
                    for (int r2 = 0; r2 < recipe.Count; r2++)
                    {
                        InventoryItem recipeIngredient = recipe[r2];

                        bool flag = false;

                        for (int i = 0; i < notDictionary.Length; i++)
                        {
                            if (recipeIngredient.type == i)
                            {
                                flag = true;
                                if (notDictionary[i] < recipeIngredient.quantity)
                                {
                                    return cookableRecipeAmount;
                                }
                                notDictionary[i] -= recipeIngredient.quantity;
                            }
                        }

                        if (!flag)
                        {
                            return cookableRecipeAmount;
                        }
                    }

                    cookableRecipeAmount++;
                }
            }

            return 0;
        }
    }
}
