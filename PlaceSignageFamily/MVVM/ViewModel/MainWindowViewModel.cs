using Autodesk.Revit.DB;
using Autodesk.Revit.Exceptions;
using Autodesk.Revit.UI;
using PlaceSignageFamily.External_Event_Handlers;
using PlaceSignageFamily.MVVM.Model;
using PlaceSignageFamily.MVVM.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using System.Xml.Linq;
using static PlaceSignageFamily.Helper;


namespace PlaceSignageFamily.MVVM.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        #region Attributes
        private ExternalEvent ev;
        private ExternalEvent evNavigae;


        #endregion
        #region Properties

        private double height;
        private double offset;
        private bool _isToggleLeft;
        private bool _isToggleBack;
        public bool IsToggleLeft
        {
            get { return _isToggleLeft; }
            set
            {
                if (_isToggleLeft != value)
                {
                    _isToggleLeft = value;
                    OnPropertyChanged(nameof(IsToggleLeft));
                }
            }
        }
        public bool IsToggleBack
        {
            get { return _isToggleBack; }
            set
            {
                if (_isToggleBack != value)
                {
                    _isToggleBack = value;
                    OnPropertyChanged(nameof(IsToggleBack));
                }
            }
        }



        public double Height
        {
            get { return height; }
            set
            {
                if (double.TryParse(value.ToString(), out double result))
                {
                    ClearErrors(nameof(Height));
                    height = result;
                }
                else
                {
                    AddError(nameof(Height), "Invalid height value.");
                }
                OnPropertyChanged(nameof(Height));
            }
        }

        public double Offset
        {
            get { return offset; }
            set
            {
                if (double.TryParse(value.ToString(), out double result))
                {
                    ClearErrors(nameof(Offset));
                    offset = result;
                }
                else
                {
                    AddError(nameof(Offset), "Invalid offset value.");
                }
                OnPropertyChanged(nameof(Offset));
            }
        }


        public ExternalEvent Ev
        {
            get { return ev; }
            set { ev = value; OnPropertyChanged(nameof(Ev)); }
        }


        #endregion
        #region Functions
        public ICommand PlaceSignageFamilyCommand { get; }
        public ICommand SetFamilyElevationCommand { get; }
        public ICommand NavigateCommand { get; }
        public ICommand SelectAllCommand { get; set; }
        public ICommand DeselectAllCommand { get; set; }
        public ExternalEvent EvElevation { get; internal set; }

        public MainWindowViewModel()
        {
            PlaceSignageFamilyCommand = new RelayCommand(P => RunExporter(P));
            SetFamilyElevationCommand = new RelayCommand(P => SetElevation(P));

        }

        public void RunExporter(object parameter)
        {

            Ev.Raise();

        }
        public void SetElevation(object parameter)
        {

            EvElevation.Raise();

        }






        #endregion


    }


}
