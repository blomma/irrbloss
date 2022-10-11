namespace Irrbloss;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Irrbloss.Extensions;
using Microsoft.Extensions.DependencyModel;

public class DependencyContextAssemblyCatalog
{
    private static readonly string IrrblossAssemblyName;

    private readonly DependencyContext _dependencyContext;

    static DependencyContextAssemblyCatalog()
    {
        var name = typeof(IrrblossExtensions).Assembly.GetName().Name;
        if (name == null)
        {
            throw new Exception();
        }
        IrrblossAssemblyName = name;
    }

    public DependencyContextAssemblyCatalog() : this(Assembly.GetEntryAssembly()) { }

    public DependencyContextAssemblyCatalog(Assembly? entryAssembly)
    {
        _dependencyContext = DependencyContext.Load(entryAssembly);
    }

    public virtual IReadOnlyCollection<Assembly> GetAssemblies()
    {
        var results = new HashSet<Assembly> { typeof(DependencyContextAssemblyCatalog).Assembly };

        foreach (var library in _dependencyContext.RuntimeLibraries)
        {
            if (!IsReferencingIrrbloss(library))
            {
                continue;
            }

            foreach (var assemblyName in library.GetDefaultAssemblyNames(_dependencyContext))
            {
                var assembly = SafeLoadAssembly(assemblyName);
                if (assembly != null)
                {
                    results.Add(assembly);
                }
            }
        }

        return results;
    }

    private static Assembly? SafeLoadAssembly(AssemblyName assemblyName)
    {
        try
        {
            return Assembly.Load(assemblyName);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool IsReferencingIrrbloss(Library library)
    {
        return library.Dependencies.Any(dependency => dependency.Name.Equals(IrrblossAssemblyName));
    }
}
