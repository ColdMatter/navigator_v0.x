﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using MOTMaster2.SequenceData;
using System.Dynamic;
using System.Collections.ObjectModel;
using System.Windows.Controls.Primitives;
using ErrorManager;
using DAQ.Environment;

namespace MOTMaster2
{
    /// <summary>
    /// Interaction logic for SequenceDataGrid.xaml
    /// </summary>
    public partial class SequenceDataGrid : UserControl
    {
        public SequenceDataGrid()
        {
            InitializeComponent();
            sequenceDataGrid.DataContext = new SequenceStepViewModel();
        }
        public bool IsReadOnly { get { return sequenceDataGrid.IsReadOnly; } set { sequenceDataGrid.IsReadOnly = value; } }
        public static readonly DependencyProperty RunningProperty
            = DependencyProperty.Register(
                  "IsReadOnly",
                  typeof(bool),
                  typeof(SequenceDataGrid),
                  new PropertyMetadata(false)
              );

        public void UpdateSequenceData()
        {
            SequenceStepViewModel model = (SequenceStepViewModel)sequenceDataGrid.DataContext;
            model.SequenceSteps.Clear(); 
            //Dispatcher.Invoke(new Action(() => Thread.Sleep(1000)), DispatcherPriority.Background); 
            foreach (SequenceStep step in Controller.sequenceData.Steps) model.SequenceSteps.Add(step);
        }
        private void sequenceDataGrid_AutoGeneratedColumns(object sender, EventArgs e)
        {
            var dg = sender as DataGrid;
            //These hide the columns that by default display the dictionaries corresponding to the analog and digital channel types
            dg.Columns[6].Visibility = Visibility.Collapsed;
            dg.Columns[7].Visibility = Visibility.Collapsed;

            var first = dg.ItemsSource.Cast<object>().FirstOrDefault() as SequenceStep;
            if (first == null) return;
            var names = first.AnalogValueTypes.Keys;
            Style analogStyle = new Style(typeof(ComboBox));
     
            analogStyle.Setters.Add(new EventSetter() { Event = ComboBox.SelectionChangedEvent, Handler = new SelectionChangedEventHandler(this.sequenceDataGrid_AnalogValueChanged) });
            foreach (var name in names)
            {
                DataGridComboBoxColumn col = new DataGridComboBoxColumn { Header = Environs.Hardware.showAnalogName(name), EditingElementStyle = analogStyle };
  
                var resource = this.FindResource("analogProvider");
                BindingOperations.SetBinding(col, DataGridComboBoxColumn.ItemsSourceProperty, new Binding() { Source = resource });
                col.SelectedItemBinding = new Binding("AnalogValueTypes[" + name + "]");
                dg.Columns.Add(col);
            }
        /*    var dignames = first.DigitalValueTypes.Keys;
            foreach (var name in dignames)
            {
                DataGridCheckBoxColumn col = new DataGridCheckBoxColumn { Header = name };
                col.Binding = new Binding() { Path = new PropertyPath("DigitalValueTypes[" + name + "].Value")};
                dg.Columns.Add(col);
            }*/
            var dignames = first.DigitalValueTypes.Keys;
         //   Style cellStyle = (Style)this.Resources["DataGridCell"];
         //   cellStyle.Setters.Add(new EventSetter() { Event = MouseMoveEvent, Handler = new MouseEventHandler(this.sequenceDataGrid_MouseMove) });

            Style digitalStyle = (Style)this.Resources["BackgroundCheckBoxStyle"];
            //Style digitalStyle = new Style();
            digitalStyle.Setters.Add(new EventSetter() { Event = CheckBox.ClickEvent, Handler = new RoutedEventHandler(this.sequenceDataGrid_chkDigitalChecked) });

            //digitalStyle.Setters.Add(new EventSetter() { Event = CheckBox.UncheckedEvent, Handler = new RoutedEventHandler(this.sequenceDataGrid_chkDigitalChecked) });
            Brush[] colours = new Brush[] { new SolidColorBrush(Colors.Red), new SolidColorBrush(Colors.Orange), new SolidColorBrush(Colors.Yellow), new SolidColorBrush(Colors.Green), new SolidColorBrush(Colors.Blue) };
            int i = 0;
            foreach (var name in dignames)
            {
                //var resource = this.FindResource("digitalProvider");
                DataGridCheckBoxColumn col = new DataGridCheckBoxColumn { Header = Environs.Hardware.showDigitalName(name) };
                col.Binding = new Binding() { Path = new PropertyPath("DigitalValueTypes[" + name + "].Value") };

                //Path p = (Path)this.Resources["CheckMark"];
                //p.Stroke = colours[i % colours.Length];
                col.ElementStyle = digitalStyle;
                dg.Columns.Add(col);
                i++;
            }
            dg.FrozenColumnCount = 5;
        }

