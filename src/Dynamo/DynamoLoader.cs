﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dynamo.Search;
using Dynamo.Controls;
using System.Reflection;
using System.IO;
using Dynamo.Nodes;
using System.Windows.Controls;
using Dynamo.Utilities;
using System.Windows;
using System.Diagnostics;

namespace Dynamo.Utilties
{
    /// <summary>
    ///     Handles loading various types of elements into Dynamo at startup
    /// </summary>
    class DynamoLoader
    {
        /// <summary>
        ///     Enumerate local library assemblies and add them to DynamoController's
        ///     dictionaries and search.  
        /// </summary>
        /// <param name="searchViewModel">The searchViewModel to which the nodes will be added</param>
        /// <param name="controller">The DynamoController, whose dictionaries will be modified</param>
        /// <param name="bench">The bench where logging errors will be sent</param>
        internal static void LoadBuiltinTypes(SearchViewModel searchViewModel, DynamoController controller, dynBench bench)
        {
            Assembly dynamoAssembly = Assembly.GetExecutingAssembly();

            string location = Path.GetDirectoryName(dynamoAssembly.Location);

            #region determine assemblies to load

            var allLoadedAssembliesByPath = new Dictionary<string, Assembly>(
                AppDomain.CurrentDomain.GetAssemblies().ToDictionary(x => x.Location));

            var allLoadedAssemblies = new Dictionary<string, Assembly>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                allLoadedAssemblies[assembly.FullName] = assembly;

            string path = Path.Combine(location, "Packages");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            IEnumerable<string> allDynamoAssemblyPaths =
                Directory.GetFiles(location, "*.dll")
                         .Concat(Directory.GetFiles(
                             path,
                             "*.dll",
                             SearchOption.AllDirectories));

            var resolver = new ResolveEventHandler(delegate(object sender, ResolveEventArgs args)
            {
                Assembly result;
                allLoadedAssemblies.TryGetValue(args.Name, out result);
                return result;
            });

            AppDomain.CurrentDomain.AssemblyResolve += resolver;

            foreach (string assemblyPath in allDynamoAssemblyPaths)
            {
                if (allLoadedAssembliesByPath.ContainsKey(assemblyPath))
                    LoadNodesFromAssembly(allLoadedAssembliesByPath[assemblyPath], searchViewModel, controller, bench);
                else
                {
                    try
                    {
                        Assembly assembly = Assembly.LoadFrom(assemblyPath);
                        allLoadedAssemblies[assembly.GetName().Name] = assembly;
                        LoadNodesFromAssembly(assembly, searchViewModel, controller, bench);
                    }
                    catch
                    {
                    }
                }
            }

            AppDomain.CurrentDomain.AssemblyResolve -= resolver;

            //AppDomain.Unload(tempDomain);

            #endregion

        }

        /// <summary>
        ///     Determine if a Type is a node.  Used by LoadNodesFromAssembly to figure
        ///     out what nodes to load from other libraries (.dlls).
        /// </summary>
        /// <parameter>The type</parameter>
        /// <returns>True if the type is node.</returns>
        public static bool IsNodeSubType(Type t)
        {
            return t.Namespace == "Dynamo.Nodes" &&
                   !t.IsAbstract &&
                   t.IsSubclassOf(typeof(dynNode));
        }

        /// <summary>
        ///     Enumerate the types in an assembly and add them to DynamoController's
        ///     dictionaries and the search view model.  Internally catches exceptions and sends the error 
        ///     to the console.
        /// </summary>
        /// <param name="searchViewModel">The searchViewModel to which the nodes will be added</param>
        /// <param name="controller">The DynamoController, whose dictionaries will be modified</param>
        /// <param name="bench">The bench where logging errors will be sent</param>
        private static void LoadNodesFromAssembly(Assembly assembly, SearchViewModel searchViewModel, DynamoController controller, dynBench bench )
        {
            try
            {
                Type[] loadedTypes = assembly.GetTypes();

                foreach (Type t in loadedTypes)
                {
                    //only load types that are in the right namespace, are not abstract
                    //and have the elementname attribute
                    object[] attribs = t.GetCustomAttributes(typeof(NodeNameAttribute), false);

                    if (IsNodeSubType(t) && attribs.Length > 0)
                    {
                        searchViewModel.Add(t);
                        string typeName = (attribs[0] as NodeNameAttribute).Name;
                        var data = new TypeLoadData(assembly, t);
                        controller.builtinTypesByNickname.Add(typeName, data);
                        controller.builtinTypesByTypeName.Add(t.FullName, data);
                    }
                }
            }
            catch (Exception e)
            {
                bench.Log("Could not load types.");
                bench.Log(e);
                if (e is ReflectionTypeLoadException)
                {
                    var typeLoadException = e as ReflectionTypeLoadException;
                    Exception[] loaderExceptions = typeLoadException.LoaderExceptions;
                    bench.Log("Dll Load Exception: " + loaderExceptions[0]);
                    bench.Log(loaderExceptions[0].ToString());
                    if (loaderExceptions.Count() > 1)
                    {
                        bench.Log("Dll Load Exception: " + loaderExceptions[1]);
                        bench.Log(loaderExceptions[1].ToString());
                    }
                }
            }
        }

