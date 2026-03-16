using System;
using System.Collections.Generic;
using System.Linq;

using XRL;
using XRL.Collections;
using XRL.World;
using XRL.World.Anatomy;

namespace UD_ChooseYourBodyPlan.Mod
{
    [HasModSensitiveStaticCache]
    public class AnatomyCategoryEntry : ILoadFromDataBucket<AnatomyCategoryEntry>, IDisposable
    {
        public struct TextShader
        {
            public static string TextShaderXTag => Const.MOD_PREFIX_SHORT + "Shader";

            public string Shader;
            public string Type;
            public string Colors;
            public string Color;

            public TextShader(
                string Shader,
                string Type = null,
                string Colors = null,
                string Color = null
                )
            {
                this.Shader = Shader.ShaderColorOrNull();
                this.Type = Type;
                this.Colors = Colors;
                this.Color = Color.ShaderColorOrNull();
                Finalize();
            }
            public TextShader(Dictionary<string, string> xTag)
                : this(
                      Shader: null,
                      Type: xTag?.GetValue(nameof(Type)),
                      Colors: xTag?.GetValue(nameof(Colors)),
                      Color: xTag?.GetValue(nameof(Color)))
            { }
            public TextShader(int AnatomyCategoryCode)
                : this(GetBodyPartCategoryColor(AnatomyCategoryCode))
            { }

            public TextShader(TextShader Source)
                : this(Source.Shader, Source.Type, Source.Colors, Source.Color)
            { }

            public TextShader Finalize(string OriginalShader = null)
            {
                Shader = Shader.ShaderColorOrNull();
                if (Shader.IsNullOrEmpty())
                {
                    if (!Colors.IsNullOrEmpty())
                    {
                        if (!Type.IsNullOrEmpty())
                            Shader = $"{Colors} {Type}";

                        if (Color.IsNullOrEmpty())
                            Color = Colors[0].ToString().ShaderColorOrNull();
                    }
                }
                if (Shader.IsNullOrEmpty())
                    Shader = Color;

                if (Shader.IsNullOrEmpty())
                    Shader = OriginalShader;

                return this;
            }

            public override readonly string ToString()
                => Shader;

            public readonly string Apply(string Text)
                => !Shader.IsNullOrEmpty()
                    && !Text.IsNullOrEmpty()
                ? "{{" + $"{this}|{Text}" + "}}"
                : Text;

            public TextShader Merge(
                string Shader,
                string Type = null,
                string Colors = null,
                string Color = null
                )
            {
                string originalShader = this.Shader;

                this.Shader = Shader;

                if (!Type.IsNullOrEmpty())
                    this.Type = Type;

                if (!Colors.IsNullOrEmpty())
                    this.Colors = Colors;

                if (!Color.IsNullOrEmpty())
                    this.Color = Color;

                return Finalize(originalShader);
            }

            public TextShader Merge(TextShader Other)
                => Merge(
                    Shader: Other.Shader,
                    Type: Other.Type,
                    Colors: Other.Colors,
                    Color: Other.Color)
                ;

            public TextShader Merge(Dictionary<string, string> xTag)
                => Merge(
                      Shader: null,
                      Type: xTag?.GetValue(nameof(Type)),
                      Colors: xTag?.GetValue(nameof(Colors)),
                      Color: xTag?.GetValue(nameof(Color)))
                ;
        }

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

                return string.Compare(x.DisplayName, y.DisplayName);
            }

