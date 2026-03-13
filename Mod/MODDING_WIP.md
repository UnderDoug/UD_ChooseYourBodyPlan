# Modding
Some amount of care has been taken to expose poarts of Choose Your Body Plan to the game's xml files/data modding system.

Below are some guides on how to restrict an anatomy, including gating its selection behind an option, as well as a guide on altering how an anatomy is displayed in the Body Plan Module during character creation.

This mod should be compatible with the stable version of the game, the beta version, and sometimes even the lang branch. If you notice something not working, please reach out to me on github or @underdoug on the official Caves of Qud discord.

## A Word On Words
The decision to use the term "body plan" came after some discussion wherein it was pointed out that "anatomy" carries with it another, less technical, connotation. "Choose Your Anatomy", to the technically uninducted might imply the ability to choose the individual components of your anatomy, instead of one a series of pre-configured sets. "Choose Your Body Plan" serves to disambiguate.

### Technically Speaking
Skip skip to the next section if you're familiar with the game's `Anatomy`s, or if the inner workings are inconsequential to your goals with this guide.

In the sense that it's how the game defines it, an `Anatomy` is a sort of collection of `AnatomyPart`s that represents the configuration of a `GameObject`'s body.

An `AnatomyPart` is a data structure (and itself a pseudo-collection of `AnatomyPart`s) that contains a bunch of meta information about a "limb" in the context of being part of an `Anatomy`, including things such as the latirality and position of the resultant limb.

Where `AnatomyPart`s represent a "node" in a branching `Anatomy`, their counterpart, a `BodyPartType`, represents a sort of "blueprint" for the aptly named `BodyPart`, which is the individual limb itself and handles most of the limb's functionality in-game.

When an `Anatomy` is applied to a `GameObject`'s `Body`, reductively, it's `AnatomyPart`s are looped over (and their subparts, too) to determine which `BodyPart`s are supposed to be attached to which others, and the `AnatomyPart`'s `BodyPartType` is used to configure the resultant `BodyPart`.

A `BodyPartType` contails information about a limb like what material it's made of, what equipment slot it represents, and display information like its name and description.

Once an `Anatomy` has been applied, each of the `BodyPart`s is an amalgam of the data stored in the `AnatomyPart` and `BodyPartType` that created it.

### Nomenclature
When this guide uses the term "body plan" it is referring to an `Anatomy` object as well as the the final configuration that object takes when applied to one of the game's `GameObject`s. 

The majority of this guide is concerned primarily with the "configuration of limbs" component of an `Anatomy`, as opposed to the final, functional "anatomy of the `Body`" that results from its application, however the term "body plan" will be used to refer to either (with clarification if necessary).

The term "limb" is used in many places to refer to both an `AnatomyPart`/`BodyPartType` and to a `BodyPart`, with context typically determing which. The term is employed in the more broad sense of "subcomponent of a body".

The "body plan module" is the class that handles most of the underlying logic behind which anatomies to display, their order, which is the default one, and which one has been picked.

The "module window", "body plan window", and simply "window" all refer to the actual UI that the the Body Plan Module handles data for.

The "information panel" or "info panel" refers to the main display UI element of the module window that includes an image, a title, and a block of text.

A "summary block" is one of the smaller UI elements that appears in the summary window, a separate, base game module/window that summarises your character.

A "selection" or "choice" refers to an entry in the list stored by the body plan module and presented by the module window. It also refers to the underlying data structure of those entries (literally `AnatomyChoice`).

An "anatomy configuration" (`AnatomyConfiguration`) is one the members of an anatomy choice, and represents a bunch of meta data about the `Anatomy` member of the `AnatomyChoice` it's assigned to. It includes things such as additional display data, whether the anatomy should show up in the list or not (including which option controls that), and also includes information about a "transformation" that might be linked to the anatomy.

A "configuration object" or "configuration blueprint" is a game object blueprint that inherits from `UD_CYBP_BaseConfiguration` and represents the data side of an anatomy configuration.

A "transformation anatomy" (sometimes "recipe anatomy") refers to an anatomy that can be accessed in-game, during normal play, by way of a cooking recipe that applies a given anatomy to the player.

A "transformation" or "xForm" is one of the members of an anatomy configuration, and contains data about the transformation that is separate from which anatomy it applies. This optionally includes an alternate tile, tile/detail color, whether any mutations are bestowed, and a property that can be applied to the player to prevent subsequent consumption of the transformation recipe.

