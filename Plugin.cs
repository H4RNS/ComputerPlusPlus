using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using ComputerPlusPlus.Screens;
using ComputerPlusPlus.Tools;

namespace ComputerPlusPlus
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    [BepInIncompatibility("tonimacaroni.computerinterface")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public ConfigFile ConfigPath;

        void Awake()
        {
            Instance = this;
            Logging.Init();
            ConfigPath = new ConfigFile(Paths.ConfigPath + "\\" + PluginInfo.Name + ".cfg", true);
        }

        void OnEnable()
        {
            HarmonyPatches.ApplyHarmonyPatches();
            if (ComputerManager.Instance != null)
                ComputerManager.Instance.enabled = true;
        }

        void OnDisable()
        {
            HarmonyPatches.RemoveHarmonyPatches();
            if (ComputerManager.Instance != null)
                ComputerManager.Instance.enabled = false;
        }

        public void Setup()
        {
            // MINIMAL FIX: avoid double-creation / duplicate registration if Setup runs twice
            if (ComputerManager.Instance != null)
            {
                Logging.Info("ComputerManager already exists — skipping Setup to avoid duplicate registrations.");
                return;
            }

            ComputerManager.Instance = gameObject.AddComponent<ComputerManager>();
            ComputerManager.Instance.RegisterScreen(new RoomScreen());
            ComputerManager.Instance.RegisterScreen(new NameScreen());
            ComputerManager.Instance.RegisterScreen(new ColorScreen());
            ComputerManager.Instance.RegisterScreen(new ModsScreen());
            ComputerManager.Instance.RegisterScreen(new AutoJoinScreen());
            ComputerManager.Instance.RegisterScreen(new ThemeScreen());
            ComputerManager.Instance.RegisterScreen(new TurnScreen());
            ComputerManager.Instance.RegisterScreen(new VoiceScreen());
            ComputerManager.Instance.RegisterScreen(new QueueScreen());
            ComputerManager.Instance.RegisterScreen(new GroupScreen());
            ComputerManager.Instance.RegisterScreen(new ItemsScreen());

            var assemblies = GetAssemblies();
            if (assemblies != null)
            {
                foreach (var assembly in assemblies)
                {
                    //exclude the executing assembly
                    if (assembly == typeof(Plugin).Assembly)
                        continue;
                    foreach (var type in GetTypes(assembly))
                    {
                        foreach (var iface in GetInterfaces(type))
                        {
                            try
                            {
                                if (iface.FullName == typeof(IScreen).FullName)
                                {
                                    var screen = Activator.CreateInstance(type) as IScreen;
                                    // MINIMAL FIX: guard against null (bad types) before using or registering
                                    if (screen == null)
                                    {
                                        Logging.Debug($"Skipping null screen instance for type {type.FullName}");
                                        continue;
                                    }

                                    try
                                    {
                                        Logging.Debug($"Registering Screen: {screen.Title} from type {type.Name}");
                                    }
                                    catch (Exception ex)
                                    {
                                        // If accessing Title throws, log and continue to registering safely
                                        Logging.Exception(ex);
                                    }

                                    try
                                    {
                                        ComputerManager.Instance.RegisterScreen(screen);
                                    }
                                    catch (Exception ex)
                                    {
                                        // If registration throws (e.g., duplicate key inside RegisterScreen), log and continue
                                        Logging.Exception(ex);
                                    }
                                }
                            }
                            catch (Exception e) { Logging.Exception(e); }
                        }
                    }
                }
            }

            ComputerManager.Instance.RegisterScreen(new VersionScreen());

            // MINIMAL FIX: guard Initialize so exceptions inside it don't crash startup
            try
            {
                ComputerManager.Instance.Initialize();
            }
            catch (Exception initEx)
            {
                Logging.Exception(initEx);
                Logging.Fatal("ComputerManager.Initialize() threw an exception — initialization may be incomplete.");
            }
        }

        public Assembly[] GetAssemblies()
        {
            try
            {
                return AppDomain.CurrentDomain.GetAssemblies();
            }
            catch (Exception e)
            {
                Logging.Fatal("Error getting assemblies");
                Logging.Exception(e);
                return null;
            }
        }

        Type[] GetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch
            {
                Logging.Fatal($"Error getting types from assembly {assembly.FullName}");
                return new Type[] { };
            }
        }

        Type[] GetInterfaces(Type type)
        {
            try
            {
                return type.GetInterfaces();
            }
            catch
            {
                Logging.Fatal($"Error getting interfaces from type {type.FullName}");
                return new Type[] { };
            }
        }
    }
}
