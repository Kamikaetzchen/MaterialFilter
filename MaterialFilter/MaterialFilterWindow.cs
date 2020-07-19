using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.Sound;
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
    protected override float Margin
		{
			get
			{
				return 5f;
			}
		}
    public MaterialFilterWindow(ThingFilter __filter , float __top, float __left, WindowLayer __layer)
    {
      this.layer = __layer;
      this.preventCameraMotion = false;
      this.soundAppear = SoundDefOf.TabOpen;
      this.soundClose = SoundDefOf.TabClose;
      this.doCloseX = false;
      this.closeOnClickedOutside = true;
      this.resizeable = false;
      this.draggable = false;
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
      this.windowRect = new Rect(this.left, this.top, this.InitialSize.x, this.InitialSize.y);
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

    public void setAllowAll(IEnumerable<SpecialThingFilterDef> sdefs, bool allow)
    {
      foreach (SpecialThingFilterDef sdef in sdefs)
      {
        filter.SetAllow(sdef, allow);
      }
    }

    public override void DoWindowContents(Rect rect)
    {
      IEnumerable<SpecialThingFilterDef> sdefs = DefDatabase<SpecialThingFilterDef>.AllDefsListForReading.FindAll(x => x.defName.StartsWith("MaterialFilter"));
      
      float lineHeight = 25f;
      float indent = 5f;
      float padding = 5f;
      string headerText = "Allow items made from";
      Color headerColor = new Color(135, 135, 135);


      float longestFilterName = 0;
      foreach (SpecialThingFilterDef sdef in sdefs)
      {
        longestFilterName = Math.Max(longestFilterName, Text.CalcSize(sdef.LabelCap).x);
      }
      //draw
      float innerWidth = Math.Max(Text.CalcSize(headerText).x, longestFilterName + padding + lineHeight);
      float scrollWidth = indent + innerWidth + GUI.skin.verticalScrollbar.margin.left + GUI.skin.verticalScrollbar.fixedWidth + GUI.skin.verticalScrollbar.margin.right;
      Rect headerRect = new Rect(0, 0, scrollWidth, lineHeight);
      Rect stuffRect = new Rect(indent, 0f, innerWidth, lineHeight);
      Rect viewRect = new Rect(0f, 0f, innerWidth, sdefs.Count() * lineHeight);
      Rect scrollRect = new Rect(stuffRect.x, lineHeight, scrollWidth, rect.height - lineHeight);

      // Header
      Text.Anchor = TextAnchor.MiddleCenter;
      Widgets.Label(headerRect, new GUIContent(headerText));
      Text.Anchor = TextAnchor.UpperLeft;
      stuffRect.width = longestFilterName + padding;


      Widgets.DrawMenuSection(scrollRect);
      scrollRect.x += 1f;
      scrollRect.width -= 2f;


      Rect rect2 = new Rect(scrollRect.x + 1f, scrollRect.y + 1f, (scrollRect.width - 2f) / 2f, lineHeight);
			if (Widgets.ButtonText(rect2, "ClearAll".Translate(), true, true, true))
			{
				setAllowAll(sdefs, false);
			  SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera(null); // using Verse.Sound;
			}
      if (Widgets.ButtonText(new Rect(rect2.xMax + 1f, rect2.y, scrollRect.xMax - 2f - (rect2.xMax + 1f), lineHeight), "AllowAll".Translate(), true, true, true))
			{
				setAllowAll(sdefs, true);
				SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera(null);
			}



      scrollRect.y += lineHeight + 2f;
      scrollRect.height -= (lineHeight + 3f);
      Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect, true);
      foreach (SpecialThingFilterDef sdef in sdefs)
      { // MaterialFilter_allow
        ThingDef currentThing = DefDatabase<ThingDef>.AllDefsListForReading.Find(x => x.defName.Equals(sdef.defName.Substring(20)));
        if (currentThing != null)
        {
          //Text.Anchor = TextAnchor.UpperRight;
          Widgets.Label(stuffRect, new GUIContent(sdef.LabelCap));
          stuffRect.x += longestFilterName + padding;
          bool isAllowed = filter.Allows(sdef);
          bool hasChanged = isAllowed;
          Widgets.Checkbox(stuffRect.x, stuffRect.y, ref isAllowed, lineHeight, false, true, WidgetsWork.WorkBoxCheckTex);
          if (isAllowed != hasChanged)
          {
            filter.SetAllow(sdef, isAllowed);
          }
        }
        stuffRect.x = indent;
        stuffRect.y += lineHeight;
      }
      // has to be reset
      Text.Anchor = TextAnchor.UpperLeft;
      Widgets.EndScrollView();

      Log.Message(" margin: " + GUI.skin.verticalScrollbar.margin);
      Log.Message("padding: " + GUI.skin.verticalScrollbar.padding);

      this.windowRect.width = scrollWidth + (2 * this.Margin) + 10;
      

      //this.Notify_ResolutionChanged();
    }
  }
}