﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using ICSharpCode.SharpZipLib.Core;
using Xbim.Common.Logging;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;
using XbimGeometry.Interfaces;

namespace Xbim.Geometry.Profiler
{

    class Program
    {
        internal static readonly ILogger Logger = LoggerFactory.GetLogger();
        /// <summary>
        /// Converts an Ifc File to xBIM if it does not already exists, then converts the geoemtry to Xbim format and profiles the results
        /// </summary>
        /// <param name="args"> file[.ifc, xbim]</param>

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("No Ifc or xBim file specified");
                return;
            }
            var fileName = args[0];
            using (var model = GetModel(fileName))
            {
                if (model != null)
                {
                    var functionStack = new ConcurrentStack<Tuple<string,double>>();
                    ReportProgressDelegate progDelegate = delegate(int percentProgress, object userState)
                    {
                        if (percentProgress == -1)
                        {
                            functionStack.Push(new Tuple<string,double>(userState.ToString(), DateTime.Now.TimeOfDay.TotalMilliseconds));
                            Logger.InfoFormat("Entering - {0}", userState.ToString());
                        }
                    
                        else if (percentProgress == 101)
                        {
                            Tuple<string,double> func; 
                            if(functionStack.TryPop(out func))
                                Logger.InfoFormat("Complete - {0} in {1} ms", func.Item1, DateTime.Now.TimeOfDay.TotalMilliseconds-func.Item2);
                        }
                    };
                    var context = new Xbim3DModelContext(model);
                    context.CreateContext(progDelegate: progDelegate);
                    model.Close();
                }
            }
            Console.WriteLine("Press any key to exit");
            Console.Read();
        }

        private static XbimModel GetModel(string fileName)
        {
            XbimModel openModel = null;
            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                if (File.Exists(Path.ChangeExtension(fileName, "xbim"))) //use xBIM if exists
                    fileName = Path.ChangeExtension(fileName, "xbim");
                else if (File.Exists(Path.ChangeExtension(fileName, "ifc"))) //use ifc if exists
                    fileName = Path.ChangeExtension(fileName, "ifc");
                else if (File.Exists(Path.ChangeExtension(fileName, "ifczip"))) //use ifczip if exists
                    fileName = Path.ChangeExtension(fileName, "ifczip");
                else if (File.Exists(Path.ChangeExtension(fileName, "ifcxml"))) //use ifcxml if exists
                    fileName = Path.ChangeExtension(fileName, "ifcxml");
            }

            if (File.Exists(fileName))
            {
                extension = Path.GetExtension(fileName);
                if (String.Compare(extension, ".xbim", StringComparison.OrdinalIgnoreCase) == 0) //just open xbim
                {

                    try
                    {
                        var model = new XbimModel();
                        model.Open(fileName, XbimDBAccess.ReadWrite);
                        //delete any geometry
                        openModel = model;
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorFormat("Unable to open model {0}, {1}", fileName, e.Message);
                        Console.WriteLine(String.Format("Unable to open model {0}, {1}", fileName, e.Message));
                    }

                }
                else //we need to create the xBIM file
                {
                    var model = new XbimModel();
                    try
                    {
                        model.CreateFrom(fileName, null, null, true);
                        openModel = model;
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorFormat("Unable to open model {0}, {1}", fileName, e.Message);
                        Console.WriteLine(String.Format("Unable to open model {0}, {1}", fileName, e.Message));
                    }

                }
            }
            return openModel;
        }
    }
}
