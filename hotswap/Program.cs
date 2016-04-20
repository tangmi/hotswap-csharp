using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace hotswap
{
    internal class Program
    {
        private static readonly string absd_app = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string absf_dll = Path.Combine(absd_app, "module.dll");
        private static readonly string absf_dll_in_use = Path.Combine(absd_app, "module.inuse.dll");

        private static void Main(string[] args)
        {
            Console.WriteLine("loading from {0}", absf_dll);

            if (File.Exists(absf_dll))
            {
                File.Move(absf_dll, absf_dll_in_use);
            } else if (!File.Exists(absf_dll_in_use))
            {
                throw new DllNotFoundException();
            }

            while (Console.ReadKey().Key != ConsoleKey.Escape)
            {
                if (File.Exists(absf_dll))
                {
                    File.Delete(absf_dll_in_use); // we currently hold this dll
                    File.Move(absf_dll, absf_dll_in_use);
                }

                var new_domain = AppDomain.CreateDomain( // lazily create thgis
                    AppDomain.CurrentDomain.FriendlyName,
                    AppDomain.CurrentDomain.Evidence);
//                register_assembly_resolve(new_domain);
                try
                {
                    var assembly = Assembly.LoadFrom(absf_dll_in_use);

                    var ret =
                        assembly
                            .GetType("module.Module")
                            .GetMethod("Update", BindingFlags.Public | BindingFlags.Static)
                            .Invoke(null, null);

                    Console.WriteLine(ret);
                }
                finally
                {
                    AppDomain.Unload(new_domain);
                }
            }

            File.Move(absf_dll_in_use, absf_dll);

            var type2 = load_from_app_domain("module.Module");
//            Debug.Assert(type2 == null); // does it matter that it's still loaded?
        }

//        static readonly string absd_app = Path.Combine(
//            Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase));

//        private static void register_assembly_resolve(AppDomain domain) => domain.AssemblyResolve += resolve;
//
//        private static Assembly resolve(object sender, ResolveEventArgs args)
//        {
//            var absf_asm_path = Path.Combine(absd_app, new AssemblyName(args.Name).Name + ".dll");
//            if (File.Exists(absf_asm_path))
//            {
//                return Assembly.LoadFrom(absf_asm_path);
//            }
//            return null;
//        }

        private static Type load_from_app_domain(string className) => AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(assembly => assembly.GetType(className))
            .SingleOrDefault(type => type != null); // throws exception if there's more than one??
    }
}