﻿using System;
using System.Linq;
using System.Reflection;
using NLog;
using Terminal_Interface;

namespace Terminal
{
	internal class Program
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		[STAThread]
		private static void Main()
		{
			logger.Trace("Initializing advanced JIT");
			AdvancedJIT.SetupJIT();

			logger.Trace("Loading GUI assembly");

			// WinForms GUI
			Assembly guiAssembly = Assembly.LoadFrom(@"C:\Users\ghahn\Documents\Dump\Hypertoken\trunk\HyperToken_WinForms_GUI\bin\Debug\HyperToken_WinForms_GUI.dll");

			// WPF Avalon GUI
			//Assembly guiAssembly = Assembly.LoadFrom(@"C:\Users\ghahn\Documents\Dump\Hypertoken\trunk\HyperToken_Avalon_GUI\bin\Debug\HyperToken_Avalon_GUI.dll");

			logger.Trace("Finding interfaces");

			// Save these so we can launch faster later
			String iterminalClass = null;
			String iinitableClass = null;
			foreach (Type guitype in guiAssembly.GetTypes().Where(guitype => guitype.IsPublic && !guitype.IsAbstract))
			{
				if (typeof(ITerminal).IsAssignableFrom(guitype))
				{
					iterminalClass = guitype.ToString();
					logger.Info("Found ITerminal implementation: {0}", iterminalClass);
				}

				if (typeof(IInitable).IsAssignableFrom(guitype))
				{
					iinitableClass = guitype.ToString();
					logger.Info("Found IInitable implementation: {0}", iinitableClass);
				}

				if (iterminalClass != null && iinitableClass != null)
					break;
			}

			if (iterminalClass == null)
			{
				logger.Fatal("ITerminal implementation was not found, shutting down");
				return;
			}

			logger.Debug("Beginning execution");

			if (iinitableClass != null)
			{
				logger.Trace("Running IInitable");
				IInitable initableClass = (IInitable)Activator.CreateInstance(guiAssembly.GetType(iinitableClass));
				initableClass.Init();
			}
			else
				logger.Warn("IInitable implementation was not found, skipping");

			logger.Trace("Attaching GUI to backend");
			ITerminal gui = (ITerminal)Activator.CreateInstance(guiAssembly.GetType(iterminalClass));
			Backend b = new Backend(gui);
			gui.SetBackend(b);

			logger.Trace("Running GUI");
			gui.Run();
			logger.Info("Application shutting down");

			//TODO Backend.Shutdown();
		}
	}
}