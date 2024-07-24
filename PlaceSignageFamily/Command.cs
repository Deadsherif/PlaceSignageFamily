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

namespace PlaceSignageFamily
{
    [Transaction(TransactionMode.Manual)]
    internal class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            RunPlaceSignageFamilyExternalEventHandler placePlaceSignageFamilyExternalEventHandler = new RunPlaceSignageFamilyExternalEventHandler();
            var ev = ExternalEvent.Create(placePlaceSignageFamilyExternalEventHandler);

            Helper.doc = commandData.Application.ActiveUIDocument.Document;

            MainWindowViewModel viewModel = new MainWindowViewModel();
            var ui = MainWindow.CreateInstance(viewModel); 

            placePlaceSignageFamilyExternalEventHandler.MainviewModel = viewModel;
     
            viewModel.Ev = ev;
  
          
            ui.DataContext = viewModel;
            ui.ViewModel = viewModel;
            placePlaceSignageFamilyExternalEventHandler.Mainview= ui;
         
            




            ui.Show();
            return Result.Succeeded;
        }
    }
}
