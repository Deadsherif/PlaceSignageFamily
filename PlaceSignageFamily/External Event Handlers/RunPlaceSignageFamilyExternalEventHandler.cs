using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using PlaceSignageFamily.MVVM.View;
using PlaceSignageFamily.MVVM.ViewModel;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Windows;
using static PlaceSignageFamily.Helper;

namespace PlaceSignageFamily.External_Event_Handlers
{
    internal class RunPlaceSignageFamilyExternalEventHandler : IExternalEventHandler
    {
        public MainWindowViewModel MainviewModel { get; set; }
        public MainWindow Mainview { get; set; }

        public RunPlaceSignageFamilyExternalEventHandler()
        {

        }

        public void Execute(UIApplication app)
        {
            try
            {
                var uidoc = app.ActiveUIDocument;
                var doc = app.ActiveUIDocument.Document;

                // Get all levels in the document and order them by elevation
                var levels = new FilteredElementCollector(doc)
                                .OfClass(typeof(Level))
                                .Cast<Level>()
                                .OrderBy(level => level.Elevation)
                                .ToList();
             



                // Step 1: Begin Transaction Group
                using (TransactionGroup tg = new TransactionGroup(doc, "Place signage families and add paramters"))
                {
                    tg.Start();

                    // Step 2: Transaction to set parameter false
                    using (Transaction tr1 = new Transaction(doc, "Place Families"))
                    {
                        tr1.Start();

                        var symbol = GetFamilySymbole(doc);
                        var doors = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType().ToElements().Cast<FamilyInstance>();
                        int i = 1;
                        foreach (var door in doors)
                        {
                            var comp = door.GetSubComponentIds().Count;
                            if (comp == 0) continue;
                 
                            var doorLocation = (door.Location as LocationPoint).Point;
                            var FromRoom = door.FromRoom;
                            var ToRoom = door.ToRoom;
                            var doorHost = door.Host as Wall ;
                            if (doorHost == null) continue;
                            var room = FromRoom==null?ToRoom:FromRoom;
                            var targetFace =  GetWallFaceNormalPointingToRoom(door,doorHost, room, FromRoom==null) ;
                            if(targetFace == null) continue;

                           (XYZ point,XYZ direction) =   DrawWallOpeningTopLeftPoint(doorHost, door, (targetFace as PlanarFace).FaceNormal,MainviewModel.IsToggleLeft);

                            XYZ referenceVector = GetPerpendicularVector((targetFace as PlanarFace).FaceNormal);
                            var doorHeight = door.get_Parameter(BuiltInParameter.INSTANCE_HEAD_HEIGHT_PARAM).AsDouble();
                            var doorWidth = doc.GetElement(door.GetTypeId()).get_Parameter(BuiltInParameter.DOOR_WIDTH)?.AsDouble();
                            direction = direction ?? new XYZ(1, 1, 0);
                            var offset = MainviewModel.IsToggleLeft? ((MainviewModel.Offset-12) / 12) * direction: ((MainviewModel.Offset)/12)* direction;
                            
                            point = point ?? doorLocation;
                            FamilyInstance familyInstance = doc.Create.NewFamilyInstance(new XYZ(point.X + offset.X, point.Y+ offset.Y, point.Z+ (MainviewModel.Height/12)), symbol, doorHost,StructuralType.NonStructural);
                           var Existing =  familyInstance.LookupParameter("Existing in");
                            if (Existing != null)
                                Existing.Set(FromRoom?.Name??"N/A");
                           var Leading =  familyInstance.LookupParameter("Leading into");
                            if (Leading != null)
                                Leading.Set(ToRoom?.Name??"N/A");

                            var index = (levels.FindIndex(X => X.Id == door.LevelId)+1)*100;

                            var idPara = familyInstance.LookupParameter("Signage Type Identifier");
                            if (idPara != null)
                                idPara.Set($"{index+i}");
                            i++;

                        }
                        tr1.Commit();
                    }
                    
                    // Step 5: Commit the transaction group
                    tg.Assimilate();
                }

                MessageBox.Show("Signale Families Placed Successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        public string GetName() => "Run Tool";
    }
}
