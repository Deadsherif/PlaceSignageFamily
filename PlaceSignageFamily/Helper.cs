using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.UI;
using System.Collections.ObjectModel;
using Autodesk.Revit.Creation;
using Document = Autodesk.Revit.DB.Document;
using System.Reflection;
using System.IO;
using Autodesk.Revit.DB.Architecture;
using System.Diagnostics;

namespace PlaceSignageFamily
{
    public class Helper
    {
        public static Document doc { get; set; }
        public static FamilySymbol GetFamilySymbole(Document doc)
        {
            /// <summary>
            /// Return complete family file path
            /// </summary>
            var addinFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            // Retrieve the family if it is already present:
            string FamilyName = "Signage Family_WallBased";
            string FamilyPath = $@"{addinFolder}\Signage Family_WallBased.rfa";

            Family family = FindElementByName(
              typeof(Family), FamilyName) as Family;

            if (null == family)
            {
                // It is not present, so check for 
                // the file to load it from:

                if (!File.Exists(FamilyPath))
                {
                    TaskDialog.Show("Error", string.Format(
                      "Please ensure that  "
                      + "family file '{0}' exists in '{1}'.",
                      FamilyName, FamilyPath));
                    return null;

                }

                // Load family from file:

                doc.LoadFamily(FamilyPath, out family);

            }

            // Determine the family symbol

            FamilySymbol symbol = null;

            foreach (var symboleId in family.GetFamilySymbolIds())
            {
                var _symbol = doc.GetElement(symboleId) as FamilySymbol;
                if (_symbol.Name == "RM7")
                { symbol = _symbol; break; }

            }
            return symbol;
        }
        public static Element FindElementByName(

      Type targetType,
      string targetName)
        {
            return new FilteredElementCollector(doc)
              .OfClass(targetType)
              .FirstOrDefault<Element>(
                e => e.Name.Equals(targetName));
        }


        public static Face GetWallFaceNormalPointingToRoom(FamilyInstance door, Wall wall, Room room, bool IsFromRoomNull)

        {
            var doorHeight = door.get_Parameter(BuiltInParameter.INSTANCE_HEAD_HEIGHT_PARAM).AsDouble();
            // Enable ComputeReferences
            Options geomOptions = new Options
            {
                ComputeReferences = true,
                IncludeNonVisibleObjects = true
            };

            GeometryElement geomElement = wall.get_Geometry(geomOptions);
            // Get the wall's geometry



            // Get the room's location
            LocationPoint roomLocation = room.Location as LocationPoint;
            XYZ roomPoint = roomLocation.Point;
            var realPointOfRoomToNotGetTheDoor = new XYZ(roomPoint.X, roomPoint.Y, doorHeight * 1.25);

            Face targetFace = null;

            // Iterate through the geometry of the wall
            foreach (GeometryObject geomObj in geomElement)
            {
                Solid solid = geomObj as Solid;
                if (solid != null && Math.Round(solid.Volume) > 0)
                {
                    var FACES = new List<PlanarFace>();
                    // Iterate through each face in the solid
                    foreach (PlanarFace face in solid.Faces)
                    {
                        // Get a point on the face to compare with the room's location
                        XYZ faceCenter = face.Evaluate(UV.Zero); // Getting a point on the face, UV.Zero represents the UV coordinates

                        // Calculate the normal at that point
                        XYZ faceNormal = face.FaceNormal;

                        var WallId = GetHittedWall(realPointOfRoomToNotGetTheDoor, IsFromRoomNull ? faceNormal: faceNormal.Negate());
                        if (WallId != null && wall.Id == WallId)
                        {
                            targetFace = face;
                            break;
                        }
                    }
                }
            }

            return targetFace;
        }
        
        public static ElementId GetHittedWall(XYZ point, XYZ faceNormal)
        {
            ElementFilter filter = new ElementCategoryFilter(BuiltInCategory.OST_Walls);

            // Project in the negative Z direction down to the floor.
            FilteredElementCollector collector = new
          FilteredElementCollector(doc);
            Func<View3D, bool> isNotTemplate = v3 => !(v3.IsTemplate);
            View3D view3D = collector.OfClass(typeof(View3D)).Cast<View3D>()
            .First<View3D>(isNotTemplate);


            ReferenceIntersector refIntersector = new ReferenceIntersector(filter, FindReferenceTarget.Face, view3D);
            refIntersector.FindReferencesInRevitLinks = true;
            var referenceWithContext = refIntersector.FindNearest(point, faceNormal);
            if (referenceWithContext != null)
                return referenceWithContext.GetReference().ElementId;
            if (referenceWithContext == null)
            {
                var referenceWithContextFar = refIntersector.Find(point, faceNormal);
                if (referenceWithContextFar.Count != 0)
                    return referenceWithContextFar.FirstOrDefault().GetReference().ElementId;
            }


            return null;

        }
        public static XYZ GetPerpendicularVector(XYZ faceNormal)
        {
            XYZ referenceVector = XYZ.BasisX;

            if (faceNormal.IsAlmostEqualTo(XYZ.BasisX) || faceNormal.IsAlmostEqualTo(XYZ.BasisX.Negate()))
            {
                referenceVector = XYZ.BasisY;
            }

            XYZ perpendicularVector = faceNormal.CrossProduct(referenceVector);

            if (perpendicularVector.IsZeroLength())
            {
                return null;
            }
            var vector = perpendicularVector.Normalize();
            return new XYZ(Math.Abs(vector.X), Math.Abs(vector.Y), Math.Abs(vector.Z));
        }



