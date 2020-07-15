using RimWorld;
using Verse;
using HarmonyLib;
using System.Linq;
using System.Collections.Generic;

namespace MaterialFilter
{
  // remove generated SpecialThingFilterDefs from ThingFilterUI list
  [HarmonyPatch(typeof(ThingFilterUI), "DoThingFilterConfigWindow")]
  public static class ThingFilterUI_DoThingFilterConfigWindow_Patch
  {
    [HarmonyPrefix]
    public static void addHiddenFilters(ref IEnumerable<SpecialThingFilterDef> forceHiddenFilters)
    {
      if (forceHiddenFilters == null)
      {
        forceHiddenFilters = DefDatabase<SpecialThingFilterDef>.AllDefsListForReading.FindAll(x => x.defName.StartsWith("MaterialFilter"));
      }
      else
      {
        forceHiddenFilters = forceHiddenFilters.Concat(DefDatabase<SpecialThingFilterDef>.AllDefsListForReading.FindAll(x => x.defName.StartsWith("MaterialFilter")));
      }
    }
  } // ThingFilterUI Patch
}