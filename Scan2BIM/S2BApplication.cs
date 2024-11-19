using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.IO;
using Autodesk.Revit.Creation;
using Autodesk.Revit.DB.Structure;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace Scan2BIM
{

    public class S2BApplication : IExternalApplication
    {
        

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            RibbonPanel panel = RibbonPanel(application);
            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;

            if (panel.AddItem(new PushButtonData("Modeler","Modeler", thisAssemblyPath, "Scan2BIM.S2BForm"))
                is PushButton button)
            {
                button.ToolTip = "Modeler";
                Uri uri = new Uri(Path.Combine(Path.GetDirectoryName(thisAssemblyPath), "Resources", "Icon2.png"));
                BitmapImage bitmap = new BitmapImage(uri);
                button.LargeImage = bitmap;
            }

            return Result.Succeeded;
        }

        public RibbonPanel RibbonPanel(UIControlledApplication a)
        {
            string tab = "Scan-to-BIM";
            RibbonPanel ribbonPanel = null;

            try
            {
                a.CreateRibbonTab(tab);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            try
            {
                a.CreateRibbonPanel(tab, "Scan-to-BIM");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            List<RibbonPanel> panels = a.GetRibbonPanels(tab);
            foreach (RibbonPanel p in panels.Where(p => p.Name == "Scan-to-BIM"))
            {
                ribbonPanel = p;
            }

            return ribbonPanel;
        }
    }



}