            public void Dispose()
            {
            }
        }

        public static CategoryComparer Comparer = new(DefaultFirst: false);
        public static CategoryComparer DefaultFirstComparer = new(DefaultFirst: true);

        public static string CategoryXTag => Const.MOD_PREFIX_SHORT + "Category";

        public static int LowestCategory => 1;
        public static int HighestCategory => 23;

        [ModSensitiveStaticCache]
        private static Dictionary<int, AnatomyCategoryEntry> _CategoryByID;
        public static Dictionary<int, AnatomyCategoryEntry> CategoryByID
        {
            get
            {
                if (_CategoryByID.IsNullOrEmpty())
                {
                    _CategoryByID ??= new();
                    Utils.Log($"{nameof(CategoryByID)}:");
                    Utils.Log($"By {nameof(BodyPartCategory)} Values", Indent: 1);
                    for (int i = 0; i <= HighestCategory; i++)
                    {
                        try
                        {
                            _CategoryByID.Add(
                                key: i,
                                value: new() 
                                {
                                    ID = i,
                                    CategoryName = GetBodyPartCategoryName(i),
                                    DisplayName = GetBodyPartCategoryName(i),
                                    Shader = new TextShader(i).Finalize(),
                                    Entries = new()
                                });

                            Utils.Log($"{i}: {_CategoryByID[i]?.CategoryName}", Indent: 2);
                        }
                        catch (Exception x)
                        {
                            MetricsManager.LogModWarning(Utils.ThisMod, $"Attempted to make {nameof(AnatomyCategoryEntry)} from invalid {nameof(BodyPartCategory)} value: {i}; {x}");
                        }
                    }
                    Utils.Log($"By Blueprints: {Const.CATEGORY_BLUEPRINT}", Indent: 1);
                    foreach (var dataBucket in GameObjectFactory.Factory?.GetBlueprintsInheritingFrom(Const.CATEGORY_BLUEPRINT))
                    {
                        var category = new AnatomyCategoryEntry(dataBucket);
                        if (_CategoryByID.Values.FirstOrDefault(c => c.CategoryName == category.CategoryName) is AnatomyCategoryEntry existingCategory)
                        {
                            existingCategory.Merge(category);
                            Utils.Log($"{dataBucket.Name}, {category.CategoryName}: Merged", Indent: 2);
                        }
                        else
                        {
                            category.ID = _CategoryByID.Count() + 1;
                            _CategoryByID.Add(category.ID, category);
                            Utils.Log($"{dataBucket.Name}, {category.CategoryName}: Added with ID {category.ID}", Indent: 2);
                        }
                    }
                    Utils.BodyPlanEntries?.ForEach(c => _ = c?.Category);

                    Utils.Log("Final Categories:");
                    _CategoryByID.Values.ToList()
                        .ForEach(c => 
                        {
                            Utils.Log(c.CategoryName, Indent: 1);
                            c.DebugOutput(Indent: 2, SkipCategoryName: true);
                        });
                }
                return _CategoryByID;
            }
        }

        [ModSensitiveStaticCache]
        private static Dictionary<string, AnatomyCategoryEntry> _CategoryByName;
        public static Dictionary<string, AnatomyCategoryEntry> CategoryByName
        {
            get
            {
                if (_CategoryByName.IsNullOrEmpty())
                {
                    _CategoryByName = new();
                    foreach (var category in CategoryByID?.Values ?? Enumerable.Empty<AnatomyCategoryEntry>())
                        _CategoryByName[category.CategoryName] = category;
                }
                return _CategoryByName;
            }
        }

        public static IEnumerable<AnatomyCategoryEntry> Categories => CategoryByID.Values;

        public string BaseDataBucketBlueprint => Const.CATEGORY_BLUEPRINT;

        public string CacheKey => CategoryName;

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

        public static AnatomyCategoryEntry GetFor(BodyPlanEntry Choice)
        {
            if (Choice == null)
                throw new ArgumentNullException(nameof(Choice));

            if (CategoryByID.IsNullOrEmpty())
                throw new InvalidOperationException($"{nameof(CategoryByID)} not initialized.");

            AnatomyCategoryEntry category = null;
            if (Choice.CategoryName is string choiceCategory)
            {
                CategoryByName.TryGetValue(choiceCategory, out category);
            }
            else
            {
                int categoryCode = Choice.Anatomy.BodyCategory
                    ?? Choice.Anatomy.Category
                    ?? Choice.Anatomy.Parts?.FirstOrDefault(p => p.Category != null)?.Category
                    ?? 1;

                categoryCode = Math.Clamp(categoryCode, LowestCategory, HighestCategory);
                if (!CategoryByID.TryGetValue(categoryCode, out category))
                {
                    category = new()
                    {
                        ID = categoryCode,
                        DisplayName = GetBodyPartCategoryName(categoryCode),
                        Shader = new TextShader(categoryCode).Finalize(),
                        Entries = new(),
                    };
                }
            }
            category.RequireEntry(Choice);
            if (CategoryByID.TryGetValue(0, out var defaultCategory))
            {
                defaultCategory.RequireEntry(Choice);
                if (Choice.IsDefault)
                    category = defaultCategory;
            }
            return category;
        }

        public static bool TryGetFor(BodyPlanEntry Choice, out AnatomyCategoryEntry Category)
        {
            Category = null;

            if (CategoryByID.IsNullOrEmpty())
            {
                MetricsManager.LogModWarning(Utils.ThisMod, $"{nameof(CategoryByID)} not initialized.");
                return false;
            }

            if (Choice?.Anatomy == null)
                return false;

            return (Category = GetFor(Choice)) != null;
        }

        public int ID;
        public string CategoryName;
        public string DisplayName;
        public TextShader Shader;

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

        public AnatomyCategoryEntry Merge(AnatomyCategoryEntry Other)
        {
            if (Other != null)
            {
                if (!Other.DisplayName.IsNullOrEmpty())
                    DisplayName = Other.DisplayName;
                
                Shader.Merge(Other.Shader);
            }
            return this;
        }

        public string GetDisplayName()
            => Shader.Apply(DisplayName);

        public bool IsValid(Predicate<BodyPlanEntry> Filter = null)
            => !DisplayName.IsNullOrEmpty()
            && GetEntries() is IEnumerable<BodyPlanEntry> entries
            && !entries.IsNullOrEmpty()
            && (Filter == null
                || entries.Any(Filter.Invoke))
            ;

        public bool IsDefaultMatching(BodyPlan BodyPlan)
            => BodyPlan != null
            && (ID == 0) == BodyPlan.IsDefault;

        public IEnumerable<BodyPlanEntry> GetEntries(Predicate<BodyPlanEntry> Filter = null)
        {
            if (Entries.IsNullOrEmpty())
                yield break;

            Entries.StableSortInPlace((x, y) => string.Compare(x?.Anatomy?.Name, y?.Anatomy?.Name));

            foreach (var entry in Entries)
            {
                if ((Filter == null
                    || Filter(entry)))
                {
                    yield return entry;
                }
            }
        }

        public void RequireEntry(BodyPlanEntry Entry)
        {
            if (Entry != null
                && !Entries.Any(e => e?.Anatomy?.Name == Entry?.Anatomy?.Name))
                Entries.Add(Entry);
        }

        public void Dispose()
        {
            Entries.Clear();
            Entries = null;
        }

        public void DebugOutput(int Indent = 0, bool SkipCategoryName = false)
        {
            Utils.Log($"{nameof(ID)}: {ID}", Indent: Indent);
            if (!SkipCategoryName)
                Utils.Log($"{nameof(CategoryName)}: {CategoryName ?? "NO_CATEGORY_NAME"}", Indent: Indent);
            Utils.Log($"{nameof(DisplayName)}: {DisplayName ?? "NO_DISPLAY_NAME"}", Indent: Indent);
            Utils.Log($"{nameof(Shader)}: {Shader}", Indent: Indent);
            Utils.Log($"{nameof(Entries)}:", Indent: Indent);
            if (Entries.IsNullOrEmpty())
                Utils.Log("::None", Indent: Indent + 1);
            else
                foreach (var entry in Entries)
                    Utils.Log($"::{entry.DisplayName}", Indent: Indent + 1);
        }

        public AnatomyCategoryEntry LoadFromDataBucket(GameObjectBlueprint DataBucket)
        {
            if (!ILoadFromDataBucket<AnatomyCategoryEntry>.CheckIsValidDataBucket(this, DataBucket))
                return null;

            if (!DataBucket.TryGetTagValueForData(nameof(CategoryName), out CategoryName))
                return null;

            ID = -1;

            DataBucket.AssignStringFieldFromTag(nameof(DisplayName), ref DisplayName);

            if (DataBucket.TryGetTagValueForData(nameof(TextShader.Color), out string color))
                Shader.Merge(color);

            if (DataBucket.TryGetTagValueForData(nameof(TextShader.Shader), out string shader)
                && shader.ShaderColorOrNull() is string validShader)
                Shader.Merge(validShader);

            if (DataBucket.xTags is Dictionary<string, Dictionary<string, string>> xTags)
            {
                if (xTags.TryGetValue(CategoryXTag, out Dictionary<string, string> categoryXTag))
                {
                    categoryXTag.AssignStringFieldFromXTag(nameof(CategoryName), ref CategoryName);
                    categoryXTag.AssignStringFieldFromXTag(nameof(DisplayName), ref DisplayName);

                    categoryXTag.TryGetValue(nameof(TextShader.Shader), out string xtagShader);
                    categoryXTag.TryGetValue(nameof(TextShader.Color), out string xtagShaderColor);
                    Shader.Merge(xtagShader, Color: xtagShaderColor);
                }

                if (xTags.TryGetValue(TextShader.TextShaderXTag, out Dictionary<string, string> textShaderXTag))
                    Shader.Merge(textShaderXTag);
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
    }
}