## Configuring Anatomy Selection
Choose Your Body Plan makes use of the base game's data bucket object blueprints and object blueprint xtags to make configuring anatomies a very straight forward process that doesn't require any scripting.

The first step is to create `Data.xml` somewhere in your mod's folder (if you don't already have one), and make a new object blueprint that inherits from `UD_CYBP_BaseConfiguration`.

You then want a `tag` element with a `Name` attribute of "Anatomy", and `Value` attribute of the name of your custom anatomy (which you've already made in a separate file).

Below is an example without any additional components:
    
    <object Name="YourModPrefix_UD_CYBP_Configuration" Inherits="UD_CYBP_BaseConfiguration">
        <tag Name="Anatomy" Value="YourModPrefix_CustomAnatomy" />
    </object>

On its own this doesn't do anything other than create an anatomy configuration and include it in your anatomy's selection.

Note: What you name your blueprint doesn't matter, only that it inherits from `UD_CYBP_BaseConfiguration`, however it's always worth prefixing the things you add to the game to minimise the potential for conflicts. Besides, it's the perfect opportunity to be vain, naming things after yourself, because there's a legitimate reason to do so.

### Displaying Text
The first most straight forward method of customizing how an anatomy apears in the body plan window (and, later, the summary window) is by adding some display text.

There are two main kinds of display text: symbols, and descriptions.

Symbols can be added to the anatomy selection name, and descriptions (as lines of text) can be added to the information panel. A description has two versions, one for the body plan window one for the summary block that appears in the summary window.

All three have their own fields that can be assigned to:
- `string DescriptionAddition`
- `string SummaryAddition`
- `List<KeyValuePair<char, string>> Symbols`

Adding the "DescriptionAddition" tag will display a message towards the top of the info panel, after a couple of messages included by the mod, but before "Includes the following body part slots:".

To add a warning that, for example, your custom anatomy will cause mobility issues, add the below to your anatomy confiiguration.

    <tag Name="DescriptionAddition" Value="This body plan is {{B|wonky}}, and you'll fall over, a lot!" />

Adding the "SummaryAddition" tag will display a message at the bottom of the summary block. There's a very limited amount of text that can go here, and most of the messages will be truncated in favour of displaying limb type counts, so important or integral information shouldn't be put here (use a symbol instead).

To add a shortened version of the message above, add the below, keeping in mind that it'll only show up for anatomies with very few limbs (which might be why it's wonky?):

    <tag Name="SummaryAddition" Value="{{B|Wonky, lots of falling!}}" />

Adding the "Symbols" tag will display a symbol after the name of the given anatomy and can be used as a short-hand representation of the other two tags. The `Symbols` tag takes Caves of Qud's usual string dictionary syntax which is `"key1::value1;;key2::value2"`. Separate `key` and `value` with `::`, and separate entries with `;;`.

To add a blue musical note symbol (uneven legs?), add the below tag:

    <tag Name="Symbols" Value="B::&#xE" />

If you added all three, you'll have something like this:

    <object Name="YourModPrefix_UD_CYBP_Configuration" Inherits="UD_CYBP_BaseConfiguration">
        <tag Name="Anatomy" Value="YourModPrefix_CustomAnatomy" />
        <tag Name="DescriptionAddition" Value="This body plan is {{B|wonky}}, and you'll fall over, a lot!" />
        <tag Name="SummaryAddition" Value="{{B|Wonky, lots of falling!}}" />
        <tag Name="Symbols" Value="B::&#xE" />
    </object>

It might be worth adding the above example (with your anatomy name swapped in), so that you can immediately see the effects.

### Restricting Anatomy Selection

If your anatomy is particularly niche, poses a marked increase in difficulty to play, is super overpowered because it's for a boss-like creature you've added, or any number of other reasons you might not want it available to the player, you can block your anatomy from appearing in the body plan window.

Restricting an anatomy is as simply as adding a "Restricted" tag to your configuration object.

Because it would be redundant to add display data to an anatomy that is restricted outright, we can drop the other tags (except for the "Anatomy" one), and we'll change the blueprint name while we're at it, to indicate the intent.

    <object Name="YourModPrefix_UD_CYBP_NotOptional" Inherits="UD_CYBP_BaseConfiguration">
        <tag Name="Anatomy" Value="YourModPrefix_CustomAnatomy" />
        <tag Name="Restricted" />
    </object>

