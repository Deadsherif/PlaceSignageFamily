﻿using Autodesk.Revit.DB;
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



                int createdDoorSignageCount = 0;
                // Step 1: Begin Transaction Group
                using (TransactionGroup tg = new TransactionGroup(doc, "Place signage families and add paramters"))
                {
                    tg.Start();

                    // Step 2: Transaction to set parameter false
                    using (Transaction tr1 = new Transaction(doc, "Place Families"))
                    {
                        tr1.Start();

                        var symbol = GetFamilySymbole(doc, "SignageFamily", MainviewModel.SelectedFamilyType.Name);
                        symbol.Activate();

                        var allFamilyInstances = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().Where(F => F.Symbol.FamilyName == "SignageFamily");
                        var Projectdoors = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType().ToElements().Cast<FamilyInstance>();
                        var LinkedDoors = GetAllDoorssFromLinkedModels(doc);
                        var doors = new List<FamilyInstance>();
                        doors.AddRange(Projectdoors);
                        doors.AddRange(LinkedDoors);

                        int i = 1;
                        foreach (var door in doors)
                        {
                            var superCom = door.SuperComponent;
                            if (superCom != null) continue;
                            var isLinked = door.Document.IsLinked;
                            var doorLocation = (door.Location as LocationPoint).Point;
                            var FromRoom = door.FromRoom;
                            var ToRoom = door.ToRoom;
                            var level = door.Document.GetElement(door.LevelId) as Level;
                            var doorHost = door.Host as Wall;
                            var index = (levels.FindIndex(X => X.Id == level.Id) + 1) * 100;

                            if (doorHost == null) continue;
                            var room = FromRoom == null ? ToRoom : FromRoom;
                            if (room == null) continue;
                            var targetFace = GetWallFaceNormalPointingToRoom(door, doorHost, room, FromRoom == null, door.Document.IsLinked);
                            if (targetFace == null) continue;

                            (XYZ point, XYZ direction) = DrawWallOpeningTopLeftPoint(doorHost, door, (targetFace as PlanarFace), MainviewModel.IsToggleLeft);

                            direction = direction ?? new XYZ(0, 0, 0);
                            var offset = MainviewModel.IsToggleLeft ? ((MainviewModel.Offset) / 12) * direction : ((MainviewModel.Offset - 12) / 12) * direction;

                            point = point ?? doorLocation;
                            FamilyInstance familyInstance = null;
                            if (!door.Document.IsLinked)
                            {
                                var location = new XYZ(point.X + offset.X, point.Y + offset.Y, point.Z);
                                 var test =    allFamilyInstances.Select(F => (F.Location as LocationPoint).Point).ToList();
                                if (allFamilyInstances.Any(F => (F.Location as LocationPoint).Point.DistanceTo(location)< 0.5)) continue;

                                familyInstance = doc.Create.NewFamilyInstance(targetFace, location, direction.Negate(), symbol);

                            }
                            else
                            {
                                // Get all the linked documents in the current Revit model
                                var linkInstance = new FilteredElementCollector(doc)
                                    .OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>().FirstOrDefault(In => In.GetLinkDocument().Title == door.Document.Title);

                                var location = new XYZ(point.X + offset.X, point.Y + offset.Y, point.Z/*+ (MainviewModel.Height/12)*/);
                                if (allFamilyInstances.Any(F => (F.Location as LocationPoint).Point.DistanceTo(location)< 0.5)) continue;
                                familyInstance = doc.Create.NewFamilyInstance(targetFace.Reference.CreateLinkReference(linkInstance), location, direction.Negate(), symbol);
                            }
                            if (familyInstance == null)
                                continue;
                            else
                                createdDoorSignageCount++;


                            var familyType = doc.GetElement(familyInstance.GetTypeId());
                            var levelPar = familyInstance.get_Parameter(BuiltInParameter.INSTANCE_SCHEDULE_ONLY_LEVEL_PARAM);
                            if (levelPar != null)
                                levelPar.Set(level.Id);

                            if (MainviewModel.IsToggleBack)

                            { familyType.LookupParameter("ShowBack").Set(1); familyType.LookupParameter("ShowFront").Set(0); }
                            else
                            { familyType.LookupParameter("ShowBack").Set(0); familyType.LookupParameter("ShowFront").Set(1); }


                            var widthPara = familyInstance.LookupParameter("L");
                            if (widthPara != null)
                                widthPara.Set(doorHost.Width);

                            var Existing = familyInstance.LookupParameter("Existing in");
                            if (Existing != null)
                                Existing.Set(FromRoom?.Name ?? "N/A");
                            var Leading = familyInstance.LookupParameter("Leading into");
                            if (Leading != null)
                                Leading.Set(ToRoom?.Name ?? "N/A");



                            var idPara = familyInstance.LookupParameter("Signage Type Identifier");
                            if (idPara != null)
                                idPara.Set($"{index + i}");
                            i++;

                        }
                        tr1.Commit();
                    }


                    using (Transaction tr2 = new Transaction(doc, "Tag"))
                    {
                        tr2.Start();

                        var FloorPlans = new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().Where(V => V.ViewType == ViewType.FloorPlan);
                        var familyInstances = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().Where(F => F.Symbol.FamilyName == "SignageFamily");
                        var symbolTag = GetFamilySymbole(doc, "SinageFamilyTag", "RM7");
                        symbolTag.Activate();

                        var allTagInstances = new FilteredElementCollector(doc).OfClass(typeof(IndependentTag)).Cast<IndependentTag>().Where(F =>  F.GetTypeId().IntegerValue == symbolTag.Id.IntegerValue);
                        

                        foreach (var viewPlan in FloorPlans)
                        {

                            foreach (var familyInstance in familyInstances)
                            {
                                var location = familyInstance.Location as LocationPoint;

                                if  (allTagInstances.Any(F=> F.TagHeadPosition.DistanceTo(location.Point) < 0.5)) continue;

                                IndependentTag.Create(doc, symbolTag.Id, viewPlan.Id, new Reference(familyInstance), false, TagOrientation.Horizontal, location.Point);
                            }
                        }


                        tr2.Commit();

                    }

                    // Step 5: Commit the transaction group
                    tg.Assimilate();
                }
                var isPlural = createdDoorSignageCount > 1 ;
                var familyPlural = isPlural ? "ies" : "y";
                var havePlural = isPlural ? "have" : "has";
                var countSTR = createdDoorSignageCount > 0 ? $"{createdDoorSignageCount}" : "No";
                MessageBox.Show($"{countSTR} New Signage Famil{familyPlural} {havePlural} been created");
                Mainview.ExportBtn.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
                Mainview.ExportBtn.IsEnabled = true;
            }
        }
        public static ICollection<FamilyInstance> GetAllDoorssFromLinkedModels(Document doc)
        {
            // Create a list to store floors from all linked models
            ICollection<FamilyInstance> allLinkedDoors = new List<FamilyInstance>();

            // Get all the linked documents in the current Revit model
            FilteredElementCollector linkedCollector = new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkInstance));

            foreach (RevitLinkInstance linkInstance in linkedCollector)
            {
                // Get the linked document
                Document linkedDoc = linkInstance.GetLinkDocument();


                if (linkedDoc != null)
                {
                    // Use a filtered element collector to get all the floors in the linked document
                    FilteredElementCollector floorCollector = new FilteredElementCollector(linkedDoc).OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType();

                    foreach (FamilyInstance floor in floorCollector)
                        // Add the floors from the linked document to the list
                        allLinkedDoors.Add(floor);

                }
            }

            return allLinkedDoors;
            // Now you have all the floors from all linked models in the "allLinkedDoors" list
        }

        public string GetName() => "Run Tool";
    }
}
