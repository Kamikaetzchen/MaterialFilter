using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;

namespace MaterialFilter
{
  [HarmonyPatch(typeof(Dialog_ManagePolicies<ApparelPolicy>), nameof(Dialog_ManagePolicies<ApparelPolicy>.DoWindowContents))]
  public static class Dialog_ManageApparelPolicies_DoWindowContents_Patch
  {
    [HarmonyPostfix]
    public static void drawFilterButton(ApparelPolicy ___policyInt, ref bool ___absorbInputAroundWindow, ref bool ___closeOnClickedOutside, ref Rect ___windowRect)
    {
      if (___policyInt != null)
      {
        ThingFilter filter = ___policyInt.filter;
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