That's all you need to do to outright restrict an anatomy. Nothing this mod does natively will include an anatomy that it finds this way without some additional tags.

If you have reason to, you can also restrict base game anatomies or anatomies added by other mods. Follow the exact same steps with one small exception.

Multiple anatomies can be restricted with the same tag with a small alteration, per below:
    
    <object Name="YourModPrefix_UD_CYBP_NotOptional" Inherits="UD_CYBP_BaseExclusion">
        <tag Name="Anatomies" Value="YourModPrefix_CustomAnatomy,SomeOtherMod_ModdedAnatomy,Bird,Fish" />
        <tag Name="Restricted" />
    </object>

Note: For the remainder of this guide I'm going to use an "Anatomies" tag, even when I'll only be putting one entry in (which _will_ work) because I want to have the ability to add additional anatomies without having to worry about changing the tag (even though this is a guide), and you might want to, too.

Simply changing the tag to "Anatomies" and using a comma delimited list will outright restrict each of the listed anatomies.

### Option-gating an Anatomy

You might want to restrict an anatomy, but only optionally. Modding _is_ largely about tailoring the gaming experience.

There are a couple of ways to do this.

The first is to add the "Optional" tag to the configuration blueprint and specify an option ID as the value, which I'll do below to the earlier example with the display text:

    <object Name="YourModPrefix_UD_CYBP_Configuration" Inherits="UD_CYBP_BaseConfiguration">
        <tag Name="Anatomies" Value="YourModPrefix_CustomAnatomy" />
        <tag Name="Optional" Value="Option_YourModPrefix_EnableYourCustomAnatomy" />
        <tag Name="DescriptionAddition" Value="This body plan is {{B|wonky}}, and you'll fall over, a lot!" />
        <tag Name="SummaryAddition" Value="{{B|Wonky, lots of falling!}}" />
        <tag Name="Symbols" Value="B::&#xE" />
    </object>

Note: With the "Optional" tag, "Restricted" is _implied_. You don't need to add the "Restricted" tag if you have the "Optional" one, but it may be worth including anyway to indicate intent.

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

If you know the ID of _any_ option, you can pass it as the value of `<tag Name="Optional" Value="" />` and that option being enabled _should_ enable the given anatomy.

Note: In the future I may look at adding simple predicate parsing so that both an option ID and a value can be specified to enable an anatomy when the supplied option ID matches the predicate value. ie "Option_YourModPrefix_EnableYourCustomAnatomy==2" would enable your custom anatomy if the option's value can be parsed as "2";

All together the below three examples can serve as something of a template for optionally restricting your custom anatomy on the basis that it's hard to play:

`Data.xml`

    <objects>
        <object Name="YourModPrefix_UD_CYBP_Configuration" Inherits="UD_CYBP_BaseConfiguration">
            <tag Name="Anatomies" Value="YourModPrefix_CustomAnatomy" />
            <tag Name="Optional" Value="Option_YourModPrefix_EnableYourCustomAnatomy" />
            <tag Name="DescriptionAddition" Value="This body plan is {{B|wonky}}, and you'll fall over, a lot!" />
            <tag Name="SummaryAddition" Value="{{B|Wonky, lots of falling!}}" />
            <tag Name="Symbols" Value="B::&#xE" />
        </object>
    </objects>

`Options.xml`

    <options>
        <option
            ID="Option_YourModPrefix_EnableYourCustomAnatomy"
            DisplayText="Enable body plans that are added by this mod."
            Category="Mod: Your Mod Title"
            Type="Checkbox"
            Default="No" />
    </options>

`Options.cs`

    using XRL;
    namespace YourModPrefix.Mod
    {
        [HasOptionFlagUpdate(Prefix = "Option_YourModPrefix_", FieldFlags = true)]
        public static class Options
        {
            public static bool EnableYourCustomAnatomy;
        }
    }

### Built-in Configurations

Difficult

Mechanical

Transformation

### Anatomies with Transformation Recipes

Some anatomies are available to the player without this mod by way of transformation recipes. There are two in the base game, and at least one modded one at the time this guide is being written.

