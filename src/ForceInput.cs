using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Modules.LocalizerManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Definition;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace MS_ForceInput
{
    public class ForceInput : IModSharpModule
    {
        public string DisplayName => "ForceInput";
        public string DisplayAuthor => "DarkerZ[RUS]";
        public ForceInput(ISharedSystem sharedSystem, string dllPath, string sharpPath, Version version, IConfiguration coreConfiguration, bool hotReload)
        {
            _modules = sharedSystem.GetSharpModuleManager();
            _entities = sharedSystem.GetEntityManager();
            _logger = sharedSystem.GetLoggerFactory().CreateLogger<ForceInput>();
            _physicsquery = sharedSystem.GetPhysicsQueryManager();
        }
        private readonly ISharpModuleManager _modules;
        private readonly IEntityManager _entities;
        private readonly ILogger<ForceInput> _logger;
        private readonly IPhysicsQueryManager _physicsquery;

        private IModSharpModuleInterface<ILocalizerManager>? _localizer;
        private static IModSharpModuleInterface<IAdminManager>? _adminManager;
        private bool _AMInit = false;

        public bool Init() => true;

        private void InitializePermissions()
        {
            if (_adminManager?.Instance is not { } adminManager || _AMInit) return;

            try
            {
                var registry = adminManager.GetCommandRegistry(DisplayName);

                registry.RegisterPermissions(["forceinput:entfire"]);
                registry.RegisterAdminCommand("entfire", OnEntFireCallback, ["forceinput:entfire"]);
                registry.RegisterAdminCommand("forceinput", OnEntFireCallback, ["forceinput:entfire"]);

                _AMInit = true;
            }
            catch (InvalidOperationException) { }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to initialize admin permissions.");
            }
        }

        public void PostInit()
        {
            TryResolveAdminManager();
        }

        public void OnLibraryConnected(string name)
        {
            if (name.Equals("Sharp.Modules.AdminManager", StringComparison.OrdinalIgnoreCase)) TryResolveAdminManager();
        }

        public void OnAllModulesLoaded()
        {
            GetLocalizer()?.LoadLocaleFile("ForceInput");
            TryResolveAdminManager();
        }

        public void Shutdown() { }

        private void OnEntFireCallback(IGameClient? client, StringCommand command)
        {
            if (client == null) return;
            var player = client.GetPlayerController();
            if (player != null)
            {
                if (command.ArgCount < 2)
                {
                    if (GetLocalizer() is { } lm)
                    {
                        var localizer = lm.For(client);
                        player.Print(command.ChatTrigger ? HudPrintChannel.Chat : HudPrintChannel.Console, $" {ChatColor.Blue}[{ChatColor.Green}ForceInput{ChatColor.Blue}]{ChatColor.White} {localizer.Text("ForceInput_usage")}");
                    }
                    return;
                }

                int iFoundEnts = 0;
                string sEntName = command.GetArg(1);
                string sInput = command.GetArg(2);
                string sParameter = command.ArgCount >= 3 ? command.GetArg(3) : "";

                if (string.Equals(sEntName, "!picker", StringComparison.OrdinalIgnoreCase))
                {
                    if (player.GetPawn() is { } pawn)
                    {
                        var start = pawn.GetEyePosition();
                        var end = start + (pawn.GetEyeAngles().AnglesToVectorForward() * 4096.0f);
                        var attribute = RnQueryShapeAttr.Bullets();
                        attribute.SetEntityToIgnore(pawn, 0);
                        var traceResult = _physicsquery.TraceLine(start, end, attribute);

                        if (traceResult.DidHit())
                        {
                            var hitEntity = _entities.MakeEntityFromPointer<IBaseEntity>(traceResult.Entity);
                            if (hitEntity.Classname.Equals("worldent", StringComparison.OrdinalIgnoreCase))
                            {
                                if (GetLocalizer() is { } lm)
                                {
                                    var localizer = lm.For(client);
                                    player.Print(command.ChatTrigger ? HudPrintChannel.Chat : HudPrintChannel.Console, $" {ChatColor.Blue}[{ChatColor.Green}ForceInput{ChatColor.Blue}]{ChatColor.White} {localizer.Text("ForceInput_AimNotFound")}");
                                }
                                return;
                            } else
                            {
                                //Console.WriteLine($" Hit entity!!! entity classname: {hitEntity.Classname} hit position: {traceResult.EndPosition} fraction: {traceResult.Fraction}");
                                hitEntity.AcceptInput(sInput, pawn, pawn, sParameter);
                                iFoundEnts++;
                            }
                        }
                        else
                        {
                            if (GetLocalizer() is { } lm)
                            {
                                var localizer = lm.For(client);
                                player.Print(command.ChatTrigger ? HudPrintChannel.Chat : HudPrintChannel.Console, $" {ChatColor.Blue}[{ChatColor.Green}ForceInput{ChatColor.Blue}]{ChatColor.White} {localizer.Text("ForceInput_AimNotFound")}");
                            }
                            return;
                        }
                    }
                }
                if (string.Equals(sEntName, "!selfpawn", StringComparison.OrdinalIgnoreCase))
                {
                    if (player.GetPlayerPawn() is { } pawn)
                    {
                        pawn.AcceptInput(sInput, pawn, pawn, sParameter);
                        iFoundEnts++;
                    }
                }

                if (iFoundEnts == 0)
                {
                    foreach (var entity in GetEntitiesByName(sEntName))
                    {
                        if (entity.IsValid())
                        {
                            entity.AcceptInput(sInput, player.GetPlayerPawn(), player.GetPlayerPawn(), sParameter);
                            iFoundEnts++;
                        }
                    }
                }

                if (iFoundEnts == 0)
                {
                    foreach (var entity in GetEntitiesByClassname(sEntName.ToLower()))
                    {
                        if (entity.IsValid())
                        {
                            //Console.WriteLine($"Index: {entity.Index} Name: {entity.Name}");
                            entity.AcceptInput(sInput, player.GetPlayerPawn(), player.GetPlayerPawn(), sParameter);
                            iFoundEnts++;
                        }
                    }
                }

                if (iFoundEnts == 0)
                {
                    if (GetLocalizer() is { } lm)
                    {
                        var localizer = lm.For(client);
                        player.Print(command.ChatTrigger ? HudPrintChannel.Chat : HudPrintChannel.Console, $" {ChatColor.Blue}[{ChatColor.Green}ForceInput{ChatColor.Blue}]{ChatColor.White} {localizer.Text("ForceInput_EntitiesNotFound")}");
                    }
                    PrintToServer(player.PlayerName, $"Entities not found", command.ArgString);
                }
                else
                {
                    if (GetLocalizer() is { } lm)
                    {
                        var localizer = lm.For(client);
                        player.Print(command.ChatTrigger ? HudPrintChannel.Chat : HudPrintChannel.Console, $" {ChatColor.Blue}[{ChatColor.Green}ForceInput{ChatColor.Blue}]{ChatColor.White} {localizer.Text("ForceInput_EntitiesFound", iFoundEnts)}");
                    }
                    PrintToServer(player.PlayerName, $"Input successful on {iFoundEnts} entities", command.ArgString);
                }
            }
        }

        private void PrintToServer(string sAdminName, string sMessage, string sArgs)
        {
            string sLogMessage = $"{sAdminName} executed the command with arguments: {sArgs}. Result: {sMessage}";
            Console.ForegroundColor = (ConsoleColor)8;
            Console.Write("[");
            Console.ForegroundColor = (ConsoleColor)6;
            Console.Write("ForceInput");
            Console.ForegroundColor = (ConsoleColor)8;
            Console.Write("] ");
            Console.ForegroundColor = (ConsoleColor)1;
            Console.WriteLine(sLogMessage);
            Console.ResetColor();
            _logger.LogInformation(sLogMessage);
        }

        private ILocalizerManager? GetLocalizer()
        {
            if (_localizer?.Instance is null)
            {
                _localizer = _modules.GetOptionalSharpModuleInterface<ILocalizerManager>(ILocalizerManager.Identity);
            }
            return _localizer?.Instance;
        }

        private void TryResolveAdminManager()
        {
            if (_adminManager?.Instance is not null) return;

            _adminManager = _modules!.GetOptionalSharpModuleInterface<IAdminManager>(IAdminManager.Identity);

            if (_adminManager?.Instance is null)
            {
                _logger.LogWarning("AdminManager is not installed. Admin commands will not work.");
                return;
            }

            InitializePermissions();
        }

        public IEnumerable<IBaseEntity> GetEntitiesByName(string name, Func<IBaseEntity, bool>? predicate = null)
        {
            IBaseEntity? entity = null;
            while ((entity = _entities.FindEntityByName(entity, name)) != null)
            {
                if (predicate is not null && !predicate(entity)) continue;
                yield return entity;
            }
        }

        public IEnumerable<IBaseEntity> GetEntitiesByClassname(string classname, Func<IBaseEntity, bool>? predicate = null)
        {
            IBaseEntity? entity = null;
            while ((entity = _entities.FindEntityByClassname(entity, classname)) != null)
            {
                if (predicate is not null && !predicate(entity)) continue;
                yield return entity;
            }
        }
    }
}
