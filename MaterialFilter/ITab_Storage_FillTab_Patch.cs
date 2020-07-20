using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;

namespace MaterialFilter
{
  [HarmonyPatch(typeof(ITab_Storage), "FillTab")]
  public static class ITab_Storage_FillTab_Patch
  {
    [HarmonyPostfix]
    public static void drawFilterButton(ITab_Storage __instance, Vector2 ___WinSize)
    {
      // TabRect.y is top of window
      Rect TabRect = Traverse.Create(__instance).Property("TabRect").GetValue<Rect>();
      ThingFilter filter = Traverse.Create(__instance).Property("SelStoreSettingsParent").GetValue<IStoreSettingsParent>().GetStoreSettings().filter;
      var buttonSize = new Vector2(80f, 29f);
      if (Widgets.ButtonText(new Rect(180, 10, buttonSize.x, buttonSize.y), "Filter".Translate() + ">>"))
      {
        MaterialFilterWindow w = Find.WindowStack.WindowOfType<MaterialFilterWindow>();
        if (w != null)
        {
          w.Close();
        }
        else
        {
          Find.WindowStack.Add(new MaterialFilterWindow(filter, TabRect.y, ___WinSize.x, WindowLayer.GameUI));
        }
      }  
    }
  }
}