        //If the properties of the SequenceData are changed, this will be called
        private void sequenceDataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var dg = sender as DataGrid;
            if (dg.CurrentItem == null) { return; }
            ObservableCollection<SequenceStep> first = (ObservableCollection<SequenceStep>)dg.ItemsSource;
            SequenceStepViewModel model = (SequenceStepViewModel)sequenceDataGrid.DataContext;
            if (dg.CurrentItem.GetType() == typeof(SequenceStep)) model.SelectedSequenceStep = (SequenceStep)dg.CurrentItem;
            if (Controller.sequenceData != null) Controller.sequenceData.Steps = first;
        }

  /*      private void sequenceDataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange != 0)
            {
                var dg = sender as DataGrid;  if (dg.CurrentItem == null) { return; }
                //var cell = dg.CurrentItem as DataGridCell; if (cell == null) { return; }
                if (lastDataGridCellInfo == sequenceDataGrid.CurrentCell)
                {
                    if (sequenceDataGrid.CurrentColumn != null && sequenceDataGrid.CurrentColumn.GetType() == typeof(DataGridCheckBoxColumn))
                    {
                        string channelName = (string)sequenceDataGrid.CurrentColumn.Header;
                        if (channelName == "RS232Commands" || channelName == "Enabled") return;
                        SequenceStepViewModel model = (SequenceStepViewModel)sequenceDataGrid.DataContext;
                        //lastCheckBox.IsChecked = model.SelectedDigitalChannel.Value.Value;
                        ErrorMng.simpleMsg(lastCheckBoxValue.ToString() + " : " + lastCheckBox.IsChecked.ToString() + " / " + model.SelectedDigitalChannel.Value.Value.ToString());
                            //= new KeyValuePair<string, DigitalChannelSelector>(channelName, new DigitalChannelSelector(lastCheckBox.IsChecked.Value));
                    }                    
                }
            }
        }*/
        private void sequenceDataGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            //Controller ctrl;
        }
        private void sequenceDataGrid_AnalogValueChanged(object sender, SelectionChangedEventArgs e)
        {
            var combo = sender as ComboBox;
            if (sequenceDataGrid.CurrentColumn == null || sequenceDataGrid.CurrentColumn.GetType() != typeof(DataGridComboBoxColumn)) return;
            string channelName1 = (string)sequenceDataGrid.CurrentColumn.Header; 
            string channelName = Environs.Hardware.nameFromAnalogShowAs(channelName1);

            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                AnalogChannelSelector c = (AnalogChannelSelector)e.AddedItems[0];
                if (c != AnalogChannelSelector.Continue)
                {
                    //Only raises an event if the analog channel is being changed to something other than continue
                    SequenceStepViewModel model = (SequenceStepViewModel)sequenceDataGrid.DataContext;
                    model.SelectedAnalogChannel = new KeyValuePair<string, AnalogChannelSelector>(channelName, c);
                    OnChangedAnalogChannelCell(sender, e);
                }
            }
        }

        CheckBox lastCheckBox; bool lastCheckBoxValue;
        DataGridCellInfo lastDataGridCellInfo;
        //TODO Fix this being called when creating a new sequence step
        private void sequenceDataGrid_chkDigitalChecked(object sender, RoutedEventArgs e)
        {
            //Console.WriteLine("654654");
            Mouse.OverrideCursor = Cursors.Wait;
            try 
            { 
                var cell = sender as CheckBox; lastCheckBox = cell; lastDataGridCellInfo = sequenceDataGrid.CurrentCell; lastCheckBoxValue = cell.IsChecked.Value;

                if (sequenceDataGrid.CurrentColumn != null && sequenceDataGrid.CurrentColumn.GetType() == typeof(DataGridCheckBoxColumn) && cell != null)
                {
                    string channelName1 = (string)sequenceDataGrid.CurrentColumn.Header;
                    string channelName = Environs.Hardware.nameFromDigitalShowAs(channelName1);

                    if (channelName1 == "RS232Commands" || channelName1 == "Enabled") return;
                    SequenceStepViewModel model = (SequenceStepViewModel)sequenceDataGrid.DataContext;
                    model.SelectedDigitalChannel = new KeyValuePair<string, DigitalChannelSelector>(channelName, new DigitalChannelSelector(cell.IsChecked.Value));
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
           // if (cell.IsChecked.Value) cell.Background = new SolidColorBrush(Colors.Red);
            //else cell.Background = new SolidColorBrush(Colors.Black);
        }

        public delegate void ChangedAnalogChannelCellHandler(object sender, SelectionChangedEventArgs e);
        public event ChangedAnalogChannelCellHandler ChangedAnalogChannelCell;

        protected void OnChangedAnalogChannelCell(object sender, SelectionChangedEventArgs e)
        {
            if (ChangedAnalogChannelCell != null) ChangedAnalogChannelCell(sender, e);
        }

        public delegate void ChangedRS232CellHandler(object sender, DataGridBeginningEditEventArgs e);
        public event ChangedRS232CellHandler ChangedRS232Cell;

        protected void OnChangedRS232Cell(object sender, DataGridBeginningEditEventArgs e)
        {
            if (ChangedRS232Cell != null) ChangedRS232Cell(sender, e);
        }


 /*       public static readonly RoutedEvent ChangedAnalogChannelCellEvent = EventManager.RegisterRoutedEvent("ChangedAnalogChannelCellEvent", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ComboBox));

        public static readonly RoutedEvent ChangedRS232CellEvent = EventManager.RegisterRoutedEvent("ChangedRS232CellEvent", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CheckBox));
        
        public event RoutedEventHandler ChangedAnalogCell
        {
            add { AddHandler(ChangedAnalogChannelCellEvent, value); }
            remove { RemoveHandler(ChangedAnalogChannelCellEvent, value); }
        }
        public event RoutedEventHandler ChangedRS232Event
        {
            add { AddHandler(ChangedRS232CellEvent, value); }
            remove { RemoveHandler(ChangedRS232CellEvent, value); }
        }*/

        private void sequenceDataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            var dg = sender as DataGrid;
            if ((string)dg.CurrentCell.Column.Header == "RS232Commands")
            {
                SequenceStepViewModel model = (SequenceStepViewModel)sequenceDataGrid.DataContext;
                model.RS232Enabled = !model.RS232Enabled;
                OnChangedRS232Cell(sender, e);
            }
            else
            { return; }

        }
        int lastColumnIdx = 0;
        private void sequenceDataGrid_MouseMove(object sender, MouseEventArgs e)
        {
            DependencyObject dep;
            if (sender is DataGridCell)
            {
                dep = sender as DataGridCell;
            }
            else
            {
                dep = (DependencyObject)e.Source;
                while ((dep != null) && !(dep is DataGridCell))
                {  
                    dep = VisualTreeHelper.GetParent(dep); // iteratively traverse the visual tree
                }
                if (dep == null)
                    return;
            }
            if (dep is DataGridCell)
            {
                DataGridCell cell = dep as DataGridCell;

                DataGridColumn c2 = cell.Column;
                int columnIdx = c2.DisplayIndex;
                if (lastColumnIdx != columnIdx)
                {
                    Point pCell = cell.PointToScreen(new Point(0, 0));
                    Point pGrid = this.PointFromScreen(pCell);
                    recSelector.Margin = new Thickness(0, pGrid.Y+cell.ActualHeight, 18, 0);
                    lastColumnIdx = columnIdx;
                }               
            }
        }

        public DataGridCell GetCell(int row, int column)
        {
            DataGridRow rowContainer = GetRow(row);

            if (rowContainer != null)
            {
                DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(rowContainer);

                DataGridCell cell = null;
                if (presenter != null) cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                if ((cell == null) && (presenter != null))
                {
                    sequenceDataGrid.ScrollIntoView(rowContainer, sequenceDataGrid.Columns[column]);
                    cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                }
                return cell;
            }
            return null;
        }

        public DataGridRow GetRow(int index)
        {
            DataGridRow row = (DataGridRow)sequenceDataGrid.ItemContainerGenerator.ContainerFromIndex(index);
            if (row == null)
            {
                sequenceDataGrid.UpdateLayout();
                sequenceDataGrid.ScrollIntoView(sequenceDataGrid.Items[index]);
                row = (DataGridRow)sequenceDataGrid.ItemContainerGenerator.ContainerFromIndex(index);
            }
            return row;
        }

        public static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }

    /*    private void sequenceDataGrid_MouseMove(object sender, MouseEventArgs e)
        {
            var element = (UIElement)e.Source;

            int c = Grid.GetColumn(element);
            int r = Grid.GetRow(element);

            Console.WriteLine(r.ToString() + " / " + c.ToString());
        }*/

    }

    public class ValueToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int input;
            try
            {
                DataGridCell dgc = (DataGridCell)value;
                System.Data.DataRowView rowView = (System.Data.DataRowView)dgc.DataContext;
                input = (int)rowView.Row.ItemArray[dgc.Column.DisplayIndex];
            }
            catch (InvalidCastException e)
            {
                return DependencyProperty.UnsetValue;
            }
            switch (input)
            {
                case 1: return Brushes.Red;
                case 2: return Brushes.White;
                case 3: return Brushes.Blue;
                default: return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
