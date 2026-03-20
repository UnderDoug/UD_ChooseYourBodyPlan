using System;
using System.Collections.Generic;
using System.Linq;

using XRL;
using XRL.Collections;
using XRL.World;
using XRL.World.Anatomy;

using UD_ChooseYourBodyPlan.Mod.Logging;
using UD_ChooseYourBodyPlan.Mod.TextHelpers;

namespace UD_ChooseYourBodyPlan.Mod
{
    [HasModSensitiveStaticCache]
    public class AnatomyCategoryEntry : ILoadFromDataBucket<AnatomyCategoryEntry>, IDisposable
    {
        public class CategoryComparer : IComparer<AnatomyCategoryEntry>, IDisposable
        {
            public bool DefaultFirst;

            protected CategoryComparer()
            {
                DefaultFirst = false;
            }
            public CategoryComparer(bool DefaultFirst)
                : this()
            {
                this.DefaultFirst = DefaultFirst;
            }

            public int Compare(AnatomyCategoryEntry x, AnatomyCategoryEntry y)
            {
                if (y == null)
                {
                    if (x != null)
                        return -1;
                    else
                        return 0;
                }
                else
                if (x == null)
                    return 1;

                if (x.ID == 0)
                    return -1;
                if (y.ID == 0)
                    return 1;

                return string.Compare(x?.DisplayName, y?.DisplayName);
            }

            public void Dispose()
            {
            }
        }

        public static CategoryComparer Comparer = new(DefaultFirst: false);
        public static CategoryComparer DefaultFirstComparer = new(DefaultFirst: true);

        public static string LoadingDataBucket => "AnatomyCategories";

        public static string XTagName => Const.MOD_PREFIX_SHORT + "Category";

        public static int LowestCategory => 1;
        public static int HighestCategory => 23;

        public static Dictionary<string, AnatomyCategoryEntry> CategoryByName => BodyPlanFactory.Factory?.AnatomyCategoryEntryByCategoryName;

        public static IEnumerable<AnatomyCategoryEntry> Categories => CategoryByName?.Values ?? Enumerable.Empty<AnatomyCategoryEntry>();

        public string BaseDataBucketBlueprint => Const.CATEGORY_BLUEPRINT;

        public string CacheKey => CategoryName;

        public int ID;
        public string CategoryName;
        public string DisplayName;
        public Shader Shader;

        public List<BodyPlanEntry> Entries;

        public AnatomyCategoryEntry()
        {
            ID = -1;
            CategoryName = null;
            DisplayName = null;
            Shader = default;
            Entries = new();
        }

        public AnatomyCategoryEntry(GameObjectBlueprint DataBucket)
            : base()
        {
            LoadFromDataBucket(DataBucket);
        }

        public static bool IsCodeValid(int Code)
            => Code == Math.Clamp(Code, LowestCategory, HighestCategory);

        public static string GetBodyPartCategoryName(int Code)
            => !IsCodeValid(Code)
            ? "Default"
            : BodyPartCategory.GetName(Code)
            ;

        public static string GetBodyPartCategoryColor(int Code)
        {
            if (IsCodeValid(Code)
                && BodyPartCategory.GetColor(Code) is string categoryColor)
                return categoryColor;

            if (GetBodyPartCategoryName(Code).ToLower() is string nameLower)
            {
                if (nameLower.ShaderColorOrNull() is string nameShader)
                    return nameShader;
            }
            return null;
        }

        public string GetDisplayName()
            => Shader.Apply(DisplayName);

        public bool IsValid(Predicate<BodyPlanEntry> Filter = null)
            => !DisplayName.IsNullOrEmpty()
            && GetEntries(Filter) is IEnumerable<BodyPlanEntry> entries
            && !entries.IsNullOrEmpty()
            && (Filter == null
                || entries.Any(Filter.Invoke))
            ;

        public bool IsValidWithAnyAvailable(Predicate<BodyPlanEntry> Filter = null)
            => IsValid(c => (Filter == null || Filter(c)) && c.IsAvailable())
            ;

        public bool IsDefaultMatching(BodyPlan BodyPlan)
            => BodyPlan != null
            && (ID == 0) == BodyPlan.IsDefault;

