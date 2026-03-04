# Modding

Some amount of care has been taken to expose poarts of Choose Your Body Plan to the game's xml files/data modding system.

Below are some guides on how to restrict an anatomy, including gating its selection behind an option, as well as a guide on altering how an anatomy is displayed in the Body Plan Module during character creation.

This mod should be compatible with the stable version of the game, the beta version, and sometimes even the lang branch. If you notice something not working, please reach out to me on github or @ me on the official Caves of Qud discord.

## Restricting Anatomy Selection

Choose Your Body Plan makes use of the base game's data bucket object blueprints and object blueprint xtags to make restricting anatomies a very straight forward process that doesn't require any scripting.

The first step is to create `Data.xml` somewhere in your mod's folder, if you don't already have one, and make a new object blueprint that inherits from `UD_BodyPlan_Selection_BaseExclusion`.

Below is an example:
    
    <object Name="YourModPrefix_UD_BodyPlan_Selection_NotOptional" Inherits="UD_BodyPlan_Selection_BaseExclusion">
        <tag Name="Anatomy" Value="YourModPrefix_CustomAnatomy" />
    </object>

What you name your blueprint doesn't matter, only that it inherits from `UD_BodyPlan_Selection_BaseExclusion`, however it's always worth prefixing the things you add to the game to minimise the potential for conflicts. Besides, it's the perfect opportunity to be vain, naming things after yourself, because there's a legitimate reason to do so.

That's all you need to do to outright restrict an anatomy. Nothing this mod does natively will include an anatomy that it finds this way without some additional tags.

If you have reason to, you can also restrict anatomies added by other mods, or from the base game. Follow the exact same steps with one small exception.

Multiple anatomies can be restricted with the same tag with a small alteration, per below:
    
    <object Name="YourModPrefix_UD_BodyPlan_Selection_NotOptional" Inherits="UD_BodyPlan_Selection_BaseExclusion">
        <tag Name="Anatomies" Value="YourModPrefix_CustomAnatomy,SomeOtherMod_ModdedAnatomy,Bird,Fish" />
    </object>

Simply changing the tag to `Anatomies` and using a comma delimited list will outright restrict each of the listed anatomies.

### Option-gating an Anatomy

You might want to restrict an anatomy, but only optionally. There are a couple of ways to do this.

The first is to add the `Optional` tag to the blueprint and specify an option ID as the value. For the remainder of this guide I'm going to use `Anatomies`, but I'll only be putting one entry in, which _will_ work.

    <object Name="YourModPrefix_UD_BodyPlan_Selection_NotOptional" Inherits="UD_BodyPlan_Selection_BaseExclusion">
        <tag Name="Anatomies" Value="YourModPrefix_CustomAnatomy" />
        <tag Name="Optional" Value="Option_YourModPrefix_EnableYourCustomAnatomy" />
    </object>

Then, in your own `Options.xml` you simply need a checkbox option with the ID `Option_YourModPrefix_EnableYourCustomAnatomy`, and the anatomy will only be restricted when that option is disabled.
    
    <options>
        <option
            ID="Option_YourModPrefix_EnableYourCustomAnatomy"
            DisplayText="Enable body plans that are added by this mod."
            Category="Mod: Your Mod Title"
            Type="Checkbox"
            Default="No" />
    </options>

And some handling code for your option.
    
    using XRL;
    namespace YourModPrefix.Mod
    {
        [HasOptionFlagUpdate(Prefix = "Option_YourModPrefix_", FieldFlags = true)]
        public static class Options
        {
            public static bool EnableYourCustomAnatomy;
        }
    }

That's the minimum necessary to restrict an anatomy on the basis of an option.

If you know the ID of option, you can pass it as the value of `<tag Name="Optional" Value="" />` and that option being enabled _should_ enable the given anatomy.

### Displaying an Exception Message

You might have a good reason for restricting an anatomy from selection, and want to make sure the reason is clear during character creation when the player has enabled your custom anatomy via an option.

You can specify two different messages to be displayed to the player at different points in character creation when they've selected an optionally restricted anatomy.

Adding the `ExceptionMessage` tag will display a message towards the top of the info panel of the Body Plan Module, after a couple of native messages, but before "Includes the following body part slots:".

