using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace MaterialFilter
{
  [HarmonyPatch(typeof(Dialog_BillConfig), "DoWindowContents")]
  public static class Dialog_BillConfig_DoWindowContents_Patch
  {
    [HarmonyPostfix]
    public static void drawFilterButton(ref Bill_Production ___bill, ref bool ___absorbInputAroundWindow, ref bool ___closeOnClickedOutside)
    {
      if (___bill.recipe.specialProducts != null && ___bill.recipe.specialProducts[0] == SpecialProductType.Smelted)
      {
        bool flag = true;
        for (int i = 0; i < ___bill.recipe.ingredients.Count; i++)
        {
          if (!___bill.recipe.ingredients[i].IsFixedIngredient)
          {
            flag = false;
            break;
          }
        }
        if (!flag)
        {
          ThingFilter filter = ___bill.ingredientFilter;
          Vector2 buttonSize = new Vector2(122f, 25f);
          float top = 240f;
          float left = 240f;
          if (Widgets.ButtonText(new Rect(642f, 25f, buttonSize.x, buttonSize.y), "Filter".Translate()))
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
}