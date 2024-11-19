using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.IO;
using Autodesk.Revit.Attributes;

namespace Scan2BIM
{
    public class Utility
    {
        public static Result ExecuteBIMGeneration(Document doc, string filePath)
        {

            string path = filePath;


            //Read all the elements' coordinates from the file
            string[] Lines = File.ReadAllLines(path);

            float floor_z = 0;
            float ceiling_z = 0;

            List<XYZ> floorPoints = new List<XYZ>();
            List<XYZ> ceilingPoints = new List<XYZ>();
            List<List<XYZ>> wallPointsCollections = new List<List<XYZ>>();
            List<List<float>> doorParams = new List<List<float>>();
            List<List<float>> windowParams = new List<List<float>>();

            List<Wall> wallElements = new List<Wall>();

            float Meter2Feet = 3.28084f;

            //Get the floor/ceiling/wall elevation and coordinates
            for (int i = 0; i < Lines.Length; i++)
            {
                string[] lineSegment = Lines[i].Split(':');
                if (lineSegment[0] == "Floor")
                {
                    string[] FloorCoorSegment = lineSegment[1].Split(';');
                    floor_z = float.Parse(FloorCoorSegment[2]) * Meter2Feet;

                    int lengthOfPoint = FloorCoorSegment.Length / 3;
                    for (int j = 0; j < lengthOfPoint; j++)
                    {
                        float x = float.Parse(FloorCoorSegment[j * 3]) * Meter2Feet;
                        float y = float.Parse(FloorCoorSegment[j * 3 + 1]) * Meter2Feet;
                        float z = float.Parse(FloorCoorSegment[j * 3 + 2]) * Meter2Feet;
                        XYZ point = new XYZ(x, y, z);
                        floorPoints.Add(point);

                    }
                }

                if (lineSegment[0] == "Ceiling")
                {
                    string[] CeilingCoorSegment = lineSegment[1].Split(';');
                    ceiling_z = float.Parse(CeilingCoorSegment[2]) * Meter2Feet;

                    int lengthOfPoint = CeilingCoorSegment.Length / 3;
                    for (int j = 0; j < lengthOfPoint; j++)
                    {
                        float x = float.Parse(CeilingCoorSegment[j * 3]) * Meter2Feet;
                        float y = float.Parse(CeilingCoorSegment[j * 3 + 1]) * Meter2Feet;
                        float z = float.Parse(CeilingCoorSegment[j * 3 + 2]) * Meter2Feet;
                        XYZ point = new XYZ(x, y, z);
                        ceilingPoints.Add(point);

                    }
                }

                List<XYZ> wallPoints = new List<XYZ>();
                if (lineSegment[0] == "Wall")
                {
                    string[] WallCoorSegment = lineSegment[1].Split(';');
                    int lengthOfPoint = WallCoorSegment.Length / 3;
                    for (int j = 0; j < lengthOfPoint; j++)
                    {
                        float x = float.Parse(WallCoorSegment[j * 3]) * Meter2Feet;
                        float y = float.Parse(WallCoorSegment[j * 3 + 1]) * Meter2Feet;
                        float z = float.Parse(WallCoorSegment[j * 3 + 2]) * Meter2Feet;
                        XYZ point = new XYZ(x, y, z);
                        wallPoints.Add(point);

                    }
                    wallPointsCollections.Add(wallPoints);
                }

                List<float> doorParam = new List<float>();
                if (lineSegment[0] == "DoorParam")
                {
                    string[] DoorParam = lineSegment[1].Split(';');
                    for (int j = 0; j < DoorParam.Length - 1; j++)
                    {
                        float element = float.Parse(DoorParam[j]);
                        //TaskDialog.Show("Title", element.ToString());
                        doorParam.Add(element);
                    }
                    doorParams.Add(doorParam);
                }

                List<float> windowParam = new List<float>();
                if (lineSegment[0] == "WindowParam")
                {
                    string[] WindowParam = lineSegment[1].Split(';');
                    for (int j = 0; j < WindowParam.Length - 1; j++)
                    {
                        float element = float.Parse(WindowParam[j]);
                        //TaskDialog.Show("Title", element.ToString());
                        windowParam.Add(element);
                    }
                    windowParams.Add(windowParam);
                }


            }
            //TaskDialog.Show("Title", wallPointsCollections.Count.ToString());


            // Get application creation object
            Autodesk.Revit.Creation.Application appCreation = doc.Application.Create;

            // Start a new transaction
            using (Transaction transaction = new Transaction(doc, "Generate BIM"))
            {
                transaction.Start();

                try
                {
                    //Create BIM level
                    Level floorLevel1 = Level.Create(doc, floor_z);
                    floorLevel1.Name = "1/F";

                    Level floorLevel2 = Level.Create(doc, ceiling_z);
                    floorLevel2.Name = "2/F";

                    #region Create wall
                    // Create a new wall type
                    // Find an existing wall type to duplicate
                    FilteredElementCollector collector = new FilteredElementCollector(doc);
                    collector.OfClass(typeof(WallType));


                    WallType existingWallType = collector.Cast<WallType>()
                        .FirstOrDefault(wallType => wallType.FamilyName == "Basic Wall");

                    // Duplicate the existing basic wall type
                    WallType newWallType = existingWallType.Duplicate("Custom Basic Wall") as WallType;
                    //TaskDialog.Show("Title", newWallType.Name);

                    //Define the thickness of the wall
                    // Remove the existing compound structure
                    CompoundStructure compoundStructure = newWallType.GetCompoundStructure();
                    IList<CompoundStructureLayer> layers = compoundStructure.GetLayers();
                    layers.Clear();

                    string targetMaterialName = "Concrete";

                    // Find the material with the specified name
                    FilteredElementCollector materialCollector = new FilteredElementCollector(doc).OfClass(typeof(Material));
                    Material targetMaterial = materialCollector.Cast<Material>().FirstOrDefault(m => m.Name.Equals(targetMaterialName, StringComparison.OrdinalIgnoreCase));
                    //TaskDialog.Show("Title", targetMaterial.Name);

                    // Get the Material ID
                    ElementId materialId = targetMaterial.Id;

                    // Create a new single layer with a 10 mm thickness
                    double newLayerThickness = 0.01 * Meter2Feet; // Thickness in meters (10 mm)
                    CompoundStructureLayer newLayer = new CompoundStructureLayer(newLayerThickness, MaterialFunctionAssignment.Structure, materialId);

                    layers.Add(newLayer);
                    compoundStructure.SetLayers(layers);

                    newWallType.SetCompoundStructure(compoundStructure);


                    for (int i = 0; i < wallPointsCollections.Count; i++)
                    {
                        XYZ first = wallPointsCollections[i][0];
                        XYZ second = wallPointsCollections[i][1];
                        XYZ third = wallPointsCollections[i][2];
                        XYZ fourth = wallPointsCollections[i][3];
                        //TaskDialog.Show("Title",first+";"+second);

                        IList<Curve> profile = new List<Curve>();



                        profile.Add(Line.CreateBound(first, second));
                        profile.Add(Line.CreateBound(second, third));
                        profile.Add(Line.CreateBound(third, fourth));
                        profile.Add(Line.CreateBound(fourth, first));


                        // Create the wall
                        Wall wall = Wall.Create(doc, profile, newWallType.Id, floorLevel1.Id, false);

                        // Get the parameter for the wall's top constraint.
                        Parameter topConstraintParameter = wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
                        topConstraintParameter.Set(floorLevel2.Id);

                        wallElements.Add(wall);
                    }

                    #endregion

                    #region create floor
                    
                    collector = new FilteredElementCollector(doc);

                    // Get a floor type for floor creation
                    collector.OfClass(typeof(FloorType));


                    FloorType existingFloorType = collector.Cast<FloorType>()
                       .FirstOrDefault(floorType => floorType.FamilyName == "Foundation Slab");

                    // Duplicate the existing basic wall type
                    FloorType newFloorType = existingFloorType.Duplicate("Custom Basic Floor") as FloorType;
                    //TaskDialog.Show("Title", newWallType.Name);

                    //Define the thickness of the wall
                    // Remove the existing compound structure
                    CompoundStructure compoundStructureFloor = newFloorType.GetCompoundStructure();
                    IList<CompoundStructureLayer> layersFloor = compoundStructureFloor.GetLayers();
                    layersFloor.Clear();


                    layersFloor.Add(newLayer);
                    compoundStructureFloor.SetLayers(layersFloor);

                    newFloorType.SetCompoundStructure(compoundStructureFloor);

                    // Create a CurveArray from the coordinates.
                    CurveLoop curveArray = new CurveLoop();
                    for (int i = 0; i < floorPoints.Count; i++)
                    {
                        XYZ startPoint = floorPoints[i];
                        XYZ endPoint = floorPoints[(i + 1) % floorPoints.Count];
                        Line line = Line.CreateBound(startPoint, endPoint);
                        curveArray.Append(line);
                    }

                    // Set the level for the floor (assuming you have a level named "Level 1").
                    Level floorlevel = floorLevel1;

                    // The normal vector (0,0,1) that must be perpendicular to the profile.
                    XYZ normal = XYZ.BasisZ;

                    Floor floor = Floor.Create(doc, new List<CurveLoop> { curveArray }, newFloorType.Id, floorlevel.Id);
                    
                    #endregion

                    #region create ceiling
                    
                    collector = new FilteredElementCollector(doc);

                    // Get a floor type for floor creation
                    collector.OfClass(typeof(CeilingType));


                    CeilingType existingCeilingType = collector.Cast<CeilingType>()
                       .FirstOrDefault(ceilingType => ceilingType.FamilyName == "Basic Ceiling");

                    // Duplicate the existing basic wall type
                    CeilingType newCeilingType = existingCeilingType.Duplicate("Custom Basic Ceiling") as CeilingType;
                    //TaskDialog.Show("Title", newWallType.Name);

                    //Define the thickness of the wall
                    // Remove the existing compound structure
                    CompoundStructure compoundStructureCeiling = newFloorType.GetCompoundStructure();
                    IList<CompoundStructureLayer> layersCeiling = compoundStructureFloor.GetLayers();
                    layersCeiling.Clear();


                    layersCeiling.Add(newLayer);
                    compoundStructureCeiling.SetLayers(layersCeiling);

                    newCeilingType.SetCompoundStructure(compoundStructureCeiling);

                    // Create a CurveArray from the coordinates.
                    CurveLoop curveArrayCeiling = new CurveLoop();
                    for (int i = 0; i < ceilingPoints.Count; i++)
                    {
                        XYZ startPoint = ceilingPoints[i];
                        XYZ endPoint = ceilingPoints[(i + 1) % ceilingPoints.Count];
                        Line line = Line.CreateBound(startPoint, endPoint);
                        curveArrayCeiling.Append(line);
                    }

                    // Set the level for the ceiling (assuming you have a level named "Level 1").
                    Level ceilinglevel = floorLevel2;


                    Ceiling ceiling = Ceiling.Create(doc, new List<CurveLoop> { curveArray }, newCeilingType.Id, ceilinglevel.Id);

                    // Get the parameter for the ceiling's offset height from level.
                    Parameter offsetParameter = ceiling.get_Parameter(BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM);

                    // Check if the parameter is read-only or not.
                    if (!offsetParameter.IsReadOnly)
                    {
                        // Set the new value for the offset height.
                        double newOffsetHeight = 0;
                        offsetParameter.Set(newOffsetHeight);
                    }
                    
                    #endregion


                    //Create door

                    collector = new FilteredElementCollector(doc);
                    ICollection<Element> doorSymbols = collector.OfClass(typeof(FamilySymbol))
                                                            .OfCategory(BuiltInCategory.OST_Doors)
                                                            .ToElements();


                    // Get the first door symbol
                    FamilySymbol doorSymbol = doorSymbols.First() as FamilySymbol;

                    doorSymbol.Activate();

                    for (int i = 0; i < doorParams.Count; i++)
                    {
                        List<float> doorParam = doorParams[i];
                        float height = doorParam[3] * Meter2Feet;
                        float width = doorParam[4] * Meter2Feet;

                        // Duplicate the existing basic wall type
                        FamilySymbol newDoorSymbol = doorSymbol.Duplicate("Custom Basic Door " + (i + 1).ToString()) as FamilySymbol;
                        Parameter heightParam = newDoorSymbol.get_Parameter(BuiltInParameter.FAMILY_HEIGHT_PARAM);
                        heightParam.Set(height);

                        Parameter widthParam = newDoorSymbol.get_Parameter(BuiltInParameter.FAMILY_WIDTH_PARAM);
                        widthParam.Set(width);




                        float cenX = doorParam[0] * Meter2Feet;
                        float cenY = doorParam[1] * Meter2Feet;
                        float cenZ = doorParam[2] * Meter2Feet;

                        //TaskDialog.Show("Title", doorParam.Count().ToString());

                        // Specify the insertion point for the door (XYZ coordinates)
                        XYZ insertionPoint = new XYZ(cenX, cenY, cenZ);

                        Level doorLevel = floorLevel1;


                        int wallIndex = (int)doorParam[5];
                        //TaskDialog.Show("Title", wallIndex.ToString());
                        Wall insertedWall = wallElements[wallIndex];


                        // Create a new FamilyInstance (door) at the specified insertion point

                        FamilyInstance doorInstance = doc.Create.NewFamilyInstance(
                            insertionPoint, newDoorSymbol, insertedWall, StructuralType.NonStructural);

                        TaskDialog.Show("Title", doorInstance.Name);

                        Parameter sillHeightParam = doorInstance.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);
                        sillHeightParam.Set(0);
                    }

                    //Create window
                    collector = new FilteredElementCollector(doc);
                    ICollection<Element> windowSymbols = collector.OfClass(typeof(FamilySymbol))
                                                            .OfCategory(BuiltInCategory.OST_Windows)
                                                            .ToElements();


                    // Get the first window symbol
                    FamilySymbol windowSymbol = windowSymbols.First() as FamilySymbol;
                    doorSymbol.Activate();

                    for (int i = 0; i < windowParams.Count; i++)
                    {
                        List<float> windowParam = windowParams[i];
                        float height = windowParam[3] * Meter2Feet;
                        float width = windowParam[4] * Meter2Feet;

                        // Duplicate the existing basic wall type
                        FamilySymbol newWindowSymbol = windowSymbol.Duplicate("Custom Basic Window " + (i + 1).ToString()) as FamilySymbol;
                        Parameter heightParam = newWindowSymbol.get_Parameter(BuiltInParameter.FAMILY_HEIGHT_PARAM);
                        heightParam.Set(height);

                        Parameter widthParam = newWindowSymbol.get_Parameter(BuiltInParameter.FAMILY_WIDTH_PARAM);
                        widthParam.Set(width);




                        float cenX = windowParam[0] * Meter2Feet;
                        float cenY = windowParam[1] * Meter2Feet;
                        float cenZ = windowParam[2] * Meter2Feet;

                        //TaskDialog.Show("Title", doorParam.Count().ToString());

                        // Specify the insertion point for the door (XYZ coordinates)
                        XYZ insertionPoint = new XYZ(cenX, cenY, cenZ);

                        Level windowLevel = floorLevel1;


                        int wallIndex = (int)windowParam[5];
                        //TaskDialog.Show("Title", wallIndex.ToString());
                        Wall insertedWall = wallElements[wallIndex];


                        // Create a new FamilyInstance (door) at the specified insertion point

                        FamilyInstance windowInstance = doc.Create.NewFamilyInstance(
                            insertionPoint, newWindowSymbol, insertedWall, StructuralType.NonStructural);

                        //TaskDialog.Show("Title", windowInstance.Name);

                        Parameter sillHeightParam = windowInstance.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);
                        sillHeightParam.Set(cenZ);
                    }





                    // Commit the transaction to save the changes
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that may occur during the transaction
                    transaction.RollBack();
                    TaskDialog.Show("Error", ex.Message);
                    // return Result.Failed;
                }
            }

            return Result.Succeeded;



            //TaskDialog.Show("Title", floor_z.ToString()+";"+ceiling_z.ToString()+";"+floorPoints.Count());
        }
    }


    [TransactionAttribute(TransactionMode.Manual)]
    public class S2BCommand: IExternalEventHandler
    {
        public string FilePath { get; set; }

        public void Execute(UIApplication app)
        {

            Document doc = app.ActiveUIDocument.Document;
            Utility.ExecuteBIMGeneration(doc, FilePath);




        }

        public string GetName() => "My External Event";
    }
}
