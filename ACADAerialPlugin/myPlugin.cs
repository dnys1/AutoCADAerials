// (C) Copyright 2020 by  
//
using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Windows;
using Exception = Autodesk.AutoCAD.Runtime.Exception;

// This line is not mandatory, but improves loading performances
[assembly: ExtensionApplication(typeof(ACADAerialPlugin.MyPlugin))]

namespace ACADAerialPlugin
{

    // This class is instantiated by AutoCAD once and kept alive for the 
    // duration of the session. If you don't do any one time initialization 
    // then you should remove this class.
    public class MyPlugin : IExtensionApplication
    {
        private ContextMenuExtension contextMenu;

        void IExtensionApplication.Initialize()
        {
            // Add one time initialization here
            // One common scenario is to setup a callback function here that 
            // unmanaged code can call. 
            // To do this:
            // 1. Export a function from unmanaged code that takes a function
            //    pointer and stores the passed in value in a global variable.
            // 2. Call this exported function in this function passing delegate.
            // 3. When unmanaged code needs the services of this managed module
            //    you simply call acrxLoadApp() and by the time acrxLoadApp 
            //    returns  global function pointer is initialized to point to
            //    the C# delegate.
            // For more info see: 
            // http://msdn2.microsoft.com/en-US/library/5zwkzwf4(VS.80).aspx
            // http://msdn2.microsoft.com/en-us/library/44ey4b32(VS.80).aspx
            // http://msdn2.microsoft.com/en-US/library/7esfatk4.aspx
            // as well as some of the existing AutoCAD managed apps.

            // Initialize your plug-in application here
            AddContextMenu();
        }

        void IExtensionApplication.Terminate()
        {
            // Do plug-in application clean up here
            RemoveContextMenu();
        }

        private void AddContextMenu()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                contextMenu = new ContextMenuExtension();
                contextMenu.Title = "Aerials";

                // Insert Aerial button
                MenuItem insertAerialMI = new MenuItem("Insert Aerial");
                insertAerialMI.Click += InsertAerialCallback;

                // Update Aerial button
                MenuItem updateAerialMI = new MenuItem("Update Aerial");
                updateAerialMI.Click += UpdateAerialCallback;

                contextMenu.MenuItems.Add(insertAerialMI);
                contextMenu.MenuItems.Add(updateAerialMI);

                Application.AddDefaultContextMenuExtension(contextMenu);

            } catch (Exception ex)
            {
                ed.WriteMessage("Error adding context menu: " + ex.Message + '\n');
            }
        }

        private void RemoveContextMenu()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;

            try
            {
                if (contextMenu != null)
                {
                    Application.RemoveDefaultContextMenuExtension(contextMenu);
                    contextMenu = null;
                }
            } catch (Exception ex)
            {
                if (doc != null)
                {
                    doc.Editor.WriteMessage("Error unloading context menu: " + ex.Message + '\n');
                }
            }
        }

        private void InsertAerialCallback(object sender, EventArgs e)
        {
            Document activeDoc = Application.DocumentManager.MdiActiveDocument;
            using (DocumentLock docLock = activeDoc.LockDocument())
            {
                activeDoc.SendStringToExecute("INSERTAERIAL\n", false, false, false);
            }
        }

        private void UpdateAerialCallback(object sender, EventArgs e)
        {
            Document activeDoc = Application.DocumentManager.MdiActiveDocument;
            using (DocumentLock docLock = activeDoc.LockDocument())
            {
                activeDoc.SendStringToExecute("UPDATEAERIAL\n", false, false, false);
            }
        }
    }

}
