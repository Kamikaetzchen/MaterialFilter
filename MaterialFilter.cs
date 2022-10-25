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
      foreach (StuffCategoryDef scd in DefDatabase<StuffCategoryDef>.AllDefsListForReading)
      {
        //Logger.Message("StuffCategoryDef: " + scd.defName);
        createSpecialThingFilterDef(scd/*, tcd*/);
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
    
    public void createSpecialThingFilterDef(StuffCategoryDef stuffToFilter/*, ThingCategoryDef parentCategory*/)
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
              if (sdef.defName == "MaterialFilter_allow" + def.defName)
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
              defName = "MaterialFilter_allow" + def.defName,
              label = def.label,
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
      ModuleBuilder moduleBuilder = ass.DefineDynamicModule("dynMod" + def.defName + ".dll");
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

      Type newType = typeBuilder.CreateType();
      //ass.Save("dynAss" + def.defName + ".dll");
      return newType;
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

      Label comparisonDone = il.DefineLabel();
      Label hasNoStuff = il.DefineLabel();

      /*
          return t.Stuff != null && t.Stuff.defName.Equals("defNameToPush");
      */

      il.Emit(OpCodes.Ldarg_1); //load parameter (Ldarg_0 is always 'this')
      il.EmitCall(OpCodes.Callvirt, typeof(Thing).GetMethod("get_Stuff"), Type.EmptyTypes);
      il.Emit(OpCodes.Brfalse_S, hasNoStuff); // i.e. Power Armor has no stuff
      il.Emit(OpCodes.Ldarg_1); //load parameter (Ldarg_0 is always 'this')
      il.EmitCall(OpCodes.Callvirt, typeof(Thing).GetMethod("get_Stuff"), Type.EmptyTypes);
      // compare defNames
      il.Emit(OpCodes.Ldfld, typeof(ThingDef).GetField("defName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance));
      il.Emit(OpCodes.Ldstr, defNameToPush);
      il.EmitCall(OpCodes.Call, typeof(string).GetMethod("Equals", new Type[] { typeof(string) }), new Type[] { typeof(string) });
      il.Emit(OpCodes.Ret);
      il.MarkLabel(hasNoStuff);
      il.Emit(OpCodes.Ldc_I4_0);
      il.Emit(OpCodes.Ret);
    }
  } // class
} // namespace
