using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PlaceSignageFamily.External_Event_Handlers;
using PlaceSignageFamily.MVVM.View;
using PlaceSignageFamily.MVVM.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using static PlaceSignageFamily.Helper;

namespace PlaceSignageFamily
{
    [Transaction(TransactionMode.Manual)]
    internal class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            RunPlaceSignageFamilyExternalEventHandler placePlaceSignageFamilyExternalEventHandler = new RunPlaceSignageFamilyExternalEventHandler();
            SetElevationExternalEventHandler setElevationExternalEventHandler = new SetElevationExternalEventHandler();
            var ev = ExternalEvent.Create(placePlaceSignageFamilyExternalEventHandler);
            var evElevation = ExternalEvent.Create(setElevationExternalEventHandler);

            var document = commandData.Application.ActiveUIDocument.Document;
            Helper.doc = document;
            Transaction tr = new Transaction(document, "LoadFamily");
            tr.Start();
            var symbols = LoadAndGetFamilyTypes(document, "SignageFamily");
            tr.Commit();
            MainWindowViewModel viewModel = new MainWindowViewModel(symbols);
            var ui = MainWindow.CreateInstance(viewModel);
            //viewModel.FamilyTypes = new ObservableCollection<FamilySymbol>(symbols);
            placePlaceSignageFamilyExternalEventHandler.MainviewModel = viewModel;
            setElevationExternalEventHandler.MainviewModel = viewModel;
            viewModel.Ev = ev;
            viewModel.EvElevation = evElevation;


            ui.DataContext = viewModel;
            ui.ViewModel = viewModel;
            placePlaceSignageFamilyExternalEventHandler.Mainview= ui;
         
            




            ui.Show();
            return Result.Succeeded;
        }
    }
}