        public IEnumerable<BodyPlanEntry> GetEntries(Predicate<BodyPlanEntry> Filter = null)
        {
            if (Entries.IsNullOrEmpty())
                yield break;

            Entries.StableSortInPlace(delegate (BodyPlanEntry x, BodyPlanEntry y)
            {
                using var xBodyPlan = x?.GetBodyPlan();
                using var yBodyPlan = y?.GetBodyPlan();
                return string.Compare(xBodyPlan?.DisplayNameStripped, yBodyPlan?.DisplayNameStripped);
            });

            foreach (var entry in Entries)
                if (Filter?.Invoke(entry) is not false)
                    yield return entry;
        }

        public AnatomyCategoryEntry LoadFromDataBucket(GameObjectBlueprint DataBucket)
        {
            if (!ILoadFromDataBucket<AnatomyCategoryEntry>.CheckIsValidDataBucket(this, DataBucket))
            {
                Dispose();
                return null;
            }

            DataBucket.TryGetTagValueForData(nameof(CategoryName), out CategoryName);

            ID = -1;

            DataBucket.AssignStringFieldFromTag(nameof(DisplayName), ref DisplayName);

            if (DataBucket.TryGetTagValueForData(nameof(Shader.Color), out string color))
                Shader = Shader.Merge(color);

            if (DataBucket.TryGetTagValueForData(nameof(Shader), out string shader)
                && shader.ShaderColorOrNull() is string validShader)
                Shader = Shader.Merge(validShader);

            if (DataBucket.xTags is Dictionary<string, Dictionary<string, string>> xTags)
            {
                if (xTags.TryGetValue(XTagName, out Dictionary<string, string> categoryXTag))
                {
                    categoryXTag.AssignStringFieldFromXTag(nameof(CategoryName), ref CategoryName);
                    categoryXTag.AssignStringFieldFromXTag(nameof(DisplayName), ref DisplayName);

                    categoryXTag.TryGetValue(nameof(Shader), out string xtagShader);
                    categoryXTag.TryGetValue(nameof(Shader.Color), out string xtagShaderColor);
                    Shader = Shader.Merge(xtagShader, Color: xtagShaderColor);
                }

                if (xTags.TryGetValue(Shader.XTagName, out Dictionary<string, string> textShaderXTag))
                    Shader = Shader.Merge(textShaderXTag);
            }
            DisplayName ??= CategoryName;
            if (CategoryName.IsNullOrEmpty())
            {
                Dispose();
                return null;
            }
            return this;
        }

        public AnatomyCategoryEntry Merge(AnatomyCategoryEntry Other)
        {
            if (Other != null)
            {
                if (!Other.DisplayName.IsNullOrEmpty())
                    DisplayName = Other.DisplayName;
                
                Shader = Shader.Merge(Other.Shader);
            }
            return this;
        }

        public AnatomyCategoryEntry Clone()
            => new()
            {
                ID = -1,
                CategoryName = CategoryName,
                DisplayName = DisplayName,
                Shader = new(Shader),
                Entries = new(),
            };

        public bool SameAs(AnatomyCategoryEntry Other)
            => CategoryName == Other.CategoryName;


        public AnatomyCategory GetAnatomyCategory()
            => new(CategoryName);

        public void Dispose()
        {
            Entries?.Clear();
            Entries = null;
        }

        public void DebugOutput(Indent Indent, bool SkipCategoryName = false)
        {
            Debug.Log(nameof(ID), ID, Indent: Indent);
            if (!SkipCategoryName)
                Debug.Log(nameof(CategoryName), CategoryName ?? "NO_CATEGORY_NAME", Indent: Indent);
            Debug.Log(nameof(DisplayName), DisplayName ?? "NO_DISPLAY_NAME", Indent: Indent);
            Debug.Log(nameof(Shader), Shader, Indent: Indent);
            Debug.Log($"{nameof(Entries)}:", Indent: Indent);
            Debug.Loggregrate(
                Source: Entries,
                Proc: e => e.DisplayName,
                Empty: "None",
                PostProc: s => $"::{s}",
                Indent: Indent[1]);
        }
    }
}
