using BepInEx;
using BepInEx.Logging;
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
    public class FunnyCookingData {

        public static InventoryItem.ITEM_TYPE[] cachedAllMeals;

        public static Dictionary<InventoryItem.ITEM_TYPE, List<List<InventoryItem>>> cachedAllRecipes = new Dictionary<InventoryItem.ITEM_TYPE, List<List<InventoryItem>>>();

        public static void Init()
        {
            //for (int i = 0; i < cachedAllMeals.Length; i++)
            //{
            //    InventoryItem.ITEM_TYPE itemType = cachedAllMeals[i];
            //    cachedAllRecipes[itemType] = CookingData.GetRecipe(itemType);
            //}
            //cachedAllMeals = CookingData.GetAllMeals();
            //IL.CookingData.GetAllMeals += CookingData_GetAllMeals;
            //IL.CookingData.GetRecipe += CookingData_GetRecipe;

            On.CookingData.GetCookableRecipeAmount += CookingData_GetCookableRecipeAmount1;
        }

        private static void CookingData_GetRecipe(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.Index = 1;
            Log.Warning(cursor);
            cursor.EmitDelegate<Func<InventoryItem.ITEM_TYPE, List<List<InventoryItem>>>>((meal) => {
                return cachedAllRecipes[meal];
            });
            cursor.Emit(OpCodes.Ret);
        }

        private static void CookingData_GetAllMeals(ILContext il) {

            ILCursor cursor = new ILCursor(il);
            cursor.Index = 0;
            Log.Warning(cursor);
            cursor.EmitDelegate<Func<InventoryItem.ITEM_TYPE[]>>(() => {
                return cachedAllMeals;
            });
            cursor.Emit(OpCodes.Ret);
        }

        private static int CookingData_GetCookableRecipeAmount1(On.CookingData.orig_GetCookableRecipeAmount orig, InventoryItem.ITEM_TYPE mealType, List<InventoryItem> ingredients) {

            List<List<InventoryItem>> recipes = CookingData.GetRecipe(mealType);
            int cookableRecipeAmount = 0;

            for (int r = 0; r < recipes.Count; r++)
            {
                List<InventoryItem> recipeList = recipes[r];

                int highest = 0;
                for (int t = 0; t < ingredients.Count; t++)
                {
                    if(ingredients[t].type > highest)
                    {
                        highest = ingredients[t].type;
                    }
                }

                int[] notDictionary = new int[highest + 1];
                for (int d = 0; d < notDictionary.Length; d++)
                {
                    notDictionary[d] = -1;
                }
                for (int t = 0; t < ingredients.Count; t++)
                {
                    InventoryItem ingredient = ingredients[t];

                    notDictionary[ingredient.type] += ingredient.quantity;
                }
                while (recipes.Count != 0)
                {
                    for (int g = 0; g < recipeList.Count; g++)
                    {
                        InventoryItem recipeIngredient = recipeList[g];

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
    [BepInPlugin("com.thetimesweeper.help", "help", "0.0.0")]
    public class HelpPluginPlugin : BaseUnityPlugin {

        public const string MODUID = "com.thetimesweeper.ra2mod";
        public const string MODNAME = "RA2Mod";
        public const string MODVERSION = "0.1.0";

        bool doing;
        bool recipdescoveredonce;
        bool configuredOnce;
        bool timeyWimey;

        bool subslime;
        bool all;

        bool skipping;


        void Update() {

            if (Input.GetKeyDown(KeyCode.G)) {
                Sublslimel(true);
            }

            if (Input.GetKeyDown(KeyCode.H)) {
                all = true;
                Log.Warning("all " + all);
                Sublslimel(true);
            }

            if (Input.GetKeyDown(KeyCode.LeftAlt)) {
                timeyWimey = !timeyWimey;
                Time.timeScale = timeyWimey ? 4 : 1;
            }
        }

        void Awake () {

            Log.Init(Logger);

            FunnyCookingData.Init();
            //TotalTimeHooks();
            Sublslimel(true);

            On.CookingData.CanMakeMeal += CookingData_CanMakeMeal;
            On.CookingData.GetCookableRecipeAmount += CookingData_GetCookableRecipeAmount;

            On.Lamb.UI.KitchenMenu.UIFollowerKitchenMenuController.OnRecipeChosen += UIFollowerKitchenMenuController_OnRecipeChosen;

            On.Lamb.UI.KitchenMenu.UIFollowerKitchenMenuController.UpdateQuantities += UIFollowerKitchenMenuController_UpdateQuantities1;
            On.Lamb.UI.KitchenMenu.UIFollowerKitchenMenuController.UpdateQueueText += UIFollowerKitchenMenuController_UpdateQueueText1;
            On.Lamb.UI.RecipeItem.OnButtonClicked += RecipeItem_OnButtonClicked;
            
        }


        private void RecipeItem_OnButtonClicked(On.Lamb.UI.RecipeItem.orig_OnButtonClicked orig, Lamb.UI.RecipeItem self) {
            orig(self);
            Log.Warning("UH");
        }

        private void UIFollowerKitchenMenuController_UpdateQueueText1(On.Lamb.UI.KitchenMenu.UIFollowerKitchenMenuController.orig_UpdateQueueText orig, Lamb.UI.KitchenMenu.UIFollowerKitchenMenuController self) {
            if (!skipping)
                orig(self);
        }

        private void UIFollowerKitchenMenuController_UpdateQuantities1(On.Lamb.UI.KitchenMenu.UIFollowerKitchenMenuController.orig_UpdateQuantities orig, Lamb.UI.KitchenMenu.UIFollowerKitchenMenuController self) {
            if (!skipping)
                orig(self);
        }

        private void UIFollowerKitchenMenuController_OnRecipeChosen(On.Lamb.UI.KitchenMenu.UIFollowerKitchenMenuController.orig_OnRecipeChosen orig, Lamb.UI.KitchenMenu.UIFollowerKitchenMenuController self, Lamb.UI.RecipeItem item) {
            skipping = true;
            Log.Warning("uh");
            for (int i = 0; i < 5; i++) {
                Log.Warning("love u poogie");
                orig(self, item);
            }
            skipping = false;

            orig(self, item);
        }

        private int CookingData_GetCookableRecipeAmount(On.CookingData.orig_GetCookableRecipeAmount orig, InventoryItem.ITEM_TYPE mealType, System.Collections.Generic.List<InventoryItem> ingredients) {
            if (doing) Log.CurrentTime("before getcook");
            var rig = orig(mealType, ingredients);
            if (doing) Log.CurrentTime("after getcook");

            return rig;
        }

        private void Sublslimel(bool slime) {
            if (subslime == slime)
                return;
            subslime = slime;
            if (subslime) {
                On.Lamb.UI.RecipeItem.UpdateQuantity += RecipeItem_UpdateQuantity;
                Log.Warning("subslimed");
            }
            else 
            {
                On.Lamb.UI.RecipeItem.UpdateQuantity -= RecipeItem_UpdateQuantity;
                Log.Warning("unsubslimed");
            }
        }

        private bool CookingData_CanMakeMeal(On.CookingData.orig_CanMakeMeal orig, InventoryItem.ITEM_TYPE mealType) {
            if(doing) Log.CurrentTime("before canmakemeal");
            bool rig = orig(mealType);
            if (doing) Log.CurrentTime("after canmakemeal");

            return rig;
        }

        private void RecipeItem_UpdateQuantity(On.Lamb.UI.RecipeItem.orig_UpdateQuantity orig, Lamb.UI.RecipeItem self) {
            Log.StartTime();
            Log.CurrentTime("hello");
            doing = true;
            orig(self);
            Log.CurrentTime("goodbye");
            Log.AllTimes();
            doing = false;
            if (!all) {
                Sublslimel(false);
            }
        }

        #region totaltime

        private void TotalTimeHooks() {
            On.Lamb.UI.KitchenMenu.UIFollowerKitchenMenuController.OnShowStarted += UIFollowerKitchenMenuController_OnShowStarted;
            On.CookingData.GetAllMeals += CookingData_GetAllMeals;
            IL.Lamb.UI.KitchenMenu.UIFollowerKitchenMenuController.OnShowStarted += UICookingFireMenuController_OnShowStarted;
            On.CookingData.HasRecipeDiscovered += CookingData_HasRecipeDiscovered;
            On.Lamb.UI.RecipeItem.Configure += RecipeItem_Configure;
            On.Lamb.UI.UIMenuBase.OverrideDefault += UIMenuBase_OverrideDefault;
            On.Lamb.UI.KitchenMenu.UIFollowerKitchenMenuController.UpdateQueueText += UIFollowerKitchenMenuController_UpdateQueueText;
            On.Lamb.UI.KitchenMenu.UIFollowerKitchenMenuController.UpdateQuantities += UIFollowerKitchenMenuController_UpdateQuantities;
        }

        private void UIFollowerKitchenMenuController_UpdateQuantities(On.Lamb.UI.KitchenMenu.UIFollowerKitchenMenuController.orig_UpdateQuantities orig, Lamb.UI.KitchenMenu.UIFollowerKitchenMenuController self) {
            if (doing) {
                Log.CurrentTime("before quanti");
            } else {
                Log.Warning("notim");
            }
            orig(self);
            if (doing) {
                Log.CurrentTime("after quanti");
            } else {
                Log.Warning("notim");
            }
        }

        private void UIFollowerKitchenMenuController_UpdateQueueText(On.Lamb.UI.KitchenMenu.UIFollowerKitchenMenuController.orig_UpdateQueueText orig, Lamb.UI.KitchenMenu.UIFollowerKitchenMenuController self) {
            if (doing) {
                Log.CurrentTime("le updayde");
            }
            orig(self);
        }

        private void UIMenuBase_OverrideDefault(On.Lamb.UI.UIMenuBase.orig_OverrideDefault orig, Lamb.UI.UIMenuBase self, UnityEngine.UI.Selectable selectable) {
            if (doing) {
                Log.CurrentTime("le override");
            }
            orig(self, selectable);
        }

        private void RecipeItem_Configure(On.Lamb.UI.RecipeItem.orig_Configure orig, Lamb.UI.RecipeItem self, InventoryItem.ITEM_TYPE type, bool showQuantity, bool isQueued) {
            if (!configuredOnce) {
                configuredOnce = true;
                Log.CurrentTime("confi");
            }
            
            orig(self, type, showQuantity, isQueued);
        }

        private bool CookingData_HasRecipeDiscovered(On.CookingData.orig_HasRecipeDiscovered orig, InventoryItem.ITEM_TYPE meal) {
            if (!recipdescoveredonce) {
                recipdescoveredonce = true;
                Log.CurrentTime($"uh {meal}");
            }
            return orig(meal);
        }

        private void UICookingFireMenuController_OnShowStarted(MonoMod.Cil.ILContext il) {

            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After,
                instruction => instruction.MatchStloc(3),
                instruction => instruction.MatchLdloc(3),
                instruction => instruction.MatchLdloc(2),
                instruction => instruction.MatchLdlen(),
                instruction => instruction.MatchConvI4()
                );
            cursor.Index += 1;
            Log.Message(cursor);
            cursor.EmitDelegate<Action>(() => {
                Log.CurrentTime("beforeSortHopefully");
            });
            cursor.Index = 85;
            Log.Message($"{cursor.Index}| {cursor}");
            cursor.EmitDelegate<Action>(() => {
                Log.CurrentTime("afterSortHopefully");
            });
        }

        private void UIFollowerKitchenMenuController_OnShowStarted(On.Lamb.UI.KitchenMenu.UIFollowerKitchenMenuController.orig_OnShowStarted orig, Lamb.UI.KitchenMenu.UIFollowerKitchenMenuController self) {
            doing = true;
            Log.StartTime();
            Log.CurrentTime("before onshowstarted");
            orig(self);
            doing = false;
            Log.CurrentTime("after onshowstarted");
            recipdescoveredonce = false;
            configuredOnce = false;
            Log.AllTimes();
        }

        private InventoryItem.ITEM_TYPE[] CookingData_GetAllMeals(On.CookingData.orig_GetAllMeals orig) {
            if (doing) {
                Log.CurrentTime("before getallmeals");
            }
            var rig = orig();
            return rig;
        }
        #endregion totaltime
    }
}

internal static class Log {
    private static ManualLogSource _logSource;

    private static DateTime _startTime;

    private static string timesLog = "";
    private static string funnyLog = "";

    internal static void Init(ManualLogSource logSource) {
        _logSource = logSource;
    }
    internal static void StartTime() {
        _startTime = DateTime.Now;
        timesLog = "";
        funnyLog = "";
    }

    internal static void Debug(object data) => _logSource.LogDebug(data);
    internal static void Error(object data) => _logSource.LogError(data);

    internal static void ErrorAssetBundle(string assetName, string bundleName) =>
        Log.Error($"failed to load asset, {assetName}, because it does not exist in asset bundle, {bundleName}");
    internal static void Fatal(object data) => _logSource.LogFatal(data);
    internal static void Info(object data) => _logSource.LogInfo(data);
    internal static void Message(object data) => _logSource.LogMessage(data);
    internal static void Warning(object data) => _logSource.LogWarning(data);

    internal static void CurrentTime(string funny) {
        funnyLog += "\n" + funny;
        TimeSpan timeSpan = DateTime.Now - _startTime;
        string milliseconds = "\n" + timeSpan.TotalSeconds.ToString("0.0000");
        timesLog += milliseconds;
        _logSource.LogWarning($"{funny}{milliseconds}");
    }

    internal static void AllTimes() {
        if (!string.IsNullOrEmpty(timesLog)) {
            Log.Warning(timesLog);
            Log.Warning(funnyLog);
        }
    }
}