        /// <summary>
        /// Retrieve all planar faces belonging to the 
        /// specified opening in the given wall.
        /// </summary>
        static List<PlanarFace> GetWallOpeningPlanarFaces(
          Wall wall,
          ElementId openingId)
        {
            List<PlanarFace> faceList = new List<PlanarFace>();

            List<Solid> solidList = new List<Solid>();

            Options geomOptions = wall.Document.Application.Create.NewGeometryOptions();

            if (geomOptions != null)
            {
                //geomOptions.ComputeReferences = true; // expensive, avoid if not needed
                //geomOptions.DetailLevel = ViewDetailLevel.Fine;
                //geomOptions.IncludeNonVisibleObjects = false;

                GeometryElement geoElem = wall.get_Geometry(geomOptions);

                if (geoElem != null)
                {
                    foreach (GeometryObject geomObj in geoElem)
                    {
                        if (geomObj is Solid)
                        {
                            solidList.Add(geomObj as Solid);
                        }
                    }
                }
            }

            foreach (Solid solid in solidList)
            {
                foreach (Face face in solid.Faces)
                {
                    if (face is PlanarFace)
                    {
                        if (wall.GetGeneratingElementIds(face)
                          .Any(x => x == openingId))
                        {
                            faceList.Add(face as PlanarFace);
                        }
                    }
                }
            }
            return faceList;
        }





        public static (XYZ point,XYZ Direction) DrawWallOpeningTopLeftPoint(Wall wall, FamilyInstance door, PlanarFace planarFace,bool IsToggleLeft)
        {

            Categories cats = doc.Settings.Categories;

            ElementId catDoorsId = cats.get_Item(
              BuiltInCategory.OST_Doors).Id;

            ElementId catWindowsId = cats.get_Item(
              BuiltInCategory.OST_Windows).Id;





            List<ElementId> newIds = new List<ElementId>();




            if (wall != null)
            {
                List<PlanarFace> faceList = new List<PlanarFace>();

                List<ElementId> insertIds = wall.FindInserts(
                  true, false, false, false).ToList();




                if (door is FamilyInstance)
                {


                    CategoryType catType = door.Category
                      .CategoryType;

                    Category cat = door.Category;

                    if (catType == CategoryType.Model
                      && (cat.Id == catDoorsId
                        || cat.Id == catWindowsId))
                    {
                        faceList.AddRange(
                          GetWallOpeningPlanarFaces(
                            wall, door.Id));
                    }
                }



                foreach (PlanarFace face in faceList)
                {
                    Plane facePlane = Plane.CreateByNormalAndOrigin(face.FaceNormal, face.Origin);

                    if (face.FaceNormal.IsAlmostEqualTo(new XYZ(0, 0, 1))|| face.FaceNormal.IsAlmostEqualTo( new XYZ(0, 0, -1)))
                            {
                        SketchPlane sketchPlane
                         = SketchPlane.Create(doc, facePlane);
                        var X = face.GetEdgesAsCurveLoops();
                        foreach (CurveLoop curveLoop in
                          face.GetEdgesAsCurveLoops())
                        {
                            foreach (Curve curve in curveLoop)
                            {

                                if (curve is Line line && Math.Round(line.Direction.DotProduct(planarFace.FaceNormal)) == 0)
                                {
                                    // Check if both endpoints of the line are on the sketch plane
                                    XYZ start = line.GetEndPoint(0);
                                    XYZ end = line.GetEndPoint(1);

                                    bool isStartOnPlane = Math.Abs((start - planarFace.Origin).DotProduct(planarFace.FaceNormal)) < 1e-6;
                                    bool isEndOnPlane = Math.Abs((end - planarFace.Origin).DotProduct(planarFace.FaceNormal)) < 1e-6;

                                    if (isStartOnPlane && isEndOnPlane)
                                    {
                                        return IsToggleLeft  ? (line.Tessellate().LastOrDefault(), line.Direction) : (line.Tessellate().FirstOrDefault(), line.Direction);
                                    }
                                }
                            }
                        }
                    }

                }
            }
            return (null,null);









        }
    }



}

