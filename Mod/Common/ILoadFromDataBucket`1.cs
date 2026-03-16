using System;
using System.Collections.Generic;
using System.Text;

using XRL;
using XRL.World;

namespace UD_ChooseYourBodyPlan.Mod
{
    public interface ILoadFromDataBucket<T> : IDisposable
        where T : IDisposable, new()
    {
        public enum MergeType
        {
            None,
            HardReplace,
            SoftReplace,
            Require,
        }

        public static string BaseDataBucket => "DataBucket";
        string BaseDataBucketBlueprint { get; }

        string CacheKey { get; }

        public static string GetBaseDataBucketBlueprint()
        {
            using var instance = new T() as ILoadFromDataBucket<T>;
            return instance.BaseDataBucketBlueprint;
        }

        public static bool IsValidDataBucket(ILoadFromDataBucket<T> Object, GameObjectBlueprint DataBucket)
            => DataBucket?.InheritsFrom(Object.BaseDataBucketBlueprint ?? BaseDataBucket)
            ?? false;

        public static bool CheckIsValidDataBucket(ILoadFromDataBucket<T> Object, GameObjectBlueprint DataBucket, bool Silent = false)
        {
            if (!IsValidDataBucket(Object, DataBucket))
            {
                if (!Silent)
                {
                    if (!ModManager.TryGetCallingMod(out var mod, out _))
                        mod = Utils.ThisMod;

                    mod.Error($"Aborted attempt to load {typeof(T)} from incorrect {nameof(DataBucket)} \"{DataBucket.Name}\" with base " +
                        $"\"{DataBucket.GetBase()}\" instead of specified \"{Object.BaseDataBucketBlueprint ?? BaseDataBucket}\"");
                }
                return false;
            }
            return true;
        }

        T LoadFromDataBucket(GameObjectBlueprint DataBucket);

        bool TryLoadFromDataBucket(GameObjectBlueprint DataBucket, out T Result)
            => (Result = LoadFromDataBucket(DataBucket)) != null;

        bool SameAs(T Other);

        T Merge(T Other);

        T Clone();
    }
}
