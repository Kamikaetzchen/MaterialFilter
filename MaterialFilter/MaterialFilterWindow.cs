using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using HugsLib;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using UnityEngine;

namespace MaterialFilter
{
  public class MaterialFilterWindow : MainTabWindow
  {
    ThingFilter filter;
    private float left;
    private float top;
    private Vector2 scrollPosition;
    public MaterialFilterWindow(ThingFilter __filter , float __top, float __left, WindowLayer __layer)
    {
      this.layer = __layer;
      this.preventCameraMotion = false;
      this.soundAppear = SoundDefOf.TabOpen;
      this.soundClose = SoundDefOf.TabClose;
      this.doCloseX = true;
      this.closeOnClickedOutside = true;
      this.resizeable = false;
      this.draggable = true;
      this.left = __left;
      top = __top;
      filter = __filter;
    }

    public override Vector2 InitialSize
    {
      get
      {
        return new Vector2(Math.Min(300,UI.screenWidth - 300), 480);
      }
    }    
    public override void PreOpen()
    {
      this.windowRect = new Rect(this.left, top, this.InitialSize.x, this.InitialSize.y);
    }

    private bool hasMadeFromStuff(ThingCategoryDef tcd)
    {
      foreach (ThingDef td in tcd.childThingDefs)
      {
        if (td.MadeFromStuff)
        {
          return true;
        }
      }
      if (!tcd.childCategories.NullOrEmpty())
      {
        foreach (ThingCategoryDef child in tcd.childCategories)
        {
          if (hasMadeFromStuff(child))
          {
            return true;
          }
        }
      }
      return false;
    }
    public override void DoWindowContents(Rect rect)
    {
      IEnumerable<SpecialThingFilterDef> sdefs = DefDatabase<SpecialThingFilterDef>.AllDefsListForReading.FindAll(x => x.defName.StartsWith("MaterialFilter"));
      
      float lineHeight = 32f;
      float padding = 5f;
      float checkSize = 25f;
      Color textColor = new Color(135, 135, 135);
      Color checkBoxColor = new Color(135, 135, 135);


      float longestFilterName = 0;
      foreach (SpecialThingFilterDef sdef in sdefs)
      {
        longestFilterName = Math.Max(longestFilterName, Text.CalcSize(sdef.LabelCap).x);
      }
      //draw
      Rect stuffRect = new Rect(0f, 0f, longestFilterName, lineHeight);
      Rect viewRect = new Rect(0f, 0f, rect.width - 16f, sdefs.Count() * lineHeight + lineHeight);
      Widgets.BeginScrollView(rect, ref scrollPosition, viewRect, true);
      
      foreach (SpecialThingFilterDef sdef in sdefs)
      { // MaterialFilter_allow
        ThingDef currentThing = DefDatabase<ThingDef>.AllDefsListForReading.Find(x => x.defName.Equals(sdef.defName.Substring(20)));
        if (currentThing != null)
        {
          Text.Anchor = TextAnchor.UpperRight;
          Widgets.Label(stuffRect, new GUIContent(sdef.LabelCap));
          stuffRect.x += longestFilterName + padding;
          bool isAllowed = filter.Allows(sdef);
          bool hasChanged = isAllowed;
          Widgets.Checkbox(stuffRect.x, stuffRect.y, ref isAllowed, checkSize, false, false, WidgetsWork.WorkBoxCheckTex);
          if (isAllowed != hasChanged)
          {
            filter.SetAllow(sdef, isAllowed);
          }
        }
        stuffRect.x = 0;
        stuffRect.y += lineHeight;
      }





      /*
      // header (ThingCats)
      foreach (ThingCategoryDef tcd in tcdList)
      {
        Text.Anchor = TextAnchor.UpperCenter;
        Widgets.Label(stuffRect, new GUIContent(tcd.LabelCap));
        stuffRect.x += (longestThingCat + padding);
      }
      stuffRect.x = 0;
      stuffRect.y += lineHeight;
      stuffRect.width = longestStuff + padding;
      // rest of table (Stuffs and checkboxes)
      foreach (ThingDef td in stuffList)
      {
        SpecialThingFilterDef sdef = DefDatabase<SpecialThingFilterDef>.AllDefsListForReading.Find(x => x.defName.Equals("MaterialFilter_allow" + td.defName));
        Text.Anchor = TextAnchor.UpperRight;
        Widgets.Label(stuffRect, new GUIContent(td.LabelCap));
        stuffRect.x += (longestStuff + padding) + (longestThingCat / 2f);
        stuffRect.width = longestThingCat + padding;
        foreach (ThingCategoryDef tcd in tcdList)
        {
          GUI.color = checkBoxColor;
          Widgets.DrawBox(new Rect(stuffRect.x - (checkSize / 2), stuffRect.y - ((lineHeight - checkSize) / 2), checkSize, checkSize), 1);
          GUI.color = textColor;
          Widgets.Checkbox(stuffRect.x - (checkSize / 2), stuffRect.y - ((lineHeight - checkSize) / 2), ref isChecked, checkSize, false, false, WidgetsWork.WorkBoxCheckTex);

          stuffRect.x += (longestThingCat + padding);
        }
        stuffRect.x = 0;
        stuffRect.y += lineHeight;
        stuffRect.width = longestStuff + padding;
      }
      */
      // has to be reset
      Text.Anchor = TextAnchor.UpperLeft;
      Widgets.EndScrollView();
    }
  }
}