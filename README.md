# MS-ForceInput for ModSharp
Allows admins to force inputs on entities. (ent_fire)

## Required packages:
1. [ModSharp](https://github.com/Kxnrl/modsharp-public)
2. [LocalizerManager](https://github.com/Kxnrl/modsharp-public/tree/master/Sharp.Modules/LocalizerManager)
3. [AdminManager](https://github.com/Kxnrl/modsharp-public/tree/master/Sharp.Modules/AdminManager)

## Installation:
1. Install `LocalizerManager`
2. Compile or copy MS-ForceInput to `sharp/modules/MS-ForceInput` folger
3. Copy ForceInput.json to `sharp/locales` folger

## Commands:
`ms_entfire <name> <input> [parameter]` - to force inputs on entities(Need: `entfire` permission)<br>
`ms_forceinput <name> <input> [parameter]` - to force inputs on entities(Need: `entfire` permission)

## Arguments:
`name` - `!picker`, `!selfpawn`, targetname, classname<br>
`input` - required input for entity<br>
`parameter` - variable parameter for input

## Example:
`ms_forceinput math_counter SetHitMax 7` - sets all math_counters on the map to a maximum of 7