Adding the `ExceptionSummary` tag will display a message at the bottom of the summary block of the Summary Module. There's a very limited amount of text that can go here, and most of the messages will be truncated in favour of displaying limb type counts.
    
    <object Name="YourModPrefix_UD_BodyPlan_Selection_NotOptional" Inherits="UD_BodyPlan_Selection_BaseExclusion">
        <tag Name="Anatomies" Value="YourModPrefix_CustomAnatomy" />
        <tag Name="Optional" Value="Option_YourModPrefix_EnableYourCustomAnatomy" />
        <tag Name="ExceptionMessage" Value="This body plan is {{B|wonky}}, and you'll fall over, a lot!" />
        <tag Name="ExceptionSummary" Value="{{B|Wonky, lots of falling!}}" />
    </object>

### Anatomies with Transformation Recipes

Some anatomies are available to the player without this mod by way of transformation recipes. There are two in the base game, and at least one modded one on the horizon at the time this guide is being written.

When you eat one the meals that these recipes creates, a few things happen under the hood to facilitate the process.

The first and most obvious is that
- you get a different body plan. (That part's handled by this mod already).
- So that you can't eat the meal a second time, your character is flagged as having eaten it.
- Both of the base game ones give you a mutation.
- You get a new tile and render string.
- You might get a color change (can't recall, but I made it possible anyway).

We can specify all of these either individually in their own `tag`s or as a large `xtag`.

`xtag`s are a special kind of tag that are parsed into a `Dictionary<string, string>`, labeled by whatever the remainder of the `xtag` element's name is _after_ "xtag". We'll cover them more in a later section about specifying which tile an anatomy is displayed as, but for now, it's really important that the `xtag` element be `xtagUD_BPS_Transformation`

Anything you leave out will simply not be used.

The targeted anatomy can be specified irectly in the `xtag`, too.
    
    <object Name="YourModPrefix_UD_BodyPlan_Selection_NotOptional" Inherits="UD_BodyPlan_Selection_BaseExclusion">
        <xtagUD_BPS_Transformation
            Anatomy="YourModPrefix_CustomAnatomy"
            RenderString="R"
            Tile="Creatures/yourmodprefix_custom_transformation.png"
            TileColor="&amp;B"
            DetailColor="b"
            Species="wonky"
            Property="Ate_YourModPrefix_WonkyBeans"
            Mutations="Lopsided" />
        <tag Name="Optional" />
        <tag Name="ExceptionMessage" Value="This body plan is {{B|wonky}}, and you'll fall over, a lot!" />
        <tag Name="ExceptionSummary" Value="{{B|Wonky, lots of falling!}}" />
    </object>

To achieve the same as the above with individual `tag`s instead, the blow is the correct format.

    <object Name="YourModPrefix_UD_BodyPlan_Selection_NotOptional" Inherits="UD_BodyPlan_Selection_BaseExclusion">
        <tag Name="Anatomy" Value="YourModPrefix_CustomAnatomy" />
        <tag Name="Transformation" />
        <tag Name="xFormRenderString" Value="R" />
        <tag Name="xFormTile" Value="Creatures/yourmodprefix_custom_transformation.png" />
        <tag Name="xFormTileColor" Value="&amp;B" />
        <tag Name="xFormDetailColor" Value="b" />
        <tag Name="xFormProperty" Value="Ate_YourModPrefix_WonkyBeans" />
        <tag Name="xFormSpecies" Value="wonky" />
        <tag Name="xFormMutations" Value="Lopsided" />
        <tag Name="Optional" />
        <tag Name="ExceptionMessage" Value="This body plan is {{B|wonky}}, and you'll fall over, a lot!" />
        <tag Name="ExceptionSummary" Value="{{B|Wonky, lots of falling!}}" />
    </object>

You can leave the `Optional` tag's value empty and the option provided by Choose Your Body Plan will automatically be used as the enabling option. The above is exactly what this mod uses to optionally restrict the two base game transformation recipe anatomies, and they specify this mod's recipe option explicitly, so if you wanted your own option, separate from the one provided by this mod, you can specify an option ID as `Optionally`'s tag value.

Transformation body plans are currently set to always use the supplied visual data to set the tile for the body plan in the Body Plan Module, but a future update might allow them to be specified/overridden explicity.

You may also specify "AlwaysEnabled" and the anatomy will be, as the value indicates, always enabled. This can be useful if all you want to do is display additional information alongside the body plan in the Body Plan Module.

Specifying "\*remove" as an xtag value will be interpreted by this mod as an instruction to null the associated value, allowing for existing entries to be deleted without causing errors.

## Specifying Body Plan Tile

Modded or otherwise, anatomies in the Body Plan Module get their display data the same way, so the only difference is whether or not merging into the blueprint the values are stored in is adding a new entry or modifying an existing one.

In the absence of any display data, the Body Plan Module will scan the game's blueprints for all objects that use the anatomy it wants to display, as well as objects whose blueprint name matches the anatomy name, and, finally, any objects that would get the anatomy if the object were animated.

This can be a little intense, and more than a handful of anatomies doing this can cause the game to hang for several seconds when first selecting `[+] New` at the start of character creation.

Choose Your Body Plan uses a wish, `UD_BPS anatomy tile tags with names` to output to `AnatomyTiles.xml` in the save directory, every blueprint that could be selected as the basis for display data, pre-formatted to use directly, but commented out individually.

You can use `UD_BPS anatomy tile tags with names @FullAnatomyName` using the same rules as for the `xtag` (spaces and hyphens replaced with `_`) to get a ready to use `AnatomyTiles.xml` with only entries for the specified anatomy. Simply remove the comment syntax from the desired line and paste the file into your mod directory.

The player's genotype/subtype selection will override the tile of whichever anatomy that combination's body object would otherwise have.

For example, if the player picked the nomad mutated human, the "Humanoid" anatomy's tile will be that of the nomad mutated human. If the player has a snapjaw genotype mod installed and the snapjaw genotype uses the "TailedBiped" anatomy, then that anatomy's tile be that of whichever snapjaw subtype they selected.

It is still worth specifying a tile for anatomies associated with a genotype/subtype, due to the mutable nature of almost every aspect of the game.

Note: Transformations are flipped by default, so that they reflect what the player's appearance will be.

### Modded Anatomies

Adding display data for your anatomy is even more straight forward than restricting its selection.

Again in `Data.xml` (although the mod uses `AnatomyTiles.xml`, so feel free), you want merge into the blueprint `UD_BodyPlan_Slection_AnatomyTiles`.

You then want an `xtag` with the following element name: `xtagUD_BDS_FullAnatomyName`. Start with `xtag`, followed by `UD_BDS_`, and then the full name of the anatomy (replace any spaces or hyphens with an `_`).

Finally, you can treat the `xtag` as though it's a `Render` `IPart` (`<part Name="Render" ... />`), but only for the visual data.

    <object Name="UD_BodyPlan_Slection_AnatomyTiles" Load="Merge">
        <xtagUD_BDS_YourModPrefix_CustomAnatomy
            Blueprint="YourModPrefix_WonkyBoi"
            Tile="Creatures/yourmodprefix_wonkyboi.png"
            RenderString="R"
            ColorString="&amp;B"
            TileColor="&amp;B"
            DetailColor="b"
            HFlip="true" />
    </object>

`Blueprint` is unused, but can be handy for remembering which blueprint it's based on.

### Existing Anatomies

Altering the display data of an anatomy external to your mod is almost as easy, but has a couple of additional steps.

Follow the instructions above, making sure to carefully specify the anatomy name as part of the `xtag` element. Any attribute of the `xtag` that you specify will overwrite the existing value if one exists, but you can't specify an empty string `""` without things breaking. You may specify "\*remove" in order to have an undesired value nulled.

If you're unsure which values have been specified, it might be worth specifying "\*remove" for every attribute you aren't using.

Below are the attributes that are used by Choose Your Body Plan (only string values are valid in xml, but the class that interprets the tags will convert them as necessary):
- string Tile,
- string RenderString
- string ColorString
- string TileColor
- char DetailColor
- bool HFlip

if you're targeting an anatomy from another mod, it would be wise to ensure your mod loads after it does so that your changes merge later and take precedence. Check out the Caves of Qud Wiki's page on [Mod Configuration](https://wiki.cavesofqud.com/wiki/Modding:Mod_Configuration) for instructions on how to achieve this.