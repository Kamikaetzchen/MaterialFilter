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
  [HarmonyPatch(typeof(ITab_Storage), "FillTab")]
  public static class ITab_Storage_FillTab_Patch
  {
	  [HarmonyPostfix]
	  public static void drawFilterButton(ITab_Storage __instance)
    {
	    var buttonSize = new Vector2(80f, 29f);
      MaterialFilterWindow mfw = new MaterialFilterWindow();
	    if (Widgets.ButtonText(new Rect(180, 10, buttonSize.x, buttonSize.y), "Filter>>"))
      {
        MaterialFilterWindow w = Find.WindowStack.WindowOfType<MaterialFilterWindow>();
        if (w != null)
        {
          w.Close();
        }
        else
        {
          Find.WindowStack.Add(new MaterialFilterWindow());
        }
	    }	
    }
  } // class

  public class MaterialFilterWindow : Window
  {
    public MaterialFilterWindow()
    {
      this.layer = WindowLayer.GameUI;
      this.preventCameraMotion = false;
      this.soundAppear = SoundDefOf.TabOpen;
      this.soundClose = SoundDefOf.TabClose;
      this.doCloseX = true;
      this.closeOnClickedOutside = true;
    }
    public override Vector2 InitialSize
    {
      get
      {
        return new Vector2(Math.Min(1000,UI.screenWidth - 300), 480);
      }
    }    
    public override void PreOpen()
    {
      this.windowRect = new Rect(300, 370, this.InitialSize.x, this.InitialSize.y);
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
      //Text.CurFontStyle.alignment = TextAnchor.LowerRight; funzt nich
      // Stuff
      List<ThingDef> stuffList = new List<ThingDef>();
      foreach(ThingDef td in DefDatabase<ThingDef>.AllDefsListForReading)
      {
        if (td.stuffProps != null)
        {
          stuffList.Add(td);
        }
      }
      float longestStuff = 0;
      foreach (ThingDef td in stuffList)
      {
        longestStuff = Math.Max(longestStuff, Text.CalcSize(td.LabelCap).x);
      }
      Rect stuffRect = new Rect(0, 30, longestStuff + 10, 29);
      foreach (ThingDef td in stuffList)
      {
        Widgets.Label(stuffRect, td.LabelCap);
        stuffRect.y += 30;
      }
      // ThingCategories
      List<ThingCategoryDef> tcdList = new List<ThingCategoryDef>();
      foreach (ThingCategoryDef tcd in ThingCategoryDefOf.Root.childCategories)
      {
        if (hasMadeFromStuff(tcd))
        {
          tcdList.Add(tcd);
        }
      }
      float longestThingCat = 0;
      foreach (ThingCategoryDef tcd in tcdList)
      {
        longestThingCat = Math.Max(longestThingCat, Text.CalcSize(tcd.LabelCap).x);
      }
      Rect thingCatRect = new Rect(longestStuff + 10, 0, longestThingCat + 10, 29);
      WidgetRow thingCatWidgets = new WidgetRow(100, 0, UIDirection.RightThenDown, UI.screenWidth - 300, 6);
      foreach (ThingCategoryDef tcd in tcdList)
      {
        thingCatWidgets.Label(tcd.LabelCap, longestThingCat);
      }
    }
    

  } // class

  public class MaterialFilter : ModBase
  {
    public static MaterialFilter instance;
    public override string ModIdentifier
    {
      get
      {
        return "MaterialFilter";
      }
    }
    
    public override void Initialize()
    {
      instance = this;
      Logger.Message("Init");

    }

    public override void DefsLoaded()
    {
      //ThingCategoryDef materials = createNewThingCategoryDef("materials", "string_Materials".Translate(), ThingCategoryDefOf.Root);
      //ThingDef dummy = DefDatabase<ThingDef>.AllDefsListForReading.Find(x => x.defName == "dummy");

      //if (dummy.thingCategories == null)
      //{
      //  dummy.thingCategories = new List<ThingCategoryDef>();
      //}

      foreach (StuffCategoryDef scd in DefDatabase<StuffCategoryDef>.AllDefsListForReading)
      {
        //ThingCategoryDef tcd = createNewThingCategoryDef(scd.defName, scd.label, materials, "string_allow".Translate() + " " + scd.label.Translate());
        Logger.Message("StuffCategoryDef: " + scd.defName);
        createSpecialThingFilterDef(scd, null/*tcd*/);
        //dummy.thingCategories.Add(tcd);
        //Logger.Message("creating ThingCatDef: " + tcd.defName);


      }

      Logger.Message("specialThingFilterDefs generated");
    }

    private ThingCategoryDef createNewThingCategoryDef(string defName, string label, ThingCategoryDef parent, string description = "")
    {
      ThingCategoryDef newCatDef = new ThingCategoryDef
      {
        defName = "allow" + defName,
        label = "string_allowed".Translate() + " " + label,
        description = description,
        parent = parent,
        modContentPack = ModContentPack
      };
      newCatDef.parent.childCategories.Add(newCatDef);
      ModContentPack.AddDef(newCatDef);
      newCatDef.PostLoad();
      return newCatDef;
    }
    
    public void createSpecialThingFilterDef(StuffCategoryDef stuffToFilter, ThingCategoryDef parentCategory)
    {
      List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading;
      Boolean skip = false;
      foreach (ThingDef def in defs)
      {
        if (def.stuffProps != null && def.stuffProps.categories.Contains(stuffToFilter))
        {
          // avoid duplicate SpecialThingFilterDefs
          if (def.stuffProps.categories.Count > 1)
          {
            List<SpecialThingFilterDef> sdefs = DefDatabase<SpecialThingFilterDef>.AllDefsListForReading;

            foreach (SpecialThingFilterDef sdef in sdefs)
            {
              if (sdef.defName == "allow" + def.defName)
              {
                Logger.Message("Skipping duplicate " + def.defName);
                skip = true;
              }
            }
          }
          if (!skip)
          {
            SpecialThingFilterDef newSpecialFilterDef = new SpecialThingFilterDef
            {
              defName = "allow" + def.defName,
              label = "string_allow".Translate() + " " + def.label,
              description = "string_allow".Translate() + " " + def.label,
              //parentCategory = parentCategory,
              parentCategory = ThingCategoryDefOf.Root,
              allowedByDefault = true,
              saveKey = "allow" + def.defName,
              configurable = true,

              modContentPack = ModContentPack
            };
            string newWorkerClassName = "SpecialThingFilterWorker_" + def.defName;
            newSpecialFilterDef.workerClass = generateDynamicClass(newWorkerClassName, def);

            if (newSpecialFilterDef.parentCategory.childSpecialFilters.NullOrEmpty())
            {
              newSpecialFilterDef.parentCategory.childSpecialFilters = new List<SpecialThingFilterDef>();
            }
            newSpecialFilterDef.parentCategory.childSpecialFilters.Add(newSpecialFilterDef);
            DefGenerator.AddImpliedDef(newSpecialFilterDef);
          }
        }
        skip = false;
      }
    }

    private Type generateDynamicClass(string newClassName, ThingDef def)
    {

      AssemblyBuilder ass = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("dynAss"), AssemblyBuilderAccess.RunAndSave);
      ModuleBuilder moduleBuilder = ass.DefineDynamicModule("dynMod");
      TypeBuilder typeBuilder = moduleBuilder.DefineType("Rimworld." + newClassName, TypeAttributes.Public | TypeAttributes.Class, typeof(SpecialThingFilterWorker));
      MethodInfo oldMatchesMethod = typeof(SpecialThingFilterWorker).GetMethod("Matches");
      MethodInfo oldCanEverMatchMethod = typeof(SpecialThingFilterWorker).GetMethod("CanEverMatch");

      MethodBuilder newMatchesMethod = typeBuilder.DefineMethod
        (
        oldMatchesMethod.Name,
        MethodAttributes.Public |
        MethodAttributes.HideBySig |
        MethodAttributes.Virtual,
        CallingConventions.HasThis,
        oldMatchesMethod.ReturnType,
        oldMatchesMethod.GetParameters().Select(x => x.ParameterType).ToArray()
        );
      newMatchesMethod.SetImplementationFlags(MethodImplAttributes.IL); // needed?
      generateMatchesILCode(newMatchesMethod, def);


      MethodBuilder newCanEverMatchMethod = typeBuilder.DefineMethod
        (
        oldCanEverMatchMethod.Name,
        MethodAttributes.Public |
        MethodAttributes.HideBySig |
        MethodAttributes.Virtual,
        CallingConventions.HasThis,
        oldCanEverMatchMethod.ReturnType,
        oldCanEverMatchMethod.GetParameters().Select(x => x.ParameterType).ToArray()
        );
      newCanEverMatchMethod.SetImplementationFlags(MethodImplAttributes.IL); // needed?
      generateCanEverMatchILCode(newCanEverMatchMethod);

      typeBuilder.DefineMethodOverride(newMatchesMethod, oldMatchesMethod);
      typeBuilder.DefineMethodOverride(newCanEverMatchMethod, oldCanEverMatchMethod);

      return typeBuilder.CreateType();
    }

    private void generateCanEverMatchILCode(MethodBuilder method)
    {
      ILGenerator il = method.GetILGenerator();
      Label invert = il.DefineLabel();

      il.Emit(OpCodes.Ldarg_1);
      il.EmitCall(OpCodes.Callvirt, typeof(BuildableDef).GetMethod("get_MadeFromStuff"), Type.EmptyTypes);
      il.Emit(OpCodes.Ret);
    }

    private void generateMatchesILCode(MethodBuilder method, ThingDef def)
    {
      ILGenerator il = method.GetILGenerator();

      string defNameToPush = def.defName;

      il.DeclareLocal(typeof(Apparel));
      il.DeclareLocal(typeof(ThingDef));
      il.DeclareLocal(typeof(Thing));

      Label apparelIsNull = il.DefineLabel();
      Label comparisonDone = il.DefineLabel();
      Label hasNoStuff = il.DefineLabel();

      //if is Apparel then push to stack else push 0 and jump to end
      il.Emit(OpCodes.Ldarg_1); //load parameter (Ldarg_0 is always 'this')
      il.Emit(OpCodes.Isinst, typeof(Apparel)); //if is Apparel then push to stack else push 0
      il.Emit(OpCodes.Stloc_0); //needs to be stored because used twice (would dup do the same thing without needing to ldloc twice?)      
      il.Emit(OpCodes.Ldloc_0);
      il.Emit(OpCodes.Brfalse_S, apparelIsNull); // if is not apparel push 0 and ret

      // if has Stuff then push to stack, else push 0 and jump to end (i.e. Power Armor has no stuff)
      il.Emit(OpCodes.Ldloc_0);
      il.EmitCall(OpCodes.Callvirt, typeof(Thing).GetMethod("get_Stuff"), Type.EmptyTypes);
      il.Emit(OpCodes.Stloc_1);
      il.Emit(OpCodes.Ldloc_1);
      il.Emit(OpCodes.Brfalse_S, hasNoStuff); // Power Armor has no stuff
      il.Emit(OpCodes.Ldloc_1);
      
      // compare defNames
      il.Emit(OpCodes.Ldfld, typeof(ThingDef).GetField("defName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance));
      il.Emit(OpCodes.Ldstr, defNameToPush);
      il.EmitCall(OpCodes.Call, typeof(string).GetMethod("Equals", new Type[] { typeof(string) }), new Type[] { typeof(string) });
      il.Emit(OpCodes.Br_S, comparisonDone);

      il.MarkLabel(apparelIsNull);
      il.MarkLabel(hasNoStuff);
      il.Emit(OpCodes.Ldc_I4_0);
      il.MarkLabel(comparisonDone);

      il.Emit(OpCodes.Ret);

    }
  } // class
} // namespace
