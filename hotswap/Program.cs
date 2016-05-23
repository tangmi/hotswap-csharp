using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace hotswap
{
    internal class Program
    {
        private static readonly string absdApp = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string absfDll = Path.Combine(absdApp, "module.dll");
        private static readonly string absfDllInUse = Path.Combine(absdApp, "module.inuse.dll");

        private const string stModuleName = "module.Module";
        private const string stMethodName = "Update";
        public static readonly Type[] methodParamTypes = {};
        static AssemblyLoader asm;

        private static void Main(string[] args)
        {
            Console.WriteLine("loading from {0}", absfDll);

            Debug.Assert(!File.Exists(absfDllInUse), "leftover state from a previous run! " + absfDllInUse + " still exists!");

            try
            {
                if (File.Exists(absfDll))
                {
                    File.Move(absfDll, absfDllInUse);
                }
                else if (!File.Exists(absfDllInUse))
                {
                    throw new DllNotFoundException();
                }

                while (Console.ReadKey().Key != ConsoleKey.Escape)
                {
                    if (File.Exists(absfDll))
                    {
                        File.Delete(absfDllInUse); // we currently hold this dll
                        File.Move(absfDll, absfDllInUse);
                    }

                    var domain = AppDomain.CreateDomain("hotswap");
                    try
                    {
                        var loader = (AssemblyLoader)domain.CreateInstanceAndUnwrap(
                            typeof(AssemblyLoader).Assembly.FullName,
                            typeof(AssemblyLoader).FullName);
                        loader.LoadAssembly(absfDllInUse);
                        var ret = loader.ExecuteStaticMethod(stModuleName, stMethodName);
                        Console.WriteLine(ret);
                    }
                    finally
                    {
                        AppDomain.Unload(domain);
                    }
                }
            }
            finally
            {
                File.Move(absfDllInUse, absfDll);
            }
        }
    }

    class AssemblyLoader : MarshalByRefObject
    {
        private Assembly assembly;

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void LoadAssembly(string path)
        {
            assembly = Assembly.Load(AssemblyName.GetAssemblyName(path));
        }

        public object ExecuteStaticMethod(string strModule, string methodName, params object[] parameters)
        {
            var type = assembly.GetType(strModule);

            var method = type.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Static);

            return method.Invoke(null, parameters);
        }
    }

}