        /// <summary>
        ///     Setup the "Samples" sub-menu with contents of samples directory.
        /// </summary>
        /// <param name="bench">The bench where the UI will be loaded</param>
        public static void LoadSamplesMenu(dynBench bench)
        {
            string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string samplesPath = Path.Combine(directory, "samples");

            if (Directory.Exists(samplesPath))
            {
                string[] dirPaths = Directory.GetDirectories(samplesPath);
                string[] filePaths = Directory.GetFiles(samplesPath, "*.dyn");

                // handle top-level files
                if (filePaths.Any())
                {
                    foreach (string path in filePaths)
                    {
                        var item = new MenuItem
                        {
                            Header = Path.GetFileNameWithoutExtension(path),
                            Tag = path
                        };
                        item.Click += OpenSample_Click;
                        bench.SamplesMenu.Items.Add(item);
                    }
                }

                // handle top-level dirs, TODO - factor out to a seperate function, make recusive
                if (dirPaths.Any())
                {
                    foreach (string dirPath in dirPaths)
                    {
                        var dirItem = new MenuItem
                        {
                            Header = Path.GetFileName(dirPath),
                            Tag = Path.GetFileName(dirPath)
                        };

                        filePaths = Directory.GetFiles(dirPath, "*.dyn");
                        if (filePaths.Any())
                        {
                            foreach (string path in filePaths)
                            {
                                var item = new MenuItem
                                {
                                    Header = Path.GetFileNameWithoutExtension(path),
                                    Tag = path
                                };
                                item.Click += OpenSample_Click;
                                dirItem.Items.Add(item);
                            }
                        }
                        bench.SamplesMenu.Items.Add(dirItem);
                    }
                    return;
                }
            }
            //this.fileMenu.Items.Remove(this.samplesMenu);
        }

        /// <summary>
        ///     Callback for opening a sample.
        /// </summary>
        private static void OpenSample_Click(object sender, RoutedEventArgs e)
        {
            var path = (string)((MenuItem)sender).Tag;

            if (dynSettings.Bench.UILocked)
                dynSettings.Controller.QueueLoad(path);
            else
            {
                if (!dynSettings.Controller.ViewingHomespace)
                    dynSettings.Controller.ViewHomeWorkspace();

                dynSettings.Controller.OpenWorkbench(path);
            }
        }

        /// <summary>
        ///     Load Custom Nodes from the default directory - the "definitions"
        ///     directory where the executing assembly is located..
        /// </summary>
        /// <param name="bench">The logger is needed in order to tell how long it took.</param>
        public static void LoadCustomNodes(dynBench bench, CustomNodeLoader customNodeLoader, SearchViewModel searchViewModel)
        {

            // custom node loader
            var sw = new Stopwatch();
            sw.Start();

            customNodeLoader.UpdateSearchPath();

            var nn = customNodeLoader.GetNodeNameCategoryAndGuidList();

            // add nodes to search
            foreach (var pair in nn)
            {
                searchViewModel.Add(pair.Item1, pair.Item2, pair.Item3);
            }
            
            sw.Stop();
            bench.Log(string.Format("{0} ellapsed for loading definitions.", sw.Elapsed));

            // update search view
            searchViewModel.SearchAndUpdateResultsSync(searchViewModel.SearchText);

        }

    }
}
