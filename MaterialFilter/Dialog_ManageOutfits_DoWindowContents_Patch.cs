using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;

namespace MaterialFilter
{
  [HarmonyPatch(typeof(Dialog_ManageOutfits), "DoWindowContents")]
  public static class Dialog_ManageOutfits_DoWindowContents_Patch
  {
    [HarmonyPostfix]
    public static void drawFilterButton(Outfit ___selOutfitInt, ref bool ___absorbInputAroundWindow, ref bool ___closeOnClickedOutside, ref Rect ___windowRect)
    {

      if (___selOutfitInt != null)
      {
        ThingFilter filter = ___selOutfitInt.filter;
        Vector2 buttonSize = new Vector2(80f, 30f);
          float top = ___windowRect.y;
          float left = ___windowRect.x + ___windowRect.width;
        if (Widgets.ButtonText(new Rect(215f, 50f, buttonSize.x, buttonSize.y), "Filter".Translate()))
        {
          MaterialFilterWindow w = Find.WindowStack.WindowOfType<MaterialFilterWindow>();
          if (w != null)
          {
            ___absorbInputAroundWindow = true;
            ___closeOnClickedOutside = true;
            w.Close();
          }
          else
          {
            ___absorbInputAroundWindow = false;
            ___closeOnClickedOutside = false;
            Find.WindowStack.Add(new MaterialFilterWindow(filter, top, left, WindowLayer.Dialog));
          }
        }
      }
    }
  }
}