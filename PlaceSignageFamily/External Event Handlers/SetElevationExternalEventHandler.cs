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
    internal class SetElevationExternalEventHandler : IExternalEventHandler
    {
        public MainWindowViewModel MainviewModel { get; set; }
        public MainWindow Mainview { get; set; }

  

        public void Execute(UIApplication app)
        {
            try
            {
                var uidoc = app.ActiveUIDocument;
                var doc = app.ActiveUIDocument.Document;



                // Step 1: Begin Transaction Group
                using (TransactionGroup tg = new TransactionGroup(doc, "Place signage families and add paramters"))
                {
                    tg.Start();

                    // Step 2: Transaction to set parameter false
                    using (Transaction tr1 = new Transaction(doc, "Place Families"))
                    {
                        tr1.Start();

                        var symbol = GetFamilySymbole(doc);
                        symbol.Activate();
                        var signages = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_GenericModel).WhereElementIsNotElementType().Cast<FamilyInstance>().Where(x=>x.Symbol.Id==symbol.Id);
                     
                        foreach (var signage in signages)
                        {

                            var elevation = signage.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM);
                             if (elevation != null)
                                elevation.Set(MainviewModel.Height / 12);
                        }
                        tr1.Commit();
                    }
                    
                    // Step 5: Commit the transaction group
                    tg.Assimilate();
                }

                //MessageBox.Show("Signale Families Elevation Edited Successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        public string GetName() => "Run Tool";
    }
}