When you eat one the meals that these recipes creates, a few things happen under the hood to facilitate the process.

The first and most obvious is that
- you get a different body plan. (That part's handled by this mod already),

Then,
- so that you can't eat the meal a second time, your character is flagged as having eaten it.
- Both of the base game ones give you a mutation.
- You get a new tile and render string.
- You might get a color change (can't recall, but I made it possible anyway).

We can specify all of these either individually in their own `tag`s or as a large `xtag`.

`xtag`s are a special kind of tag that are parsed into a `Dictionary<string, string>`, labeled by whatever the remainder of the `xtag` element's name is _after_ "xtag". We'll cover them more in a later section about specifying which tile an anatomy is displayed as, but for now, it's really important that the `xtag` element be `xtagUD_BPS_Transformation`

Anything you leave out will simply not be used.

Note: Passing `""` will breaks things. Instead specifying "\*remove" as an xtag attribute value will be interpreted by this mod as an instruction to null the associated value, allowing for existing entries to be deleted without causing errors.

The targeted anatomy can be specified directly in the `xtag`, too.
    
    <object Name="YourModPrefix_UD_CYBP_WonkyBoiConfig" Inherits="UD_CYBP_BaseConfiguration">
        <xtagUD_BPS_Transformation
            Anatomy="YourModPrefix_WonkyBoi"
            RenderString="R"
            Tile="Creatures/yourmodprefix_wonkyboi.png"
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

    <object Name="YourModPrefix_UD_CYBP_WonkyBoiConfig" Inherits="UD_CYBP_BaseConfiguration">
        <tag Name="Anatomy" Value="YourModPrefix_WonkyBoi" />
        <tag Name="Transformation" />
        <tag Name="xFormRenderString" Value="R" />
        <tag Name="xFormTile" Value="Creatures/yourmodprefix_wonkyboi.png" />
        <tag Name="xFormTileColor" Value="&amp;B" />
        <tag Name="xFormDetailColor" Value="b" />
        <tag Name="xFormProperty" Value="Ate_YourModPrefix_WonkyBeans" />
        <tag Name="xFormSpecies" Value="wonky" />
        <tag Name="xFormMutations" Value="Lopsided" />
        <tag Name="Optional" />
        <tag Name="ExceptionMessage" Value="This body plan is {{B|wonky}}, and you'll fall over, a lot!" />
        <tag Name="ExceptionSummary" Value="{{B|Wonky, lots of falling!}}" />
    </object>

You can leave the "Optional" tag's value empty and the option provided by Choose Your Body Plan will automatically be used as the enabling option. The above is exactly what this mod uses to optionally restrict the two base game transformation recipe anatomies, and they specify this mod's recipe option explicitly, so if you wanted your own option, separate from the one provided by this mod, you can specify an option ID as the "Optional" tag's value.

Transformation choices are currently set to always use the visual data supplied to the transformation to set the tile for the associated choice in the body plan window, but a future update might allow them to be specified/overridden explicity.

## Specifying Body Plan Tile

Modded or otherwise, anatomies in the body plan window get their display data the same way, so the only difference is whether or not merging into the AnatomyTiles blueprint is adding a new entry or modifying an existing one.

In the absence of any display data, the Body Plan Module will scan the game's blueprints for all objects that use the anatomy it wants to display, as well as objects whose blueprint name matches the anatomy name, and, finally, any objects that would get the anatomy if the object were animated.

This can be a little intense, and more than a handful of anatomies doing this can cause the game to hang for several seconds when first selecting `[+] New` at the start of character creation.

Choose Your Body Plan uses a wish, `UD_BPS anatomy tile tags with names`, to output to `AnatomyTiles.xml` in the save directory, every blueprint that could be selected as the basis for display data, pre-formatted to use directly, but commented out individually.

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
        <xtagUD_BDS_YourModPrefix_WonkyBoi
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
- `string Tile`
- `string RenderString`
- `string ColorString`
- `string TileColor`
- `char DetailColor`
- `bool HFlip`

if you're targeting an anatomy from another mod, it would be wise to ensure your mod loads after it does so that your changes merge later and take precedence. Check out the Caves of Qud Wiki's page on [Mod Configuration](https://wiki.cavesofqud.com/wiki/Modding:Mod_Configuration) for instructions on how to achieve this.